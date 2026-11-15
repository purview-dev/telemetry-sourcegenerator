---
name: github-repo-metadata
description: How GitHub repository metadata feeds the website (topics become tags, description backs shortDescription, archived/description/discussions drive status and links) and how to inspect and correct it with gh.
category: purview-dev-website
roles:
    - catalogue
    - metadata
tags:
    - github
    - topics
    - tags
    - nuget
---

# github-repo-metadata Skill

The manifest is curated, but several site surfaces read **live repository metadata** that is fetched
at build time into `src/.cache/releases/releases.json` (`fetchGitHubRepo`, `fetchNuGetIndex`,
`fetchNuGetSearch` in `src/src/lib/releases/github.ts`). NuGet **version numbers** are the one
exception: the browser re-checks them against the flat-container index and stores the result
(ADR 0008, `src/src/lib/releases/live.ts`), so a published version appears without a redeploy.
Everything else in the table below is build-time.

## What comes from where

| Site surface | Source | Notes |
| --- | --- | --- |
| Tag chips (cards, docs portal, docs-page front matter `tags`) | GitHub **topics** | `projectRepoEnrichment()` in `src/src/lib/releases/repo.ts` |
| `shortDescription` fallback | GitHub **description** | Used only when the manifest value is empty |
| "supplemental" description on the project page | GitHub description | Shown when it differs from the manifest description |
| Successor/archived messaging | GitHub **archived** flag + `status`/`supersededBy` | |
| Version numbers, release dates, badges | GitHub releases + NuGet flat-container index | |
| Downloads / deprecated / listed | NuGet search entry | Rendered on the release tables |
| Discussions link | repository discussion settings (detected) | `hasDiscussions` from the cache; a record's `discussions` is only an override |

`search` corpora on `/projects/` deliberately include the topics, so missing topics make a project
undiscoverable by its domain keywords.

## Inspecting

```shell
gh api repos/purview-dev/<repo> --jq '{description, topics, archived, default_branch, has_discussions, homepage, visibility}'
gh api "repos/purview-dev/<repo>/releases?per_page=10" --jq '.[] | {tag_name, prerelease, draft}'
curl.exe -s "https://api.nuget.org/v3-flatcontainer/<packageid>/index.json"
curl.exe -s "https://azuresearch-usnc.nuget.org/query?q=packageid:<packageid>&take=1"
```

## Correcting

Topics are replaced wholesale (PUT), so send the complete set:

```shell
echo '{"names":["csharp","dotnet","roslyn","source-generators","telemetry"]}' > topics.json
gh api repos/purview-dev/<repo>/topics --method PUT --input topics.json
rm topics.json
```

`gh` also accepts repeated array fields: `gh api repos/<owner>/<repo>/topics --method PUT -f 'names[]=csharp' -f 'names[]=dotnet'`.

Other metadata (`description`, `homepage`, `has_discussions`) is set in the repository settings or
with `gh api repos/<owner>/<repo> --method PATCH -f description='...' -f homepage='...'`. Enabling
Discussions on an organisation repository requires organisation permission; report it as an
`upstream` item when you cannot do it.

## Tag taxonomy guidance

- Lowercase, hyphenated, and specific: `event-sourcing`, `source-generators`, `zero-allocation`,
  `msbuild-sdk`, `dotnet-aspire`, `csharp`, `roslyn`.
- 6–15 topics is the useful range: enough to describe the domain, few enough that the card's first
  four chips are meaningful (cards render `tags.slice(0, 4)`).
- Prefer ecosystem-standard names (`dotnet`, `csharp`, `nuget`, `opentelemetry`) so cross-repo
  searches behave predictably.
- Topics are the **only** way tags reach the site. There is no `tags` field in a catalogue record — do
  not add one.

## Stability signals to check against `status`

| Signal | Implication |
| --- | --- |
| `archived: true` | `status` must be `archived` (and `supersededBy` when a successor exists) |
| No stable release, only `-prerelease.N` tags | `preview` at most |
| Latest NuGet version is stable and listed | `stable` is allowed |
| The README/repository presents the project as an experiment | Set `experimental: true` (it does not replace `status`, which stays the release channel) |
| NuGet `deprecated: true` | Report as `drift`/`upstream`; a deprecated package should not front a `stable` project without explanation |
| `default_branch` is not `main` | Docs aggregation produces nothing — a blocker for any project with `docs` |

Note that the organisation's Changesets automation publishes `vX.Y.Z-prerelease.N` tags **without**
setting GitHub's `prerelease` flag, so version selection is semver-based; do not judge the channel
from the GitHub `prerelease` boolean alone.
