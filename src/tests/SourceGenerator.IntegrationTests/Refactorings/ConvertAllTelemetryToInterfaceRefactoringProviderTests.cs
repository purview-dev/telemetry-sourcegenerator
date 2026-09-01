namespace Purview.Telemetry.SourceGenerator.Refactorings;

public sealed class ConvertAllTelemetryToInterfaceRefactoringProviderTests : CodeRefactoringTestBase
{
	static readonly ConvertAllTelemetryToInterfaceRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// No-op cases
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithNoTelemetry_ReturnsNoActions(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			namespace Testing;

			public class $$OrderService
			{
				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Action detection with each telemetry type
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithILogger_ReturnsAction(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;

				public WeatherService(ILogger<WeatherService> logger) => _logger = logger;

				public void GetWeather(string city)
				{
					_logger.LogInformation("Getting weather for {City}", city);
				}
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsNotEmpty();
		await Assert.That(actions[0].Title).IsEqualTo("Convert all telemetry to IWeatherServiceTelemetry");
	}

	[Test]
	public async Task ComputeRefactorings_GivenClassWithActivitySource_ReturnsAction(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ActivitySource _activitySource = new("Weather");

				public void GetWeather(string city)
				{
					using var activity = _activitySource.StartActivity("get-weather");
				}
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsNotEmpty();
		await Assert.That(actions[0].Title).IsEqualTo("Convert all telemetry to IWeatherServiceTelemetry");
	}

	[Test]
	public async Task ComputeRefactorings_GivenClassWithMetrics_ReturnsAction(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$WeatherService
			{
				readonly Counter<int> _requestCounter;

				public WeatherService(Meter meter) =>
					_requestCounter = meter.CreateCounter<int>("requests");

				public void GetWeather()
				{
					_requestCounter.Add(1);
				}
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsNotEmpty();
		await Assert.That(actions[0].Title).IsEqualTo("Convert all telemetry to IWeatherServiceTelemetry");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Combined interface generation
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenAllThreeTelemetryTypes_GeneratesCombinedInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;
			using System.Diagnostics.Metrics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$FullService
			{
				readonly ILogger<FullService> _logger;
				readonly ActivitySource _activitySource = new("Full");
				readonly Counter<int> _requestCounter;

				public FullService(ILogger<FullService> logger, Meter meter)
				{
					_logger = logger;
					_requestCounter = meter.CreateCounter<int>("requests");
				}

				public void DoWork(string input)
				{
					using var activity = _activitySource.StartActivity("do-work");
					_logger.LogInformation("Doing work with {Input}", input);
					_requestCounter.Add(1);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[ActivitySource]");
		await Assert.That(result).Contains("[Logger]");
		await Assert.That(result).Contains("[Meter]");
		await Assert.That(result).Contains("IFullServiceTelemetry");
		await Assert.That(result).Contains("using Purview.Telemetry;");
	}

	[Test]
	public async Task ApplyRefactoring_GivenILoggerOnly_GeneratesTelemetryInterfaceWithLoggerAttribute(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$ReportService
			{
				readonly ILogger<ReportService> _logger;

				public ReportService(ILogger<ReportService> logger) => _logger = logger;

				public void GenerateReport(string reportId)
				{
					_logger.LogInformation("Generating report {ReportId}", reportId);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Logger]");
		await Assert.That(result).Contains("IReportServiceTelemetry");
		await Assert.That(result).DoesNotContain("[ActivitySource]");
		await Assert.That(result).DoesNotContain("[Meter]");
	}

	[Test]
	public async Task ApplyRefactoring_GivenActivitySourceOnly_GeneratesTelemetryInterfaceWithActivityAttribute(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$TracingService
			{
				readonly ActivitySource _activitySource = new("Tracing");

				public void DoTracedWork(string id)
				{
					using var activity = _activitySource.StartActivity("traced-work");
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[ActivitySource]");
		await Assert.That(result).Contains("ITracingServiceTelemetry");
		await Assert.That(result).DoesNotContain("[Logger]");
		await Assert.That(result).DoesNotContain("[Meter]");
	}
}
