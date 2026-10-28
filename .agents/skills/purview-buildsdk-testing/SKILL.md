---
name: purview-buildsdk-testing
description: Write and run tests in a repository that uses Purview.BuildSdk. Use when authoring or refactoring TUnit tests (AAA structure, Subject_Scenario_Expectation naming, awaited assertions, CancellationToken), when choosing a test project's framework packages, and when running or filtering tests - the SDK wires Microsoft.Testing.Platform, so `dotnet test --filter` reports "Zero tests ran" and `--treenode-filter` is the one that works.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Testing in a Purview.BuildSdk repository

Two things to know before anything else:

1. A test project is detected by **name**, and detection is what wires the runner. See
   `purview-buildsdk-project-layout` if the project is new or being moved.
2. The runner is **Microsoft.Testing.Platform (MTP)**, not VSTest. `--filter` does nothing useful.

## What a test project gets automatically

With the defaults (`TestingFramework=TUnit`, `SubstituteFramework=TUnitMocks`,
`TestDataFramework=Bogus`), a detected test project receives:

- `TUnit`, `TUnit.Mocks`, `Bogus`
- Microsoft.Testing.Platform integration
- `CollectCoverage=true`
- test-friendly `NoWarn` defaults (`PurviewTestContextNoWarn`), while keeping the strict style
  contract — `IDE0040`, field naming, formatting, `IDE1006`
- `IsPackable=false`, `IsPublishable=false`

Add nothing else unless the project's purpose demands it. Two that stay explicit:

- `TUnit.Aspire` — Aspire lifecycle and AppHost-backed integration tests
- `Purview.Containers` — container-backed integration tests. One API over Microsoft WSL Containers
  (no Docker install needed on Windows) or Docker, with typed modules for SQL Server, PostgreSQL,
  Redis, MySQL, RabbitMQ, Azurite and NATS. Experimental and prerelease, so pin the version.
  Reference `Testcontainers` directly only where no typed module exists yet.

## Authoring rules

- Use TUnit (`TUnit.Core`, `TUnit.Assertions`). Do not introduce xUnit, NUnit, MSTest or
  FluentAssertions unless explicitly asked.
- Every test method carries `[Test]`.
- **Every assertion is awaited**: `await Assert.That(actual).IsEqualTo(expected);`. An un-awaited
  assertion silently passes.
- Arrange / Act / Assert, with the `// Arrange`, `// Act`, `// Assert` comments present.
- Test classes are `{SubjectName}Tests`, in the **same namespace** as the subject.
- Test methods are `{SubjectOrMemberUnderTest}_{Scenario}_{Expectation}`. Use `ctor` as the subject
  for constructor tests. Methods, constructors, properties, operators, conversions and validation
  hooks are all valid subjects.
- When any method called in the test body accepts a `CancellationToken`, take
  `CancellationToken cancellationToken` as the **last parameter** and pass it through.

```csharp
[Test]
public async Task ProcessAsync_GivenAnEmptyBatch_ReturnsZero(CancellationToken cancellationToken)
{
    // Arrange
    var sut = new BatchProcessor();

    // Act
    var result = await sut.ProcessAsync([], cancellationToken);

    // Assert
    await Assert.That(result).IsEqualTo(0);
}
```

For a suite that is not subject-based — a build, generator, workflow, package or full-system suite —
a broader, more truthful class name is better than a forced `{Subject}Tests`. Use TUnit display
names and categories to keep it navigable.

When refactoring existing tests, bring them into compliance with all of the above while preserving
test intent.

## Categories

The detected test type becomes the baseline category automatically — `Api.IntegrationTests` yields
`Category=Integration`. Add further categories when they make a large suite easier to navigate;
they are what `--treenode-filter` property predicates select on.

## Running tests

```bash
dotnet test                                            # the whole suite
dotnet test --treenode-filter "/*/*/LoginTests/*"      # one class
dotnet test <project> --treenode-filter "/*/*/*/*[Category=Unit]"
```

Prefer the repository's own task (`just test`, or equivalent) for a full run.

The filter path is `/<Assembly>/<Namespace>/<Class>/<Test>`, with `*` wildcards, `=`/`!=` property
predicates, `&`/`|` inside a single bracket group, and `**` only at the end. The full grammar, the
`--` separator rules across SDK versions, and the zero-tests checklist are in
[references/treenode-filter.md](references/treenode-filter.md).

### "Zero tests ran"

In order:

1. Are you using `--treenode-filter` and not `--filter`? This is the cause most of the time.
2. Try `dotnet test --treenode-filter "..."`; on an older SDK use `dotnet test -- --treenode-filter "..."`.
3. Broaden to `/*/*/*/*`, then narrow one segment at a time.
4. Does the project actually contain `[Test]` methods?
5. Is the project **detected** as a test project? A name without the dot — `UnitTests` rather than
   `Api.UnitTests` — gets no runner at all. Check with
   `dotnet msbuild <project> -getProperty:IsTestProject`.
6. Are all property predicates inside one bracket group, `[(A=1)&(B=2)]`?

## Cross-platform requirement

CI runs the full suite on Linux, so a test that only passes on Windows is a failing change.

- Never hardcode a platform-specific path — no `C:\...`, no backslash-only separators, no
  case-insensitive path assumptions.
- Build paths with `Path.Combine`, `Path.GetTempPath()`, `Path.DirectorySeparatorChar`,
  `Path.GetFullPath()`.
- Feed production path-math code only native-format paths. Normalise a fixed Windows-style literal
  to the current platform before passing it in.
- Do not rely on a case-insensitive filesystem, a `C:` drive, or trailing-separator behaviour.
- Keep `SyntaxTree` file paths and `build_property.*` values consistent and native when writing
  analyzer tests.
