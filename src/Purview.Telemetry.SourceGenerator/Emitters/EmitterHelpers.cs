using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class EmitterHelpers
{
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
			interfaceGenerationType.HasFlag(GenerationType.Activities)
				? currentEmitterType == GenerationType.Activities
				: interfaceGenerationType.HasFlag(GenerationType.Logging)
					? currentEmitterType == GenerationType.Logging
					: currentEmitterType == GenerationType.Metrics;

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
