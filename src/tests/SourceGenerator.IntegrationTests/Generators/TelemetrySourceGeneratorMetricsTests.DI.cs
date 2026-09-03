using Microsoft.Extensions.DependencyInjection;
using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenAssemblyEnableDI_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(
				metricsClass.HasMethod(
					query,
					"Counter",
					TypeReference.Create<int>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated metrics class must contain the counter method");
		var diClass = query.GetClass("TestMetricsCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestMetrics", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must register the metrics via AddTestMetrics");
	}

	[Test]
	public async Task Generate_GivenInterfaceEnableDI_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
[TelemetryGeneration(GenerateDependencyExtension = true)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(
				metricsClass.HasMethod(
					query,
					"Counter",
					TypeReference.Create<int>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated metrics class must contain the counter method");
		var diClass = query.GetClass("TestMetricsCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestMetrics", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must be generated when the interface opts in");
	}

	[Test]
	public async Task Generate_GivenDIDisabledAtAssemblyAndInterfaceEnableDI_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: TelemetryGeneration(GenerateDependencyExtension = false)]

namespace Testing;

[Meter("testing-meter")]
[TelemetryGeneration(GenerateDependencyExtension = true)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(
				metricsClass.HasMethod(
					query,
					"Counter",
					TypeReference.Create<int>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated metrics class must contain the counter method");
		var diClass = query.GetClass("TestMetricsCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestMetrics", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must be generated when the interface overrides the disabled assembly default");
	}

	[Test]
	public async Task Generate_GivenDIEnabledAtAssemblyAndInterfaceDisabledDI_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Meter("testing-meter")]
[TelemetryGeneration(GenerateDependencyExtension = false)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(
				metricsClass.HasMethod(
					query,
					"Counter",
					TypeReference.Create<int>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated metrics class must contain the counter method");
		await Assert
			.That(query.HasClass("TestMetricsCoreDIExtension", "Microsoft.Extensions.DependencyInjection"))
			.IsFalse()
			.Because("the DI extension must not be generated when the interface opts out");
	}
}
