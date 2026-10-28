---
name: purview-sourcegenerator-pipeline
description: >-
  Use when composing an incremental pipeline with Purview.SourceGeneratorFramework -
  IncrementalPipeline helpers, GenerationContext and custom contexts, CreateCodeWriter lifetime,
  GeneratorResult and blocking versus non-blocking diagnostics, disabling a generator from MSBuild,
  scope validation, test logging, and stage tracking names.
metadata:
  product: Purview.SourceGeneratorFramework
  repository: purview-dev/sourcegenerator-framework
---

# Incremental pipeline composition

`IncrementalPipeline` composes `IncrementalValueProvider<T>` and `IncrementalValuesProvider<T>`:
attribute-based discovery, generation-context creation, disable-property checks, and thin
source-output registration.

Everything here follows from one rule: **pipeline values must be immutable and value-equatable.**
Roslyn caches the complete value a provider publishes, and offers no way to exclude one property of
it from caching.

## `GenerationContext`

A base execution-services context carrying the Roslyn `Compilation`, immutable `GenerationSettings`,
an optional `ISourceGenLogger`, and a factory for independently owned `CodeWriter` instances.

```csharp
var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider<MyGenerator>(context);
```

### Custom contexts

A custom context does not need to accept or read build properties itself:

```csharp
public sealed class MyGenerationContext : GenerationContext
{
    public MyGenerationContext(Compilation compilation, GenerationSettings settings, ISourceGenLogger? logger)
        : base(compilation, settings, logger)
    {
    }
}

var contextProvider = IncrementalPipeline.GenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0",
    factory: static (compilation, settings, logger, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new MyGenerationContext(compilation, settings, logger);
    },
    disablePropertyName: "MyGenerator_Disable"
);
```

The provider resolves scope validation, generator disabling and test logging from analyzer-config
properties *before* invoking the factory. The logger is created internally only when logging is
enabled and a sink is registered for that run.

## `CodeWriter` lifetime — the rule that bites

Treat `GenerationContext` as **cached incremental state** and each `CodeWriter` as **mutable,
output-scoped execution state**. Create the writer inside the registered source-output callback,
after the incremental cache boundary:

```csharp
IncrementalPipeline.RegisterSourceOutput(
    context,
    targets,
    contextProvider,
    static (spc, target, generationContext) =>
    {
        var writer = generationContext.CreateCodeWriter();
        EmitTarget(generationContext, writer, target);
        spc.AddSource($"{target.Name}.g.cs", writer.ToString());
    }
);
```

Creating it in the callback and passing it to emitter helpers called from that same callback is the
intended pattern. The only forbidden thing is **persisting the writer in pipeline state**.

Why, concretely:

- Roslyn caches the whole published value; you cannot exempt a field.
- `CodeWriter` is mutable, so a cached one retains previously written source when the context is
  reused for another output or run.
- Source-output callbacks may process independent targets **concurrently**; a shared writer mixes
  their output and races.
- A fresh writer gives each generated source independent scope tracking and deterministic ownership.

`CreateCodeWriter()` returns a new, independently owned instance every call.

**Never add a `CodeWriter` property or field to a class derived from `GenerationContext`.** A custom
context is still produced by an incremental provider and cached as one complete value. Store only
compilation-derived services and immutable configuration there.

When an emitter needs both services and writing, pass the context and the writer separately, or
compose a short-lived wrapper created inside the callback — which must never be returned from a
provider:

```csharp
public sealed class GenerationOutputContext<TContext> : ISourceGenLogger
    where TContext : GenerationContext
{
    public GenerationOutputContext(TContext generation)
    {
        Generation = generation;
        Writer = generation.CreateCodeWriter();
    }

    public TContext Generation { get; }

    public CodeWriter Writer { get; }

    public void Log(SourceGenLogLevel level, int indentation, string message, params object[] args) =>
        Generation.Log(level, indentation, message, args);
}
```

## `GeneratorResult<T>` and diagnostics that do not stop generation

`RegisterSourceOutput` combines targets with the generation context, reports diagnostics, and runs
the callback only for successful results:

```csharp
var targets = IncrementalPipeline.ForAttributeWithMetadataName(
    context,
    AttributeType,
    static (ctx, ct) =>
    {
        var symbol = ctx.TargetSymbol;

        return symbol is null
            ? GeneratorResult<string>.Empty
            : GeneratorResult<string>.Create(symbol.Name);
    }
);
```

The callback runs only when `GeneratorResult<T>.ShouldProcess` is `true` — the result carries a value
**and** none of its diagnostics are blocking.

`ReportableDiagnostic.IsBlocking` is an explicit per-diagnostic decision, **independent of
severity**. An `Error`-severity diagnostic can still allow generation to continue, which is what you
want when the generated code is what helps the developer fix the problem — a generator emitting an
abstract base class can report an error per missing override while still emitting the base class, so
the user can see exactly what to implement:

```csharp
static readonly DiagnosticDescriptor MissingOverride = new(
    "MYGEN001",
    "Missing override",
    "Type '{0}' must override '{1}'",
    "Usage",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true
);

// IsBlocking is false, so ShouldProcess stays true and the base class is still emitted.
var diagnostic = ReportableDiagnostic.Create(MissingOverride, isBlocking: false, symbol, symbol.Name, "Execute");

return GeneratorResult<BaseModel>.Create(model, diagnostic);
```

`isBlocking: true` stops generation for that target while still reporting.
`HasBlockingDiagnostics` reports whether any carried diagnostic blocked;
`HasErrorDiagnostics` reports the severity-based view.

## Disabling a generator at build time

Pass the compiler-visible disable property to the context provider; its resolved value lands in
`GenerationSettings` automatically:

```xml
<PropertyGroup>
  <MyGenerator_Disable>true</MyGenerator_Disable>
</PropertyGroup>
```

```csharp
var contextProvider = IncrementalPipeline.DefaultGenerationContextValueProvider(
    context,
    nameof(MyGenerator),
    "1.0.0",
    disablePropertyName: "MyGenerator_Disable"
);

// In the output stage:
if (generationContext.Settings.IsSourceGeneratorDisabled)
    return;
```

`IsDisabledValueProvider` is still there for when expensive upstream transforms must be filtered
before being combined with the generation context.

Declare the property with `PurviewGeneratorVisibleProperty` so it is compiler-visible wherever the
generator runs — `PSGF0003` otherwise fails the build. See the `purview-sourcegenerator-packaging`
skill.

## Scope validation

The default context provider reads
`PurviewSourceGeneratorFrameworkValidateCodeWriterScopes` and threads it into
`GenerationSettings.ValidateCodeWriterScopes`. When enabled, `ToString()` throws
`CodeWriterScopeValidationException` if `OpenScopeCount` is not zero.

## Test logging

Framework logging is off in ordinary compiler runs. The testing integration enables it by registering
an isolated sink and supplying a per-run session ID through analyzer config. Context providers create
the internal logger automatically — generators implement no logging interface and no logging-support
source is generated.

The sink registry stores **callbacks only**; it never buffers entries. With logging disabled, a
missing session ID, or no matching sink, the provider supplies no logger and log calls are discarded.
Test sinks own whatever they capture and are removed when the run completes.

## Tracking names

The pipeline helpers assign a tracking name to every stage, so a cache test can assert which stages
were recomputed. The `purview-sourcegenerator-testing` skill covers the stage matrix and the four
scenarios a cache test should assert.

## Related skills

`purview-sourcegenerator-codewriter` for emission, `purview-sourcegenerator-diagnostics` for the
`PSGFR` rules that enforce most of the above, and `purview-sourcegenerator-testing` for proving the
pipeline caches.
