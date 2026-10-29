# Project responsibilities, naming and dependency direction

The catalogue in brief. The authoritative definition is
[`.config/architecture-policy.json`](https://github.com/purview-dev/engineering-playbook/blob/main/.config/architecture-policy.json),
and the full page — with diagrams — is
[Project Responsibilities](https://github.com/purview-dev/engineering-playbook/blob/main/docs/wiki/Project-Responsibilities.md).
Do not restate either here or in another skill; link to them.

## The catalogue

`A → B` means A depends on B **at compile time**.

| Responsibility | Also known as | Owns | May depend on |
| -- | -- | -- | -- |
| `Core` | `Domain` | Entities, value objects, invariants, domain events and errors | Shared |
| `Abstractions` | — | Cross-boundary capability contracts | Core, Shared |
| `Application` | `UseCases` | Use cases, workflows, orchestration | Core, Abstractions, Contracts, Shared |
| `Contracts` | — | Externally exposed request/response types; browser-safe when a WebAssembly client references it | Core, Shared |
| `Persistence` | — | DbContext, mappings, migration *definitions* | Core, Abstractions, Application, Shared |
| `Providers` | `Integrations` | Integration with a *named external platform* | Core, Abstractions, Application, Shared |
| `Adapters` | — | An *application-defined capability* via one technology | Core, Abstractions, Application, Shared |
| `Composition` | — | DI registration and implementation selection | Everything above |
| `API` | — | HTTP host | Composition, Contracts, ServiceDefaults, … |
| `Worker` | — | Background-processing host | Composition, ServiceDefaults, … |
| `Migrator` | — | *Executes* migrations and controlled seeding | Persistence, Composition, ServiceDefaults |
| `AppHost` | — | Aspire orchestration | The hosts it orchestrates |
| `ServiceDefaults` | — | Telemetry, health, resilience, discovery defaults | Shared |
| `WebApp` | — | Frontend, including a Blazor WebAssembly client that references Contracts | Contracts, ServiceDefaults, Shared |
| `Shared` | — | Small domain-independent primitives | *nothing* |

**None of these is mandatory.** A small application with `Core` and one host has a complete and
correct architecture. The catalogue names responsibilities so you can tell when splitting one out
buys something — it is not a list of projects to create.

### Backend-for-Frontend: guidance, not yet policy

A BFF is a host responsibility that the policy does **not** define yet; Blazor and BFF support is
planned for a future Build SDK release. Until then it is guidance, and every rule below is a review
obligation:

| Owns | May depend on | Split it out when | Leave it combined when |
| -- | -- | -- | -- |
| Sign-in and token custody for a browser or mobile client; forwards with the user's token. In Blazor, the host that serves the WebAssembly client | The WebAssembly client project, ServiceDefaults, Shared — and Contracts only when it prerenders data or combines responses | Tokens must be kept out of the browser, or several API calls must become one response | It would only forward calls that the client could make with a token it already holds safely |

A repository that adds a BFF project today needs its own responsibility entry or a waiver in its
policy, or the architecture tests reject the project as unclassified. See `contract-strategy.md`.

## When to split

| Responsibility | Split it out when | Leave it combined when |
| -- | -- | -- |
| `Core` | A domain model exists | The app is a thin utility |
| `Abstractions` | Two implementations, or the consumer must not see the implementation | One implementation — keep the interface beside it |
| `Application` | Workflows are reused by more than one host | It would only forward CRUD |
| `Contracts` | The contract strategy is Contracts or Hybrid — see `contract-strategy.md` | The API host is the only consumer, which is the OpenAPI strategy || `Persistence` | Persistence exists at all | A single-project utility writing a file |
| `Providers` | Two platforms with different SDKs or lifecycles | One platform |
| `Adapters` | One capability, several technologies | One capability, one technology |
| `Composition` | Two hosts compose the same implementations | One host — compose in the host |
| `Migrator` | Schema changes are a deliberate deployment step | Development-only startup migration |
| `Shared` | A primitive is used by two or more responsibilities | Almost always |

## Naming

```text
<Responsibility>[.<Implementation>]
```

| Situation | Name |
| -- | -- |
| One implementation | `Persistence` — do **not** qualify it just because it is SQL Server |
| Several, same dependencies and lifecycle | `Persistence` — one project, several classes |
| Several, genuinely different | `Persistence.SqlServer`, `Persistence.PostgreSql` |
| Several, with shared code | Add `Persistence` alongside the qualified projects |
| Several, nothing shared | Qualified projects only — no empty parent |

Discouraged as project names, because each names a category rather than a responsibility:
`Infrastructure`, `Services`, `Managers`, `Models`, `Helpers`, `Common`, `Utils`, `Utilities`.

`Infrastructure` is the common case. It is a useful *category* — covering Persistence, Providers and
Adapters — and a poor *project*, because every technical class qualifies for it.

**Do not rename an established project** merely to match canonical terminology. A rename costs more
than the consistency is worth, and `Domain`/`UseCases`/`Integrations` are recognised aliases.

## Providers versus Adapters

Not a vendor distinction:

- **Provider** — the requirement names the platform: "sync issues from GitHub". Swapping it changes
  the product.
- **Adapter** — the requirement names the capability: "send an email". Swapping the technology
  changes nothing the user sees.

Azure Blob behind `IFileStore` is an Adapter. The Azure DevOps work-item API in a product that
integrates Azure DevOps is a Provider.

## Dependency rules that catch people out

1. **Composition is referenced by nothing** except a host. It selects implementations, so it depends
   on them; the reverse would make the choice circular.
2. **Application never references a concrete infrastructure project.** It depends on an abstraction
   and receives an implementation it cannot name.
3. **Sibling implementations stay independent.** If `Providers.GitHub` needs something from
   `Providers.AzureDevOps`, that shared thing belongs in the unqualified project or an abstraction.
4. **Persistence owns migration definitions; Migrator executes them.** Production API startup does
   not mutate a schema.
5. **Runtime is not compile time.** DI registration, an HTTP call and Aspire orchestration are not
   assembly references. A Worker and an API can share Application without referencing each other.

## Recommending this to a repository that predates it

Do not propose a mass rename. Suggest, in order:

1. Fix dependency *directions* that are actually wrong — those are bugs.
2. Split a project only where a named problem justifies it.
3. Record a waiver for an established name rather than renaming it, so the rule stays enforceable
   without churn.

A waiver excuses the name only; the project still classifies, so every dependency rule still applies.
