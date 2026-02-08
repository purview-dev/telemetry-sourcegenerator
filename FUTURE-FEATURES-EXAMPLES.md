# Future Feature Examples

This document shows code examples of potential future features for the Purview Telemetry Source Generator.

## Example 1: Enhanced Diagnostic Messages with Fixes

### Current Diagnostic
```
TSG1001: Multiple generation targets not supported on method 'MyMethod'
```

### Enhanced Diagnostic (Future)
```csharp
// TSG1001: Multiple generation targets not supported
// The method 'MyMethod' has both [Activity] and [Log] attributes.
// Only one telemetry type can be used per method.
//
// Suggested fixes:
//   1. Remove one of the attributes
//   2. Split into separate methods
//   3. Use interface multi-targeting
//
// Learn more: https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/TSG1001

[Activity("ProcessData")]  // ← Remove this
[Log("Processing data")]   // ← Or remove this
void MyMethod(string data);

// Option 1: Choose one telemetry type
[Activity("ProcessData")]
void MyMethod(string data);

// Option 2: Split into separate methods
[Activity("ProcessData")]
void ProcessDataActivity(string data);

[Log("Processing data")]
void LogProcessing(string data);
```

## Example 2: Roslyn Analyzer with Code Fixes

### Real-Time Validation

```csharp
[ActivitySource("MyService")]
public interface IMyService
{
    // ⚠️ Warning TSG2001: Activity name should be PascalCase
    [Activity("process_user")]  // Squiggly underline
    void ProcessUser(string userId);
    
    // 💡 Code fix available: Change to "ProcessUser"
    
    // ✅ After fix:
    [Activity("ProcessUser")]
    void ProcessUser(string userId);
}
```

### Invalid Attribute Combination Detection

```csharp
[LoggerGen("MyService")]
public interface IMyLogger
{
    // ❌ Error TSG1001: Multiple log levels not allowed
    [Info]
    [Error]  // Squiggly underline
    void LogMessage(string message);
    
    // 💡 Code fix: Remove conflicting attribute
    
    // ✅ After fix:
    [Info]
    void LogMessage(string message);
}
```

## Example 3: Activity Auto-Correlation

### Current (Manual)
```csharp
[ActivitySource("OrderService")]
public interface IOrderService
{
    [Activity("ProcessOrder")]
    Task<Order> ProcessOrderAsync(string orderId);
    
    [Event("OrderValidated")]
    void OnOrderValidated(string orderId);
}

// Usage - Manual correlation required
public class OrderService : IOrderService
{
    public async Task<Order> ProcessOrderAsync(string orderId)
    {
        using var activity = _activitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", orderId);
        
        // Validate order
        activity?.AddEvent(new("OrderValidated"));
        
        return order;
    }
}
```

### Future (Auto-Correlation)
```csharp
[ActivitySource("OrderService")]
public interface IOrderService
{
    [Activity("ProcessOrder", AutoCorrelate = true)]
    Task<Order> ProcessOrderAsync(string orderId);
    
    [Event("OrderValidated", AutoLinkToParent = true)]
    void OnOrderValidated(string orderId);
}

// Generated code automatically:
// - Starts parent activity
// - Links events to parent
// - Propagates context
// - Adds correlation IDs
```

## Example 4: Structured Logging Templates

### Future Feature
```csharp
[LoggerGen("ApiService")]
public interface IApiLogger
{
    // Use pre-built template for HTTP requests
    [HttpRequestLog]
    void LogRequest(
        string method,
        string path,
        int statusCode,
        double elapsedMs);
    
    // Use pre-built template for exceptions
    [ExceptionLog]
    void LogException(
        Exception exception,
        string context);
    
    // Use pre-built template for security events
    [SecurityAuditLog]
    void LogAccessAttempt(
        string user,
        string resource,
        bool granted,
        string reason);
}

// Generated code produces structured logs like:
// {
//   "timestamp": "2026-02-07T19:14:41.444Z",
//   "level": "Information",
//   "category": "ApiService",
//   "event": "HttpRequest",
//   "method": "GET",
//   "path": "/api/users/123",
//   "statusCode": 200,
//   "elapsedMs": 45.2,
//   "correlationId": "abc-123"
// }
```

## Example 5: Custom Metrics with Aggregation

### Future Feature
```csharp
[Meter("PaymentService")]
public interface IPaymentMetrics
{
    // Histogram with custom buckets
    [Histogram(
        Unit = "milliseconds",
        Buckets = [10, 50, 100, 500, 1000, 5000],
        Description = "Payment processing time")]
    void RecordPaymentDuration(double milliseconds);
    
    // Counter with rate calculation
    [Counter(
        RateCalculation = RateType.PerSecond,
        Description = "Payment requests per second")]
    void IncrementPayments();
    
    // Auto-calculated percentiles
    [Histogram(
        Percentiles = [50, 90, 95, 99],
        Description = "Payment amount distribution")]
    void RecordPaymentAmount(decimal amount);
}

// Generated code automatically calculates and exposes:
// - payment_duration (histogram)
// - payment_duration_p50 (gauge)
// - payment_duration_p90 (gauge)
// - payment_duration_p95 (gauge)
// - payment_duration_p99 (gauge)
// - payments_total (counter)
// - payments_per_second (gauge)
```

## Example 6: Conditional Compilation

### Future Feature
```csharp
[TelemetryGeneration(
    Activities = TelemetryLevel.Verbose,
    Logging = TelemetryLevel.Standard,
    Metrics = TelemetryLevel.Minimal)]
#if PRODUCTION
[TelemetryGeneration(
    Activities = TelemetryLevel.Minimal,
    Logging = TelemetryLevel.Standard,
    Metrics = TelemetryLevel.Standard)]
#elif DEVELOPMENT
[TelemetryGeneration(
    Activities = TelemetryLevel.Verbose,
    Logging = TelemetryLevel.Verbose,
    Metrics = TelemetryLevel.Verbose)]
#endif
public interface IMyService
{
    [Activity("ProcessData")]
    [LogLevel.Verbose]  // Only in DEVELOPMENT
    void ProcessData(string data);
}

// In PRODUCTION build:
// - Minimal activities (start/stop only)
// - Standard logging
// - Standard metrics

// In DEVELOPMENT build:
// - Verbose activities (all events, tags)
// - Verbose logging (debug messages)
// - Verbose metrics (all dimensions)
```

## Example 7: OpenTelemetry Conventions

### Future Feature
```csharp
[ActivitySource("ShoppingCart")]
public interface IShoppingCartActivity
{
    // Automatically applies OTel semantic conventions
    [Activity("http.request", ApplyOTelConventions = true)]
    Task<Response> ProcessRequestAsync(
        [OTel("http.method")] string method,
        [OTel("http.url")] string url,
        [OTel("http.status_code")] int statusCode);
}

// Generated code automatically:
// - Uses standard attribute names (http.method, http.url)
// - Sets correct span kind (SpanKind.Server)
// - Applies status codes correctly
// - Follows OTel naming conventions
```

## Example 8: Telemetry Testing Helpers

### Future Feature
```csharp
// In your test file
[Test]
public async Task ProcessOrder_ShouldRecordTelemetry()
{
    // Arrange
    var telemetry = TelemetryRecorder.Create();
    var service = new OrderService(telemetry);
    
    // Act
    await service.ProcessOrderAsync("order-123");
    
    // Assert - Activities
    telemetry.AssertActivityStarted("ProcessOrder")
        .WithTag("order.id", "order-123")
        .WithEvent("OrderValidated")
        .WithDuration(d => d < TimeSpan.FromSeconds(5));
    
    // Assert - Logging
    telemetry.AssertLogWritten(LogLevel.Information)
        .WithMessage("Order processed successfully")
        .WithProperty("OrderId", "order-123");
    
    // Assert - Metrics
    telemetry.AssertMetricRecorded("orders_processed")
        .WithValue(1)
        .WithTag("status", "success");
    
    // Verify call sequence
    telemetry.AssertSequence(
        "Activity:ProcessOrder:Start",
        "Event:OrderValidated",
        "Log:OrderProcessed",
        "Metric:OrdersProcessed",
        "Activity:ProcessOrder:Stop");
}
```

## Example 9: Documentation Generation

### Generated Documentation (Future)

```markdown
# MyService Telemetry Reference

## Activities

### ProcessOrder
**Name**: `ProcessOrder`
**Description**: Processes a customer order
**Tags**:
- `order.id` (string): The order identifier
- `order.amount` (decimal): The order total amount
**Events**:
- `OrderValidated`: Emitted when order passes validation
- `OrderProcessed`: Emitted when order is successfully processed

**Usage**:
```csharp
await _orderService.ProcessOrderAsync("order-123");
```

## Metrics

### orders_processed_total
**Type**: Counter
**Description**: Total number of processed orders
**Tags**:
- `status` (string): Order processing status (success, failed)
**Unit**: orders

## Logs

### OrderProcessed
**Level**: Information
**Template**: "Order {OrderId} processed successfully"
**Parameters**:
- `OrderId` (string): The order identifier
```

## Implementation Notes

These examples show the direction for future features. Each would require:
1. Design specification
2. Community feedback
3. Proof of concept
4. Full implementation
5. Testing
6. Documentation

See `FEATURE-RECOMMENDATIONS.md` for priority ranking and effort estimates.
