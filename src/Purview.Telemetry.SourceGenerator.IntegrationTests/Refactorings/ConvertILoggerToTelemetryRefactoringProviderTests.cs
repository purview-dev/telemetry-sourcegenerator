using Purview.Telemetry.SourceGenerator.Refactorings;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

public sealed class ConvertILoggerToTelemetryRefactoringProviderTests : CodeRefactoringTestBase
{
	// ─────────────────────────────────────────────────────────────────────────
	// No-op cases (refactoring should NOT trigger)
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithoutILogger_ReturnsNoActions(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	[Test]
	public async Task ComputeRefactorings_GivenClassWithILoggerButNoLogCalls_ReturnsNoActions(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;

				public WeatherService(ILogger<WeatherService> logger)
				{
					_logger = logger;
				}

				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Basic conversion cases
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithILoggerAndLogCalls_ReturnsAction(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;

				public WeatherService(ILogger<WeatherService> logger)
				{
					_logger = logger;
				}

				public void GetWeather(string city)
				{
					_logger.LogInformation("Getting weather for {City}", city);
				}
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, cancellationToken: cancellationToken);

		await Assert.That(actions).IsNotEmpty();
		await Assert.That(actions[0].Title).IsEqualTo("Convert ILogger usage to Purview Telemetry interface");
	}

	[Test]
	public async Task ApplyRefactoring_GivenSingleLogInformationCall_GeneratesInfoInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;

				public WeatherService(ILogger<WeatherService> logger)
				{
					_logger = logger;
				}

				public void GetWeather(string city)
				{
					_logger.LogInformation("Getting weather for {City}", city);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Logger]");
		await Assert.That(result).Contains("IWeatherServiceLogger");
		await Assert.That(result).Contains("[Info]");
		await Assert.That(result).Contains("void LogInformation(string city)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenMultipleLogLevels_GeneratesCorrectAttributes(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$OrderService
			{
				readonly ILogger<OrderService> _logger;

				public OrderService(ILogger<OrderService> logger) => _logger = logger;

				public void ProcessOrder(int orderId)
				{
					_logger.LogTrace("Trace {OrderId}", orderId);
					_logger.LogDebug("Debug {OrderId}", orderId);
					_logger.LogInformation("Info {OrderId}", orderId);
					_logger.LogWarning("Warn {OrderId}", orderId);
					_logger.LogError("Error {OrderId}", orderId);
					_logger.LogCritical("Critical {OrderId}", orderId);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Trace]");
		await Assert.That(result).Contains("[Debug]");
		await Assert.That(result).Contains("[Info]");
		await Assert.That(result).Contains("[Warning]");
		await Assert.That(result).Contains("[Error]");
		await Assert.That(result).Contains("[Critical]");
	}

	[Test]
	public async Task ApplyRefactoring_GivenLogCallWithException_IncludesExceptionParameter(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$PaymentService
			{
				readonly ILogger<PaymentService> _logger;

				public PaymentService(ILogger<PaymentService> logger) => _logger = logger;

				public void ProcessPayment(string paymentId)
				{
					try { }
					catch (Exception ex)
					{
						_logger.LogError(ex, "Payment failed for {PaymentId}", paymentId);
					}
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Error]");
		await Assert.That(result).Contains("System.Exception");
		await Assert.That(result).Contains("exception");
		await Assert.That(result).Contains("paymentId");
	}

	[Test]
	public async Task ApplyRefactoring_GivenDuplicateLogLevel_DeduplicatesMethodNames(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$UserService
			{
				readonly ILogger<UserService> _logger;

				public UserService(ILogger<UserService> logger) => _logger = logger;

				public void Login(string userId, string ip)
				{
					_logger.LogInformation("User {UserId} logged in", userId);
					_logger.LogInformation("User {UserId} logged in from {Ip}", userId, ip);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		// Both methods should appear, with second one being deduplicated
		await Assert.That(result).Contains("void LogInformation(");
		await Assert.That(result).Contains("void LogInformation2(");
	}

	[Test]
	public async Task ApplyRefactoring_GivenILoggerGeneric_ReplacesFieldTypeWithInterface(
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

				public void Generate(string reportName)
				{
					_logger.LogInformation("Generating {ReportName}", reportName);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		// ILogger<ReportService> should be replaced with IReportServiceLogger
		await Assert.That(result).Contains("IReportServiceLogger _logger");
		await Assert.That(result).Contains("IReportServiceLogger logger");
		// The old ILogger<ReportService> should no longer appear as a field/param type
		await Assert.That(result).DoesNotContain("ILogger<ReportService>");
	}

	[Test]
	public async Task ApplyRefactoring_GivenLogCallWithNoParams_GeneratesMethodWithNoParams(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$HealthCheckService
			{
				readonly ILogger<HealthCheckService> _logger;

				public HealthCheckService(ILogger<HealthCheckService> logger) => _logger = logger;

				public void Check()
				{
					_logger.LogInformation("Health check started");
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("void LogInformation()");
	}

	[Test]
	public async Task ApplyRefactoring_GivenLogMethodCallOnNonGenericILogger_DetectsAndConverts(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$SimpleService
			{
				readonly ILogger _logger;

				public SimpleService(ILogger logger) => _logger = logger;

				public void Run(string name)
				{
					_logger.LogWarning("Running {Name}", name);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Logger]");
		await Assert.That(result).Contains("ISimpleServiceLogger");
		await Assert.That(result).Contains("[Warning]");
		await Assert.That(result).Contains("void LogWarning(string name)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenLogCallReplacesTemplateArgs_RemovesMessageTemplate(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$DataService
			{
				readonly ILogger<DataService> _logger;

				public DataService(ILogger<DataService> logger) => _logger = logger;

				public void Save(string key, int value)
				{
					_logger.LogDebug("Saving {Key}={Value}", key, value);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		// The invocation should now be _logger.LogDebug(key, value) — no template string
		await Assert.That(result).Contains("_logger.LogDebug(key, value)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenGenericLogMethod_MapsToCorrectAttribute(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$AuditService
			{
				readonly ILogger<AuditService> _logger;

				public AuditService(ILogger<AuditService> logger) => _logger = logger;

				public void Audit(string action)
				{
					_logger.Log(LogLevel.Warning, "Audit: {Action}", action);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Warning]");
		await Assert.That(result).Contains("void LogWarning(string action)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenNamespace_IncludesNamespaceInInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace My.Company.Services;

			public class $$NotificationService
			{
				readonly ILogger<NotificationService> _logger;

				public NotificationService(ILogger<NotificationService> logger) => _logger = logger;

				public void Notify(string message)
				{
					_logger.LogInformation("Notification: {Message}", message);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Logger]");
		await Assert.That(result).Contains("INotificationServiceLogger");
	}

	[Test]
	public async Task ApplyRefactoring_GivenMultipleParams_GeneratesCorrectParamList(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$SearchService
			{
				readonly ILogger<SearchService> _logger;

				public SearchService(ILogger<SearchService> logger) => _logger = logger;

				public void Search(string query, int page, bool isCached)
				{
					_logger.LogInformation("Search {Query} page {Page} cached={IsCached}", query, page, isCached);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("query");
		await Assert.That(result).Contains("page");
		await Assert.That(result).Contains("isCached");
	}
}
