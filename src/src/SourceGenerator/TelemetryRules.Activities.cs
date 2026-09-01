using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

static partial class TelemetryRules
{
	/// <summary>
	/// Activity-specific diagnostics derived from the pipeline's <see cref="ActivitySourceTarget"/> so the
	/// parameter inference matches generation exactly.
	/// </summary>
	public static ImmutableArray<DiagnosticInfo> GetActivityDiagnostics(
		ActivitySourceTarget target,
		INamedTypeSymbol interfaceSymbol,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		// TSG3001: no activity source specified.
		if (string.IsNullOrWhiteSpace(target.ActivitySourceName))
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Activities.NoActivitySourceSpecified),
					interfaceSymbol
				)
			);

		var validMethods = target.ActivityMethods.Where(static m => m.TargetGenerationState.IsValid).ToImmutableArray();

		// TSG3012: event/context methods exist but no Activity method.
		if (
			!validMethods.Any(static m => m.MethodType == ActivityMethodType.Activity)
			&& validMethods.Any(static m => m.MethodType != ActivityMethodType.Activity)
		)
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Activities.NoActivityMethodsDefined),
					interfaceSymbol
				)
			);

		var generateDiagnosticsForMissingActivity =
			target.ActivitySourceGenerationAttribute?.GenerateDiagnosticsForMissingActivity.Value ?? true;

		foreach (var method in target.ActivityMethods)
		{
			token.ThrowIfCancellationRequested();

			if (!method.TargetGenerationState.IsValid)
				continue;

			var methodSymbol = FindMethod(interfaceSymbol, method.MethodName);
			if (methodSymbol is null)
				continue;

			ApplyActivityMethodRules(method, methodSymbol, generateDiagnosticsForMissingActivity, diagnostics, token);
		}

		return diagnostics.ToImmutable();
	}

	static void ApplyActivityMethodRules(
		ActivityBasedGenerationTarget method,
		IMethodSymbol methodSymbol,
		bool generateDiagnosticsForMissingActivity,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		// TSG3002: invalid return type. Events must return void; activity/context methods may return void or Activity.
		var isEvent = method.MethodType == ActivityMethodType.Event;
		var isValidReturnType = isEvent
			? method.ReturnType.Identity.SpecialType == SpecialType.System_Void
			: method.ReturnType.Identity.SpecialType == SpecialType.System_Void
				|| method.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity);

		// TSG3013/TSG3022: activity methods should return a nullable Activity.
		if (method.MethodType == ActivityMethodType.Activity && isValidReturnType)
		{
			var returnsActivity = method.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity);
			if (!returnsActivity)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Activities.DoesNotReturnActivity),
						methodSymbol
					)
				);
			else if (!method.ReturnType.IsNullable)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Activities.ActivityReturnTypeShouldBeNullable),
						methodSymbol
					)
				);
		}

		if (!isValidReturnType)
			diagnostics.Add(
				DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Activities.InvalidReturnType), methodSymbol)
			);

		// TSG3014/TSG3015: best-practice diagnostics for missing/misplaced Activity parameters,
		// opt-in via [ActivitySourceGeneration(GenerateDiagnosticsForMissingActivity = ...)].
		if (generateDiagnosticsForMissingActivity && method.MethodType != ActivityMethodType.Activity)
		{
			if (!method.HasActivityParameter)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Activities.DoesNotAcceptActivityParameter),
						methodSymbol
					)
				);
		}

		if (generateDiagnosticsForMissingActivity && method.HasActivityParameter && method.Parameters.Count > 0)
		{
			if (method.Parameters[0].ParamDestination != ActivityParameterDestination.Activity)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Activities.ActivityShouldBeTheFirstParameter),
						methodSymbol
					)
				);
		}

		// TSG3021: an event recording an exception should use the OpenTelemetry standard name.
		if (method.MethodType == ActivityMethodType.Event)
		{
			var recordsException = method.Parameters.Any(static p => p.IsException);
			if (recordsException && !string.Equals(method.ActivityOrEventName, "exception", StringComparison.Ordinal))
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Activities.ExceptionEventNotStandardName),
						methodSymbol,
						method.ActivityOrEventName
					)
				);
		}

		// TSG3000: baggage parameters must be strings.
		foreach (var baggage in method.Baggage)
		{
			token.ThrowIfCancellationRequested();

			if (baggage.ParameterType.Identity.SpecialType != SpecialType.System_String)
			{
				var parameterSymbol = FindParameter(methodSymbol, baggage.ParameterName);
				var location =
					parameterSymbol?.Locations.FirstOrDefault(static l => l.IsInSource)
					?? methodSymbol.Locations.FirstOrDefault(static l => l.IsInSource)
					?? Location.None;
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Activities.BaggageParameterShouldBeString),
						location
					)
				);
			}
		}

		// TSG3003: more than one parameter sharing the same reserved destination.
		var duplicateReserved = method
			.Parameters.Where(static p =>
				p.ParamDestination is not (ActivityParameterDestination.Tag or ActivityParameterDestination.Baggage)
			)
			.GroupBy(static p => p.ParamDestination)
			.Where(static g => g.Count() > 1);

		foreach (var group in duplicateReserved)
		{
			var names = string.Join(", ", group.Select(static p => p.ParameterName));
			var secondParameter = group.ElementAt(1);
			var secondSymbol = FindParameter(methodSymbol, secondParameter.ParameterName);
			var duplicateLocation =
				secondSymbol?.Locations.FirstOrDefault(static l => l.IsInSource)
				?? methodSymbol.Locations.FirstOrDefault(static l => l.IsInSource)
				?? Location.None;

			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.Activities.DuplicateParameterTypes),
					duplicateLocation,
					names,
					group.Key.ToString()
				)
			);
		}

		// Reserved-parameter rules.
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			var parameterSymbol = FindParameter(methodSymbol, parameter.ParameterName);
			var location =
				parameterSymbol?.Locations.FirstOrDefault(static l => l.IsInSource)
				?? methodSymbol.Locations.FirstOrDefault(static l => l.IsInSource)
				?? Location.None;
			var parameterName = parameter.GeneratedName;

			switch (parameter.ParamDestination)
			{
				case ActivityParameterDestination.Activity when method.MethodType == ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.ActivityParameterNotAllowed),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.Timestamp when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.TimestampParameterNotAllowed),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.StartTime when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.StartTimeParameterNotAllowed),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.ParentContextOrId
					when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.ParentContextOrIdParameterNotAllowed),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.LinksEnumerable when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.LinksParameterNotAllowed),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.TagsEnumerable when method.MethodType == ActivityMethodType.Context:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.TagsParameterNotAllowed),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.Escape
					when parameter.ParameterType.Identity.SpecialType != SpecialType.System_Boolean:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.EscapedParameterInvalidType),
							location
						)
					);
					break;
				case ActivityParameterDestination.Escape when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.EscapedParameterIsOnlyValidOnEvent),
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.StatusDescription
					when parameter.ParameterType.Identity.SpecialType != SpecialType.System_String:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.StatusDescriptionMustBeString),
							location
						)
					);
					break;
				case ActivityParameterDestination.StatusDescription when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.Activities.StatusDescriptionParameterInvalidType),
							location,
							parameterName
						)
					);
					break;
			}
		}
	}
}
