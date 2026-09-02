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
					DiagnosticLibrary.Activities.NoActivitySourceSpecified.Descriptor,
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
				DiagnosticInfo.Create(DiagnosticLibrary.Activities.NoActivityMethodsDefined.Descriptor, interfaceSymbol)
			);

		var generateDiagnosticsForMissingActivity =
			target.ActivitySourceGenerationAttribute?.GenerateDiagnosticsForMissingActivity ?? true;

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
					DiagnosticInfo.Create(DiagnosticLibrary.Activities.DoesNotReturnActivity.Descriptor, methodSymbol)
				);
			else if (!method.ReturnType.IsNullable)
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.Activities.ActivityReturnTypeShouldBeNullable.Descriptor,
						methodSymbol
					)
				);
		}

		if (!isValidReturnType)
			diagnostics.Add(
				DiagnosticInfo.Create(DiagnosticLibrary.Activities.InvalidReturnType.Descriptor, methodSymbol)
			);

		// TSG3014/TSG3015: best-practice diagnostics for missing/misplaced Activity parameters,
		// opt-in via [ActivitySourceGeneration(GenerateDiagnosticsForMissingActivity = ...)].
		if (generateDiagnosticsForMissingActivity && method.MethodType != ActivityMethodType.Activity)
		{
			if (!method.HasActivityParameter)
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.Activities.DoesNotAcceptActivityParameter.Descriptor,
						methodSymbol
					)
				);
		}

		if (generateDiagnosticsForMissingActivity && method.HasActivityParameter && method.Parameters.Count > 0)
		{
			if (method.Parameters[0].ParamDestination != ActivityParameterDestination.Activity)
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.Activities.ActivityShouldBeTheFirstParameter.Descriptor,
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
						DiagnosticLibrary.Activities.ExceptionEventNotStandardName.Descriptor,
						methodSymbol,
						method.ActivityOrEventName
					)
				);
		}

		// TSG3000: baggage parameters must be strings.
		ApplyBaggageRules(method, methodSymbol, diagnostics, token);

		// TSG3003: more than one parameter sharing the same reserved destination.
		ApplyDuplicateReservedRules(method, methodSymbol, diagnostics);

		// Reserved-parameter rules.
		ApplyReservedParameterRules(method, methodSymbol, diagnostics, token);
	}

	static void ApplyBaggageRules(
		ActivityBasedGenerationTarget method,
		IMethodSymbol methodSymbol,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		foreach (var baggage in method.Baggage)
		{
			token.ThrowIfCancellationRequested();

			if (baggage.ParameterType.Identity.SpecialType != SpecialType.System_String)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						DiagnosticLibrary.Activities.BaggageParameterShouldBeString.Descriptor,
						GetParameterLocation(methodSymbol, baggage.ParameterName)
					)
				);
			}
		}
	}

	static void ApplyDuplicateReservedRules(
		ActivityBasedGenerationTarget method,
		IMethodSymbol methodSymbol,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics
	)
	{
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

			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.Activities.DuplicateParameterTypes.Descriptor,
					GetParameterLocation(methodSymbol, secondParameter.ParameterName),
					names,
					group.Key.ToString()
				)
			);
		}
	}

	static void ApplyReservedParameterRules(
		ActivityBasedGenerationTarget method,
		IMethodSymbol methodSymbol,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			var location = GetParameterLocation(methodSymbol, parameter.ParameterName);
			var parameterName = parameter.GeneratedName;

#pragma warning disable IDE0010 // Add missing cases
			switch (parameter.ParamDestination)
			{
				case ActivityParameterDestination.Activity when method.MethodType == ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.ActivityParameterNotAllowed.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.Timestamp when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.TimestampParameterNotAllowed.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.StartTime when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.StartTimeParameterNotAllowed.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.ParentContextOrId
					when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.ParentContextOrIdParameterNotAllowed.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.LinksEnumerable when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.LinksParameterNotAllowed.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.TagsEnumerable when method.MethodType == ActivityMethodType.Context:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.TagsParameterNotAllowed.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.Escape
					when parameter.ParameterType.Identity.SpecialType != SpecialType.System_Boolean:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.EscapedParameterInvalidType.Descriptor,
							location
						)
					);
					break;
				case ActivityParameterDestination.Escape when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.EscapedParameterIsOnlyValidOnEvent.Descriptor,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.StatusDescription
					when parameter.ParameterType.Identity.SpecialType != SpecialType.System_String:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.StatusDescriptionMustBeString.Descriptor,
							location
						)
					);
					break;
				case ActivityParameterDestination.StatusDescription when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						DiagnosticInfo.Create(
							DiagnosticLibrary.Activities.StatusDescriptionParameterInvalidType.Descriptor,
							location,
							parameterName
						)
					);
					break;
			}
#pragma warning restore IDE0010 // Add missing cases
		}
	}
}
