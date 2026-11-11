// C# 15's `closed` modifier is a language feature, but the compiler records it with runtime attributes that
// netstandard2.0 does not carry. Declaring them here is what lets the generator source use `closed` while still
// targeting netstandard2.0 for compiler-host compatibility. They are internal (the default for a top-level type), so
// they never reach the merged analyzer's public surface, and they are never emitted into a consumer's compilation.
#pragma warning disable IDE0130 // Namespace does not match folder structure: these must live in the BCL namespace
namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Records that a class is <c>closed</c>: its direct descendants are fixed to the declaring assembly, so a
	/// <c>switch</c> over it is exhaustive once every direct descendant is handled.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	sealed class IsClosedTypeAttribute : Attribute
	{
		public Type[]? DerivedTypes { get; set; }
	}

	/// <summary>
	/// Marks a member the compiler requires for a language feature the declaring compiler must understand, which for
	/// a closed class is the constructor that lets a subtype derive from it.
	/// </summary>
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
	{
		public string FeatureName { get; } = featureName;

		public bool IsOptional { get; init; }
	}
}
