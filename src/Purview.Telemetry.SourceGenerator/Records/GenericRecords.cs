using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Purview.Telemetry.SourceGenerator.Records;

[Flags]
enum GenerationType
{
	None = 0,
	Activities = 1,
	Logging = 2,
	Metrics = 4,
	All = Activities | Logging | Metrics,
}

sealed record TargetGeneration(
	bool IsValid,
	bool RaiseInferenceNotSupportedWithMultiTargeting,
	bool RaiseMultiGenerationTargetsNotSupported,
	bool IsMultiTarget = false,
	GenerationType MethodTargets = GenerationType.None,
	string? ActivityParameterWithoutTarget = null,
	bool RaiseMissingInterfaceSource = false
);

sealed record AttributeValue<T>
	where T : struct
{
	public AttributeValue(T? value)
	{
		Value = value;
		IsSet = true;
	}

	public AttributeValue() => IsSet = false;

	public T? Or(T value) => IsSet ? Value : value;

	public T? Value { get; }

	public bool IsSet { get; }

	public static implicit operator AttributeValue<T>(T? value) => new(value);

	public static implicit operator T?(AttributeValue<T> value) => value.Value;
}

record AttributeStringValue
{
	public AttributeStringValue(string? value)
	{
		Value = value;
		IsSet = true;
	}

	public AttributeStringValue() => IsSet = false;

	public string Or(string value) => IsSet && !string.IsNullOrWhiteSpace(Value) ? Value! : value;

	public string? Value { get; }

	public bool IsSet { get; }

	public static implicit operator AttributeStringValue(string? value) => new(value);

	public static implicit operator string?(AttributeStringValue value) => value.Value;
}

public record struct MessageTemplateHole(
	string? Name,
	int? Ordinal,
	int? Alignment,
	string? Format,
	bool Destructure,
	bool Stringify
)
{
	public readonly bool IsPositional => Ordinal.HasValue;

	public readonly void Validate()
	{
		if (Name == null && Ordinal == null)
			throw new Exception("Name and Ordinal cannot both be null.");
		if (Name != null && Ordinal != null)
			throw new Exception("Name and Ordinal cannot both be set.");
		if (Destructure && Stringify)
			throw new Exception("Destructure and Stringify cannot both be true.");
	}

	public static EquatableArray<MessageTemplateHole> FromMatches(MatchCollection matches)
	{
		List<MessageTemplateHole>? holes = null;
		if (matches != null)
		{
			foreach (Match match in matches)
			{
				var hole = FromMatch(match);
				holes ??= [];
				holes.Add(hole);
			}
		}

		return holes?.ToImmutableArray() ?? new EquatableArray<MessageTemplateHole>([]);
	}

	static MessageTemplateHole FromMatch(Match match)
	{
		if (match?.Success != true)
			throw new ArgumentException("Match must be successful.", nameof(match));

		string? name = null;
		string? ordinal = null;
		string? alignment = null;
		string? format = null;
		var destructure = match.Groups["destructure"].Success;
		var stringify = match.Groups["stringify"].Success;

		// We're tracking a lot of additional information here, but we're not using it.
		// We just need to replace the matched named group or ordinal with the value eventually.
		// There's no point in tracking start/ end and doing the replacement in the reverse,
		// as replacing repeated named properties will just alter other positions.
		if (match.Groups["named"].Success)
			name = match.Groups["named"].Value;
		if (match.Groups["ordinal"].Success)
			ordinal = match.Groups["ordinal"].Value;
		if (match.Groups["alignment"].Success)
			alignment = match.Groups["alignment"].Value;
		if (match.Groups["format"].Success)
			format = match.Groups["format"].Value;

		return new(
			name,
			ordinal == null ? null : int.Parse(ordinal, CultureInfo.InvariantCulture),
			alignment == null ? null : int.Parse(alignment, CultureInfo.InvariantCulture),
			format,
			destructure,
			stringify
		);
	}
}
