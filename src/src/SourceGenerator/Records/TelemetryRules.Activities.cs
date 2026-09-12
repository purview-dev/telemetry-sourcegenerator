using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

static partial class TelemetryRules
{
	/// <summary>
	/// Activity-specific diagnostics derived from the pipeline's <see cref="ActivitySourceTarget"/> so the
	/// parameter inference matches generation exactly.
	/// </summary>
	public static ImmutableArray<ReportableDiagnostic> GetActivityDiagnostics(
		ActivitySourceTarget target,
		INamedTypeSymbol interfaceSymbol,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<ReportableDiagnostic>();

		// TSG3001: no activity source specified.
		if (string.IsNullOrWhiteSpace(target.ActivitySourceName))
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Activities.NoActivitySourceSpecified.Descriptor,
					isBlocking: false,
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
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Activities.NoActivityMethodsDefined.Descriptor,
					isBlocking: false,
					interfaceSymbol
				)
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
		ImmutableArray<ReportableDiagnostic>.Builder diagnostics,
		CancellationToken token
	)
	{
		// TSG3002: invalid return type. Events must return void; activity/context methods may return void or Activity.
		var isEvent = method.MethodType == ActivityMethodType.Event;
		var isValidReturnType = isEvent
			? method.ReturnType.Identity.SpecialType == SpecialType.System_Void
			: method.ReturnType.Identity.SpecialType == SpecialType.System_Void
				|| method.ReturnType.Identity.Equals(TypeLibrary.System.Diagnostics.Activity);

		// TSG3013/TSG3022: activity methods should return a nullable Activity.
		if (method.MethodType == ActivityMethodType.Activity && isValidReturnType)
		{
			var returnsActivity = method.ReturnType.Identity.Equals(TypeLibrary.System.Diagnostics.Activity);
			if (!returnsActivity)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Activities.DoesNotReturnActivity.Descriptor,
						isBlocking: false,
						methodSymbol
					)
				);
			else if (!method.ReturnType.IsNullable)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Activities.ActivityReturnTypeShouldBeNullable.Descriptor,
						isBlocking: false,
						methodSymbol
					)
				);
		}

		if (!isValidReturnType)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Activities.InvalidReturnType.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG3014/TSG3015: best-practice diagnostics for missing/misplaced Activity parameters,
		// opt-in via [ActivitySourceGeneration(GenerateDiagnosticsForMissingActivity = ...)].
		if (generateDiagnosticsForMissingActivity && method.MethodType != ActivityMethodType.Activity)
		{
			if (!method.HasActivityParameter)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Activities.DoesNotAcceptActivityParameter.Descriptor,
						isBlocking: false,
						methodSymbol
					)
				);
		}

		if (generateDiagnosticsForMissingActivity && method.HasActivityParameter && method.Parameters.Count > 0)
		{
			if (method.Parameters[0].ParamDestination != ActivityParameterDestination.Activity)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Activities.ActivityShouldBeTheFirstParameter.Descriptor,
						isBlocking: false,
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
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Activities.ExceptionEventNotStandardName.Descriptor,
						isBlocking: false,
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
		ImmutableArray<ReportableDiagnostic>.Builder diagnostics,
		CancellationToken token
	)
	{
		foreach (var baggage in method.Baggage)
		{
			token.ThrowIfCancellationRequested();

			if (baggage.ParameterType.Identity.SpecialType != SpecialType.System_String)
			{
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Activities.BaggageParameterShouldBeString.Descriptor,
						isBlocking: false,
						GetParameterLocation(methodSymbol, baggage.ParameterName)
					)
				);
			}
		}
	}

	static void ApplyDuplicateReservedRules(
		ActivityBasedGenerationTarget method,
		IMethodSymbol methodSymbol,
		ImmutableArray<ReportableDiagnostic>.Builder diagnostics
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
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Activities.DuplicateParameterTypes.Descriptor,
					isBlocking: false,
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
		ImmutableArray<ReportableDiagnostic>.Builder diagnostics,
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
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.ActivityParameterNotAllowed.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.Timestamp when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.TimestampParameterNotAllowed.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.StartTime when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.StartTimeParameterNotAllowed.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.ParentContextOrId
					when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.ParentContextOrIdParameterNotAllowed.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.LinksEnumerable when method.MethodType != ActivityMethodType.Activity:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.LinksParameterNotAllowed.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.TagsEnumerable when method.MethodType == ActivityMethodType.Context:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.TagsParameterNotAllowed.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.Escape
					when parameter.ParameterType.Identity.SpecialType != SpecialType.System_Boolean:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.EscapedParameterInvalidType.Descriptor,
							isBlocking: false,
							location
						)
					);
					break;
				case ActivityParameterDestination.Escape when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.EscapedParameterIsOnlyValidOnEvent.Descriptor,
							isBlocking: false,
							location,
							parameterName
						)
					);
					break;
				case ActivityParameterDestination.StatusDescription
					when parameter.ParameterType.Identity.SpecialType != SpecialType.System_String:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.StatusDescriptionMustBeString.Descriptor,
							isBlocking: false,
							location
						)
					);
					break;
				case ActivityParameterDestination.StatusDescription when method.MethodType != ActivityMethodType.Event:
					diagnostics.Add(
						ReportableDiagnostic.Create(
							DiagnosticLibrary.Activities.StatusDescriptionParameterInvalidType.Descriptor,
							isBlocking: false,
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
