#if NET48
namespace System.Runtime.CompilerServices;

/// <summary>
/// .NET Framework 4.8 has no <c>IsExternalInit</c>, which the compiler needs to emit a positional record's
/// <c>init</c> accessors. The benchmark suite deliberately includes a .NET Framework leg, so the shim the modern
/// framework supplies is declared here.
/// </summary>
internal static class IsExternalInit { }
#endif
