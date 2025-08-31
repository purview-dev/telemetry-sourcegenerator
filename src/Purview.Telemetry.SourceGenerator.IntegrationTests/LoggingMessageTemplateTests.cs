namespace Purview.Telemetry.SourceGenerator;

public class LoggingMessageTemplateTests(ITestOutputHelper output)
    : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
    [Fact]
    public async Task SpecificLevelAttributes_EmitExpectedLevelsAndTemplates()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface ILevels
            {
                [Trace("Trace {val}")] void TraceLog(string val);
                [Debug("Debug {val}")] void DebugLog(string val);
                [Info("Info {val}")] void InfoLog(string val);
                [Warning("Warn {val}")] void WarnLog(string val);
                [Error("Error {val}")] void ErrorLog(string val, System.Exception? ex = null);
                [Critical("Critical {val}")] void CriticalLog(string val);
            }
            """;

        var result = await GenerateAsync(src);
        var content = string.Join("\n", result.DriverResult.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));

        content.ShouldContain("LogLevel.Trace");
        content.ShouldContain("\"Trace {val}\"");

        content.ShouldContain("LogLevel.Debug");
        content.ShouldContain("\"Debug {val}\"");

        content.ShouldContain("LogLevel.Information");
        content.ShouldContain("\"Info {val}\"");

        content.ShouldContain("LogLevel.Warning");
        content.ShouldContain("\"Warn {val}\"");

        content.ShouldContain("LogLevel.Error");
        content.ShouldContain("\"Error {val}\"");

        content.ShouldContain("LogLevel.Critical");
        content.ShouldContain("\"Critical {val}\"");
    }

    [Fact]
    public async Task NamedPlaceholders_ArePreservedInTemplate()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface INamed
            {
                [Log("Hello {user} did {count}")]
                void Hello(string user, int count);
            }
            """;

        var result = await GenerateAsync(src);
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources).First(s => s.HintName.EndsWith("INamedCore.Logging.g.cs")).SourceText.ToString();
        text.ShouldContain("\"Hello {user} did {count}\"");
    }

    [Fact]
    public async Task OrdinalPlaceholders_WithinParameterCount_Succeeds()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IOrd
            {
                [Log("A={0}, B={1}")] void M(string a, int b);
            }
            """;

        var result = await GenerateAsync(src);
        // No diagnostics expected
        result.Diagnostics.ShouldBeEmpty();
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources).First(s => s.HintName.EndsWith("IOrdCore.Logging.g.cs")).SourceText.ToString();
        text.ShouldContain("\"A={0}, B={1}\"");
    }

    [Fact]
    public async Task MixedNamedAndOrdinal_RaisesDiagnostic_TSG2004()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IMixed
            {
                [Log("A={0}, B={name}")] void M(string name, int b);
            }
            """;

        var result = await GenerateAsync(src);
        result.Diagnostics.Any(d => d.Id == "TSG2004" && d.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    public async Task OrdinalExceedsParameterCount_RaisesDiagnostic_TSG2005()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface ITooMany
            {
                [Log("X={2}")] void M(string a, int b);
            }
            """;

        var result = await GenerateAsync(src);
        result.Diagnostics.Any(d => d.Id == "TSG2005" && d.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    public async Task ScopedLoggingWithLevel_RaisesWarning_TSG2007()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IScoped
            {
                [Debug] System.IDisposable BeginScope(string op);
            }
            """;

        var result = await GenerateAsync(src);
        result.Diagnostics.Any(d => d.Id == "TSG2007" && d.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
    }

    [Fact]
    public async Task LogProperties_ExpandsProperties()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            public class Person { public string Name { get; set; } = ""; public int Age { get; set; } }

            [Logger]
            public partial interface IProps
            {
                [Log] void Write([LogProperties] Person person);
            }
            """;

        var result = await GenerateAsync(src);
        var text = result.DriverResult.Results.SelectMany(r => r.GeneratedSources).First(s => s.HintName.EndsWith("IPropsCore.Logging.g.cs")).SourceText.ToString();
        text.ShouldContain("Name = {Name}");
        text.ShouldContain("Age = {Age}");
    }

    [Fact]
    public async Task ExpandEnumerable_And_LogProperties_Together_RaisesError_TSG2006()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            public class Item { public string Id { get; set; } = ""; }

            [Logger]
            public partial interface IInvalid
            {
                [Log] void Write([LogProperties][ExpandEnumerable] Item[] items);
            }
            """;

        var result = await GenerateAsync(src);
        result.Diagnostics.Any(d => d.Id == "TSG2006" && d.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Fact]
    public async Task ExpandEnumerable_UnboundedHighMax_RaisesWarning_TSG2008()
    {
        const string src = """
            using Microsoft.Extensions.Logging;
            using Purview.Telemetry.Logging;

            namespace Tests;

            [Logger]
            public partial interface IEnum
            {
                [Log] void Write([ExpandEnumerable(MaximumValueCount = 10)] string[] values);
            }
            """;

        var result = await GenerateAsync(src);
        result.Diagnostics.Any(d => d.Id == "TSG2008" && d.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
    }
}

