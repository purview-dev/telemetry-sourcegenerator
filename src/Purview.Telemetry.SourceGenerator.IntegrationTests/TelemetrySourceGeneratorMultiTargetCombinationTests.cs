namespace Purview.Telemetry.SourceGenerator;

public class TelemetrySourceGeneratorMultiTargetCombinationTests(ITestOutputHelper output)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
	[Fact]
	public async Task MultiTarget_ActivityAndLogging_GeneratesBothSections()
	{
		// Arrange
		const string src = """
			using Microsoft.Extensions.Logging;
			using System.Diagnostics;

			[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

			namespace Tests;

			[Purview.Telemetry.TelemetryGeneration]
			public partial interface ICombined
			{
			    [Purview.Telemetry.Telemetry(GenerateActivity = true, GenerateLogging = true)]
			    void DoWork([Purview.Telemetry.Tag] string user, int count);
			}
			""";

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.CombinedCore.MultiTarget.g.cs");

		content.ShouldContain("StartActivity(\"DoWork\")");
		content.ShouldContain("_logger.LogInformation");
		content.ShouldContain("DoWork called");
	}

	[Fact]
	public async Task MultiTarget_TagsAndBaggage_AppliesToActivity()
	{
		// Arrange
		const string src = """
			using System.Diagnostics;

			[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

			namespace Tests;

			[Purview.Telemetry.TelemetryGeneration]
			public partial interface IWithTags
			{
			    [Purview.Telemetry.Telemetry(GenerateActivity = true)]
			    void Operate([Purview.Telemetry.Tag("user_id")] string user,
			                 [Purview.Telemetry.Activities.Baggage("ctx")] string ctx);
			}
			""";

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.WithTagsCore.MultiTarget.g.cs");

		content.ShouldContain("SetTag(\"user_id\", user)");
		content.ShouldContain("SetBaggage(\"ctx\", ctx?.ToString())");
	}

	[Fact]
	public async Task MultiTarget_MetricsOnly_EmitsPlaceholder()
	{
		// Arrange
		const string src = """
			[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

			namespace Tests;

			[Purview.Telemetry.TelemetryGeneration]
			public partial interface IMeasure
			{
			    [Purview.Telemetry.Telemetry(GenerateMetrics = true)]
			    void Track(int amount);
			}
			""";

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.MeasureCore.MultiTarget.g.cs");

		content.ShouldContain("// Metrics instrumentation for Track");
		content.ShouldContain("var tags = global::System.Array.Empty");
	}

	[Fact]
	public async Task MultiTarget_Exclusions_AreHonoured()
	{
		// Arrange
		const string src = """
			using Microsoft.Extensions.Logging;

			[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

			namespace Tests;

			[Purview.Telemetry.TelemetryGeneration]
			public partial interface IExclusions
			{
			    [Purview.Telemetry.Telemetry(GenerateActivity = true, GenerateLogging = true, GenerateMetrics = true)]
			    void Action(
			        string user,
			        [Purview.Telemetry.ExcludeFromActivity] string a,
			        [Purview.Telemetry.ExcludeFromLogging] int b,
			        [Purview.Telemetry.ExcludeFromMetrics] string c);
			}
			""";

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.ExclusionsCore.MultiTarget.g.cs");

		// Activity (no reference to 'a')
		content.ShouldContain("StartActivity(\"Action\")");
		content.ShouldNotContain(" a)");

		// Logging (no reference to 'b' in template args)
		content.ShouldContain("Action called");
		content.ShouldNotContain(", b)");

		// Metrics placeholder (no reference to 'c')
		content.ShouldContain("// Metrics instrumentation for Action");
		content.ShouldNotContain(" c);");
	}
}
