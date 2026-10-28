# Project context discovery

The procedure for establishing enough context to make an architectural recommendation, and — just as
importantly — for knowing when to stop asking.

## The principle

Every question you ask a user is a question they have usually already answered in the repository.
Reading is free and silent; asking is slow and, when the answer was already in a project file, it
reads as not having looked. So the order is fixed:

1. Read the repository.
2. Read the conversation so far.
3. Ask only about what remains *and* changes your answer.

A dimension whose answer would not change your recommendation is not worth a question. If a project
is a stateless CLI, the database GUID strategy is not an unknown — it is irrelevant, and raising it
is noise.

## Dimension 1 — complexity

Assess from requirements, never from line or project count.

| Signal | Suggests |
| -- | -- |
| One functional area, one consumer, no integrations | Small |
| A handful of features, one or two integrations, a single team | Medium |
| Several bounded areas, multiple services or teams, operational obligations (SLOs, auditability, money) | Large |

Where to look: `README.md`, the wiki, issue or ticket references, the solution file's project list,
`PackageReference` entries naming external systems, the number of distinct hosts.

Traps:

- **A big repository is not a complex one.** A monorepo of ten tiny tools is ten small projects.
- **A small repository can be complex.** Few files, hard invariants.
- **Generated code inflates everything.** Discount it.

## Dimension 2 — project type

Read it; do not ask it.

| Evidence | Type |
| -- | -- |
| `Microsoft.NET.Sdk.Web` | Web application or HTTP API — distinguish by whether views/pages or endpoints dominate |
| `OutputType` of `Exe` plus an argument parser | CLI |
| `IsPackable`, no entry point | Reusable library |
| `IHostedService` / `BackgroundService`, `Worker` SDK | Background worker |
| An AppHost project, `Aspire.Hosting` | Distributed application |
| WPF/WinForms/Avalonia/MAUI SDK or `UseWPF` | Desktop application |

A reusable library carries the strongest constraint on the rest of your advice: it must not configure
global telemetry providers, must not read configuration from the ambient environment, and must not
decide a persistence technology for its consumer.

## Dimension 3 — persistence

| Question | Read it from |
| -- | -- |
| Is persistence required at all? | Any data-access `PackageReference`, a `DbContext`, a connection string, a repository type, an Aspire resource |
| Which technology? | The EF Core provider package (`Microsoft.EntityFrameworkCore.SqlServer`, `Npgsql.EntityFrameworkCore.PostgreSQL`), the client package, or the `Purview.Containers` resource in the AppHost |
| Is more than one implementation required? | Two providers referenced, a provider abstraction already present, or an explicitly stated requirement |
| EF Core or something else? | Presence of `Microsoft.EntityFrameworkCore` versus Dapper, a raw client, or a document store SDK |

"No persistence" is a complete and common answer. When it is the answer, every storage-shaped
recommendation — repositories, migrations, GUID strategies, connection resilience — is out of scope.

## Dimension 4 — telemetry

| Question | Read it from |
| -- | -- |
| Is telemetry configured? | `ILogger` injection, `ActivitySource`/`Meter` construction, `AddOpenTelemetry`, a `ServiceDefaults` project |
| Is Purview Telemetry available? | A `Purview.Telemetry.SourceGenerator` `PackageReference`, or interfaces attributed `[Logger]`, `[ActivitySource]`, `[Meter]` |
| Which signals are relevant? | Logs always; traces once a call crosses a process or a dependency; metrics once someone would alert on a rate or a duration |
| Is there an exporter? | An OTLP endpoint in configuration, a console exporter, an Aspire dashboard, an APM package |

Existing instrumentation is a constraint, not a starting point to replace. Preserve it, extend it,
and never register the same source twice.

## Conditional dimension — identifier generation

Resolve **only** when persistence exists and GUIDs are, or are about to be, primary keys.

- Are GUIDs primary keys, or just surface identifiers?
- Is the value generated in the application or by the database?
- Which provider and version? (This changes the answer — see `purview-persistence`.)
- Does index locality matter at the expected write volume?
- Must the identifier exist before the entity is persisted (outbox, correlation, client-side graph)?

The last question usually decides it: if the application needs the value before `SaveChanges`, the
database cannot be the generator.

## Asking well

When you must ask, ask few and ask concretely. Batch the unknowns into one short set, offer the
likely answers, and state what you will do if the user does not care.

Good:

> Two things I can't tell from the repository: is this service expected to own its own database, or
> read from the existing one? And should failures be returned to the caller or thrown? I'll assume
> its own database and `Purview.Results` for expected failures unless you say otherwise.

Bad:

> What is the project complexity? What is the project type? Is persistence required? Which database?
> Is telemetry configured? Do you use GUID primary keys?

The second asks six questions, five of which the repository answers, and one of which is irrelevant.

## Recording the answer

When a dimension is resolved by conversation rather than by the repository, that is a decision the
next agent will not be able to read. Write it down — in `AGENTS.md`, a wiki page, or an ADR — so the
question is asked once and never again.
