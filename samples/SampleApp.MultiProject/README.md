# Multi-Project Sample (.NET 10)

A console solution that shows how telemetry names travel across a **multi-project** solution: each
library records the names it generates in its own assembly metadata, and the one project that
registers telemetry aggregates all of them — including names it only reaches transitively.

This is the pattern to use when a large solution has many projects with different telemetry
requirements, and a single `ServiceDefaults`-style project needs the complete set of names for
`AddSource(...)` / `AddMeter(...)`.

## Reference graph

```text
SampleApp.MultiProject.ConsoleApp          # the host: aggregates and registers
└── SampleApp.MultiProject.Ordering        # tracing + logging + metrics (default names)
    └── SampleApp.MultiProject.Payments    # tracing + metrics (explicit names)
        └── SampleApp.MultiProject.Notifications   # tracing only (explicit name)

SampleApp.MultiProject.Observability       # referenced BY all of the above
```

The host references **only** `Ordering` directly. `Payments` and `Notifications` are transitive, and
their names are aggregated all the same.

`Observability` is the `ServiceDefaults` analogue: because everything references *it*, its own
reference graph contains none of them, so it can never discover their names. It takes the two arrays
as parameters instead.

## How the names get there

Every project with an `[ActivitySource]` or `[Meter]` gets this emitted automatically, with no
configuration at all:

```csharp
// SampleApp.MultiProject.Notifications.GeneratedTelemetryNames.g.cs
[assembly: global::Purview.Telemetry.GeneratedTelemetryNamesAttribute(new string[] { "multiproject-notifications" }, new string[0])]
```

Only the console app opts into the aggregated class — see
[`Properties/AssemblyInfo.cs`](SampleApp.MultiProject.ConsoleApp/Properties/AssemblyInfo.cs):

```csharp
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
```

which produces, in `SampleApp.MultiProject.ConsoleApp.TelemetryNames.g.cs`:

```csharp
public static readonly string[] MeterNames = new string[] { "SampleApp.MultiProject.Ordering", "multiproject.payments" };

public static readonly string[] ActivitySourceNames = new string[]
{
	"SampleApp.MultiProject.Ordering",
	"multiproject-host",
	"multiproject-notifications",
	"multiproject-payments",
};
```

Four assemblies' activity sources and two meters, deduplicated and ordered, resolved entirely at
compile time — no reflection and no assembly scanning, so it is safe under trimming and native AOT.

## Running

```bash
just run-mp
# or: dotnet run --project samples/SampleApp.MultiProject/SampleApp.MultiProject.ConsoleApp
```

```text
=== Purview Telemetry — multi-project aggregation ===

Reference graph: Host -> Ordering -> Payments -> Notifications (all four reference Observability).
Only Ordering is referenced directly by the host.

TelemetryNames.ActivitySourceNames (4):
  - SampleApp.MultiProject.Ordering
  - multiproject-host
  - multiproject-notifications
  - multiproject-payments

TelemetryNames.MeterNames (2):
  - SampleApp.MultiProject.Ordering
  - multiproject.payments

Placing 3 orders:

    [trace  ] start multiproject-host/RunningBatch
  ord-1001 (£49.99)
    [trace  ] start SampleApp.MultiProject.Ordering/PlacingOrder
    [trace  ] start multiproject-payments/ChargingCard
    [trace  ] start multiproject-notifications/SendingReceipt
    [trace  ] stop  multiproject-notifications/SendingReceipt (0.0 ms)
    [metric ] multiproject.payments/payment.payment_duration = 0.5767
    [metric ] multiproject.payments/payment.payments_processed = 1
    [trace  ] stop  multiproject-payments/ChargingCard (1.8 ms)
    [metric ] SampleApp.MultiProject.Ordering/ordering.orders_placed = 1
    [trace  ] stop  SampleApp.MultiProject.Ordering/PlacingOrder (9.2 ms)
...
```

Every line comes from a subscription that `Observability` created from the aggregated arrays alone.

## What each project demonstrates

| Project | Telemetry | Points of interest |
| --- | --- | --- |
| `Observability` | none | The `ServiceDefaults` analogue: receives names, owns no telemetry. |
| `Notifications` | tracing | Explicit source name; `EXCLUDE_PURVIEW_TELEMETRY_LOGGING` keeps it free of `Microsoft.Extensions.Logging.Abstractions`. Transitive from the host. |
| `Payments` | tracing + metrics | Explicit source and meter names; counter and histogram; takes `IMeterFactory`. Transitive from the host. |
| `Ordering` | tracing + logging + metrics | One multi-target interface using the default (assembly-derived) names. |
| `ConsoleApp` | tracing | Owns telemetry *and* aggregates; the only project with `GenerateTelemetryNamesClass = true`. |

Each library also owns its DI registration (`AddNotifications()`, `AddPayments()`, `AddOrdering()`),
chaining to its dependencies and to the generated `Add*Telemetry()` extension — so the host makes a
single `services.AddOrdering()` call for four assemblies' worth of telemetry.

## Adapting it

- **Defaults:** `GenerateTelemetryNamesAttribute` is `true` (every assembly publishes its names) and
  `GenerateTelemetryNamesClass` is `false` (only the project that registers them generates the
  class). Set the former to `false` to keep an assembly's names out of its metadata.
- **Opting out of aggregation:** set `AggregateReferencedTelemetryNames = false` for this assembly's
  own names only.
- **Several hosts:** repeat the `[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]`
  in each executable. Libraries never need it.
- **Real OpenTelemetry:** replace `TelemetryDefaults.Listen(...)` with the standard builder calls,
  passing the same two arrays:

  ```csharp
  builder.Services.AddOpenTelemetry()
      .WithTracing(tracing => tracing.AddSource(TelemetryNames.ActivitySourceNames))
      .WithMetrics(metrics => metrics.AddMeter(TelemetryNames.MeterNames));
  ```

## See also

- [Generation options](../../docs/wiki/Generation.md#telemetry-names) — the attribute and the class
- [`SampleApp`](../SampleApp/README.md) — the full .NET Aspire sample
- [`SampleApp.Net48`](../SampleApp.Net48/README.md) — the same generator on .NET Framework 4.8
