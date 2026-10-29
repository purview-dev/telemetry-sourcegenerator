---
name: purview-engineering-principles
description: >-
  Use when setting up, reviewing or rationalising a .NET repository against the Purview engineering
  conventions - convention over configuration, derived identity, architecture as tests, warnings as
  errors, proving consumer-facing claims, documentation ownership, test data builders, agent
  legibility, and tiering verification by risk. Each rule names the failure it prevents and whether a
  build or a reviewer enforces it.
metadata:
  product: Purview engineering playbook
  repository: purview-dev/engineering-playbook
---

# Purview engineering principles

Ten rules, each with the failure it prevents and the mechanism that makes it true. The distinction
that matters throughout: **a convention a build can check is checked by the build; a convention only
a reviewer can check is labelled a review obligation and never described as enforced.** A rule that
claims enforcement it does not have is worse than no rule, because people stop looking.

## 1. Convention over configuration

A repository declares as little as possible. One MSBuild SDK supplies the defaults, and a project
file is short because the SDK already knows what a project of that shape needs.

Configuration copied between repositories diverges. When the rule lives in one package, fixing it
once fixes it everywhere; when it lives in eleven project files, it is fixed eleven times — or, more
often, once.

*Enforced by:* `<Import Sdk="Purview.BuildSdk" Project="Sdk.props" />` in a single
`Directory.Build.props`. The SDK detects project type from the filename and the `Sdk` attribute.

*At your scale:* a single-project repository still benefits. The import is two lines and removes the
target framework, language version, nullable context, analyzer set and package metadata.

## 2. Identity is derived, never repeated

`RootNamespace` comes from `$(NamespacePrefix).$(ProjectName)`, and `AssemblyName` follows the
resolved `RootNamespace`. A name appears once — in the folder and file name — and everything else is
computed.

A hand-typed namespace drifts from the assembly it lives in, and the drift is invisible until
something reflects over types: an architecture test, a serializer, a DI scan.

*Enforced by:* the SDK during `.props` evaluation, plus a build-time check
(`PurviewProjectFileNameMismatch`) when a `.csproj` filename does not match its parent directory.

*At your scale:* always. It costs nothing and removes a class of bug.

## 3. Architecture is a test, not a diagram

The dependencies a layer may have are asserted by tests that read the compiled assemblies. A
`ProjectReference` pointing the wrong way fails the build.

A diagram is a claim about the past. Dependency direction erodes one convenient reference at a time,
and every such reference looks reasonable in isolation.

*Enforced by:* a test project that references every layer and inspects their metadata references.

*At your scale:* small repositories can skip it. Adopt it the moment a **second layer** exists —
that is when the wrong direction first becomes possible.

## 4. The build is the only authority

Style is enforced by analyzers and `EnforceCodeStyleInBuild`, and the SDK's own style policy is
re-validated on every compile, so a repository cannot quietly downgrade a rule.

*At your scale:* always. If a rule matters and cannot be checked, say so plainly.

## 5. Warnings are not acceptable output

A clean build has no warnings. One that will never be fixed is either fixed, suppressed *where it
occurs* with a stated justification, or turned off deliberately.

Warnings are a budget. A build that emits twenty is a build where the twenty-first is invisible — and
the twenty-first is the one that mattered.

*Enforced by:* severity, partly. The SDK's shipped `.editorconfig` sets many rules to `error`. The
rest is review, backed by the pipeline running `dotnet build` with the repository's configuration.

*At your scale:* a small repository can tolerate `warning`; a large one cannot. Decide deliberately
rather than by accumulation.

## 6. Every claim about a consumer is proved by a consumer

When a package documents how it must be used — a target framework, an asset, a guard, a workaround —
something in the repository builds that documented consumer and asserts the documented outcome.

Consumer-facing documentation is the part of a library least likely to be exercised by its own tests,
because **the library's own build is not a consumer**. It drifts silently and fails in somebody
else's repository.

*Enforced by:* a verification script that packs, then builds throwaway consumer projects against the
produced packages, asserting the happy path *and* the documented guard errors. `purview-dev/containers`
is the worked example (`just verify-consumers`), deliberately excluded from the default unit filter
because it restores from nuget.org.

*At your scale:* only once a package is consumed outside its own repository. Before that, the
repository's own tests already are the consumer.

## 7. Documentation is a build artefact of the repository

The wiki lives in the repository it describes, in `docs/wiki`, and the repository owns its
`mkdocs.yml`, its navigation and its `edit_uri`.

Documentation that lives elsewhere is updated elsewhere. A page with no owner and no review path is
wrong within one release.

*Enforced by:* review, plus the navigation file — a page absent from `_Sidebar.md` is not published.

*At your scale:* always. Even a repository with one page should own that page.

## 8. Everything a test needs is built by one builder

Domain content in a test comes from `TestBuilder`. A test never constructs a domain object inline,
and it overrides **only** the value it is proving.

Inline construction encodes a type's shape into every test that touches it, so one constructor change
becomes a repository-wide edit. It also hides which values the test actually depends on, because
every value looks deliberate.

*Enforced by:* review.

*At your scale:* adopt from the first test. Retrofitting is a mechanical edit across every test file,
and exactly the kind of edit that is postponed forever.

## 9. An agent is a reader with no memory

A repository states its purpose, its layout and its conventions in `AGENTS.md`. **Nothing important
is discoverable only by reading the code.**

A coding agent has the repository and nothing else — no colleague to ask, no institutional memory, no
intuition about which of two plausible patterns is intended. It will confidently generalise from
whatever it finds first. That is also every new-joiner failure mode; the agent just makes the cost
visible sooner, because an agent does not ask.

*Enforced by:* review, plus publishing skills so agent-facing guidance tracks the library version
rather than a repository's memory of it.

*At your scale:* always. `AGENTS.md` is cheaper than the questions it prevents.

## 10. A change is verified in proportion to its risk

Checks are tiered, and a repository declares which tiers its pipeline runs. A pull request runs the
fast tier; the slow tier runs deliberately, locally or on a dedicated runner.

A pipeline that runs everything on every pull request gets made optional. One that runs too little
gets ignored. Tiering is what keeps the gate credible.

*Enforced by:* the pipeline's `TestFilter` and the test project's own **name** — the SDK categorises
a `*UnitTests` project as `Unit`, so the tier is a property of the project rather than of a
hand-written attribute.

*At your scale:* one tier is fine until the slow tests exist. Then split by project name, not by
attribute.

## Applying these in review

Three questions catch most of it:

1. **Is this configuration, or is it a default the SDK should own?** If two repositories would copy
   it, it belongs in the SDK.
2. **Is this rule enforced, or merely stated?** If stated, say so where it is stated.
3. **Could an agent — or a new joiner — find this without being told?** If not, it goes in
   `AGENTS.md` or the wiki.

## Related skills

`purview-architecture` for project-shape decisions, `purview-architecture-testing` for turning a
boundary into a failing build, `purview-telemetry` and `purview-persistence` for the per-concern
guidance.
