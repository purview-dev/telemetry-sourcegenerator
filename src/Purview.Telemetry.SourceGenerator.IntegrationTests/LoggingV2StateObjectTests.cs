namespace Purview.Telemetry.SourceGenerator;

public class LoggingV2StateObjectTests(ITestOutputHelper output)
    : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
    [Fact]
    public async Task StateObject_IsBuilt_WithOriginalFormat_And_Properties()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            public class Person { public string Name { get; set; } = string.Empty; public int Age { get; set; } }

            [Logger]
            public partial interface ILogProps
            {
                [Log("Hello {name}")]
                void Write([LogProperties] Person person, string name);
            }
            """;

        var result = await GenerateAsync(src);
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources)
            .First(s => s.HintName.EndsWith("LogPropsCore.Logging.g.cs", StringComparison.Ordinal))
            .SourceText.ToString();

        text.ShouldContain("LoggerMessageHelper.ThreadLocalState");
        text.ShouldContain("ReserveTagSpace(");
        text.ShouldContain("\"{OriginalFormat}\"");
        text.ShouldContain("TagArray[0] = new(\"{OriginalFormat}\"");
        text.ShouldContain("TagArray");
        text.ShouldContain("person?.Name");
        text.ShouldContain("person?.Age");
        text.ShouldContain(".Log(");
    }

    [Fact]
    public async Task LogProperties_SkipNull_And_OmitReferenceName()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            public class Data { public string? Value { get; set; } }

            [Logger]
            public partial interface IProps
            {
                [Log]
                void M([LogProperties(SkipNullProperties = true, OmitReferenceName = true)] Data data);
            }
            """;

        var result = await GenerateAsync(src);
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources)
            .First(s => s.HintName.EndsWith("IPropsCore.Logging.g.cs", StringComparison.Ordinal))
            .SourceText.ToString();

        // Temporary var used when skipping nulls
        text.ShouldContain("var tmp");
        // OmitReferenceName => property key is just property name, not prefixed with parameter name
        text.ShouldContain("\"Value\"");
        text.ShouldNotContain("data.Value\"");
    }

    [Fact]
    public async Task Enumerable_Stringify_IsUsed()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IEnum
            {
                [Log]
                void M(string[] values);
            }
            """;

        var result = await GenerateAsync(src);
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources)
            .First(s => s.HintName.EndsWith("IEnumCore.Logging.g.cs", StringComparison.Ordinal))
            .SourceText.ToString();

        text.ShouldContain("LoggerMessageHelper.Stringify(values)");
    }

    [Fact]
    public async Task ExplicitEventId_And_NameOverride()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IEvents
            {
                [Log(42, LogLevel.Information, name: "CustomName", messageTemplate: "X {x}")]
                void A(int x);

                [Log]
                void B(int y);
            }
            """;

        var result = await GenerateAsync(src);
        var content = string.Join("\n", result.DriverResult.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));

        content.ShouldContain("new (42, nameof(CustomName))");
        content.ShouldContain("nameof(B)");
    }

    [Fact]
    public async Task Scoped_Returns_BeginScope_With_FormattedMessage()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IScopes
            {
                [Log("Scope {id}")]
                System.IDisposable Begin(string id);
            }
            """;

        var result = await GenerateAsync(src);
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources)
            .First(s => s.HintName.EndsWith("IScopesCore.Logging.g.cs", StringComparison.Ordinal))
            .SourceText.ToString();

        text.ShouldContain("formattedMessage");
        text.ShouldContain("BeginScope(");
    }
}

