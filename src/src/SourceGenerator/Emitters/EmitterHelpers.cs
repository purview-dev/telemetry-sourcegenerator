using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class EmitterHelpers
{
	/// <summary>
	/// Finalizes a generated target class: closes the class and namespace, injects the
	/// auto-generated header, and adds the file to the compilation. Shared by all emitters.
	/// </summary>
	public static void EmitTargetSource(
		string? classNamespace,
		EquatableArray<string> parentClasses,
		int indent,
		StringBuilder builder,
		string hintName,
		SourceProductionContext context,
		bool emitNullable
	)
	{
		EmitHelpers.EmitClassEnd(builder, indent);
		EmitHelpers.EmitNamespaceEnd(classNamespace, parentClasses, indent, builder, context.CancellationToken);

		var sourceText = EmbeddedResources.Instance.AddHeader(builder.ToString(), emitNullable);
		context.AddSource(hintName, Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8));
	}

	/// <summary>
	/// Determines whether the current emitter should generate a throw stub for an invalid method.
	/// A throw stub is emitted to satisfy CS0535 (missing interface implementation) when no real
	/// implementation will be generated.
	/// </summary>
	/// <param name="targetGenerationState">The generation state of the method target.</param>
	/// <param name="currentEmitterType">The GenerationType flag for the current emitter.</param>
	/// <param name="interfaceGenerationType">The registered telemetry types for the interface.</param>
	public static bool ShouldEmitThrowStub(
		TargetGeneration targetGenerationState,
		GenerationType currentEmitterType,
		GenerationType interfaceGenerationType
	)
	{
		if (targetGenerationState.IsValid)
			return false;

		// Methods with no telemetry attributes on a multi-target interface emit TSG1001
		// and the user is required to provide their own partial implementation.
		// Do NOT emit a throw stub or it would conflict with the user's implementation.
		if (targetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
			return false;

		var methodTargets = targetGenerationState.MethodTargets;

		// Case 1: This emitter type is explicitly targeted by the method (e.g. post-pass duplicate).
		if (methodTargets.HasFlag(currentEmitterType))
			return true;

		// For all remaining cases we need canonical emitter priority (Activities > Logging > Metrics).
		var isCanonical =
			SharedHelpers.GetCanonicalTargetType(interfaceGenerationType, includeActivities: true)
			== currentEmitterType;

		// Case 2: Method has no telemetry attributes at all — only canonical emitter stubs.
		if (methodTargets == GenerationType.None)
			return isCanonical;

		// Case 3: All of the method's target types fall outside the registered interface types
		// (RaiseMissingInterfaceSource) — only canonical emitter stubs.
		if ((methodTargets & interfaceGenerationType) == GenerationType.None)
			return isCanonical;

		// The method belongs to another registered emitter; that emitter provides the real implementation.
		return false;
	}
}
