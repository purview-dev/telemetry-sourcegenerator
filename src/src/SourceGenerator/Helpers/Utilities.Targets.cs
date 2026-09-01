using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class Utilities
{
	public static string OrNullKeyword(this LogParameterTarget? paramTarget) =>
		paramTarget is null ? "null" : paramTarget.Name;
}
