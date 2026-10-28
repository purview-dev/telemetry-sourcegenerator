# Blazor WebAssembly, browser-safe libraries and BFF hosts

Every behaviour below applies only to the project type it names, and each one names the failure it
prevents. Nothing here changes a project that is not a WebAssembly client, a Blazor host, a BFF or a
`PurviewBrowserSafe` library.

## Blazor WebAssembly client (`IsBlazorWebAssemblyProject`)

| Applied | Failure it prevents | Opt out |
| -- | -- | -- |
| `IsPackable=false` | packing an app's `wwwroot` as a package nobody can consume | `IsPackable=true` in the project |
| `IsContainerProject=false`, so no `PublishAot`, `InvariantGlobalization` or Docker defaults | `PublishAot` on a client is a publish that fails, and the Blazor SDK owns globalisation | `DisablePurviewBlazorWebAssemblyDefaults=true` before the SDK import |
| no automatic `../Shared*/Shared*.csproj` reference | a server-side library joining the browser download because of a folder name | `DisablePurviewBlazorWebAssemblyDefaults=true` before the SDK import |
| the browser-safe reference check (`PRBLZ0001`) | a library joining the download without anyone deciding it was safe there | `DisablePurviewBrowserSafeReferenceCheck=true` |

Globalisation, trimming mode and the Blazor runtime settings are deliberately left alone: the Blazor
SDK owns them, and a second default would compete with it.

### The automatic packages

The SDK adds `Purview.Telemetry.SourceGenerator` and `Microsoft.Extensions.Telemetry.Abstractions`
to every non-test project, a client included. Both were checked rather than assumed:

- the generator is analyzer-only (`PrivateAssets=all`), so nothing of it is downloaded;
- its generated `ActivitySource`, `ILogger` and `Meter` code, and `Telemetry.Abstractions` itself,
  publish with no trim warnings and run on the WebAssembly runtime
  (`BlazorWebAppTests.AutomaticTelemetryPackages_RunInsideTheWebAssemblyRuntime` boots it);
- the SDK adds no exporter. An OTLP exporter cannot work from a browser, which is exactly why the SDK
  never adds one - that is the application's choice.

Nothing is therefore excluded from a client by default. `ExcludePurviewTelemetry` and
`ExcludeMSTelemetryExtension` still remove them.

## Blazor host (`IsBlazorHostProject`)

A web SDK project named `<Name>` beside a WebAssembly client named `<Name>.Client` is the Blazor Web
App pairing. The host gets an automatic `ProjectReference` to the client - probed the same way a test
project's target is, and skipped when the project already references it - so the client's framework
files are served. Opt out with `PurviewAutoBlazorClientReference=false`.

`IsBlazorHostProject` says nothing about `IsBffProject`. In a Blazor Web App the host is the BFF only
if it keeps the tokens.

## BFF host (`IsBffProject`)

- **No packages are added.** YARP, OpenID Connect and any BFF library are the application's choice,
  and Duende.BFF carries a commercial licence.
- The `Microsoft.AspNetCore.OpenApi.Generated` interceptor namespace every other web project gets is
  removed, because a BFF that only forwards has no API document to generate. A BFF referencing
  `Microsoft.AspNetCore.OpenApi` keeps it; `PurviewBffExposesApi` decides explicitly.
- `PDS0008` reports `HttpOnly = false` on the `CookieAuthenticationOptions` cookie (script could read
  the session). `PDS0009` reports `SameSite = None` on it (cross-site requests would carry it). Only a
  constant assigned to that cookie is reported - never the antiforgery cookie, which script may
  legitimately read, nor the OpenID Connect correlation and nonce cookies, which need
  `SameSite=None`.

Deliberately **not** checked, because a build cannot check them precisely:

- **a persisted Data Protection key ring.** The call that persists it (`PersistKeysTo*`) is routinely
  made in a shared ServiceDefaults project, which the BFF's compilation cannot see, so the rule would
  fire on correctly configured BFFs - and a noisy rule gets suppressed wholesale.
- **anti-forgery on forwarded routes.** It stays a review obligation.

## Browser-safe library (`PurviewBrowserSafe=true`)

Applies to class libraries only, and only what the project has not set:

- `IsAotCompatible=true` (on a TFM that supports it), which turns on `IsTrimmable` and the trim,
  single-file and AOT analyzers;
- `<SupportedPlatform Include="browser" />`, so `CA1416` reports an API the browser does not support;
- `WarningsAsErrors` for every trim (`IL2001`-`IL2128`), single-file (`IL3000`-`IL3005`) and AOT
  (`IL3050`-`IL3059`) diagnostic and `CA1416`, listed in `PurviewBrowserSafeWarningCodes`.
  `TreatWarningsAsErrors` is not touched.

`CA1416` is `suggestion` in the shipped `.editorconfig`. The compiler's `/warnaserror+:CA1416`
outranks an `.editorconfig` severity, so the error still fires without the repository's setting
changing. The consequence of `PurviewBrowserSafeWarningsAsErrors=false` is that the trim and AOT
diagnostics stay warnings while `CA1416` falls back to the repository's configured severity.

What it cannot catch: file and network I/O. `File` works against the browser's in-memory file system
and `HttpClient` works through `fetch`, so neither is annotated as unsupported, and no analyzer reports
them. Reviewing for them stays a human obligation.

### The reference check (`PRBLZ0001`)

A WebAssembly client asks each `ProjectReference` - through an MSBuild call on the referenced project,
never by guessing from its name - for its `PurviewBrowserSafe` value, and warns for each one that does
not set it. A browser-safe library runs the same check over its own references, so the guarantee
covers the whole download, not only the first level.

- `PurviewBrowserSafe="true"` metadata on the `ProjectReference` records the decision for a project
  that does not use this SDK.
- References with `ReferenceOutputAssembly="false"` or an `OutputItemType` are not downloaded and are
  not checked. Package references are not checked either.
- `PurviewBrowserSafeReferenceCheckAsError=true` makes it an error.

### Purview packages a Contracts library builds on

Checked against a `PurviewBrowserSafe` library, then run on the WebAssembly runtime:

| Package | Result |
| -- | -- |
| `Purview.Results` 1.0.4 | clean |
| `Purview.ZodSharp` 2.4.2 | clean, including `[ZodSchema]` |
| `Purview.ValueObjects` 1.2.3 | the runtime assembly is AOT-compatible, but the JSON converter the generator emits for a scalar calls the reflection-based `JsonSerializer.Serialize`/`Deserialize<T>(..., options)` overloads, which is `IL2026` and `IL3050` - errors in a browser-safe library. Set `GenerateJsonConverter = false` on the scalar until the generator writes the value directly. |
