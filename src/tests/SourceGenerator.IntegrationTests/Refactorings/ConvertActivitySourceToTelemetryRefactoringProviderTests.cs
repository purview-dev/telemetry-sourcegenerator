namespace Purview.Telemetry.SourceGenerator.Refactorings;

public sealed class ConvertActivitySourceToTelemetryRefactoringProviderTests : CodeRefactoringTestBase
{
	static readonly ConvertActivitySourceToTelemetryRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// No-op cases
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithoutActivitySource_ReturnsNoActions(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$OrderService
			{
				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	[Test]
	public async Task ComputeRefactorings_GivenActivitySourceFieldButNoStartActivityCalls_ReturnsNoActions(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$OrderService
			{
				readonly ActivitySource _activitySource = new("Orders");

				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Action detection
	// ─────────────────────────────────────────────────────────────────────────

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
		await Assert.That(actions[0].Title).IsEqualTo("Convert ActivitySource to IWeatherServiceTracing");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Interface generation
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenSingleStartActivityCall_GeneratesTracingInterface(
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

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[ActivitySource]");
		await Assert.That(result).Contains("IWeatherServiceTracing");
		await Assert.That(result).Contains("GetWeather");
		await Assert.That(result).Contains("using Purview.Telemetry;");
		await Assert.That(result).Contains("using System.Diagnostics;");
	}

	[Test]
	public async Task ApplyRefactoring_GivenStartActivityWithKind_GeneratesActivityAttribute(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$PaymentService
			{
				readonly ActivitySource _activitySource = new("Payments");

				public void ProcessPayment(string paymentId)
				{
					using var activity = _activitySource.StartActivity("process-payment", ActivityKind.Client);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[ActivitySource]");
		await Assert.That(result).Contains("IPaymentServiceTracing");
		await Assert.That(result).Contains("ProcessPayment");
	}

	[Test]
	public async Task ApplyRefactoring_GivenActivitySourceField_ReplacesFieldTypeWithInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$OrderService
			{
				readonly ActivitySource _activitySource = new("Orders");

				public void PlaceOrder(int orderId)
				{
					using var activity = _activitySource.StartActivity("place-order");
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("IOrderServiceTracing _activitySource");
		await Assert.That(result).DoesNotContain("ActivitySource _activitySource");
	}

	[Test]
	public async Task ApplyRefactoring_GivenStartActivityCall_ReplacesCallWithInterfaceMethod(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$ShippingService
			{
				readonly ActivitySource _activitySource = new("Shipping");

				public void ShipOrder(string trackingNumber)
				{
					using var activity = _activitySource.StartActivity("ship-order");
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("_activitySource.ShipOrder(");
		await Assert.That(result).DoesNotContain(".StartActivity(");
	}
}
