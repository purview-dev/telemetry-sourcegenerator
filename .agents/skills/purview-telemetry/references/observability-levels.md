# Observability levels

Instrumentation is constant across these levels. Export, infrastructure and operational practice are
what scale. Read this after establishing project complexity — see `purview-architecture`.

## Small

*A utility, CLI, prototype, simple API or limited-scope application.*

**Do:**

- Structured logging with parameters as fields.
- Exceptions recorded with enough context to reproduce.
- An `ActivitySource` around the unit of work, so a failure has a shape and a duration.
- Console export, or a single OTLP endpoint when one already exists.
- Log level from configuration, so a user can raise it without a rebuild.

**Do not:**

- Stand up a collector.
- Adopt a centralised monitoring platform.
- Build distributed tracing infrastructure for a single process.
- Create dashboards nobody reads.
- Write a custom exporter.

The test: a user reports a failure and you can ask for one log output that tells you what happened.

## Medium

*Several functional areas, integrations, moderate domain complexity.*

Everything above, plus:

- **Dependency instrumentation** — HTTP clients, EF Core, messaging. These are where latency and
  failure actually live, and the instrumentation is a package reference, not code.
- **Trace propagation** across every integration boundary, so a trace survives the process hop. W3C
  `traceparent` by default; do not invent a correlation header.
- **Application metrics** for the operations someone would notice: request rate, error rate, duration,
  queue depth. Prefer histograms over averages.
- **OTLP export** to a real backend.
- **Health checks**, distinguishing liveness from readiness.
- **A deliberate sampling decision.** Head sampling at a fixed ratio is fine; the point is that it was
  chosen rather than defaulted into.

Common mistake at this level: adding metrics for everything measurable instead of the few that drive
a decision. A metric nobody alerts or decides on is cost without signal.

## Large

*Complex domain, multiple services or teams, substantial operational requirements.*

Everything above, plus:

- **End-to-end distributed tracing**, including asynchronous hops — message producers and consumers
  linked, not orphaned.
- **Standardised resource attributes** — `service.name`, `service.version`, `deployment.environment`
  — applied uniformly so cross-service queries work at all. Follow OpenTelemetry semantic conventions
  rather than inventing names.
- **Operational metrics** beyond application ones: saturation, dependency health, cost drivers.
- **Dashboards** owned by the team that gets paged.
- **Alerting** on symptoms users feel, not on causes. Alert on error rate and latency; investigate
  with CPU and queue depth.
- **Service-level indicators** derived from the telemetry, with objectives agreed with whoever cares.
- **Retention and sampling policy**, documented and costed. Tail sampling if error traces must always
  survive.

## Signal selection

| Signal | Add when | Warning |
| -- | -- | -- |
| Logs | Always | Unstructured logs are not telemetry; they are strings |
| Traces | Work crosses a process, a dependency or an async boundary | A span per method produces noise, not insight |
| Metrics | Someone would alert on, or decide from, a rate or duration | High-cardinality attributes (user id, request id) are a cost incident |

Put high-cardinality context on spans and logs, never on metric dimensions.

## Sensitive information

At every level, and without exception:

- No credentials, tokens, keys or connection strings in messages, attributes or baggage.
- No personal data beyond what the system is entitled to process — and where an identifier is
  necessary, prefer a pseudonymous one.
- Remember that **baggage propagates across service boundaries**; treat it as the most public place
  in your telemetry.
- Redact at the point of emission. A redaction step in the pipeline is a backstop, not a control.

## Export configuration

The host owns this. Keep the endpoint, protocol, sampling ratio and resource attributes in
configuration, not in code, so the same build runs in every environment.

Libraries contribute names and signals; they never contribute configuration.
