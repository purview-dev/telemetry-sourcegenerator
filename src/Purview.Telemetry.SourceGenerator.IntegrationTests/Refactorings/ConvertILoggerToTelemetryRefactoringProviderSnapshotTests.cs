namespace Purview.Telemetry.SourceGenerator.Refactorings;

/// <summary>
/// Snapshot tests for <see cref="ConvertILoggerToTelemetryRefactoringProvider"/>.
/// Each test defines a <em>before</em> scenario and the snapshot captures the <em>after</em> output.
/// To regenerate snapshots: run <c>dotnet test</c>; <c>*.received.txt</c> files are auto-accepted.
/// </summary>
public sealed class ConvertILoggerToTelemetryRefactoringProviderSnapshotTests
	: CodeRefactoringTestBase
{
	static readonly ConvertILoggerToTelemetryRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// Basic log-level scenarios
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_SingleLogInformation_NoParams(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;

				public WeatherService(ILogger<WeatherService> logger) => _logger = logger;

				public void Run()
				{
					_logger.LogInformation("Service started");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_SingleLogInformation_WithParams(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;

				public WeatherService(ILogger<WeatherService> logger) => _logger = logger;

				public void GetWeather(string city, int days)
				{
					_logger.LogInformation("Getting weather for {City} over {Days} days", city, days);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_AllSixLogLevels(CancellationToken cancellationToken)
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

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_LogError_WithException(CancellationToken cancellationToken)
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

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_LogError_WithException_NoParams(CancellationToken cancellationToken)
	{
		const string code = """
			using System;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$StorageService
			{
				readonly ILogger<StorageService> _logger;

				public StorageService(ILogger<StorageService> logger) => _logger = logger;

				public void Save()
				{
					try { }
					catch (Exception ex)
					{
						_logger.LogError(ex, "Storage operation failed");
					}
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// ILogger field variations
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_GenericILogger_T(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$ReportService
			{
				readonly ILogger<ReportService> _logger;

				public ReportService(ILogger<ReportService> logger)
				{
					_logger = logger;
				}

				public void GenerateReport(string reportId)
				{
					_logger.LogInformation("Generating report {ReportId}", reportId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_NonGenericILogger(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$AuditService
			{
				readonly ILogger _logger;

				public AuditService(ILogger logger) => _logger = logger;

				public void LogAudit(string action)
				{
					_logger.LogInformation("Audit: {Action}", action);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_PropertyInjection(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$NotificationService
			{
				public ILogger<NotificationService> Logger { get; set; } = null!;

				public void Notify(string userId, string message)
				{
					Logger.LogInformation("Notifying {UserId}: {Message}", userId, message);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Constructor styles
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_PrimaryConstructorInjection(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$InventoryService(ILogger<InventoryService> logger)
			{
				public void UpdateStock(string sku, int delta)
				{
					logger.LogInformation("Stock update: {Sku} by {Delta}", sku, delta);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_ExpressionBodyConstructor(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$CacheService
			{
				readonly ILogger<CacheService> _log;

				public CacheService(ILogger<CacheService> log) => _log = log;

				public void Evict(string key)
				{
					_log.LogDebug("Cache evict: {Key}", key);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Method name disambiguation
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_DuplicateMessageTemplate_Disambiguates(
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

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_MultipleCallSites_SameMessage(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$ShippingService
			{
				readonly ILogger<ShippingService> _logger;

				public ShippingService(ILogger<ShippingService> logger) => _logger = logger;

				public void Ship(string orderId)
				{
					_logger.LogInformation("Shipping order {OrderId}", orderId);
				}

				public void Reship(string orderId)
				{
					_logger.LogInformation("Shipping order {OrderId}", orderId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Edge cases
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_LogWithExplicitLogLevel(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$DiagnosticsService
			{
				readonly ILogger<DiagnosticsService> _logger;

				public DiagnosticsService(ILogger<DiagnosticsService> logger) => _logger = logger;

				public void Diagnose(string component, LogLevel level)
				{
					_logger.Log(level, "Component {Component} checked", component);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_MixedLogLevels_MultipleParams(CancellationToken cancellationToken)
	{
		const string code = """
			using System;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$TransactionService
			{
				readonly ILogger<TransactionService> _logger;

				public TransactionService(ILogger<TransactionService> logger) => _logger = logger;

				public void Execute(string txId, decimal amount, string currency)
				{
					_logger.LogDebug("Starting tx {TxId} for {Amount} {Currency}", txId, amount, currency);
					_logger.LogInformation("Tx {TxId} processing {Amount}", txId, amount);
					_logger.LogWarning("Tx {TxId} amount {Amount} exceeds threshold", txId, amount);
				}

				public void Fail(string txId, Exception ex)
				{
					_logger.LogError(ex, "Tx {TxId} failed", txId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Document scope
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_DocumentScope_TwoClassesInSameFile(CancellationToken cancellationToken)
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
					_logger.LogInformation("Fetching weather for {City}", city);
				}
			}

			public class OrderService
			{
				readonly ILogger<OrderService> _logger;

				public OrderService(ILogger<OrderService> logger) => _logger = logger;

				public void PlaceOrder(int orderId)
				{
					_logger.LogInformation("Order {OrderId} placed", orderId);
				}
			}
			""";

		await VerifyRefactoringAsync(
			code,
			Provider,
			"Purview.Telemetry.ConvertILoggerToTelemetry.Document",
			cancellationToken
		);
	}
}
