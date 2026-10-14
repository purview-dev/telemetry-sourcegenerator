---
name: telemetry-sourcegenerator-design
description: Design best practices for Purview.Telemetry.SourceGenerator interfaces. Use when deciding between a single telemetry interface and per-area interfaces, choosing telemetry names, designing multi-target methods, or reviewing telemetry structure in a C# project.
license: ISC
compatibility: C# projects using Purview.Telemetry.SourceGenerator.
metadata:
  author: Purview-Dev
  version: "1.0"
---

# Design best practices for Purview Telemetry

This skill guides the design of generated telemetry interfaces.

## Single combined interface vs. per-area interfaces

The generator lets you put all telemetry types on one interface, or split them by concern.

### Combined interface

Use one interface when:

- A single service owns all telemetry and the operations naturally overlap.
- You want a single injection point and fewer constructor parameters.
- Multi-target methods emit Activity + Log + Metric from one call.

Naming convention:

```csharp
[ActivitySource]
[Logger]
[Meter]
public interface IOrderServiceTelemetry
```

### Per-area interfaces

Split into separate interfaces when:

- Different consumers need only one telemetry type (e.g., tests mock only logs).
- The team prefers clear separation between tracing, logging, and metrics.
- The service is large and each telemetry type has many operations.

Naming convention:

```csharp
[ActivitySource] public interface IOrderServiceTracing { ... }
[Logger]         public interface IOrderServiceLogs     { ... }
[Meter]          public interface IOrderServiceMetrics { ... }
```

Default recommendation: start with one combined interface per service. Split only when a clear boundary emerges.

## Naming conventions

Default to the OpenTelemetry naming convention (`NamingConvention.OpenTelemetry`).

- ActivitySource names: preserve assembly casing (e.g., `MyApp`).
- Activity names: descriptive, sentence-case-ish, no spaces: `placing-order`.
- Log message templates: human-readable, with `{PascalCase}` placeholders.
- Metric instrument names: hierarchical, dot-separated, lowercase: `myapp.orders.placed`.
- Tag/baggage keys: `snake_case`.

Only use `NamingConvention.Legacy` for exact v3 backward compatibility.

## Multi-target method design

Combine attributes on one method when a single operation emits multiple telemetry types:

```csharp
[Activity]
[Info]
[AutoCounter]
Activity? PlacingOrder(int orderId);
```

Rules:

- Methods that return `Activity?` create an Activity.
- Methods that return `void` typically add events, log, or record metrics.
- Pass the `Activity?` from the starting method to event/context methods.

## Parameter attributes

| Attribute | Use |
|---|---|
| `[Baggage]` | Adds the parameter to Activity baggage; also included in logs. |
| `[Tag]` | Adds the parameter as an Activity tag. |
| `[ExcludeTargets(Targets.Activities)]` | Excludes the parameter from Activity generation only. |
| `[ExpandEnumerable(maximumValueCount: N)]` | Expands an array/IEnumerable into indexed log properties. |

## Testing

Inject the interface in your service and mock it in tests. The generated interface has no implementation code in the interface itself, so standard mocking frameworks (NSubstitute, Moq, TUnitMocks) work directly.

See [references/REFERENCE.md](references/REFERENCE.md) for a decision matrix and naming examples.
