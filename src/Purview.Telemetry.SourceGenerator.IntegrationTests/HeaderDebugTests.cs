using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Purview.Telemetry.SourceGenerator;

public partial class HeaderDebugTests : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Fact] // Temporary debug
	public async Task DumpHeaders()
	{
		const string src =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

[ActivitySource(""activity-source"")]
[Logger]
[Meter]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();
}
";

		var result = await GenerateAsync(src);
		foreach (var g in result.GetGeneratedSourceResults())
		{
			var text = g.SourceText.ToString();
			var firstLines = string.Join(Environment.NewLine, text.Split('\n').Take(12));
			Console.WriteLine($"### {g.HintName}\n{firstLines}\n");
		}
	}
}
