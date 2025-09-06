namespace Purview.Telemetry.SourceGenerator;

public class LoggingV2StateObjectTests(ITestOutputHelper output)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
	[Fact]
	public async Task StateObject_IsBuilt_WithOriginalFormat_And_Properties()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src, includeLoggerTypes: IncludeLoggerTypes.Telemetry);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.LogPropsCore.Logging.g.cs");

		content.ShouldContain("LoggerMessageHelper.ThreadLocalState");
		content.ShouldContain("ReserveTagSpace(");
		content.ShouldContain("\"{OriginalFormat}\"");
		content.ShouldContain("TagArray[0] = new(\"{OriginalFormat}\"");
		content.ShouldContain("TagArray");
		content.ShouldContain("person?.Name");
		content.ShouldContain("person?.Age");
		content.ShouldContain(".Log(");
	}

	[Fact]
	public async Task LogProperties_SkipNull_And_OmitReferenceName()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src, includeLoggerTypes: IncludeLoggerTypes.Telemetry);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.PropsCore.Logging.g.cs");

		// Temporary var used when skipping nulls
		content.ShouldContain("var tmp");
		// OmitReferenceName => property key is just property name, not prefixed with parameter name
		content.ShouldContain("\"Value\"");
		content.ShouldNotContain("data.Value\"");
	}

	[Fact]
	public async Task Enumerable_Stringify_IsUsed()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src, includeLoggerTypes: IncludeLoggerTypes.Telemetry);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.EnumCore.Logging.g.cs");

		content.ShouldContain("LoggerMessageHelper.Stringify(values)");
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

		var result = await GenerateAsync(src, includeLoggerTypes: IncludeLoggerTypes.Telemetry);
		var content = string.Join(
			"\n",
			result
				.DriverResult.Results.SelectMany(r => r.GeneratedSources)
				.Select(s => s.SourceText.ToString())
		);

		content.ShouldContain("new (42, nameof(CustomName))");
		content.ShouldContain("nameof(B)");
	}

	[Fact]
	public async Task Scoped_Returns_BeginScope_With_FormattedMessage()
	{
		// Arrange
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

		// Act
		var result = await GenerateAsync(src, includeLoggerTypes: IncludeLoggerTypes.Telemetry);

		// Assert
		var content = result.GetGeneratedSourceText("Tests.ScopesCore.Logging.g.cs");

		content.ShouldContain("formattedMessage");
		content.ShouldContain("BeginScope(");
	}
}
