#if !NET7_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

[System.AttributeUsage(
	AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property,
	Inherited = false
)]
sealed class NotNullWhenAttribute : System.Attribute
{
	public bool Value { get; }

	public NotNullWhenAttribute(bool value)
	{
		Value = value;
	}
}

#endif
