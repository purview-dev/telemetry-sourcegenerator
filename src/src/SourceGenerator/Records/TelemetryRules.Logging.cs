using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

static partial class TelemetryRules
{
	/// <summary>
	/// Logging-specific diagnostics for a logger interface. Reuses the shared attribute parsing so the
	/// conditions match the pipeline's record-building exactly.
	/// </summary>
	public static ImmutableArray<DiagnosticInfo> GetLoggerDiagnostics(
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		if (!Utilities.ContainsAttribute(interfaceSymbol, TemplateLibrary.Logging.LoggerAttribute, token))
			return diagnostics.ToImmutable();

		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var loggerAttribute = SharedHelpers.GetLoggerAttribute(interfaceSymbol, token);
		var loggerGenerationAttribute = SharedHelpers.GetLoggerGenerationAttribute(compilation.Assembly, token);

		var interfaceGenerationMode =
			loggerAttribute?.GenerationModeOrNull ?? loggerGenerationAttribute?.GenerationModeOrNull ?? 0;

		var methods = interfaceSymbol
			.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(m =>
				!Utilities.ContainsAttribute(m, TemplateLibrary.Shared.ExcludeAttribute, token) && m.Arity == 0
			);

		foreach (var method in methods)
		{
			token.ThrowIfCancellationRequested();

			if (generationType != GenerationType.Logging)
			{
				var hasLoggingAttribute = SharedHelpers.GetLogAttribute(method, token) != null;
				if (!hasLoggingAttribute)
					continue;
			}

			ApplyLoggerMethodRules(method, interfaceGenerationMode, diagnostics, token);
		}

		return diagnostics.ToImmutable();
	}

	static void ApplyLoggerMethodRules(
		IMethodSymbol method,
		int interfaceGenerationMode,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		var logAttribute = SharedHelpers.GetLogAttribute(method, token);

		var isScoped = TypeLibrary.System.IDisposable.Equals(method.ReturnType);
		var exceptionParameters = method.Parameters.Where(p => p.Type.IsExceptionType()).ToImmutableArray();
		var hasMultipleExceptions = !isScoped && exceptionParameters.Length > 1;

		if (hasMultipleExceptions)
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Logging.MultipleExceptionsDefined),
					method,
					method.Name
				)
			);

		// TSG2021: invalid return type.
		if (IsInvalidLogReturnType(method, token))
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Logging.LogMustReturnVoidOrAsync),
					method.ReturnType.Locations
				)
			);

		// Resolve the effective generation mode (method > interface/assembly > auto) so the
		// v1-gated diagnostics match the pipeline exactly.
		var methodGenMode = logAttribute?.GenerationMode ?? 0;
		var nonExceptionCount = method.Parameters.Count(p => !p.Type.IsExceptionType());
		var hasExpandOrLogProperties = method.Parameters.Any(p =>
			SharedHelpers.GetExpandEnumerableAttribute(p, token) != null
			|| SharedHelpers.GetLogPropertiesAttribute(p, token) != null
		);
		var useV1Generation = methodGenMode switch
		{
			1 => true,
			2 => false,
			_ => interfaceGenerationMode switch
			{
				1 => true,
				2 => false,
				_ => !hasMultipleExceptions
					&& nonExceptionCount <= PropertyLibrary.Logging.MaxNonExceptionParameters
					&& !hasExpandOrLogProperties,
			},
		};

		// TSG2001: explicitly V1 with too many non-exception parameters.
		if (useV1Generation && nonExceptionCount > PropertyLibrary.Logging.MaxNonExceptionParameters)
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Logging.MaximumLogEntryParametersExceeded),
					method,
					method.Name
				)
			);

		// TSG2002: inferred error level (exception present, no explicit level).
		if (useV1Generation && !isScoped && exceptionParameters.Length == 1 && logAttribute?.LevelOrNull == null)
			diagnostics.Add(
				DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Logging.InferringErrorLogLevel), method)
			);

		// TSG2007: scoped method must not have an explicit level.
		if (isScoped && logAttribute?.LevelOrNull != null)
			diagnostics.Add(
				DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Logging.ScopedMethodShouldNotHaveLevel), method)
			);

		// Per-parameter rules.
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();
			ApplyParameterRules(parameter, diagnostics, token);
		}

		// Message-template rules (only when an explicit template is supplied).
		if (logAttribute?.MessageTemplate is { } messageTemplate && !string.IsNullOrWhiteSpace(messageTemplate))
			ApplyMessageTemplateRules(method, messageTemplate, diagnostics);
	}

	static void ApplyParameterRules(
		IParameterSymbol parameter,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		var logPropertiesAttribute = SharedHelpers.GetLogPropertiesAttribute(parameter, token);
		var expandEnumerableAttribute = SharedHelpers.GetExpandEnumerableAttribute(parameter, token);

		// TSG2006: LogProperties + ExpandEnumerable on the same parameter.
		if (logPropertiesAttribute != null && expandEnumerableAttribute != null)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Logging.ExpandEnumerableAndLogPropertiesNotSupported),
					parameter
				)
			);
		}

		// TSG2008: unbounded enumeration with a high max count.
		if (
			expandEnumerableAttribute != null
			&& expandEnumerableAttribute.Value.MaximumValueCount
				> PropertyLibrary.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic
		)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Logging.UnboundedIEnumerableMaxCount), parameter)
			);
		}
	}

	static void ApplyMessageTemplateRules(
		IMethodSymbol method,
		string messageTemplate,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics
	)
	{
		var holes = MessageTemplateHole.FromMatches(PropertyLibrary.MessageTemplateMatcher.Matches(messageTemplate));
		if (holes.IsEmpty)
			return;

		var isOrdinalBased = holes.Any(static h => h.Ordinal.HasValue);
		var isNamedBased = holes.Any(static h => h.Name != null);

		// TSG2004: mixed ordinal and named placeholders.
		if (isOrdinalBased && isNamedBased)
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Logging.MixedOrdinalAndNamedProperties),
					method,
					method.Name
				)
			);

		// TSG2005: ordinal values exceed the parameter count.
		var maxOrdinal = holes.Any(static h => h.IsPositional)
			? holes.Where(static h => h.IsPositional).Max(static h => h.Ordinal!.Value)
			: 0;
		if (maxOrdinal > method.Parameters.Length)
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Logging.OrdinalsExceedParameters),
					method,
					method.Name
				)
			);
	}

	internal static bool IsInvalidLogReturnType(IMethodSymbol method, CancellationToken token)
	{
		var isVoid = method.ReturnsVoid;
		var returnType = method.ReturnType;
		var isIDisposable = TypeLibrary.System.IDisposable.Equals(returnType);

		if (!isIDisposable && returnType.NullableAnnotation == NullableAnnotation.Annotated)
		{
			if (returnType is INamedTypeSymbol namedType && !namedType.IsValueType)
			{
				isIDisposable =
					TypeLibrary.System.IDisposable.Equals(namedType.OriginalDefinition)
					|| TypeLibrary.System.IDisposable.MetadataFullName == namedType.ConstructedFrom.ToString();
			}
		}

		var isActivity = TypeLibrary.Activities.SystemDiagnostics.Activity.Equals(returnType);
		if (isActivity && SharedHelpers.IsActivityMethod(method, token))
			return false;

		// TSG2021: invalid return type (not void or IDisposable).
		return !isVoid && !isIDisposable;
	}
}
