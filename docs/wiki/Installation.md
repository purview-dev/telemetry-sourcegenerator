# Installation

This page covers supported frameworks, installation methods, per-feature dependencies, and build configuration for the `Purview.Telemetry.SourceGenerator` package.

## Supported frameworks

**Consumer runtime targets:**

- .NET Framework 4.8
- .NET 8 or higher

**Build toolchain requirement:**

- Visual Studio 2026 (18.x) or the .NET 10 SDK (Roslyn 5.9.0+)

The generator itself is a Roslyn component targeting `netstandard2.0`, so it works in any supported compiler host.

## Install the package

### .NET CLI

```bash
dotnet add package Purview.Telemetry.SourceGenerator
```

### Package Manager Console

```powershell
Install-Package Purview.Telemetry.SourceGenerator
```

### Project file (.csproj or Directory.Build.props)

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

`PrivateAssets="all"` keeps the generator out of consumers' dependencies and `IncludeAssets="analyzers"` treats it purely as an analyzer.

## Runtime dependencies

| Telemetry type | Required reference | Notes |
| --- | --- | --- |
| Activities | `System.Diagnostics.DiagnosticSource` | Included in the SDK for .NET 5+ |
| Logging | `Microsoft.Extensions.Logging.Abstractions` | Required for `[Logger]` interfaces; `TSG2003` is raised if `ILogger` cannot be resolved |
| Logging (v2) | `Microsoft.Extensions.Telemetry.Abstractions` | Required for state-based v2 generation (`LoggerMessageHelper`, `[LogProperties]`) |
| Metrics | `System.Diagnostics.Metrics` | Included in the SDK for .NET 8+ |

## Verifying the install

Create an interface with a telemetry attribute and build:

```csharp
using Purview.Telemetry;

[Logger]
interface IMyTelemetry
{
    [Info]
    void Hello();
}
```

If generation succeeds you will see a `MyTelemetryCore` implementation and an `AddMyTelemetry` extension method. To inspect the generated code, enable:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Generated files land in `obj/Debug|Release/<tfm>/generated/Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.TelemetrySourceGenerator/`.

## Build configuration

### `PURVIEW_TELEMETRY_ATTRIBUTES`

Marker attributes are generated as `[Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]` and are internal to your assembly. Define this constant if you want to retain them in your build (for example, so the analyzer sees attribute usage in projects that share attribute source):

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);PURVIEW_TELEMETRY_ATTRIBUTES</DefineConstants>
</PropertyGroup>
```

### `EXCLUDE_PURVIEW_TELEMETRY_LOGGING`

Define this constant to disable logging generation entirely. This is useful when the `Microsoft.Extensions.Logging` types are not available in the compilation:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);EXCLUDE_PURVIEW_TELEMETRY_LOGGING</DefineConstants>
</PropertyGroup>
```

### `PURVIEW_TELEMETRY_NON_NULLABLE`

Define this constant to opt out of the `TSG1011` unsupported-target-framework check for non-net8+/net48+ compilations:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);PURVIEW_TELEMETRY_NON_NULLABLE</DefineConstants>
</PropertyGroup>
```

## Project-type snippets

### ASP.NET Core

```csharp
builder.Services.AddWeatherServiceTelemetry();
```

### Console / library

```csharp
services.AddOrderServiceTelemetry();
```

### .NET Aspire

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

See the [sample application](Sample-Application.md) for a complete Aspire setup on .NET 10.

## Troubleshooting

**"Type or namespace 'ActivitySourceAttribute' could not be found"**

- Ensure `using Purview.Telemetry;` is present (all attributes live in the single `Purview.Telemetry` namespace).
- Check the package reference includes `IncludeAssets="analyzers"`.

**"No implementation found for interface"**

- Verify the interface has at least one of `[ActivitySource]`, `[Logger]`, or `[Meter]`.
- Check that methods have the appropriate method-level attributes.
- Rebuild to trigger source generation.

**"ActivitySource not producing traces"**

- Ensure the generated `Add{InterfaceName}()` DI registration method has been called.
- Configure OpenTelemetry to listen to your ActivitySource name.

**"Logs not appearing"**

- Verify `ILogger` is configured in your application.
- Check that log-level filters are not excluding your messages.
- Ensure DI registration was called.

**"Metrics not collected"**

- Configure a metrics exporter in your application.
- Verify the meter name matches what your collector expects.
- Confirm instruments are recorded with valid measurement values.

## Next steps

- [Getting Started](Getting-Started.md) — write your first telemetry interface
- [Generation Options](Generation.md) — control class names, DI, and naming
- [Diagnostics](Diagnostics.md) — analyzer warnings and errors