# Multi-Targeting

Multi-targeting lets you generate **multiple types of telemetry** (Activities, Logs, and Metrics) from a single interface or even a single method. One method call can emit an Activity, a structured log entry, and a metric simultaneously.

## Interface-level multi-targeting

Apply multiple generation attributes to an interface to enable all telemetry types:

```csharp
using Purview.Telemetry;

[ActivitySource("OrderService")]
[Logger]
[Meter("OrderService")]
interface IOrderTelemetry
{
    // Methods can use one or more telemetry types
}
```

## Method-level multi-targeting

Multiple telemetry attributes can be combined on a single method. When called, the method emits all specified telemetry types:

```csharp
[ActivitySource("OrderService")]
[Logger]
[Meter]
interface IOrderTelemetry
{
    // MULTI-TARGET: Creates Activity + Logs Info + Increments Counter from one call
    [Activity]
    [Info]
    [AutoCounter]
    Activity? ProcessingOrder([Baggage]int orderId, [Tag]string customerName);

    // MULTI-TARGET: Adds ActivityEvent + Logs as Debug
    [Event]
    [Debug]
    void OrderValidated(Activity? activity, decimal amount);

    // SINGLE-TARGET: only logs
    [Warning]
    void OrderRejected(int orderId, string reason);

    // SINGLE-TARGET: only metric
    [Histogram]
    void OrderProcessingDuration([InstrumentMeasurement]int milliseconds);
}
```

```csharp
// Single method call emits 3 telemetry types
using var activity = telemetry.ProcessingOrder(123, "Acme Corp");
// ✓ Activity created and started
// ✓ Info log entry written
// ✓ Counter incremented by 1
```

### Supported combinations

| Combination | Supported | Example |
| --- | --- | --- |
| Activity + Log | ✅ | `[Activity]` + `[Info]` |
| Activity + Metric | ✅ | `[Activity]` + `[AutoCounter]` |
| Log + Metric | ✅ | `[Info]` + `[Histogram]` |
| Activity + Log + Metric | ✅ | `[Activity]` + `[Info]` + `[AutoCounter]` |
| Event + Log | ✅ | `[Event]` + `[Debug]` |
| Context + Log | ✅ | `[Context]` + `[Trace]` |

**Rules:**

- Only **one attribute per telemetry family** is allowed per method (`TSG1002`).
- Activities: use one of `[Activity]`, `[Event]`, or `[Context]`.
- Logging: use one of `[Log]`, `[Trace]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]`, `[Critical]`.
- Metrics: use one of `[Counter]`, `[AutoCounter]`, `[Histogram]`, `[UpDownCounter]`, or the observable variants.

```csharp
// ERROR TSG1002: multiple activity attributes
[Activity]
[Event]
void InvalidMethod(Activity? activity);

// ERROR TSG1002: multiple logging attributes
[Info]
[Warning]
void InvalidMethod(string message);

// ERROR TSG1002: multiple metric attributes
[Counter]
[Histogram]
void InvalidMethod([InstrumentMeasurement]int value);
```

## Excluding parameters from specific targets

Use `[ExcludeTargets(Targets.X)]` on a parameter to exclude it from specific telemetry families:

```csharp
[ActivitySource("PaymentService")]
[Logger]
[Meter]
interface IPaymentTelemetry
{
    [Activity]
    [Info]
    [Counter]
    Activity? ProcessingPayment(
        [Baggage]Guid paymentId,

        // Exclude the verbose message from metrics (would be wasted as a tag)
        [ExcludeTargets(Targets.Metrics)]
        string processingMessage,

        // Measurement only applies to metrics
        [InstrumentMeasurement]
        decimal amount,

        // Exclude internal details from Activity baggage
        [ExcludeTargets(Targets.Activities)]
        [Tag] // still included in logs and metrics
        string internalReference
    );
}
```

### The `Targets` enum

```csharp
[Flags]
public enum Targets
{
    None = 0,
    Activities = 1,
    Logging = 2,
    Metrics = 4,
    All = Activities | Logging | Metrics
}
```

```csharp
// Exclude from multiple targets
[ExcludeTargets(Targets.Activities | Targets.Metrics)]
string loggingOnlyParameter;

// Exclude from a single target
[ExcludeTargets(Targets.Logging)]
int metricsAndActivityParameter;
```

## Inference is disabled with multi-targeting

When an interface has **multiple** class-level attributes (`[ActivitySource]`, `[Logger]`, `[Meter]`), inference is disabled and every method must declare its targets explicitly:

```csharp
[ActivitySource("MyApp")]
[Logger]
[Meter]
interface IMyTelemetry
{
    // ERROR TSG1001: no explicit attribute
    void ProcessItem(int id);

    // ✅ CORRECT: explicit attribute
    [Info]
    void ProcessItem(int id);

    // ✅ CORRECT: excluded from generation
    [Exclude]
    void ProcessItem(int id);
}
```

Single-target interfaces (only one class-level attribute) keep inference — see [Activities](Activities.md#inferring-method-type) and [Logging](Logging.md).

## Return types

- **Activity + other targets** — return `Activity?` so callers can dispose and reuse the Activity:

  ```csharp
  [Activity]
  [Info]
  [AutoCounter]
  Activity? ProcessingOrder(int orderId);
  ```

- **Log + Metric (no Activity)** — return `void` or `IDisposable?`:

  ```csharp
  [Info]
  [Histogram]
  void RecordOperation([InstrumentMeasurement]int duration, string operation);

  [Info]
  [AutoCounter]
  IDisposable? ProcessingBatch(int batchId); // scoped log + counter
  ```

- **Event/Context + Log** — return `void`:

  ```csharp
  [Event]
  [Debug]
  void OrderCompleted(Activity? activity, decimal total);
  ```

## Common patterns

### Complete observability method

```csharp
[Activity]      // distributed tracing
[Info]          // structured logging
[AutoCounter]   // count occurrences
Activity? ProcessingRequest(
    [Baggage]string requestId,
    [Tag]string endpoint,
    [Tag]string method
);
```

### Event with context logging

```csharp
[Event]
[Debug]
void StepCompleted(
    Activity? activity,
    [Tag]string stepName,
    [Tag]int duration
);
```

### Selective parameter usage

```csharp
[Activity]
[Info]
[Counter]
Activity? ApiCall(
    [Baggage]string traceId,                          // Activity baggage only
    [ExcludeTargets(Targets.Metrics)]
    string verboseMessage,                            // Activity + Log only
    [InstrumentMeasurement]
    [ExcludeTargets(Targets.Activities | Targets.Logging)]
    int callCount,                                    // Metrics only
    [Tag]string endpoint                              // All three
);
```

## Benefits

1. **Less code** — one method definition generates multiple telemetry types
2. **Consistency** — the same parameters feed every telemetry type
3. **Atomicity** — all telemetry emitted together
4. **Maintainability** — change once, affects all telemetry types
5. **Performance** — a single method call instead of several

## Diagnostics

| Diagnostic | When | Resolution |
| --- | --- | --- |
| `TSG1001` | Method has no explicit attribute on a multi-target interface | Add `[Activity]`, `[Info]`, `[Counter]`, etc., or `[Exclude]` |
| `TSG1002` | Multiple attributes from the same family on one method | Use only one Activity, Logging, or Metrics attribute per method |
| `TSG1006` | `[ExcludeTargets]` references a target not present on the method | Remove `[ExcludeTargets]` or add the target attribute to the method |
| `TSG1007` | `[ExcludeTargets]` leaves an invalid parameter set for a target | Adjust exclusions so valid parameters remain for each target |

## See also

- [Activities](Activities.md) — Activity generation details
- [Logging](Logging.md) — logging generation details
- [Metrics](Metrics.md) — metrics generation details
- [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md) — `[Tag]`, `[Baggage]`, `[ExcludeTargets]`
- [Diagnostics](Diagnostics.md) — error codes and resolutions