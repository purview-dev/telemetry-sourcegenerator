---
name: use-case-authoring
description: Use when writing or reviewing useCases in the catalogue records — the ADR 0002 rules for audience-tagged, concrete, evidence-backed examples with short code snippets and resolvable documentation deep links.
category: purview-dev-website
roles:
    - catalogue
    - content
tags:
    - use-cases
    - adr-0002
    - documentation
---

# use-case-authoring Skill

Use cases are the "what does it actually do for me" evidence on the home page, on each project page,
and on `/use-cases/`. They are authored in the catalogue records (`src/src/data/projects/<id>.yml`) and validated by the manifest
schema. The governing decision is `docs/decisions/0002-concrete-use-cases.md`.

## Shape

```yaml
    useCases:
      - audience: developer        # developer | team-lead | architect | contributor
        title: Tracing, logging, and metrics from a single interface
        scenario: >-               # the situation the reader is in
          You want OpenTelemetry activities, structured logs, and metrics around an order service,
          but you do not want to hand-write the ActivitySource, ILogger, and Meter wiring.
        code: |                    # short snippet; `language` becomes required
          [ActivitySource]
          [Logger]
          [Meter]
          public interface IOrderServiceTelemetry
          {
              [Activity]
              [Info]
              [AutoCounter]
              Activity? PlacingOrder(int orderId, [Baggage] string region);
          }
        language: csharp
        outcome: >-                # what the reader gets back
          The generator emits the implementation and an AddOrderServiceTelemetry() DI extension.
        evidence: >-               # a fact, not a claim
          One interface replaces the ActivitySource, ILogger, and Meter boilerplate repeated per service.
        docsPage: getting-started  # slug of an aggregated page under /docs/<id>/
```

## Rules

1. **Audience is from a fixed vocabulary** — `developer`, `team-lead`, `architect`, `contributor`.
   Pick the person who would actually read this example; do not tag everything `developer`.
2. **At least one use case per non-archived project**, and no project should have a single
   `developer` example when the project also has an adoption/architecture story worth telling
   (an audit `polish` finding). Do not pad: two or three strong examples beat six weak ones.
3. **`code` requires `language`.** The snippet is highlighted at build time by Shiki; use a real
   language id (`csharp`, `json`, `xml`, `yaml`, `bash`, `typescript`).
4. **Snippets are short and point at the docs for depth.** Long walkthroughs belong in the
   aggregated documentation. The card links to `docsPage`.
5. **`docsPage` must be a lowercase `[a-z0-9-]` slug** and must resolve to a real aggregated page
   (`/docs/<project-id>/<docsPage>/`). The schema rejects other shapes, the unit tests reject a
   `docsPage` on a project without a `docs` configuration, and the post-build link crawl
   (`just check-links`) fails on a dead link. Slug the source file name: `SQL Server Guide.md` →
   `sql-server-guide`.
6. **`evidence` must be a fact.** Good: a before/after, a count of generated members, a measured
   allocation property, a generated-output excerpt. Bad: "very fast", "easy to use", "saves time".
7. **`scenario` is a situation, `outcome` is the result.** The pair should let a reader decide in ten
   seconds whether the example matches their problem.
8. **Keep the language factual** — no marketing superlatives, no invented benchmarks, no claims that
   cannot be traced to the repository or the aggregated docs.

## Where they render

- Home page ("What it looks like in practice") — server-rendered, no island.
- Project page (`/projects/<id>/`) — the use-case section with highlighted code.
- `/use-cases/` — the full index, filterable by audience via one Preact island; the page is usable
  without JavaScript and each card carries `data-usecase-audience` and a search corpus.

## Verification

```shell
bun run test            # manifest/use-case unit tests (audience, language, docsPage rules)
just check-projects     # deterministic coverage and docsPage-resolution checks
just build && just check-links   # every rendered docs deep link resolves in dist/
```
