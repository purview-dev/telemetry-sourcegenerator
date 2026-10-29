# Detection detail

## Shared project names (exact)

`Shared`, `SharedFramework`, `SharedInfrastructure`, `SharedInfra`, `SharedUtilities`,
`SharedUtils`, `SharedLibrary`, `SharedLib`, `SharedHelpers`

## Shared testing project names (exact)

`SharedTestingFramework`, `SharedTestingInfrastructure`, `SharedTestingInfra`,
`SharedTestingUtilities`, `SharedTestingUtils`, `SharedTestingLibrary`, `SharedTestingLib`,
`SharedTestingHelpers`

These are matched whole, not as substrings. `SharedModels` is not a shared project.

A shared-testing project is forced to `PurviewSharedTestingOutputType` (default `Library`), with
`IsTestProject` and `IsTestingPlatformApplication` cleared, because the test packages otherwise flip
it into an executable test host. Set `PurviewSharedTestingOutputType=Exe` before the SDK import to
keep that package-driven shape.

## Packable projects

Defaults applied only where the project supplied nothing: `GenerateDocumentationFile`,
`IncludeSymbols`, `SymbolPackageFormat=snupkg`, `PublishRepositoryUrl`, `EmbedUntrackedSources`,
`IncludeSource`, `DebugType=portable`.

- Portable PDBs ship in the `.snupkg`. The `.nupkg` receives none unless the project opts in.
- The repository-root `README.md` is packed and registered as `PackageReadmeFile` when it exists and
  nothing else is already being packed as one.
- Non-packable projects, web apps included, default `WarnOnPackingNonPackableProject=false`, so a
  solution-wide `dotnet pack` skips them silently.
- `ContinuousIntegrationBuild` is set only by real CI environment variables. Packability alone never
  forces SourceLink's CI-mode dirty-repository checks.

## Roslyn components

A project that explicitly declares `IsRoslynComponent=true` gets:

- a single `netstandard2.0` target, `LangVersion=latest`, `Nullable=enable`,
  `TreatWarningsAsErrors=true`, `Deterministic=true`, extended analyzer rules
- SourceLink with `EmbedUntrackedSources=true`, no dependency file, telemetry excluded
- compiler-generated output under the framework-specific intermediate directory
- `PrivateAssets=all` on `Microsoft.CodeAnalysis.*` and `Microsoft.CodeAnalysis.Analyzers`, so they
  never leak into the packed nuspec

`IsRoslynComponentOnly` defaults to `true`: `IncludeBuildOutput=false`, `IncludeSymbols=false`, no
`.symbols.nupkg` or `.snupkg`, and the analyzer PDB shipped inside the main `.nupkg` under
`analyzers/dotnet/cs/` beside the assembly (`PurviewPackAnalyzerPdb=true`). Set it to `false` for a
dual-role component that wants normal library symbol packaging.

`ValidateRoslynComponentCompilerSettings` fails the pack of a packable Roslyn component missing
`LangVersion`, `Nullable`, `TreatWarningsAsErrors` or `EnforceExtendedAnalyzerRules`. Opt out with
`DisableRoslynCompilerDefaultsValidation=true`.

`PackProjectReferencedSourceGenerators` (default `true`) makes a packable project include analyzer
`ProjectReference` outputs and their runtime dependencies under `analyzers/dotnet/cs/`. Set it
`false` globally, or `Pack="false"` on one analyzer reference.

## Special project types

| Type | Applied |
| -- | -- |
| CLI | `OutputType=Exe`, `appsettings*.json` included as content |
| Container | `InvariantGlobalization`, `PublishAot`, Linux Docker defaults, container tooling packages |
| Web SDK | `Microsoft.AspNetCore.OpenApi.Generated` added to `InterceptorsNamespaces`, unless marked as a separate web-project mode |
| Aspire host | `OutputType=Exe` |
| Blazor WebAssembly client | not packable, no container defaults, no automatic `Shared` reference, the browser-safe reference check - see [blazor.md](blazor.md) |
| Blazor host | an automatic reference to its `<Name>.Client` - see [blazor.md](blazor.md) |
| BFF | no packages, no OpenAPI interceptor unless it exposes an API, `PDS0008`/`PDS0009` - see [blazor.md](blazor.md) |
| `PurviewBrowserSafe` library | `IsAotCompatible`, `SupportedPlatform browser`, trim/AOT/`CA1416` as errors - see [blazor.md](blazor.md) |

## The Visual Studio refactorings

Two ship with the code-fix assembly, both serving the `PDS0002` convention:

- **Split extensions class into one class per receiver type** — offered on a static extensions class
  targeting several receiver types. It produces one `<ReceiverType>Extensions` class per receiver
  (a generic `this TBuilder where TBuilder : IHostApplicationBuilder` receiver becomes
  `HostApplicationBuilderExtensions`) and places each under `Extensions/<receiver namespace>/`.
- **Move extensions class to conventional location** — offered on a misplaced single-receiver
  extensions class. It re-paths the file to `Extensions/<receiver namespace>/`, fixes the namespace
  to the receiver's, and updates `using` directives in other referencing documents so the move
  compiles.
