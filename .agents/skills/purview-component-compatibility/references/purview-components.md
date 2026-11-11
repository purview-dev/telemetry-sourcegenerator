# The Purview component hierarchy

Which Purview package depends on which, and which target frameworks each family supports, so a harvest
or a floor decision starts from the shape rather than from a memory of it.

Every row was read from the registry with `scripts/dependencies.mjs`, which reads the published
`.nuspec`. **The registry is the authority; this file is the map.** Re-run the script before acting on
any row — a family that multi-targets changes its floors on every release.

## The families

| Family | Packages | Target frameworks | Depends on |
| -- | -- | -- | -- |
| Build SDK | `Purview.BuildSdk` | `netstandard2.0` (an MSBuild SDK) | nothing |
| Test-support rule sets | `Purview.Skills.Testing`, `Purview.Architecture.Testing`, `Purview.Architecture.Testing.Cli` | `net10.0`; `Architecture.Testing` also `net11.0` | `TUnit.Core`, `TUnit.Assertions`, `JsonSchema.Net` |
| Telemetry | `Purview.Telemetry.SourceGenerator` | `netstandard2.0` (an analyzer) | nothing |
| Containers | `Purview.Containers` + `.Core`, `.Docker`, `.Wsl`, `.MsSql`, `.PostgreSql` | `net10.0`; `.Wsl` adds `net10.0-windows10.0.19041` | its own family at the same version, `Microsoft.Extensions.Telemetry.Abstractions`, `Testcontainers` |
| Results | `Purview.Results`, `.AspNetCore`, `.ZodSharp`, `.ZodSharp.AspNetCore` | `net10.0`, `net11.0` | nothing; the adapters depend on the core and on ZodSharp |
| ZodSharp | `Purview.ZodSharp`, `.AspNetCore`, `.SystemTextJson`, `.NewtonsoftJson` | `net9.0`, `net10.0`, `net11.0` | `Microsoft.Extensions.Options`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| Value Objects | `Purview.ValueObjects` | `net8.0`, `net9.0`, `net10.0`, `net11.0` | nothing |
| Source Generator Framework | `Purview.SourceGeneratorFramework`, `.Testing`, `.Testing.TUnit` | `netstandard2.0` | Roslyn (`Microsoft.CodeAnalysis.CSharp`), `System.Collections.Immutable`, `System.Reflection.Metadata` |
| Aspire ResourceKit | `Purview.Aspire.ResourceKit` | `net8.0`, `net9.0`, `net10.0` | `Aspire.Hosting`, OpenTelemetry exporter and hosting, `Microsoft.Extensions.*` |
| Astroc4 | `@purview-dev/astroc4` (npm, not NuGet) | an Astro/Starlight plugin | `likec4`, `@astrojs/starlight`, `astro` as peers |

## Reading the shape

Three patterns are worth internalising, because each one changes what "the floor" means:

- **A family ships at one version.** `Purview.Containers.Core` is required at the umbrella's own version,
  and `Purview.Results.ZodSharp` requires `Purview.Results` and `Purview.ZodSharp` at their current ones.
  Declare the family with one property, so its floor moves as a unit.
- **An analyzer has no consumer-side floor worth arguing about.** `Purview.BuildSdk`,
  `Purview.Telemetry.SourceGenerator` and `Purview.SourceGeneratorFramework` target `netstandard2.0` and do
  their work at compile time. They still appear in a transitive audit, so they still get a real version.
- **A multi-targeting component has a different floor per framework**, and ZodSharp shows it most
  clearly: the `net11.0` group declares older `Microsoft.Extensions.*` minimums than the `net10.0` group,
  because the lower framework carries the newer requirements. Reading one group and applying it to
  another is how a floor is set wrong in the direction that breaks consumers.

## What the script prints

```text
Purview.Containers 1.0.0-prerelease.11
  net10.0: Purview.Containers.Core >= 1.0.0-prerelease.11
  net10.0: Purview.Containers.Docker >= 1.0.0-prerelease.11
  net10.0: Purview.Containers.Wsl >= 1.0.0-prerelease.11
  net10.0: Microsoft.Extensions.Telemetry.Abstractions >= 10.11.0
  net10.0: Testcontainers >= 4.16.0
  Purview.Containers.Core 1.0.0-prerelease.11
    net10.0: Microsoft.Extensions.Telemetry.Abstractions >= 10.11.0
```

A bare version is a minimum; brackets would mean a range. The numbers above are what the registry said
when this page was written — the shape is the part that lasts, which is why the command is here and the
versions are not presented as targets.

## Related components that are not Purview packages

`AspireC4.Hosting` is published under its own name and versions on its own schedule, so it is not part of
a Purview family floor. Read it the same way, with the same script.
