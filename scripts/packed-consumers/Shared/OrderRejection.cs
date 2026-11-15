namespace PackedTelemetry;

/// <summary>
/// A hand-written C# 15 union used as a telemetry parameter, so the packed generator's accessor is compiled into every
/// consumer. It declares the non-boxing access pattern (<see cref="HasValue"/> plus one <c>TryGetValue</c> per case), so
/// the generated accessor reads the case without boxing the union.
/// </summary>
/// <remarks>
/// Hand-written rather than declared with the <c>union</c> keyword: the shape is then identical on every compiler that
/// can see this file, which is what the consumers assert. Only the case value must reach the event and the log entry.
/// </remarks>
[System.Runtime.CompilerServices.Union]
public readonly struct OrderRejection
{
	readonly object? _value;

	public OrderRejection(CardDeclined value) => _value = value;

	public OrderRejection(OutOfStock value) => _value = value;

	public object? Value => _value;

	public bool HasValue => _value is not null;

	public bool TryGetValue(out CardDeclined value)
	{
		if (_value is CardDeclined declined)
		{
			value = declined;
			return true;
		}

		value = null!;
		return false;
	}

	public bool TryGetValue(out OutOfStock value)
	{
		if (_value is OutOfStock outOfStock)
		{
			value = outOfStock;
			return true;
		}

		value = null!;
		return false;
	}
}

public sealed record CardDeclined(string Reason);

public sealed record OutOfStock(string Sku);
