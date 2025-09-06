namespace Purview.Telemetry.SourceGenerator;

public class LoggingMessageTemplateTests(ITestOutputHelper output)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
	[Fact]
	public async Task SpecificLevelAttributes_EmitExpectedLevelsAndTemplates()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.LevelsCore.Logging.g.cs");

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
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.NamedCore.Logging.g.cs");

		content.ShouldContain("\"Hello {user} did {count}\"");
	}

	[Fact]
	public async Task OrdinalPlaceholders_WithinParameterCount_Succeeds()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.OrdCore.Logging.g.cs");

		content.ShouldContain("\"A={0}, B={1}\"");
	}

	[Fact]
	public async Task MixedNamedAndOrdinal_RaisesDiagnostic_TSG2004()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		result
			.Diagnostics.Any(d => d.Id == "TSG2004" && d.Severity == DiagnosticSeverity.Error)
			.ShouldBeTrue();
	}

	[Fact]
	public async Task OrdinalExceedsParameterCount_RaisesDiagnostic_TSG2005()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		result
			.Diagnostics.Any(d => d.Id == "TSG2005" && d.Severity == DiagnosticSeverity.Error)
			.ShouldBeTrue();
	}

	[Fact]
	public async Task ScopedLoggingWithLevel_RaisesWarning_TSG2007()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		result
			.Diagnostics.Any(d => d.Id == "TSG2007" && d.Severity == DiagnosticSeverity.Warning)
			.ShouldBeTrue();
	}

	[Fact]
	public async Task LogProperties_ExpandsProperties()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.PropsCore.Logging.g.cs");

		content.ShouldContain("Name = {Name}");
		content.ShouldContain("Age = {Age}");
	}

	[Fact]
	public async Task ExpandEnumerable_And_LogProperties_Together_RaisesError_TSG2006()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		result
			.Diagnostics.Where(d => d.Id == "TSG2006" && d.Severity == DiagnosticSeverity.Error)
			.ShouldNotBeEmpty();
	}

	[Fact]
	public async Task ExpandEnumerable_UnboundedHighMax_RaisesWarning_TSG2008()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src);

		// Assert
		result
			.Diagnostics.Any(d => d.Id == "TSG2008" && d.Severity == DiagnosticSeverity.Warning)
			.ShouldBeTrue();
	}
}
