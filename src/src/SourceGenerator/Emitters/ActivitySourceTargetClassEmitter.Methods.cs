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
		ISourceGenLogger? logger,
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
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Generating {PropertyLibrary.Activities.RecordExceptionMethodName}.");

		writer
			.Write("static void ")
			.Write(PropertyLibrary.Activities.RecordExceptionMethodName)
			.Write('(')
			.Write(TypeLibrary.Activities.SystemDiagnostics.Activity)
			.Write(emitNullable ? "? activity, " : " activity, ")
			.Write(TypeLibrary.System.Exception)
			.Write(emitNullable ? "? exception, " : " exception, ")
			.Write(PropertyLibrary.BuiltInTypes.BoolKeyword)
			.WriteLine(" escape)");

		using (writer.OpenBlockScope())
		{
			writer.WriteLine("if (activity == null || exception == null)");
			using (writer.OpenBlockScope())
				writer.WriteLine("return;");

			writer.NewLine();

			const string tagsListVariableName = "tagsCollection";
			writer
				.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityTagsCollection)
				.Write(' ')
				.Write(tagsListVariableName)
				.Write(" = new ")
				.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityTagsCollection)
				.WriteLine("();");

			EmitExceptionParam(writer, tagsListVariableName, "escape", "exception");

			const string eventVariableName = "recordExceptionEvent";

			writer
				.NewLine()
				.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityEvent)
				.Write(' ')
				.Write(eventVariableName)
				.Write(" = new ")
				.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityEvent)
				// name:
				.Write("(name: ")
				.Write(PropertyLibrary.Activities.Tag_ExceptionEventName.Wrap())
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
			.Write(PropertyLibrary.Activities.Tag_ExceptionEscaped.Wrap())
			.Write(", ")
			.Write(escapeParam)
			.WriteLine(");");

		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(PropertyLibrary.Activities.Tag_ExceptionMessage.Wrap())
			.Write(", ")
			.Write(exceptionParam)
			.WriteLine(".Message);");

		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(PropertyLibrary.Activities.Tag_ExceptionType.Wrap())
			.Write(", ")
			.Write(exceptionParam)
			.WriteLine(".GetType().FullName);");

		writer
			.Write(tagsListVariableName)
			.Write(".Add(")
			.Write(PropertyLibrary.Activities.Tag_ExceptionStackTrace.Wrap())
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
		ISourceGenLogger? logger,
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

		if (!GuardMethod(methodTarget, target, logger))
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
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		var privateMethodName = methodTarget.MethodName + "_Activity";
		context.CancellationToken.ThrowIfCancellationRequested();

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(
					privateMethodName,
					methodTarget.ReturnType,
					TypeDeclarationAccessibility.Private
				)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.ParameterName,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
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
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		logger?.Debug($"Building public delegating method for {methodTarget.MethodName}.");
		context.CancellationToken.ThrowIfCancellationRequested();

		writer.NewLine();

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(
					methodTarget.MethodName,
					methodTarget.ReturnType,
					TypeDeclarationAccessibility.Public
				)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.ParameterName,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			var returnsActivity = methodTarget.ReturnType.Identity.Equals(
				TypeLibrary.Activities.SystemDiagnostics.Activity
			);
			var paramList = string.Join(", ", methodTarget.Parameters.Select(p => p.ParameterName));

			// Create filtered parameter list for Logging/Metrics (excludes Activity-related types)
			var loggingMetricsParamList = string.Join(
				", ",
				methodTarget
					.Parameters.Where(p =>
						!p.ParameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity)
						&& !p.ParameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityContext)
						&& !p.ParameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLink)
						&& !p.ParameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLinkArray)
						&& !p.ParameterType.Identity.Equals(TypeLibrary.System.TagList)
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
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(
					methodTarget.MethodName,
					methodTarget.ReturnType,
					TypeDeclarationAccessibility.Public
				)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.ParameterName,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
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
		ISourceGenLogger? logger
	)
	{
		if (!methodTarget.TargetGenerationState.IsValid)
		{
			if (methodTarget.TargetGenerationState.RaiseMultiGenerationTargetsNotSupported)
			{
				logger?.Debug(
					$"Identified {target.InterfaceType.Identity.Name}.{methodTarget.MethodName} as problematic as it has another target types."
				);
			}
			else if (methodTarget.TargetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
			{
				logger?.Debug(
					$"Identified {target.InterfaceType.Identity.Name}.{methodTarget.MethodName} as problematic as it is inferred."
				);
			}

			return false;
		}

		// Event methods must return void only
		// Activity and Context methods can return void or Activity?
		var isEvent = methodTarget.MethodType == ActivityMethodType.Event;

		var isValidReturnType = isEvent
			? methodTarget.ReturnType.Identity.SpecialType == SpecialType.System_Void
			: methodTarget.ReturnType.Identity.SpecialType == SpecialType.System_Void
				|| methodTarget.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity);

		if (!isValidReturnType)
		{
			logger?.Diagnostic(
				$"The return type {methodTarget.ReturnType} isn't valid for an activity, event, or context method."
			);

			return false;
		}

		if (target.ActivitySourceGenerationAttribute?.GenerateDiagnosticsForMissingActivity.Value ?? true)
		{
			// Here we're opting in to generate diagnostics for missing activity return/ params.
			if (methodTarget.MethodType == ActivityMethodType.Activity)
			{
				if (!methodTarget.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
				{
					logger?.Diagnostic($"No Activity returned for {methodTarget.MethodName}.");
				}
				else if (!methodTarget.ReturnType.IsNullable)
				{
					logger?.Diagnostic($"Activity return type is not nullable for {methodTarget.MethodName}.");
				}
			}
			else
			{
				if (!methodTarget.HasActivityParameter)
				{
					logger?.Diagnostic($"No Activity parameter is defined on {methodTarget.MethodName}.");
				}
				else if (
					!methodTarget
						.Parameters[0]
						.ParameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity)
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
