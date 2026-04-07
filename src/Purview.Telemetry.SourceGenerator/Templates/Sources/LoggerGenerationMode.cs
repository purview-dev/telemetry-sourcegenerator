#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry
{

/// <summary>
/// Controls which generation mode is used for <see cref="global::Microsoft.Extensions.Logging.ILogger"/>-based log methods.
/// </summary>
{CodeGen}
enum LoggerGenerationMode
{
	/// <summary>
	/// Automatically selects the best generation mode per method, based on parameters and their types.
	/// Uses <see cref="V1"/> when the method has 6 or fewer non-exception parameters, a single
	/// <see cref="global::System.Exception"/> parameter (or none), and no <c>ExpandEnumerableAttribute</c>
	/// or <c>LogPropertiesAttribute</c> usage. Otherwise uses <see cref="V2"/>.
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Forces generation using the high-performance
	/// <c>LoggerMessage.Define</c> pattern.
	/// Limited to 6 non-exception parameters and a single <see cref="global::System.Exception"/> parameter.
	/// </summary>
	V1 = 1,

	/// <summary>
	/// Forces generation using typed state structs, matching the output of the
	/// <c>Microsoft.Gen.Logging</c> source generator.
	/// Supports unlimited parameters, multiple exceptions, <c>ExpandEnumerableAttribute</c>,
	/// and <c>LogPropertiesAttribute</c>.
	/// Requires the <c>Microsoft.Extensions.Telemetry.Abstractions</c> package.
	/// </summary>
	V2 = 2
}

}
#endif
