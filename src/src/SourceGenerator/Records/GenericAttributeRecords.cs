namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// A parameter that carries either a <c>[Tag]</c> or <c>[Baggage]</c> marker attribute.
/// </summary>
sealed record TagOrBaggageAttributeRecord(string? Name, bool SkipOnNullOrEmpty);

/// <summary>
/// sealed record for ExcludeTargetsAttribute on parameters.
/// Tracks which target families a parameter should be excluded from.
/// </summary>
sealed record ExcludeTargetsAttributeRecord(GenerationType ExcludedTargets);
