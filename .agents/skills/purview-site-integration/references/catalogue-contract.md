# The catalogue entry — what to hand the site maintainers

The entry lives in `purview-dev/purview-dev` at `src/src/data/projects/<id>.yml` and is validated by the
site's schema, so its vocabulary is fixed. Collect the answers below from this repository and propose
them; the maintainers (or the site's onboarding agent) turn them into the entry.

## Identity

| Field | What to provide |
| -- | -- |
| `id` | lower-case slug, normally the repository name without `purview-` (`unions`, `build-sdk`). It is the URL: `/projects/<id>/`, `/docs/<id>/` |
| `name` | display name; must slugify to `id` when the project has docs (`Build SDK` → `build-sdk`). Not prefixed with "Purview" |
| `repository` | `purview-dev/<repo>` |
| `category` | one of `application-framework`, `validation`, `observability`, `source-generation`, `aspire`, `build-tooling`, `developer-tooling` |
| `status` | `stable` (a stable release is published), `preview` (prereleases only), `archived` |
| `experimental` | `true` when the API and packaging may change without notice |

## The words

Write these for a reader deciding whether to use the project. Backticks become inline code on the site.

| Field | Shape |
| -- | -- |
| `shortDescription` | one sentence; the card text |
| `description` | 2–4 sentences: the problem and what the project does about it |
| `origin` | why it became a reusable project rather than code in one application |
| `useWhen` / `avoidWhen` | the honest fit, including when something simpler is better |
| `useCases` | at least one, each: `audience` (`developer`, `team-lead`, `architect`, `contributor`), `title`, `scenario`, optional `code` + `language`, `outcome`, optional `evidence`, optional `docsPage` |
| `acknowledgments` | upstream work the project builds on: `name`, `url`, `description` |
| `related` | ids of related Purview projects |

A use case is concrete: a real situation, real code, a measurable outcome, and evidence that can be
checked (a test, a benchmark, a diagnostic id). `docsPage` is a **single-segment** slug of a docs page
at the docs root — so a page used as a deep link stays at the root of the tree.

## Pointers

| Field | What to provide |
| -- | -- |
| `docs.source` | `github-path` (preferred), `wiki`, or `readme` |
| `docs.path` | the docs folder, e.g. `docs/wiki` |
| `docs.rootPage` | the landing page, e.g. `Home.md` or `Getting-Started.md` |
| `docs.exclude` | base file names that are not pages, e.g. `_Sidebar.md` |
| `docs.channels` | only when `main` runs ahead of the last release: `live: { ref: latest-stable }`, `prerelease: { ref: default }` |
| `packages` | each package id, its `registry` (`nuget` default, or `npm`), a one-line `description`, and which is `primary` |
| `install` | for NuGet: `nuget` (a `PackageReference`), `msbuild-sdk`, or `dotnet-tool` |
| `targetFrameworks` | what the packages actually ship (`net8.0`, `net10.0`, `netstandard2.0`) — the site checks them against NuGet |
| `skills` | usually omitted: every `skills/*/SKILL.md` is discovered and published. `false` opts out; a list of `{ name, path?, description? }` overrides discovery |
| `assets` | `{ path, output, description? }` for files the site should serve, e.g. a JSON schema at `/schemas/<name>.json` |

Redirects for moved pages belong in this repository's `_redirects.yml`, not in the entry.

## A template to fill in

```yaml
id: <id>
name: <Name>
shortDescription: <one sentence>
description: >-
  <2–4 sentences>
origin: >-
  <why it became a project>
useWhen: >-
  <when it fits>
avoidWhen: >-
  <when it does not>
repository: purview-dev/<repo>
category: <category>
status: <stable|preview>
docs:
  source: github-path
  path: docs/wiki
  rootPage: Home.md
  exclude:
    - _Sidebar.md
packages:
  - id: <Package.Id>
    description: <one line>
    primary: true
useCases:
  - audience: developer
    title: <outcome-first title>
    scenario: >-
      <the situation>
    code: |
      <minimal real code>
    language: csharp
    outcome: >-
      <what the reader gets>
    evidence: >-
      <what proves it>
    docsPage: <root-page-slug>
```
