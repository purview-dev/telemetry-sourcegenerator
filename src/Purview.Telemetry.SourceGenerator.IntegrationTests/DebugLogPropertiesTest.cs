using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Purview.Telemetry.SourceGenerator;

public class DebugLogPropertiesTest(ITestOutputHelper output)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(output)
{
	[Fact]
	public async Task Debug_LogProperties_SimpleCase()
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

		// Assert - Let's see what's actually generated
		var content = result.GetGeneratedSourceText("Tests.PropsCore.Logging.g.cs");

		// Output the full content for debugging
		Console.WriteLine("=== GENERATED CONTENT ===");
		Console.WriteLine(content);
		Console.WriteLine("=== END GENERATED CONTENT ===");

		// Now check for expected patterns
		content.ShouldContain("Name = {Name}");
		content.ShouldContain("Age = {Age}");
	}
}
