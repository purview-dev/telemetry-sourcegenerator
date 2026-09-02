namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// A parameter that carries either a <c>[Tag]</c> or <c>[Baggage]</c> marker attribute.
/// </summary>
readonly record struct TagOrBaggageAttributeRecord(string? Name, bool SkipOnNullOrEmpty);

/// <summary>
/// readonly record struct for ExcludeTargetsAttribute on parameters.
/// Tracks which target families a parameter should be excluded from.
/// </summary>
readonly record struct ExcludeTargetsAttributeRecord(GenerationType ExcludedTargets);
