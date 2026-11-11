# EF Core integration

Read this when the project actually uses EF Core. The identifier decisions are in `guid-generation.md`
and `well-known-identifiers.md`; this page is the rest of the integration.

## The rule

1. **`DbContext` is the boundary.** Read and write through it directly, or through a narrow query type
   when a query deserves a name. Do not add `IRepository<T>` or a unit-of-work wrapper for testability.
   `DbContext` already is both, and a real container is a more faithful test than a substitute.
2. **The provider is chosen where the context is registered.** Carry the choice as context-options data;
   never branch on `Database.ProviderName` while the model is being built. The model is built once and
   cached, so that branch serves one provider's model to the other.
3. **Map a value object once.** Configure the converter and comparer in the model, not on each property.
   The `Purview.ValueObjects` generator emits the converters when EF Core is referenced. A value-object
   key also needs a converter-aware value generator, or the property receives a raw `Guid`.
4. **One owner per generated value.** The application supplies it, the store generates it, or it is
   derived — never two. A derived identifier is `ValueGeneratedNever()`; a store default or computed
   column is left to the store.
5. **Design time and runtime agree.** The design-time factory or host registration makes the same
   provider, strategy and `ReplaceService` choices as the application. Otherwise the migration snapshot
   describes a model the application does not run.
6. **The model cache key covers every input that varies the model.** Provider, strategy, tenant and
   design time. EF's service-provider cache and model cache are separate; both need a key that matches
   the decision.
7. **Migrations are code.** Persistence owns the definitions; a deliberate step applies them. The API
   does not migrate at startup in production.
8. **Integration tests use a real store.** The in-memory provider does not enforce foreign keys, unique
   indexes, computed columns or SQL semantics. Use `Purview.Containers` and a `<Target>.IntegrationTests`
   project.
9. **The connection string is configuration.** Keep it out of source, out of `appsettings.json` in the
   repository, and out of telemetry. A redaction test is worth more than a convention.

## The composition

The provider, the strategy and the service replacements belong together, where the context is
registered:

```csharp
services.AddDbContext<AcmeDbContext<TProvider>>((serviceProvider, options) =>
{
	ConfigureStore<TProvider>(options, connectionString);

	options
		.UseSequentialGuids<TProvider>()
		.ReplaceService<IModelCacheKeyFactory, AcmeModelCacheKeyFactory>()
		.ReplaceService<IModelCustomizer, AcmeModelCustomizer<TProvider>>();
});

services.AddSingleton<ISequentialGuidFactory, SequentialGuidFactory<TProvider>>();
```

- `ConfigureStore` is the only place that names `UseSqlServer` or `UseNpgsql`.
- `UseSequentialGuids` adds an options extension; the model reads it instead of asking about the provider.
- `IModelCacheKeyFactory` is replaced because the service-provider hash alone does not key the model
  cache.
- The application-facing factory is registered separately, because EF's internal registration is not
  reachable from an outbox writer or a correlation-id generator.

## Migrations

- Generation and application are separate: `dotnet ef migrations add` in development, a controlled
  `Migrator` step in a deployment.
- A migration and `HasData` are compiled and replayed, so they need a literal GUID. A derived identifier
  cannot be computed there; print the constant from the derivation and guard it with a test.
- Expect the schema to be ahead of or behind the code during a rollout, and design the change to be
  compatible in both directions.
- Never let the API apply migrations at startup in production: a deploy then mutates the schema without
  review and without a clean rollback.

## Checks to write

| Check | Shape |
| -- | -- |
| Model covers every GUID key | Each non-foreign-key GUID key is generated on add with the expected factory |
| Foreign keys are not generated | A foreign-key GUID keeps the value copied from its principal |
| Explicit configuration wins | A `ValueGeneratedNever()` key keeps its supplied value through `SaveChanges` |
| Two strategies, one context type | Different strategies produce different service providers and models |
| Design time matches runtime | Both models produce the same migration snapshot |
| Real store semantics | A unique index, a foreign key and a computed column behave as the model says in a container |

The Engineering Playbook's *EF Core Integration* page states the same rules with the sample as evidence;
the runnable implementation is `samples/Acme.Persistence` in that repository, run with `just samples`.
