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
| `GenerateTelemetryNamesAttribute` | `true` | Emits the assembly-level `[GeneratedTelemetryNames]` attribute recording this assembly's generated names, so downstream assemblies can aggregate them. Set to `false` to keep the names out of the assembly's metadata. |
| `GenerateTelemetryNamesClass` | `false` | When `true`, emits the whole-assembly `TelemetryNames` class. Opt-in: the names are already recorded in assembly metadata, so only the project that registers them needs the class. |
| `TelemetryNamesClassName` | `null` | Custom name for the `TelemetryNames` class (default `TelemetryNames`). |
| `TelemetryNamesNamespace` | `null` | When set, relocates the implementation classes, the DI extension class, and the `TelemetryNames` class into this namespace. |
| `AggregateReferencedTelemetryNames` | `true` | When `true`, the `TelemetryNames` class also includes the names recorded by referenced assemblies. Set to `false` for this assembly's names only. |

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

## Telemetry names

### `[assembly: GeneratedTelemetryNames]`

When a compilation contains at least one `[ActivitySource]` or `[Meter]` target, the generator records that assembly's distinct source names in an assembly-level attribute:

```csharp
[assembly: GeneratedTelemetryNames(new string[] { "MyApp.Orders" }, new string[] { "MyApp.Orders" })]
```

This is emitted for every assembly by default (`GenerateTelemetryNamesAttribute = true`) and needs no configuration. It is a compile-time record — no reflection and no assembly scanning — so it works under trimming and native AOT, and it is what lets one project collect the names of everything it references. Set `GenerateTelemetryNamesAttribute = false` to keep an assembly's names out of its metadata.

### The `TelemetryNames` class

Set `GenerateTelemetryNamesClass = true` to also emit a static class of names, aggregating this assembly's names with the names recorded by every referenced assembly:

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

The class is opt-in because only the project that registers the names needs it. Turn it on in the executable or host project — the libraries it references publish their names through the attribute:

```csharp
// Program.cs or Properties/AssemblyInfo.cs of the host project
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
```

The names are deduplicated and ordered, and the aggregate covers the whole reference graph that the compiler was handed — which includes transitive project and package references in a normal MSBuild build.

The host project does not need telemetry of its own. An API or console project that defines no `[ActivitySource]` or `[Meter]` interface still gets the class from the assembly-level attribute alone, containing the names its references recorded. If neither the project nor its references generated any names, nothing is emitted.

> [!NOTE]
> The reference direction still applies: an Aspire-style `ServiceDefaults` project is referenced *by* the projects that own the telemetry, so it cannot aggregate them. Generate the class in the host project and pass the arrays into `ServiceDefaults`, as the [sample application](Sample-Application.md) does.

Control the two outputs with `GenerateTelemetryNamesAttribute` (default `true`) and `GenerateTelemetryNamesClass` (default `false`), plus `TelemetryNamesClassName`, `TelemetryNamesNamespace`, and `AggregateReferencedTelemetryNames` for the class.

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