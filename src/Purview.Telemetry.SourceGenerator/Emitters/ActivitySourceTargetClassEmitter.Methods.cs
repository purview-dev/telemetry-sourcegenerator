using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static int EmitMethods(
		ActivitySourceTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		indent++;

		EmitRecordExceptionEvent(builder, indent, context, logger, emitNullable);

		// Filter to only methods that are valid for Activities target
		// (have explicit Activity/Event/Context attributes, or valid inference in single-target)
		var validActivityMethods = target
			.ActivityMethods.Where(m => m.TargetGenerationState.IsValid)
			.ToArray();

		// Check for TSG3012: Event/Context methods exist but no Activity method
		if (!validActivityMethods.Any(m => m.MethodType == ActivityMethodType.Activity))
		{
			if (validActivityMethods.Any(m => m.MethodType != ActivityMethodType.Activity))
			{
				logger?.Diagnostic(
					"There are no Activity methods defined, however there are Events/ Context methods."
				);
				TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Activities.NoActivityMethodsDefined);
			}
		}

		foreach (var methodTarget in target.ActivityMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			EmitMethod(builder, indent, methodTarget, target, context, logger, emitNullable);
		}

		return --indent;
	}

	static void EmitRecordExceptionEvent(
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating {Constants.Activities.RecordExceptionMethodName}.");

		builder
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "static void ", withNewLine: false)
			.Append(Constants.Activities.RecordExceptionMethodName)
			.Append('(')
			.Append(Constants.Activities.SystemDiagnostics.Activity)
			.Append(emitNullable ? "? activity, " : " activity, ")
			.Append(Constants.System.Exception)
			.Append(emitNullable ? "? exception, " : " exception, ")
			.Append(Constants.System.BuiltInTypes.BoolKeyword)
			.AppendLine(" escape)")
			.Append(indent, '{');

		indent++;

		builder
			.Append(indent, "if (activity == null || exception == null)")
			.Append(indent, '{')
			.Append(indent + 1, "return;")
			.Append(indent, '}')
			.AppendLine();

		const string tagsListVariableName = "tagsCollection";
		builder
			.Append(
				indent,
				Constants.Activities.SystemDiagnostics.ActivityTagsCollection,
				withNewLine: false
			)
			.Append(' ')
			.Append(tagsListVariableName)
			.Append(" = new ")
			.Append(Constants.Activities.SystemDiagnostics.ActivityTagsCollection)
			.AppendLine("();");

		EmitExceptionParam(builder, indent, tagsListVariableName, "escape", "exception");

		const string eventVariableName = "recordExceptionEvent";

		builder
			.AppendLine()
			.Append(
				indent,
				Constants.Activities.SystemDiagnostics.ActivityEvent,
				withNewLine: false
			)
			.Append(' ')
			.Append(eventVariableName)
			.Append(" = new ")
			.Append(Constants.Activities.SystemDiagnostics.ActivityEvent)
			// name:
			.Append("(name: ")
			.Append(Constants.Activities.Tag_ExceptionEventName.Wrap())
			// timestamp:
			.Append(", timestamp: default")
			// tags:
			.Append(", tags: ")
			.Append(tagsListVariableName)
			.AppendLine(");");

		builder
			.AppendLine()
			.Append(indent, "activity.AddEvent(", withNewLine: false)
			.Append(eventVariableName)
			.AppendLine(");");

		builder.Append(--indent, '}').AppendLine();
	}

	static void EmitExceptionParam(
		StringBuilder builder,
		int indent,
		string tagsListVariableName,
		string escapeParam,
		string exceptionParam
	)
	{
		builder
			.Append(indent, tagsListVariableName, withNewLine: false)
			.Append(".Add(")
			.Append(Constants.Activities.Tag_ExceptionEscaped.Wrap())
			.Append(", ")
			.Append(escapeParam)
			.AppendLine(");");

		builder
			.Append(indent, tagsListVariableName, withNewLine: false)
			.Append(".Add(")
			.Append(Constants.Activities.Tag_ExceptionMessage.Wrap())
			.Append(", ")
			.Append(exceptionParam)
			.AppendLine(".Message);");

		builder
			.Append(indent, tagsListVariableName, withNewLine: false)
			.Append(".Add(")
			.Append(Constants.Activities.Tag_ExceptionType.Wrap())
			.Append(", ")
			.Append(exceptionParam)
			.AppendLine(".GetType().FullName);");

		builder
			.Append(indent, tagsListVariableName, withNewLine: false)
			.Append(".Add(")
			.Append(Constants.Activities.Tag_ExceptionStackTrace.Wrap())
			.Append(", ")
			.Append(exceptionParam)
			.AppendLine(".StackTrace);");
	}

	static void EmitThrowStub(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget
	)
	{
		builder
			.AppendLine()
			.Append(indent, "public ", withNewLine: false)
			.Append(methodTarget.ReturnType);

		builder.Append(' ').Append(methodTarget.MethodName);

		if (methodTarget.TypeParameters.Length > 0)
		{
			builder.Append('<');
			for (var i = 0; i < methodTarget.TypeParameters.Length; i++)
			{
				if (i > 0)
					builder.Append(", ");
				builder.Append(methodTarget.TypeParameters[i]);
			}
			builder.Append('>');
		}

		builder.Append('(');

		for (var i = 0; i < methodTarget.Parameters.Length; i++)
		{
			if (i > 0)
				builder.Append(", ");
			builder
				.Append(methodTarget.Parameters[i].ParameterType)
				.Append(' ')
				.Append(methodTarget.Parameters[i].ParameterName);
		}

		builder.AppendLine(") => throw new global::System.NotSupportedException();").AppendLine();
	}

	static void EmitMethod(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		ActivitySourceTarget target,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (!methodTarget.TargetGenerationState.IsValid)
		{
			if (EmitterHelpers.ShouldEmitThrowStub(
				methodTarget.TargetGenerationState,
				GenerationType.Activities,
				target.GenerationType
			))
			{
				EmitThrowStub(builder, indent, methodTarget);
			}
			return;
		}

		if (!GuardMethod(methodTarget, target, context, logger))
			return;

		var isMultiTarget = methodTarget.TargetGenerationState.IsMultiTarget;
		var methodTargets = methodTarget.TargetGenerationState.MethodTargets;

		// For multi-target, Activity emitter generates the public method that delegates
		// to private implementation methods for each target
		if (isMultiTarget)
		{
			// Generate private activity implementation method
			EmitPrivateActivityMethod(builder, indent, methodTarget, target, context, logger, emitNullable);

			// Generate public delegating method (Activity emitter owns this for multi-target)
			EmitPublicDelegatingMethod(
				builder,
				indent,
				methodTarget,
				methodTargets,
				context,
				logger,
				emitNullable
			);
		}
		else
		{
			// Single-target: generate public method as before
			EmitPublicActivityMethod(builder, indent, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitPrivateActivityMethod(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		ActivitySourceTarget _, // target
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		var privateMethodName = methodTarget.MethodName + "_Activity";

		builder
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "private ", withNewLine: false)
			.Append(methodTarget.ReturnType);

		builder.Append(' ').Append(privateMethodName).Append('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder.Append(parameter.ParameterType).Append(' ').Append(parameter.ParameterName);

			if (index < methodTarget.Parameters.Length - 1)
				builder.Append(", ");

			index++;
		}

		builder.AppendLine(')').Append(indent, '{');

		indent++;

		if (methodTarget.MethodType == ActivityMethodType.Activity)
			EmitActivityMethodBody(builder, indent, methodTarget, context, logger, emitNullable);
		else if (methodTarget.MethodType == ActivityMethodType.Event)
			EmitEventMethodBody(builder, indent, methodTarget, context, logger, emitNullable);
		else if (methodTarget.MethodType == ActivityMethodType.Context)
			EmitContextMethodBody(builder, indent, methodTarget, context, logger, emitNullable);

		builder.Append(--indent, '}').AppendLine();
	}

	static void EmitPublicDelegatingMethod(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		GenerationType methodTargets,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		logger?.Debug($"Building public delegating method for {methodTarget.MethodName}.");

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "public ", withNewLine: false)
			.Append(methodTarget.ReturnType);

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder.Append(parameter.ParameterType).Append(' ').Append(parameter.ParameterName);

			if (index < methodTarget.Parameters.Length - 1)
				builder.Append(", ");

			index++;
		}

		builder.AppendLine(')').Append(indent, '{');

		indent++;

		var returnsActivity = Constants.Activities.SystemDiagnostics.Activity.Equals(
			methodTarget.ReturnType
		);
		var paramList = string.Join(", ", methodTarget.Parameters.Select(p => p.ParameterName));

		// Create filtered parameter list for Logging/Metrics (excludes Activity-related types)
		var loggingMetricsParamList = string.Join(
			", ",
			methodTarget
				.Parameters.Where(p =>
					!Constants.Activities.SystemDiagnostics.Activity.Equals(p.ParameterType)
					&& !Constants.Activities.SystemDiagnostics.ActivityContext.Equals(
						p.ParameterType
					)
					&& !Constants.Activities.SystemDiagnostics.ActivityLink.Equals(p.ParameterType)
					&& !Constants.Activities.SystemDiagnostics.ActivityLinkArray.Equals(
						p.ParameterType
					)
					&& !Constants.System.TagList.Equals(p.ParameterType)
				)
				.Select(p => p.ParameterName)
		);

		// Call Activity private method first (returns Activity? if applicable)
		if (methodTargets.HasFlag(GenerationType.Activities))
		{
			if (returnsActivity)
			{
				builder
					.Append(indent, "var activityResult = ", withNewLine: false)
					.Append(methodTarget.MethodName)
					.Append("_Activity(")
					.Append(paramList)
					.AppendLine(");");
			}
			else
			{
				builder
					.Append(indent, methodTarget.MethodName, withNewLine: false)
					.Append("_Activity(")
					.Append(paramList)
					.AppendLine(");");
			}
		}

		// Call Logging private method
		if (methodTargets.HasFlag(GenerationType.Logging))
		{
			builder
				.Append(indent, methodTarget.MethodName, withNewLine: false)
				.Append("_Logging(")
				.Append(loggingMetricsParamList)
				.AppendLine(");");
		}

		// Call Metrics private method
		if (methodTargets.HasFlag(GenerationType.Metrics))
		{
			builder
				.Append(indent, methodTarget.MethodName, withNewLine: false)
				.Append("_Metrics(")
				.Append(loggingMetricsParamList)
				.AppendLine(");");
		}

		// Return result if applicable
		if (returnsActivity)
		{
			builder
				.AppendLine()
				.Append(
					indent,
					"return activityResult"
						+ (!emitNullable || methodTarget.ReturnType.IsNullable ? null : "!")
						+ ";"
				);
		}

		builder.Append(--indent, '}').AppendLine();
	}

	static void EmitPublicActivityMethod(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		builder
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "public ", withNewLine: false)
			.Append(methodTarget.ReturnType);

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder.Append(parameter.ParameterType).Append(' ').Append(parameter.ParameterName);

			if (index < methodTarget.Parameters.Length - 1)
				builder.Append(", ");

			index++;
		}

		builder.AppendLine(')').Append(indent, '{');

		indent++;

		if (methodTarget.MethodType == ActivityMethodType.Activity)
			EmitActivityMethodBody(builder, indent, methodTarget, context, logger, emitNullable);
		else if (methodTarget.MethodType == ActivityMethodType.Event)
			EmitEventMethodBody(builder, indent, methodTarget, context, logger, emitNullable);
		else if (methodTarget.MethodType == ActivityMethodType.Context)
			EmitContextMethodBody(builder, indent, methodTarget, context, logger, emitNullable);

		builder.Append(--indent, '}').AppendLine();
	}

	static bool GuardMethod(
		ActivityBasedGenerationTarget methodTarget,
		ActivitySourceTarget target,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		if (!methodTarget.TargetGenerationState.IsValid)
		{
			if (methodTarget.TargetGenerationState.RaiseMultiGenerationTargetsNotSupported)
			{
				logger?.Debug(
					$"Identified {target.InterfaceType.TypeName}.{methodTarget.MethodName} as problematic as it has another target types."
				);
			}
			else if (
				methodTarget.TargetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting
			)
			{
				logger?.Debug(
					$"Identified {target.InterfaceType.TypeName}.{methodTarget.MethodName} as problematic as it is inferred."
				);
			}

			return false;
		}

		// Event methods must return void only
		// Activity and Context methods can return void or Activity?
		var isEvent = methodTarget.MethodType == ActivityMethodType.Event;

		var isValidReturnType = isEvent
			? methodTarget.ReturnType.SpecialType == SpecialType.System_Void
			: methodTarget.ReturnType.SpecialType == SpecialType.System_Void
				|| Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType);

		if (!isValidReturnType)
		{
			logger?.Diagnostic(
				$"The return type {methodTarget.ReturnType} isn't valid for an activity, event, or context method."
			);

			TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Activities.InvalidReturnType);

			return false;
		}

		if (
			target.ActivitySourceGenerationAttribute?.GenerateDiagnosticsForMissingActivity.Value
			?? true
		)
		{
			// Here we're opting in to generate diagnostics for missing activity return/ params.
			if (methodTarget.MethodType == ActivityMethodType.Activity)
			{
				if (
					!Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType)
				)
				{
					logger?.Diagnostic($"No Activity returned for {methodTarget.MethodName}.");
					TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Activities.DoesNotReturnActivity);
				}
				else if (!methodTarget.ReturnType.IsNullable)
				{
					logger?.Diagnostic(
						$"Activity return type is not nullable for {methodTarget.MethodName}."
					);
					TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Activities.ActivityReturnTypeShouldBeNullable);
				}
			}
			else
			{
				if (!methodTarget.HasActivityParameter)
				{
					logger?.Diagnostic(
						$"No Activity parameter is defined on {methodTarget.MethodName}."
					);
				}
				else if (
					!Constants.Activities.SystemDiagnostics.Activity.Equals(
						methodTarget.Parameters[0].ParameterType
					)
				)
				{
					logger?.Diagnostic(
						$"Activity parameter is defined, but it's not the first on {methodTarget.MethodName}."
					);
				}
			}
		}

		return true;
	}
}
