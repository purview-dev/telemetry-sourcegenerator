using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitMethods(
		ActivitySourceTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		EmitRecordExceptionEvent(writer, context, logger, emitNullable);

		// Filter to only methods that are valid for Activities target
		// (have explicit Activity/Event/Context attributes, or valid inference in single-target)
		var validActivityMethods = target.ActivityMethods.Where(m => m.TargetGenerationState.IsValid).ToArray();

		// Check for TSG3012: Event/Context methods exist but no Activity method
		if (!validActivityMethods.Any(m => m.MethodType == ActivityMethodType.Activity))
		{
			if (validActivityMethods.Any(m => m.MethodType != ActivityMethodType.Activity))
			{
				logger?.Diagnostic("There are no Activity methods defined, however there are Events/ Context methods.");
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Activities.NoActivityMethodsDefined
				);
			}
		}

		foreach (var methodTarget in target.ActivityMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			EmitMethod(writer, methodTarget, target, context, logger, emitNullable);
		}
	}

	static void EmitRecordExceptionEvent(
		CodeWriter writer,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating {Constants.Activities.RecordExceptionMethodName}.");

		writer
			.WriteLine(Constants.System.GeneratedCode.Value)
			.WriteLine(Constants.System.AggressiveInlining)
			.Write("static void ")
			.Write(Constants.Activities.RecordExceptionMethodName)
			.Write('(')
			.Write(Constants.Activities.SystemDiagnostics.Activity)
			.Write(emitNullable ? "? activity, " : " activity, ")
			.Write(Constants.System.Exception)
			.Write(emitNullable ? "? exception, " : " exception, ")
			.Write(Constants.System.BuiltInTypes.BoolKeyword)
			.WriteLine(" escape)");

		using (writer.OpenBlockScope())
		{
			writer.WriteLine("if (activity == null || exception == null)");
			using (writer.OpenBlockScope())
				writer.WriteLine("return;");

			writer.NewLine();

			const string tagsListVariableName = "tagsCollection";
			writer
				.Write(Constants.Activities.SystemDiagnostics.ActivityTagsCollection)
				.Write(' ')
				.Write(tagsListVariableName)
				.Write(" = new ")
				.Write(Constants.Activities.SystemDiagnostics.ActivityTagsCollection)
				.WriteLine("();");

			EmitExceptionParam(writer, tagsListVariableName, "escape", "exception");

			const string eventVariableName = "recordExceptionEvent";

			writer
				.NewLine()
				.Write(Constants.Activities.SystemDiagnostics.ActivityEvent)
				.Write(' ')
				.Write(eventVariableName)
				.Write(" = new ")
				.Write(Constants.Activities.SystemDiagnostics.ActivityEvent)
				// name:
				.Write("(name: ")
				.Write(Constants.Activities.Tag_ExceptionEventName.Wrap())
				// timestamp:
				.Write(", timestamp: default")
				// tags:
				.Write(", tags: ")
				.Write(tagsListVariableName)
				.WriteLine(");");

			writer.NewLine().Write("activity.AddEvent(").Write(eventVariableName).WriteLine(");");
		}

		writer.NewLine();
	}

	static void EmitExceptionParam(
		CodeWriter writer,
		string tagsListVariableName,
		string escapeParam,
		string exceptionParam
	)
	{
		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(Constants.Activities.Tag_ExceptionEscaped.Wrap())
			.Write(", ")
			.Write(escapeParam)
			.WriteLine(");");

		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(Constants.Activities.Tag_ExceptionMessage.Wrap())
			.Write(", ")
			.Write(exceptionParam)
			.WriteLine(".Message);");

		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(Constants.Activities.Tag_ExceptionType.Wrap())
			.Write(", ")
			.Write(exceptionParam)
			.WriteLine(".GetType().FullName);");

		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(Constants.Activities.Tag_ExceptionStackTrace.Wrap())
			.Write(", ")
			.Write(exceptionParam)
			.WriteLine(".StackTrace);");
	}

	static void EmitThrowStub(CodeWriter writer, ActivityBasedGenerationTarget methodTarget)
	{
		writer.NewLine().Write("public ").Write(methodTarget.ReturnType);

		writer.Write(' ').Write(methodTarget.MethodName);

		if (methodTarget.TypeParameters.Count > 0)
		{
			writer.Write('<');
			for (var i = 0; i < methodTarget.TypeParameters.Count; i++)
			{
				if (i > 0)
					writer.Write(", ");
				writer.Write(methodTarget.TypeParameters[i]);
			}
			writer.Write('>');
		}

		writer.Write('(');

		for (var i = 0; i < methodTarget.Parameters.Count; i++)
		{
			if (i > 0)
				writer.Write(", ");
			writer
				.Write(methodTarget.Parameters[i].ParameterType)
				.Write(' ')
				.Write(methodTarget.Parameters[i].ParameterName);
		}

		writer.WriteLine(") => throw new global::System.NotSupportedException();").NewLine();
	}

	static void EmitMethod(
		CodeWriter writer,
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
			if (
				EmitterHelpers.ShouldEmitThrowStub(
					methodTarget.TargetGenerationState,
					GenerationType.Activities,
					target.GenerationType
				)
			)
			{
				EmitThrowStub(writer, methodTarget);
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
			EmitPrivateActivityMethod(writer, methodTarget, target, context, logger, emitNullable);

			// Generate public delegating method (Activity emitter owns this for multi-target)
			EmitPublicDelegatingMethod(writer, methodTarget, methodTargets, context, logger, emitNullable);
		}
		else
		{
			// Single-target: generate public method as before
			EmitPublicActivityMethod(writer, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitPrivateActivityMethod(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		ActivitySourceTarget _, // target
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		var privateMethodName = methodTarget.MethodName + "_Activity";

		writer
			.WriteLine(Constants.System.GeneratedCode.Value)
			.WriteLine(Constants.System.AggressiveInlining)
			.Write("private ")
			.Write(methodTarget.ReturnType);

		writer.Write(' ').Write(privateMethodName).Write('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			writer.Write(parameter.ParameterType).Write(' ').Write(parameter.ParameterName);

			if (index < methodTarget.Parameters.Count - 1)
				writer.Write(", ");

			index++;
		}

		writer.WriteLine(")");

		using (writer.OpenBlockScope())
		{
			if (methodTarget.MethodType == ActivityMethodType.Activity)
				EmitActivityMethodBody(writer, methodTarget, context, logger, emitNullable);
			else if (methodTarget.MethodType == ActivityMethodType.Event)
				EmitEventMethodBody(writer, methodTarget, context, logger, emitNullable);
			else if (methodTarget.MethodType == ActivityMethodType.Context)
				EmitContextMethodBody(writer, methodTarget, context, logger, emitNullable);
		}

		writer.NewLine();
	}

	static void EmitPublicDelegatingMethod(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		GenerationType methodTargets,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		logger?.Debug($"Building public delegating method for {methodTarget.MethodName}.");

		writer
			.NewLine()
			.WriteLine(Constants.System.GeneratedCode.Value)
			.WriteLine(Constants.System.AggressiveInlining)
			.Write("public ")
			.Write(methodTarget.ReturnType);

		writer.Write(' ').Write(methodTarget.MethodName).Write('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			writer.Write(parameter.ParameterType).Write(' ').Write(parameter.ParameterName);

			if (index < methodTarget.Parameters.Count - 1)
				writer.Write(", ");

			index++;
		}

		writer.WriteLine(")");

		using (writer.OpenBlockScope())
		{
			var returnsActivity = Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType);
			var paramList = string.Join(", ", methodTarget.Parameters.Select(p => p.ParameterName));

			// Create filtered parameter list for Logging/Metrics (excludes Activity-related types)
			var loggingMetricsParamList = string.Join(
				", ",
				methodTarget
					.Parameters.Where(p =>
						!Constants.Activities.SystemDiagnostics.Activity.Equals(p.ParameterType)
						&& !Constants.Activities.SystemDiagnostics.ActivityContext.Equals(p.ParameterType)
						&& !Constants.Activities.SystemDiagnostics.ActivityLink.Equals(p.ParameterType)
						&& !Constants.Activities.SystemDiagnostics.ActivityLinkArray.Equals(p.ParameterType)
						&& !Constants.System.TagList.Equals(p.ParameterType)
					)
					.Select(p => p.ParameterName)
			);

			// Call Activity private method first (returns Activity? if applicable)
			if (methodTargets.HasFlag(GenerationType.Activities))
			{
				if (returnsActivity)
				{
					writer
						.Write("var activityResult = ")
						.Write(methodTarget.MethodName)
						.Write("_Activity(")
						.Write(paramList)
						.WriteLine(");");
				}
				else
				{
					writer.Write(methodTarget.MethodName).Write("_Activity(").Write(paramList).WriteLine(");");
				}
			}

			// Call Logging private method
			if (methodTargets.HasFlag(GenerationType.Logging))
			{
				writer.Write(methodTarget.MethodName).Write("_Logging(").Write(loggingMetricsParamList).WriteLine(");");
			}

			// Call Metrics private method
			if (methodTargets.HasFlag(GenerationType.Metrics))
			{
				writer.Write(methodTarget.MethodName).Write("_Metrics(").Write(loggingMetricsParamList).WriteLine(");");
			}

			// Return result if applicable
			if (returnsActivity)
			{
				writer
					.NewLine()
					.Write(
						"return activityResult"
							+ (!emitNullable || methodTarget.ReturnType.IsNullable ? null : "!")
							+ ";"
					);
			}
		}

		writer.NewLine();
	}

	static void EmitPublicActivityMethod(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		writer
			.WriteLine(Constants.System.GeneratedCode.Value)
			.WriteLine(Constants.System.AggressiveInlining)
			.Write("public ")
			.Write(methodTarget.ReturnType);

		writer.Write(' ').Write(methodTarget.MethodName).Write('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			writer.Write(parameter.ParameterType).Write(' ').Write(parameter.ParameterName);

			if (index < methodTarget.Parameters.Count - 1)
				writer.Write(", ");

			index++;
		}

		writer.WriteLine(")");

		using (writer.OpenBlockScope())
		{
			if (methodTarget.MethodType == ActivityMethodType.Activity)
				EmitActivityMethodBody(writer, methodTarget, context, logger, emitNullable);
			else if (methodTarget.MethodType == ActivityMethodType.Event)
				EmitEventMethodBody(writer, methodTarget, context, logger, emitNullable);
			else if (methodTarget.MethodType == ActivityMethodType.Context)
				EmitContextMethodBody(writer, methodTarget, context, logger, emitNullable);
		}

		writer.NewLine();
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
			else if (methodTarget.TargetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
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

		if (target.ActivitySourceGenerationAttribute?.GenerateDiagnosticsForMissingActivity.Value ?? true)
		{
			// Here we're opting in to generate diagnostics for missing activity return/ params.
			if (methodTarget.MethodType == ActivityMethodType.Activity)
			{
				if (!Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType))
				{
					logger?.Diagnostic($"No Activity returned for {methodTarget.MethodName}.");
					TelemetryDiagnostics.Report(
						context.ReportDiagnostic,
						TelemetryDiagnostics.Activities.DoesNotReturnActivity
					);
				}
				else if (!methodTarget.ReturnType.IsNullable)
				{
					logger?.Diagnostic($"Activity return type is not nullable for {methodTarget.MethodName}.");
					TelemetryDiagnostics.Report(
						context.ReportDiagnostic,
						TelemetryDiagnostics.Activities.ActivityReturnTypeShouldBeNullable
					);
				}
			}
			else
			{
				if (!methodTarget.HasActivityParameter)
				{
					logger?.Diagnostic($"No Activity parameter is defined on {methodTarget.MethodName}.");
				}
				else if (
					!Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.Parameters[0].ParameterType)
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
