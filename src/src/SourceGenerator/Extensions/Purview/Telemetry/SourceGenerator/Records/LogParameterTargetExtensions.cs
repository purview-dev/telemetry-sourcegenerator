using System.ComponentModel;

namespace Purview.Telemetry.SourceGenerator.Records;

[EditorBrowsable(EditorBrowsableState.Never)]
static class LogParameterTargetExtensions
{
	extension(LogParameterTarget? paramTarget)
	{
		public string OrNullKeyword() => paramTarget is null ? "null" : paramTarget.Name;
	}
}
