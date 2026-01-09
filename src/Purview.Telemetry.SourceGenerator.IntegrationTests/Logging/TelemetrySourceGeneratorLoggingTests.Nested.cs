namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	[Arguments("Testing.Test1")]
	[Arguments("Testing.Test1.Test2")]
	[Arguments("Testing.Test1.Test2.Test3")]
	public async Task Generate_GivenLoggerWithNamespaces_GeneratesScopedLogTarget(
		string @namespace,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace {@namespace};

[Logger]
public interface ITestLogger {{
	IDisposable Log(string stringParam, int intParam);
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken,
			parameters: @namespace
		);
	}

	[Test]
	[Arguments("Testing.Test1")]
	[Arguments("Testing.Test1.Test2")]
	[Arguments("Testing.Test1.Test2.Test3")]
	public async Task Generate_GivenLoggerWithNamespacesAndNestedClass_GeneratesScopedLogTarget(
		string @namespace,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace {@namespace};

public partial class TestClass1 {{
	[Logger]
	public interface ITestLogger {{
		IDisposable Log(string stringParam, int intParam);
	}}
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken,
			parameters: @namespace
		);
	}

	[Test]
	[Arguments("Testing.Test1")]
	[Arguments("Testing.Test1.Test2")]
	[Arguments("Testing.Test1.Test2.Test3")]
	public async Task Generate_GivenLoggerWithNamespacesAndNestedClasses_GeneratesScopedLogTarget(
		string @namespace,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace {@namespace};

public partial class TestClass1 {{
	public partial class TestClass2 {{
		public partial class TestClass3 {{
			[Logger]
			public interface ITestLogger {{
				IDisposable Log(string stringParam, int intParam);
			}}
		}}
	}}
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken,
			parameters: @namespace
		);
	}
}
