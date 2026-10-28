using System.Text;
using Microsoft.Extensions.Logging;

namespace PackedTelemetry;

/// <summary>
/// What one <see cref="OrderWorkflow.Place"/> call must emit through the packed generator's code. Shared by the
/// Native AOT console and the Blazor Server host, so both prove the same contract.
/// </summary>
public static class Expectations
{
	public const string OrdersName = "packed.orders";
	public const string InventoryName = "packed.inventory";

	public static readonly string[] Names = [OrdersName, InventoryName];

	/// <summary>Returns every mismatch between what was captured and what one order must emit; empty when it all matches.</summary>
	public static List<string> Verify(TelemetryCapture capture, string orderId, string customerId, double total)
	{
		List<string> failures = [];

		void Check(bool condition, string failure)
		{
			if (!condition)
				failures.Add(failure);
		}

		// Activities.
		var order = capture.Activities.Where(a => a.Source == OrdersName && a.Name == "order.place").ToArray();
		Check(order.Length == 1, $"expected one '{OrdersName}/order.place' activity, got {order.Length}");
		if (order.Length == 1)
		{
			var activity = order[0];
			Check(
				Get(activity.Tags, "order_id") == orderId,
				$"order.place tag order_id is '{Get(activity.Tags, "order_id")}', not '{orderId}'"
			);
			Check(
				Get(activity.Baggage, "customer_id") == customerId,
				$"order.place baggage customer_id is '{Get(activity.Baggage, "customer_id")}', not '{customerId}'"
			);

			var validated = activity.Events.Where(e => e.Name == "order.validated").ToArray();
			Check(validated.Length == 1, $"expected one order.validated event, got {validated.Length}");
			if (validated.Length == 1)
				Check(
					Get(validated[0].Tags, "line_count") == "3",
					$"order.validated tag line_count is '{Get(validated[0].Tags, "line_count")}', not '3'"
				);

			var exception = activity.Events.Where(e => e.Name == "exception").ToArray();
			Check(exception.Length == 1, $"expected one exception event, got {exception.Length}");
			if (exception.Length == 1)
			{
				Check(
					Get(exception[0].Tags, "exception.type") == "System.InvalidOperationException",
					$"exception.type is '{Get(exception[0].Tags, "exception.type")}'"
				);
				Check(
					Get(exception[0].Tags, "exception.message") == "card declined",
					$"exception.message is '{Get(exception[0].Tags, "exception.message")}'"
				);
				Check(
					Get(exception[0].Tags, "order_id") == orderId,
					$"exception event tag order_id is '{Get(exception[0].Tags, "order_id")}'"
				);
			}
		}

		var reserve = capture.Activities.Where(a => a.Source == InventoryName && a.Name == "stock.reserve").ToArray();
		Check(
			reserve.Length == 1,
			$"expected one '{InventoryName}/stock.reserve' activity (the library's), got {reserve.Length}"
		);
		if (reserve.Length == 1)
			Check(
				Get(reserve[0].Tags, "sku") == OrderWorkflow.Sku,
				$"stock.reserve tag sku is '{Get(reserve[0].Tags, "sku")}'"
			);

		// Metrics. Instrument names carry the prefix the generator derives from the interface name (IOrderTelemetry ->
		// "order", IInventoryTelemetry -> "inventory"); the meter name is only prepended with
		// MeterNameGenerationType.OpenTelemetry, which these interfaces do not set.
		var placed = Measurements(capture, OrdersName, "order.orders.placed");
		Check(
			placed.Length == 1 && placed[0].Value == 1,
			$"expected one orders.placed measurement of 1, got [{Values(placed)}]"
		);

		var totals = Measurements(capture, OrdersName, "order.order.total");
		Check(
			totals.Length == 1 && totals[0].Value == total,
			$"expected one order.total measurement of {total}, got [{Values(totals)}]"
		);
		if (totals.Length == 1)
		{
			Check(
				Get(totals[0].Tags, "currency") == "GBP",
				$"order.total tag currency is '{Get(totals[0].Tags, "currency")}'"
			);
			Check(totals[0].Unit == "GBP", $"order.total unit is '{totals[0].Unit}', not 'GBP'");
		}

		var inFlight = Measurements(capture, OrdersName, "order.orders.in_flight");
		Check(Values(inFlight) == "1, -1", $"expected orders.in_flight measurements [1, -1], got [{Values(inFlight)}]");

		var reserved = Measurements(capture, InventoryName, "inventory.stock.reserved");
		Check(
			reserved.Length == 1 && reserved[0].Value == 2,
			$"expected one stock.reserved measurement of 2 (the library's), got [{Values(reserved)}]"
		);
		if (reserved.Length == 1)
			Check(
				Get(reserved[0].Tags, "sku") == OrderWorkflow.Sku,
				$"stock.reserved tag sku is '{Get(reserved[0].Tags, "sku")}'"
			);

		// Logs. Only this assembly's category: a host logs plenty of its own.
		var logs = capture
			.Logs.Where(l => l.Category.StartsWith("PackedTelemetry.", StringComparison.Ordinal))
			.ToArray();

		var placing = logs.Where(l => l.EventName?.Contains("PlacingOrder", StringComparison.Ordinal) == true)
			.ToArray();
		Check(
			placing.Length == 1 && placing[0].Level == LogLevel.Information,
			$"expected one Information PlacingOrder log entry, got {Describe(placing)}"
		);
		if (placing.Length == 1)
		{
			Check(
				placing[0].Message.Contains(orderId, StringComparison.Ordinal),
				$"the PlacingOrder message '{placing[0].Message}' does not carry the order id"
			);
			Check(
				Get(placing[0].State, "OrderId") == orderId,
				$"the PlacingOrder state OrderId is '{Get(placing[0].State, "OrderId")}'"
			);
		}

		var validatedLogs = logs.Where(l => l.EventName?.Contains("OrderValidated", StringComparison.Ordinal) == true)
			.ToArray();
		Check(
			validatedLogs.Length == 0,
			$"the Debug OrderValidated entry was logged although Debug is disabled (log-level gating): {Describe(validatedLogs)}"
		);
		Check(capture.IsEnabledCalls > 0, "the generated logging never asked the logger whether a level was enabled");

		var shipped = logs.Where(l => l.EventName?.Contains("OrderShipped", StringComparison.Ordinal) == true)
			.ToArray();
		Check(
			shipped.Length == 1 && shipped[0].Level == LogLevel.Information,
			$"expected one Information OrderShipped (v2) log entry, got {Describe(shipped)}"
		);
		if (shipped.Length == 1)
		{
			var state = string.Join(", ", shipped[0].State.Select(p => $"{p.Key}={p.Value}"));
			Check(
				shipped[0].State.Values.Contains("dhl") && shipped[0].State.Values.Contains("ups"),
				$"the v2 OrderShipped state does not expand the carriers: {state}"
			);
		}

		var failed = logs.Where(l => l.EventName?.Contains("OrderFailed", StringComparison.Ordinal) == true).ToArray();
		Check(
			failed.Length == 1 && failed[0].Level == LogLevel.Error,
			$"expected one Error OrderFailed log entry, got {Describe(failed)}"
		);
		if (failed.Length == 1)
			Check(
				failed[0].Exception is InvalidOperationException,
				"the OrderFailed log entry does not carry the exception"
			);

		return failures;
	}

	/// <summary>Everything captured, for the console when an expectation fails.</summary>
	public static string Dump(TelemetryCapture capture)
	{
		StringBuilder builder = new();
		foreach (var activity in capture.Activities)
		{
			builder.AppendLine(
				$"activity {activity.Source}/{activity.Name} tags[{Pairs(activity.Tags)}] baggage[{Pairs(activity.Baggage)}]"
			);
			foreach (var e in activity.Events)
				builder.AppendLine($"  event {e.Name} [{Pairs(e.Tags)}]");
		}

		foreach (var m in capture.Measurements)
			builder.AppendLine($"metric {m.Meter}/{m.Instrument} ({m.Unit}) = {m.Value} [{Pairs(m.Tags)}]");

		foreach (
			var log in capture.Logs.Where(l => l.Category.StartsWith("PackedTelemetry.", StringComparison.Ordinal))
		)
			builder.AppendLine($"log {log.Level} {log.Category} {log.EventName}: {log.Message} [{Pairs(log.State)}]");

		builder.AppendLine($"IsEnabled calls: {capture.IsEnabledCalls}");
		return builder.ToString();
	}

	static CapturedMeasurement[] Measurements(TelemetryCapture capture, string meter, string instrument) =>
		[.. capture.Measurements.Where(m => m.Meter == meter && m.Instrument == instrument)];

	static string Values(CapturedMeasurement[] measurements) => string.Join(", ", measurements.Select(m => m.Value));

	static string Describe(CapturedLog[] logs) =>
		logs.Length == 0 ? "none" : string.Join("; ", logs.Select(l => $"{l.Level} {l.EventName}: {l.Message}"));

	static string Pairs(IReadOnlyDictionary<string, string?> pairs) =>
		string.Join(", ", pairs.Select(p => $"{p.Key}={p.Value}"));

	static string? Get(IReadOnlyDictionary<string, string?> values, string key) =>
		values.TryGetValue(key, out var value) ? value : null;
}
