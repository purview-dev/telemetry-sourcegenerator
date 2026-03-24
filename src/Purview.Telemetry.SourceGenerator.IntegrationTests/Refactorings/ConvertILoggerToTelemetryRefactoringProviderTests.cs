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

	// ─────────────────────────────────────────────────────────────────────────
	// EventId handling
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenLogCallWithEventId_SkipsEventIdArgument(
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
					_logger.LogInformation(new EventId(1, "OrderProcessed"), "Processing order {OrderId}", orderId);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Info]");
		await Assert.That(result).Contains("void LogInformation(int orderId)");
		await Assert.That(result).DoesNotContain("EventId");
	}

	[Test]
	public async Task ApplyRefactoring_GivenLogCallWithEventIdAndException_SkipsEventIdKeepsException(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$ShippingService
			{
				readonly ILogger<ShippingService> _logger;

				public ShippingService(ILogger<ShippingService> logger) => _logger = logger;

				public void Ship(string trackingId)
				{
					try { }
					catch (Exception ex)
					{
						_logger.LogError(new EventId(2), ex, "Shipping failed for {TrackingId}", trackingId);
					}
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Error]");
		await Assert.That(result).Contains("exception");
		await Assert.That(result).Contains("trackingId");
		await Assert.That(result).DoesNotContain("EventId");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Log(LogLevel, …) level mapping
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenLogLevelInformation_MapsToInfoAttribute(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$CacheService
			{
				readonly ILogger<CacheService> _logger;

				public CacheService(ILogger<CacheService> logger) => _logger = logger;

				public void Refresh(string key)
				{
					_logger.Log(LogLevel.Information, "Refreshing cache for {Key}", key);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Info]");
		await Assert.That(result).Contains("void LogInformation(string key)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenLogLevelNone_EmitsFallbackLogAttribute(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$DiagService
			{
				readonly ILogger<DiagService> _logger;

				public DiagService(ILogger<DiagService> logger) => _logger = logger;

				public void Diag(string info)
				{
					_logger.Log(LogLevel.None, "Diag: {Info}", info);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Log(");
		await Assert.That(result).Contains("LogLevel.None");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Template placeholder edge cases
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenTemplateWithDestructuringPrefix_ExtractsParameterName(
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

				public void Login(object user)
				{
					_logger.LogInformation("User logged in: {@User}", user);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("void LogInformation(object user)");
		await Assert.That(result).DoesNotContain("@User");
	}

	[Test]
	public async Task ApplyRefactoring_GivenTemplateWithStringifyPrefix_ExtractsParameterName(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$ProductService
			{
				readonly ILogger<ProductService> _logger;

				public ProductService(ILogger<ProductService> logger) => _logger = logger;

				public void Created(object product)
				{
					_logger.LogDebug("Product created: {$Product}", product);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("void LogDebug(object product)");
		await Assert.That(result).DoesNotContain("$Product");
	}

	[Test]
	public async Task ApplyRefactoring_GivenTemplateWithFormatSpecifier_ExtractsParameterName(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$MetricsService
			{
				readonly ILogger<MetricsService> _logger;

				public MetricsService(ILogger<MetricsService> logger) => _logger = logger;

				public void Record(double value)
				{
					_logger.LogInformation("Metric value: {Value:0.00}", value);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("void LogInformation(double value)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenTemplateWithAlignmentSpecifier_ExtractsParameterName(
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

				public void Report(int count)
				{
					_logger.LogWarning("Item count: {Count,-10}", count);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("void LogWarning(int count)");
	}

	[Test]
	public async Task ApplyRefactoring_GivenExtraArgsWithNoPlaceholders_GeneratesArgParameters(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$DebugService
			{
				readonly ILogger<DebugService> _logger;

				public DebugService(ILogger<DebugService> logger) => _logger = logger;

				public void Dump(string label, int a, int b)
				{
					_logger.LogDebug("Values: {Label}", label, a, b);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("label");
		await Assert.That(result).Contains("arg1");
		await Assert.That(result).Contains("arg2");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Namespace / class structure
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenClassWithNoNamespace_GeneratesInterfaceWithoutNamespace(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			public class $$GlobalService
			{
				readonly ILogger<GlobalService> _logger;

				public GlobalService(ILogger<GlobalService> logger) => _logger = logger;

				public void Run()
				{
					_logger.LogInformation("Running");
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Logger]");
		await Assert.That(result).Contains("IGlobalServiceLogger");
		await Assert.That(result).DoesNotContain("namespace");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Multiple logger fields
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithMultipleILoggerFields_ReturnsAction(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$CompositeService
			{
				readonly ILogger<CompositeService> _logger;
				readonly ILogger _genericLogger;

				public CompositeService(
					ILogger<CompositeService> logger,
					ILogger genericLogger)
				{
					_logger = logger;
					_genericLogger = genericLogger;
				}

				public void Work(string name)
				{
					_logger.LogInformation("Working on {Name}", name);
					_genericLogger.LogDebug("Debug: {Name}", name);
				}
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, cancellationToken: cancellationToken);

		await Assert.That(actions).IsNotEmpty();
	}

	[Test]
	public async Task ApplyRefactoring_GivenConstructorWithMatchingLoggerParam_ReplacesOnlyMatchingParam(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$FilterService
			{
				readonly ILogger<FilterService> _logger;

				public FilterService(ILogger<FilterService> logger, string config) => _logger = logger;

				public void Filter(string input)
				{
					_logger.LogInformation("Filtering {Input}", input);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("IFilterServiceLogger logger");
		// non-logger param must be untouched
		await Assert.That(result).Contains("string config");
	}
}
