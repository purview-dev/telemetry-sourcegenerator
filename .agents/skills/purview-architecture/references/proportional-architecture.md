# Proportional architecture

Structure is a cost paid now against a failure expected later. Proportional architecture is the
practice of only paying when the failure is named, present, and more expensive than the structure.

## The justification test

Before adding a project, an interface, a layer or a pattern, answer in one sentence:

> **What failure does this prevent in *this* codebase, and has it happened or is it certain to?**

If the sentence needs the words "in case", "later", "when we scale", or "it's good practice", the
structure is not yet justified. Write the simpler thing. The simpler thing is also the thing that is
cheapest to change when the failure finally does arrive.

This is not an argument against structure. It is an argument against *unfalsifiable* structure.

## By scale

### Small — a utility, CLI, prototype, simple API, limited-scope application

- One project. A second only when there is a test project, or when a genuinely reusable library
  falls out.
- Call the database, the API or the file system directly from the code that needs it.
- Validate at the edge, once.
- Logging from the first commit; traces and metrics when there is something to correlate.

Do not add: a repository interface over `DbContext`, a service layer that forwards, a mapping layer
between two near-identical shapes, an interface with one implementation and no test double need
(`Purview.Containers` and TUnit.Mocks remove most of that need), a `Domain`/`Application`/
`Infrastructure` split.

### Medium — several functional areas, some integrations, moderate domain complexity

- Introduce a boundary where you can name what it separates. Usually that is domain rules from the
  host, so a domain project and an application host.
- An abstraction over an external dependency earns its place when the dependency is genuinely
  swappable, or when testing against it is otherwise painful. "Swappable in principle" is not
  swappable.
- Dependency instrumentation, trace propagation across the integrations, metrics on the operations
  someone would be paged about.

Do not add: CQRS unless reads and writes have actually diverged in shape, lifetime or scaling;
event sourcing unless the history *is* the product; a second infrastructure implementation that has
no second consumer.

### Large — complex domain, multiple services or teams, substantial operational requirements

- Modularity along ownership lines, because the real constraint is people, not code.
- Independent infrastructure implementations where they are genuinely independent.
- Boundaries made enforceable rather than documented — a dependency-direction rule that a wrong
  `ProjectReference` fails the build. See the playbook's Architecture-As-Tests page.
- End-to-end tracing, standardised resource attributes, SLIs, retention and sampling policy.

Even here, the justification test holds per decision. "Large" licenses the *option* to add
structure, not the obligation to add all of it on day one.

## Patterns and the conditions that earn them

| Pattern | Earned when | Not earned by |
| -- | -- | -- |
| Repository over EF Core | You must swap the store, or the `DbContext` API genuinely leaks into domain decisions | Testability — EF Core's in-memory and container-backed testing already covers it |
| CQRS | Read and write models have diverged in shape or scaling needs | A desire for symmetry with a reference architecture |
| Event sourcing | The sequence of changes is the product, or audit is a hard requirement | "We might want history" |
| Mediator / in-process bus | Many handlers, genuinely decoupled registration | Three call sites that could be method calls |
| Mapping layer | Two shapes that must diverge and are owned separately | Two shapes that are identical today |
| Extra DI indirection | A real second implementation, or a composition root that must vary | Inversion for its own sake |
| Generic infrastructure framework | Three or more concrete uses exist now | One use and an expectation of more |

## Evolving incrementally

The honest sequence is: write the direct implementation; let the second caller arrive; extract the
boundary the second caller revealed. Each step is small, reversible and informed.

The dishonest sequence is: predict the second caller; build the boundary; never get the second
caller; maintain the boundary forever. The cost is invisible because it is spread across every
future change.

When you do extract, extract along the seam the code showed you — not the seam the diagram
predicted.

## What this does not license

Proportional is not an excuse for:

- Skipping tests, or skipping telemetry.
- Leaving validation duplicated across entry points.
- Letting domain rules live in the UI or in the database.
- Two sources of truth for the same fact.

Those are not structure bought in advance; they are defects. Simplicity means fewer moving parts,
not fewer guarantees.
