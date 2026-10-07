using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.MultiProject.ConsoleApp;

/// <summary>
/// The host owns telemetry too, so the aggregate is its own names plus everything it references.
/// </summary>
[ActivitySource("multiproject-host")]
interface IBatchTelemetry
{
	[Activity]
	Activity? RunningBatch([Tag] int orderCount);
}
