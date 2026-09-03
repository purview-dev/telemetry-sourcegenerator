#if NET48

using System.ComponentModel;

// net48 lacks ModuleInitializerAttribute (net5+) and declares NotNullAttribute as internal.
// The TUnit infrastructure source generator emits [ModuleInitializer], and the tests use [NotNull];
// public polyfills are required so those compile on net48.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	sealed class ModuleInitializerAttribute : Attribute { }
}
#pragma warning restore IDE0130

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
	sealed class NotNullAttribute : Attribute { }
}
#pragma warning restore IDE0130

#endif
