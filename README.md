# Purview Telemetry Source Generator

Generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface.

[![CI](https://github.com/kjldev/purview-telemetry-sourcegenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/kjldev/purview-telemetry-sourcegenerator/actions/workflows/ci.yml)

This approach allows for:

- Faster iteration cycles - simply create the method on your interface and the implementation will be automatically generated
- Easy mocking/ substitution for testing - a full sample project, including tests can be found [here](https://github.com/kjldev/purview-telemetry-sourcegenerator/tree/main/samples/SampleApp)
- Built-in dependency injection helper generation

Use the latest version available on [NuGet](https://www.nuget.org/packages/Purview.Telemetry.SourceGenerator/), which supports the following frameworks:

- .NET Framework 4.7.2, or higher
- .NET 8 or higher

Reference in your `Directory.Build.props` or `.csproj` file:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator" Version="4.0.0-alpha.1">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

For more information see the [wiki](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki).

## Example Interface

This is called a **multi-target interface** because it generates more than one output type: **Activities, Logging, and Metrics**.

> [!TIP]
> When generating a single target, the generator will automatically infer the necessary attributes. More information about multi-targeting can be found in [here](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Multi-Targeting).

```csharp
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

/// <summary>
/// Generates an implementation of the methods for each generation type (Activity, Logging, or Metrics)
/// and an extension method to enable easy registration with the IServiceCollection.
/// </summary>
[ActivitySource]
[Logger]
[Meter]
interface IEntityStoreTelemetry
{
    /// <summary>
    /// Creates and starts an Activity and adds the parameters as Tags and Baggage.
    /// </summary>
    [Activity]
    Activity? GettingEntityFromStore(int entityId, [Baggage]string serviceUrl);

    /// <summary>
    /// Adds an ActivityEvent to the Activity with the parameters as Tags.
    /// </summary>
    [Event]
    void GetDuration(Activity? activity, int durationInMS);

    /// <summary>
    /// Adds the parameters as Baggage to the Activity.
    /// </summary>
    [Context]
    void RetrievedEntity(Activity? activity, float totalValue, int lastUpdatedByUserId);

    /// <summary>
    /// Generates a structured log message using an ILogger - defaults to Informational.
    /// </summary>
    [Log]
    void ProcessingEntity(int entityId, string updateState);

    /// <summary>
    /// Generates a structured log message using an ILogger, specifically defined as Informational.
    /// </summary>
    [Info]
    void ProcessingAnotherEntity(int entityId, string updateState);

    /// <summary>
    /// Adds 1 to a Counter<T> with the entityId as a Tag.
    /// </summary>
    [AutoCounter]
    void RetrievingEntity(int entityId);
}
```

To see the code generated for the `IEntityStoreTelemetry` interface, see the [`Generated Output`](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Generated-Output) page in the wiki.

## Example Project

The [.NET Aspire Sample](https://github.com/kjldev/purview-telemetry-sourcegenerator/tree/main/samples/SampleApp) demos the Activities, Logs, and Metrics generation working together with the Aspire Dashboard.

Check the page in the the [wiki](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Sample-Application) for information.

> [!TIP]
> This sample project has [`EmitCompilerGeneratedFiles`](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator#enable-the-configuration-source-generator) set to `true`, so you can easily see the generated output.

## Notes on Logging Generation

There are two types of logging generation based on:

1. **Microsoft Logging Extension Packages** – Determined by the NuGet packages referenced in your project.
2. **Attribute-based Configuration** – Controlled using attributes in your code.

For more details, see the [Logging](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Logging) page in the wiki.  
