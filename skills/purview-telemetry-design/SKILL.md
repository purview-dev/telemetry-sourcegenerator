---
name: purview-telemetry-design
description: Design best practices for Purview.Telemetry.SourceGenerator interfaces. Use when deciding between a single telemetry interface and per-area interfaces, choosing telemetry names, designing multi-target methods, or reviewing telemetry structure in a C# project.
license: MIT
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
- Metric instrument names: dot-separated, snake_case words. The generator prefixes them with the interface name, not
  the meter name: `IOrderTelemetry.OrdersPlaced` becomes `order.orders_placed`. Opt in to a meter-name prefix with
  `MeterNameGenerationType.OpenTelemetry`.
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

## Union-typed parameters

A parameter whose type is a union is recorded as its **case value**, never as the union struct, which has no meaningful
`ToString`. The generator emits one private, aggressively inlined accessor per union type and reads each case through
the union's own `TryGetValue` overloads when it declares them (a `bool HasValue` property plus one `TryGetValue(out T)`
per case), so the union struct is never boxed to read it. A union that holds no value records `null`. Works on every
supported target, `net48` included: detection is by symbol, not by framework.

```csharp
public union OrderOutcome(OrderPlaced, OrderRejected);

[Activity]
Activity? PlacingOrder([Tag] OrderOutcome outcome);
```

Three kinds of union are recognised:

| Union | How the case list is found |
| --- | --- |
| C# 15 `union` declaration | the language's own generated constructors; read through `Value` |
| Hand-written `[System.Runtime.CompilerServices.Union]` | its `IUnionMembers` provider, otherwise its constructors; the non-boxing pattern is *detected* |
| Purview.Unions `[GenerateUnion]` | the surfaces that are in **source**, in this order: the attribute's `typeof` list, partial static factory signatures, a nested `IUnionMembers` provider, then the constructors |

A `[GenerateUnion]` union declares nothing but the attribute in its own source — the `[Union]` attribute, constructors,
`HasValue` and `TryGetValue` are emitted by the unions generator into generated output, which this generator never sees.
The accessor is therefore emitted against members that will exist in the final compilation, and it always uses the
non-boxing pattern, because that generator emits it for every surface. Cases are normalised as that generator exposes
them (`int?` → `int`, nullable annotation stripped). A `[GenerateUnion]` target with no case list visible in source is
recorded as an ordinary value, not as a union: Purview.Unions reports `UNI0003` for it.

If another generator also needs the case list, a nested `IUnionMembers` provider in your own source is the portable
route — both generators can read it. There is no reference from this package to Purview.Unions.

Prefer a union over `object` for a tag, baggage or log value that can be one of a fixed set of types: the call site
keeps exhaustive matching, and the generated accessor knows every case instead of discovering it at run time.

## Testing

Inject the interface in your service and mock it in tests. The generated interface has no implementation code in the interface itself, so standard mocking frameworks (NSubstitute, Moq, TUnitMocks) work directly.

See [references/REFERENCE.md](references/REFERENCE.md) for a decision matrix and naming examples.
