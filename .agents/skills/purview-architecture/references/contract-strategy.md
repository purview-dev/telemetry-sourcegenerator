# Contract strategy: Contracts, OpenAPI or Hybrid

When a solution has an HTTP API and one or more clients, something has to define the shapes that cross
between them. There are three ways to do it, and the wrong one fails in one of two directions:

- **Rules drift.** The client holds a second, weaker copy of the domain rules, and a request the UI
  accepted is rejected by the server — or a break only surfaces at runtime.
- **Decoupling bought in advance.** A .NET client in the same solution regenerates its models from
  the API's own OpenAPI document, and throws away every value object and rule to buy an independence
  the solution does not have.

The strategy is chosen from the clients that **exist today**. A planned client is noted; it does not
count. "Later" and "in case" do not justify a strategy any more than they justify a project — see
`proportional-architecture.md`.

## The three strategies

### Contracts

A .NET class library holding the request and response types, built on the domain's value objects and
carrying `[ZodSchema]`. The server references it, and so does every .NET client — including a Blazor
WebAssembly client.

There is one copy of every rule, error code and normalisation, and both sides run the same generated
validators.

### OpenAPI

The server publishes an OpenAPI document, and clients generate their models and calls from it. It is
language-neutral and versions independently.

Generated models are primitives. Value objects, custom ZodSharp rules, error codes, and trimming and
casing are lost; only what JSON Schema can express survives.

### Hybrid

Contracts is the **single source of truth**. The OpenAPI document is **generated from it**, with its
component schemas taken from ZodSharp's export — the generated `{T}Schema.BuildJsonSchema()` is built
from the same metadata as the validator, so the document and the validators cannot drift.

.NET clients reference Contracts. Non-.NET clients generate from the document and take their
validation rules from the exported JSON Schema — for TypeScript, `z.fromJSONSchema` (Zod 4) reads it
back as Zod schemas. The `purview-zodsharp-json` and `purview-zodsharp-source-generator` skills are
authoritative on the export.

Three facts about the export that a Hybrid design has to plan for:

- **ZodSharp ships no OpenAPI integration.** It produces JSON Schema; putting that into the OpenAPI
  document — for example with a schema transformer in `Microsoft.AspNetCore.OpenApi` — is the API
  host's job. Without it, ASP.NET Core describes a value object from its CLR shape, not from its rules,
  and the document and the validators drift after all.
- **A non-.NET client still gets only what JSON Schema can express.** A value object exports as its
  underlying value, and a rule the exporter cannot represent is listed under `x-zodsharp-unsupported`
  rather than dropped. The contract test should fail when that list grows.
- **Exported objects set `additionalProperties: false`.** A client that validates a *response* against
  that schema rejects a field the server has added — so "additive changes only" is not safe until
  clients tolerate unknown response fields. Validate requests strictly and responses leniently, or the
  versioning rule below protects nobody. (The reverse also holds: System.Text.Json ignores unknown
  members, so the server accepts a request with an extra key that the schema rejects.)

## Decision procedure

1. **List the clients that exist today.** For each, record:
   - its language and runtime;
   - who owns it;
   - whether it lives in the same repository and solution;
   - whether it ships **in lockstep** with the server or on its own cadence.

   A planned client is noted, but it does not count yet.
2. **Every client is .NET, in the same solution, and ships in lockstep → Contracts.** This prevents:
   - rules drifting between client and server;
   - a second, weaker copy of the domain rules in the UI;
   - contract breaks that only surface at runtime.

   Publish OpenAPI anyway if documentation or tooling wants it, but generate it from Contracts. It is
   an output, never an input.
3. **At least one client is not .NET, or ships independently → Hybrid.** This prevents:
   - non-.NET clients hand-writing shapes and rules;
   - the .NET clients losing their value objects and rules to a generated client.

   It requires the independent-versioning rules under [Version skew](#version-skew).
4. **No .NET client exists, or the API is a surface whose clients the team does not own (public or
   partner) → OpenAPI**, still generated from the server's types. A separate Contracts project is not
   earned when the server is its only consumer.
5. **A .NET client in another repository with its own release cadence** prefers Contracts shipped as
   a package, if that team accepts the dependency. Otherwise treat it as step 3.

```text
flowchart TD
    A["List the clients that exist today"] --> C{"Every client .NET,<br/>same solution, lockstep?"}
    C -- yes --> K["Contracts<br/>OpenAPI only as an output"]
    C -- no --> B{"Any .NET client<br/>at all?"}
    B -- no --> O["OpenAPI<br/>generated from the server's types"]
    B -- yes --> D{"The only other clients are .NET<br/>in another repository,<br/>and they accept a package?"}
    D -- yes --> P["Contracts, shipped as a package"]
    D -- no --> H["Hybrid<br/>Contracts is the source, OpenAPI generated from it"]
```

Steps 3 and 4 do not overlap: a .NET client in the solution alongside a partner or non-.NET client is
step 3, because the .NET client still has value objects and rules to lose.

## Rules that follow

- **Never regenerate a C# client from your own API's OpenAPI document when that client is in the same
  solution.** It throws away value objects and rules to buy decoupling the solution does not have.
- **Never hand-maintain a parallel set of models** on either side. Two definitions of one wire shape
  disagree eventually, and nothing fails when they start to.
- **The server always validates.** Sharing validators with a client is for user experience and
  consistency. It is never a substitute for validating on the server, because the client is untrusted.
- **Moving from Contracts to Hybrid must be additive.** Because Contracts is already the source, adding
  a non-.NET client means turning on generation of the OpenAPI document and a contract test. Nothing in
  Contracts changes. Design for that, but do not build it until the client exists.

## Contracts project constraints

- **References only the domain library (value objects) and the runtime Purview packages** —
  `Purview.ValueObjects`, `Purview.ZodSharp`, `Purview.Results`. Never the server, persistence, HTTP
  clients or hosting, and never an ASP.NET Core bridge such as `Purview.ZodSharp.AspNetCore`; those
  belong to the API host. A Contracts project that references the server is a server that every client
  now ships.
- **Browser-safe when a WebAssembly client references it.** No file, network or process I/O, safe to
  trim, and no reflection-dependent behaviour. The same applies transitively to the domain library,
  because the client downloads it too. Make the build enforce it, not a reviewer — three mechanisms,
  each catching what the others cannot:
  - **Your repository's architecture policy** forbids `Microsoft.AspNetCore` packages
    (`forbiddenPackagePrefixes`) and the `Microsoft.AspNetCore.App` framework reference
    (`forbiddenFrameworkReferences`) in `Contracts` and `Core`. The playbook's own policy forbids the
    packages in `Core` only; the rest arrives with the Build SDK's planned Blazor support, so add it
    yourself until then. See `purview-architecture-testing`.
  - **`<IsAotCompatible>true</IsAotCompatible>`** on both projects turns on the trim and AOT analyzers,
    so reflection a trimmed client would break on is a warning at build time. `Purview.ValueObjects`
    and `Purview.ZodSharp` already set it.
  - **`<SupportedPlatform Include="browser" />`** makes the platform-compatibility analyzer (`CA1416`)
    flag an API that is unsupported in the browser, such as starting a process.

  The policy rules fail a test run. The two analyzers produce warnings, and the SDK does not turn on
  `TreatWarningsAsErrors` for an ordinary project — set it on `Contracts` and `Core`, or the "clean
  build has no warnings" rule is the only thing standing behind them. No analyzer proves the absence of
  every kind of I/O; what they miss is a review obligation, and the client's own tests running in the
  browser are the last check.
- **Deserialisation mode is a deliberate choice per direction.** The server reads requests through
  `Create` (strict), because the client is untrusted. Record what the client uses for responses, and
  why — strict catches a server that has drifted, and `Hydrate` trusts a server the client cannot
  verify.

## Version skew

| Shipping | Strategy | Failure it prevents | What to do |
| -- | -- | -- | -- |
| Lockstep | Contracts, internal apps | A silent failure that looks like a bug, when an old cached client talks to a new server | The server stamps its build on responses; the client prompts the user to reload when the stamps differ |
| Independent | Hybrid, OpenAPI | A deployed client breaking without warning | Additive changes only; a breaking change gets a new route version; a contract test compares the generated document against the last published one and fails on a breaking difference |

## Backend-for-Frontend

A BFF is a host, not a layer, and it is held to the same justification test as anything else.
Microsoft's [Blazor Web App with OIDC and the BFF pattern](https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=with-bff-pattern)
is the authority on the mechanics; what follows is the rules and the failures behind them.

### What earns it

- **Keeping tokens out of the browser.** The BFF signs the user in (OpenID Connect), holds the access
  and refresh tokens, and gives the browser only an authentication cookie. Script in the page — a
  compromised dependency, an XSS hole — cannot read a token that never reached it. This prevents tokens
  being stolen from browser storage.
- **It forwards with the user's access token, never its own identity.** The API validates that token
  exactly as it would any other, so authorisation and the audit trail stay per-user. A BFF calling with
  a client-credentials token turns every action into the BFF's and moves authorisation into the BFF.

### In a Blazor solution, the BFF is the host

The project that serves the WebAssembly client is the natural BFF: it already runs on the server, at
the client's origin. Do not add a separate BFF in front of it.

- **It references the client project** (`Bff → WebApp`) because it serves the client's assets. The
  client never references it; they talk over HTTP with the cookie.
- **Same origin is the point.** The client calls the BFF's own routes, the BFF forwards them to the API
  (with YARP's `MapForwarder`, for example), and there is no CORS configuration and no third-party
  cookie.
- **The client shares Contracts with the API directly.** The WebAssembly client references Contracts,
  sends Contracts types, and runs the generated validators. The BFF in the middle passes the bytes
  through untouched.

### Rules that come with a cookie

Moving the credential into a cookie trades token theft for a different attack, and the BFF has to
close it:

- **Every forwarded call requires an anti-forgery signal.** The browser attaches the cookie to any
  request to the BFF's origin, including one a hostile page triggers. Require a custom header the
  client always sends, which a cross-site form cannot set, and reject calls without it — Duende.BFF's
  `X-CSRF: 1` is the established form. `SameSite` narrows the attack; it does not replace the check.
  This prevents cross-site request forgery.
- **The cookie is `HttpOnly`, `Secure` and `SameSite=Strict` or `Lax`.** `HttpOnly` is what keeps
  script from reading it; without it the BFF protects nothing.
- **Data Protection keys are persisted and shared** when the BFF runs on more than one instance or
  survives a restart. The cookie is encrypted with them, so per-instance keys sign users out at random.
- **Refresh happens on the server.** The BFF refreshes the access token before it expires and signs
  the user out when refresh fails. The client never sees either token.
- **A large session moves to a server-side store.** Tokens kept inside the cookie make it large enough
  to be split across several cookies, and the session cannot be revoked server-side. Use a ticket store
  when either matters.

### What it may reference

- **A BFF that only forwards does not reference Contracts** and never reads payloads. Reading a payload
  it does not own makes it a second consumer that has to change in lockstep for no benefit.
- **Prerendering is the common exception.** If the BFF prerenders client components that load data,
  it must run those data calls on the server, and it references Contracts to do so. That is a real
  reason; turning prerendering off for those components is the alternative, and the choice should be
  written down.
- **A BFF that combines several API calls into one response owns its own contract** for those
  responses, chosen with this same procedure. That is a separate justification. The forwarding-only
  test — "a service layer that forwards" is not earned — still applies to everything else it does.
- **One BFF per client kind, not one per screen.** Web and mobile may each get one when their sign-in
  or response needs differ.

**None of this is enforced yet.** The architecture policy has no BFF responsibility; Blazor and BFF
support is planned for a future Build SDK release. Until then every rule in this section is a review
obligation, and a repository that adds a BFF project records it in its own policy — see
`project-responsibilities.md`. Even once it is in the policy, "Contracts only when it prerenders or
combines" stays a review obligation, because a dependency matrix cannot express a condition.

## Worked examples

| Clients today | Strategy |
| -- | -- |
| Blazor WebAssembly app, same solution, lockstep, signed in through a BFF | Contracts, referenced by the API and the WebAssembly client. The BFF is the host that serves the client, holds the tokens and forwards with the user's token; it references Contracts only if it prerenders data. OpenAPI generated for documentation. |
| Blazor WebAssembly app plus a React Native mobile app on its own release cadence | Hybrid. Mobile generates from the document and takes Zod rules from the exported schema. Additive-only versioning. A mobile BFF if sign-in differs. |
| Partner integrations only | OpenAPI, generated from the server's types. No Contracts project. |

## See also

- [Contracts and Data Exchange](https://github.com/purview-dev/engineering-playbook/blob/main/docs/wiki/Contracts-And-Data-Exchange.md)
  — `Create` and `Hydrate`, and the schema as the source of truth for a wire shape
- [zodsharp](https://github.com/purview-dev/zodsharp) — authoritative on `[ZodSchema]` and JSON Schema
  export
