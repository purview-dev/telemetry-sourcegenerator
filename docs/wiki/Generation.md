# Generation Options

This page describes the `[TelemetryGeneration]` attribute and the other assembly-level generation attributes that control how code is generated: class names, DI registration, naming conventions, and the `TelemetryNames` class.

## Generated class naming

For an interface `IOrderServiceTelemetry`, the generator produces:

- An implementation class named `OrderServiceTelemetryCore` (the interface name without the leading `I`, plus `Core`).
- A static DI extension class `OrderServiceTelemetryCoreDIExtension` with an `AddOrderServiceTelemetry` extension method.

Both can be overridden with the `[TelemetryGeneration]` attribute.

## `[TelemetryGeneration]`

`[TelemetryGeneration]` can be applied at the **assembly** level (affects every generated interface) or on an individual **interface**.

```csharp
// Assembly-level
[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]

// Interface-level
[TelemetryGeneration(ClassName = "MyCustomTelemetry")]
interface IOrderServiceTelemetry { }
```

| Property | Default | Description |
| --- | --- | --- |
| `GenerateDependencyExtension` | `true` | Whether to emit the static DI extension class with the `Add{InterfaceName}()` method. |
| `ClassName` | `null` | Overrides the generated implementation class name. When unset, the name is `{InterfaceName without I}Core`. |
| `DependencyInjectionClassName` | `null` | Overrides the DI extension class name. When unset, the name is `{implementationClassName}DIExtension`. |
| `DependencyInjectionClassIsPublic` | `false` | When `true` the DI extension class is `public`; otherwise it is `internal`. |
| `NamingConvention` | `NamingConvention.OpenTelemetry` | Controls generated telemetry names; see [Naming conventions](#naming-conventions). |
| `GenerateTelemetryNamesClass` | `true` | When `false`, suppresses the whole-assembly `TelemetryNames` class. |
| `TelemetryNamesClassName` | `null` | Custom name for the `TelemetryNames` class (default `TelemetryNames`). |
| `TelemetryNamesNamespace` | `null` | When set, relocates the implementation classes, the DI extension class, and the `TelemetryNames` class into this namespace. |

Resolution order is interface-level first, then assembly-level, then built-in defaults.

## Naming conventions

The `NamingConvention` enum controls the generated telemetry names.

```csharp
public enum NamingConvention
{
    Legacy = 0,          // v3 behaviour: lowercase, smashed compounds
    OpenTelemetry = 1    // v4+ default: OTel conventions
}
```

| Telemetry type | Legacy (v3) | OpenTelemetry (default) |
| --- | --- | --- |
| ActivitySource name | Assembly name lowercased: `"myapp"` | Assembly name preserved: `"MyApp"` |
| Tag / baggage keys | Lowercased, smashed: `"entityid"` | snake_case: `"entity_id"` |
| Metric instrument names | Lowercased, smashed: `"recordhistogram"` | Hierarchical dot.separated: `"myapp.products.record.histogram"` |
| Metric tag keys | Lowercased, smashed: `"requestcount"` | snake_case: `"request_count"` |

```csharp
// Revert all telemetry to v3 naming (assembly-level)
[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]

// Or set per-interface
[TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
interface IMyTelemetry { }
```

> [!TIP]
> Use `NamingConvention.OpenTelemetry` (the default) for new projects. Only use `Legacy` if you need exact v3 name compatibility.

## Dependency injection

By default every telemetry interface generates a static extension class:

```csharp
public static class OrderServiceTelemetryCoreDIExtension
{
    public static IServiceCollection AddOrderServiceTelemetry(this IServiceCollection services);
}
```

- The extension method name is `Add` + `{InterfaceName without the leading I}`.
- The class lives in the `Microsoft.Extensions.DependencyInjection` namespace so the extension method is discoverable with the standard `using`.
- The generated registration is `services.AddSingleton<IFace, Impl>()`.
- Set `GenerateDependencyExtension = false` to disable it, or `DependencyInjectionClassIsPublic = true` to make the class public.

## The `TelemetryNames` class

When a compilation contains at least one `[ActivitySource]` or `[Meter]` target, the generator emits a single `TelemetryNames` static class per assembly with the distinct source names:

```csharp
public static class TelemetryNames
{
    public static readonly string[] MeterNames;
    public static readonly string[] ActivitySourceNames;
}
```

It is used to register names with OpenTelemetry/ServiceDefaults:

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

Control it with `GenerateTelemetryNamesClass`, `TelemetryNamesClassName`, and `TelemetryNamesNamespace`.

## `[Exclude]`

Mark a method with `[Exclude]` to skip it entirely during generation. This is useful for interface members that should not produce telemetry.

```csharp
[Logger]
interface IOrderServiceTelemetry
{
    [Info]
    void OrderPlaced(int orderId);

    [Exclude]
    void DiagnosticOnly();
}
```

## Assembly-level generation attributes

| Attribute | What it controls |
| --- | --- |
| `[ActivitySourceGeneration]` | Default ActivitySource name, `DefaultToTags` behaviour, baggage/tag prefix and separator, and whether missing-Activity diagnostics (`TSG3014`/`TSG3015`) are generated. |
| `[LoggerGeneration]` | Assembly-level default log level, default `LogPrefixType`, and default logging generation mode. |
| `[MeterGeneration]` | Default meter name, meter-name generation type, instrument prefix/separator, and casing defaults. |

See [Activities](Activities.md), [Logging](Logging.md), and [Metrics](Metrics.md) for the full property tables.

## Next steps

- [Activities](Activities.md), [Logging](Logging.md), [Metrics](Metrics.md) — per-target attribute reference
- [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md) — parameter-level attributes
- [Diagnostics](Diagnostics.md) — analyzer rules