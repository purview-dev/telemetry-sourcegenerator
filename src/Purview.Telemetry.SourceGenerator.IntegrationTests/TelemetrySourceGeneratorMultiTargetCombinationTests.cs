namespace Purview.Telemetry.SourceGenerator;

public class TelemetrySourceGeneratorMultiTargetCombinationTests(ITestOutputHelper output)
    : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
    [Fact]
    public async Task MultiTarget_ActivityAndLogging_GeneratesBothSections()
    {
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

        var result = await GenerateAsync(src);

        // Find the MultiTarget generated file
        var generated = result.DriverResult.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(g => g.HintName.EndsWith("ICombinedCore.MultiTarget.g.cs", StringComparison.Ordinal));

        generated.HintName.ShouldNotBeNull();
        var text = generated.SourceText.ToString();

        text.ShouldContain("StartActivity(\"DoWork\")");
        text.ShouldContain("_logger.LogInformation");
        text.ShouldContain("DoWork called");
    }

    [Fact]
    public async Task MultiTarget_TagsAndBaggage_AppliesToActivity()
    {
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

        var result = await GenerateAsync(src);
        var generated = result.DriverResult.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(g => g.HintName.EndsWith("IWithTagsCore.MultiTarget.g.cs", StringComparison.Ordinal));

        generated.HintName.ShouldNotBeNull();
        var text = generated.SourceText.ToString();

        text.ShouldContain("SetTag(\"user_id\", user)");
        text.ShouldContain("SetBaggage(\"ctx\", ctx?.ToString())");
    }

    [Fact]
    public async Task MultiTarget_MetricsOnly_EmitsPlaceholder()
    {
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

        var result = await GenerateAsync(src);
        var generated = result.DriverResult.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(g => g.HintName.EndsWith("IMeasureCore.MultiTarget.g.cs", StringComparison.Ordinal));

        generated.HintName.ShouldNotBeNull();
        var text = generated.SourceText.ToString();
        text.ShouldContain("// Metrics instrumentation for Track");
        text.ShouldContain("var tags = global::System.Array.Empty");
    }

    [Fact]
    public async Task MultiTarget_Exclusions_AreHonoured()
    {
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

        var result = await GenerateAsync(src);
        var generated = result.DriverResult.Results
            .SelectMany(r => r.GeneratedSources)
            .FirstOrDefault(g => g.HintName.EndsWith("IExclusionsCore.MultiTarget.g.cs", StringComparison.Ordinal));

        generated.HintName.ShouldNotBeNull();
        var text = generated.SourceText.ToString();

        // Activity (no reference to 'a')
        text.ShouldContain("StartActivity(\"Action\")");
        text.ShouldNotContain(" a)");

        // Logging (no reference to 'b' in template args)
        text.ShouldContain("Action called");
        text.ShouldNotContain(", b)");

        // Metrics placeholder (no reference to 'c')
        text.ShouldContain("// Metrics instrumentation for Action");
        text.ShouldNotContain(" c);");
    }
}
