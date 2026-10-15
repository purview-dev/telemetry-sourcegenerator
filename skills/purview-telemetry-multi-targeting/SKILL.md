---
name: purview-telemetry-multi-targeting
description: >-
  Use when one Purview.Telemetry.SourceGenerator method should emit several kinds of telemetry at
  once - an Activity, a log entry and a metric from a single call. Covers combining [ActivitySource],
  [Logger] and [Meter] on an interface, the supported per-method attribute combinations, excluding a
  parameter from one target with [ExcludeTargets], return types, and why inference turns off.
license: MIT
compatibility: C# projects using Purview.Telemetry.SourceGenerator.
metadata:
  author: Purview-Dev
  product: Purview.Telemetry.SourceGenerator
  repository: purview-dev/telemetry-sourcegenerator
---

# Multi-targeting

One interface — and one *method* — can generate Activities, logs and metrics together. A single call
then emits all of them, from one set of parameters.

```csharp
using Purview.Telemetry;

[ActivitySource("OrderService")]
[Logger]
[Meter]
interface IOrderTelemetry
{
    // Activity + Info log + counter increment, from one call.
    [Activity]
    [Info]
    [AutoCounter]
    Activity? ProcessingOrder([Baggage] int orderId, [Tag] string customerName);

    // ActivityEvent + Debug log.
    [Event]
    [Debug]
    void OrderValidated(Activity? activity, decimal amount);

    // Log only.
    [Warning]
    void OrderRejected(int orderId, string reason);

    // Metric only.
    [Histogram]
    void OrderProcessingDuration([InstrumentMeasurement] int milliseconds);
}
```

```csharp
using var activity = telemetry.ProcessingOrder(123, "Acme Corp");
// Activity created and started, Info log written, counter incremented by 1.
```

## What you may combine

| Combination | Example |
| --- | --- |
| Activity + Log | `[Activity]` + `[Info]` |
| Activity + Metric | `[Activity]` + `[AutoCounter]` |
| Log + Metric | `[Info]` + `[Histogram]` |
| Activity + Log + Metric | `[Activity]` + `[Info]` + `[AutoCounter]` |
| Event + Log | `[Event]` + `[Debug]` |
| Context + Log | `[Context]` + `[Trace]` |

**One attribute per family, per method** — more is `TSG1002`:

- Activities: one of `[Activity]`, `[Event]`, `[Context]`.
- Logging: one of `[Log]`, `[Trace]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]`, `[Critical]`.
- Metrics: one of `[Counter]`, `[AutoCounter]`, `[Histogram]`, `[UpDownCounter]`, or an observable
  variant.

```csharp
[Activity]
[Event]                                          // TSG1002
void InvalidMethod(Activity? activity);

[Counter]
[Histogram]                                      // TSG1002
void InvalidMethod([InstrumentMeasurement] int value);
```

## Inference turns off — and that is the point

An interface with **more than one** class-level attribute (`[ActivitySource]`, `[Logger]`, `[Meter]`)
disables inference. Every method must then declare its targets explicitly, or be `[Exclude]`d:

```csharp
[ActivitySource("MyApp")]
[Logger]
[Meter]
interface IMyTelemetry
{
    void ProcessItem(int id);           // TSG1001 - no explicit attribute

    [Info]
    void ProcessItemLogged(int id);     // fine

    [Exclude]
    void NotTelemetry(int id);          // fine
}
```

A single-target interface keeps inference. The alternative — guessing which of three families an
unannotated method meant — would silently emit the wrong telemetry, so the generator refuses.

## Excluding a parameter from one target

`[ExcludeTargets(Targets.X)]` on a parameter keeps it out of specific families. Use it when a value
is useful in one place and harmful in another — a verbose message is wasted as a metric tag, and an
internal reference may not belong in Activity baggage:

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
        [Baggage] Guid paymentId,

        [ExcludeTargets(Targets.Metrics)]
        string processingMessage,

        [InstrumentMeasurement]
        decimal amount,

        [ExcludeTargets(Targets.Activities)]
        [Tag]                            // still in logs and metrics
        string internalReference
    );
}
```

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

Flags combine, so `[ExcludeTargets(Targets.Activities | Targets.Metrics)]` leaves a parameter to
logging alone.

## Return types

| Shape | Return |
| --- | --- |
| Activity + anything else | `Activity?` — so the caller can dispose it and pass it to Event/Context methods |
| Log + Metric, no Activity | `void`, or `IDisposable?` for a scoped log |
| Event/Context + Log | `void` |

```csharp
[Activity]
[Info]
[AutoCounter]
Activity? ProcessingOrder(int orderId);

[Info]
[AutoCounter]
IDisposable? ProcessingBatch(int batchId);   // scoped log + counter

[Event]
[Debug]
void OrderCompleted(Activity? activity, decimal total);
```

Return `Activity?`, not `Activity`: it is null when no listeners are active (`TSG3022`).

## Patterns worth copying

Full observability on one call:

```csharp
[Activity]
[Info]
[AutoCounter]
Activity? ProcessingRequest(
    [Baggage] string requestId,
    [Tag] string endpoint,
    [Tag] string method
);
```

Selective parameter routing:

```csharp
[Activity]
[Info]
[Counter]
Activity? ApiCall(
    [Baggage] string traceId,                              // Activity baggage only
    [ExcludeTargets(Targets.Metrics)] string verboseMessage, // Activity + log
    [InstrumentMeasurement]
    [ExcludeTargets(Targets.Activities | Targets.Logging)]
    int callCount,                                          // metrics only
    [Tag] string endpoint                                   // all three
);
```

## Why bother

One method definition instead of three call sites; the same parameters feed every family, so they
cannot drift; everything is emitted together; a change lands once; and it is one call rather than
several.

## Diagnostics you will meet here

| Diagnostic | Fix |
| --- | --- |
| `TSG1001` | Add an explicit target attribute, or `[Exclude]`. |
| `TSG1002` | One Activity, Logging and Metrics attribute per method. |
| `TSG1006` | `[ExcludeTargets]` names a target the method does not have — remove it, or add the target attribute. |
| `TSG1007` | Exclusions left a target with no valid parameters — adjust them. |

The `purview-telemetry-diagnostics` skill carries the full `TSG` catalogue.
