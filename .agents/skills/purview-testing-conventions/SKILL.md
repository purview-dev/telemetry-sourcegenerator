---
name: purview-testing-conventions
description: >-
  Use when writing or reviewing tests in a .NET repository following the Purview conventions - TUnit
  and TUnit.Mocks, the AAA structure and naming, placing a test in the namespace of the class under
  test, CreateXXX construction helpers, building domain content through TestBuilder with Bogus, the
  assertion surface, parameterised tests, which tier a test project belongs to, BDTest for
  acceptance tests, and Purview.Containers for a suite that needs a real database or queue.
metadata:
  product: Purview engineering playbook
  repository: purview-dev/engineering-playbook
---

# Testing conventions

| Concern | The rule |
| --- | --- |
| Framework | **TUnit**. Not xUnit, NUnit or MSTest. |
| Substitutes | **TUnit.Mocks**. Not NSubstitute or Moq. |
| Test data | **Bogus**, reached through `TestBuilder`. |
| Acceptance tests | **BDTest** over TUnit, via `BDTest.TUnit`. Not Gherkin, SpecFlow or Reqnroll. |
| A real dependency | **`Purview.Containers`**. Not Testcontainers directly, never a shared instance. |
| Structure | Explicit `// Arrange`, `// Act`, `// Assert` — Given/When/Then in an acceptance test. |
| Naming | `{SubjectUnderTest}_{Scenario}_{Expectation}`, and `async Task`. |
| Placement | One `{Class}Tests` class per class under test, **in the namespace of the class under test**. |
| Construction | Dependencies and the system under test come from `CreateXXX` helpers with optional parameters. |
| Async | The async overload wherever the API offers one — in the test **and in every helper it calls**. Convert a sync helper, or state why it cannot be. |
| Cancellation | If any method the test calls accepts a `CancellationToken`, the test's **last parameter** is one, forwarded to every call that accepts one. |
| Domain content | Built through `TestBuilder`. Never inline. |

## Why these, specifically

**One framework.** The SDK wires TUnit into any project whose name ends in a test suffix — that
wiring is why a test project file is empty. A second framework means every project file grows a
conditional and the pipeline grows a second discovery path. That is a rule about the **runner**, not
the style of a test — `BDTest.TUnit` is an adapter *over* TUnit, so an acceptance suite does not break
it.

**Same namespace as the subject.** The test reads as part of the thing it describes, needs no `using`
to reach the type, and **cannot silently test a different type with the same name**.

**`CreateXXX` helpers.** A test body that constructs its own dependencies buries the assertion under
setup. A helper with optional parameters says what the test changed and defaults everything else:

```csharp
static ConventionCatalogue CreateCatalogue(IConventionRepository? repository = null) =>
    new(repository ?? CreateRepository().Object);
```

**`TestBuilder`.** Inline construction encodes a domain type's shape into every test that touches it,
so one constructor change becomes a repository-wide edit. It also destroys the reader's signal about
which values the test depends on, because every literal looks deliberate.

## A test

```csharp
using Acme.Builders;

namespace Acme.Domain;

public sealed class CustomerIdTests
{
    [Test]
    public async Task Create_GivenLowercaseSlug_ReturnsTheValue()
    {
        // Arrange
        var id = TestBuilder.CustomerId("build-enforced");

        // Act
        var value = id.Value;

        // Assert
        await Assert.That(value).IsEqualTo("build-enforced");
    }
}
```

Two deliberate details:

- **The empty `// Arrange` block stays.** The comments are what makes a three-line test readable next
  to a thirty-line one, and a convention applied "when needed" is applied inconsistently.
- **Assertions are awaited.** `Assert.That(...)` is terminal and returns a value, so
  `await Assert.That(result).IsEqualTo(expected)` is the form. **An un-awaited assertion is a test
  that cannot fail.**

## Assertions

| Form | Use |
| --- | --- |
| `await Assert.That(x).IsEqualTo(y)` / `.IsNotEqualTo(y)` | value equality |
| `await Assert.That(x).IsTrue()` / `.IsFalse()` | booleans |
| `await Assert.That(x).IsEmpty()` | collections — the workhorse of architecture rules |
| `await Assert.That(x).Count().IsEqualTo(2)` | collection size |
| `await Assert.That(x).Contains(y)` / `.DoesNotContain(y)` | membership |
| `await Assert.That(act).Throws<TException>()` | a synchronous delegate that should throw |
| `await Assert.That(async () => await f()).Throws<TException>()` | an async one |
| `await Assert.That(x).IsTypeOf<T>()` | exact runtime type |
| `await Assert.That(x).IsEquivalentTo(sequence)` | order-independent sequence equality |

For an exception test the two shapes are `Action` and `Func<Task>`. The compiler will not infer
either, so the explicit type is required and is **not** a style violation:

```csharp
Action act = () => CustomerId.Create("");

await Assert.That(act).Throws<ArgumentException>();
```

## Parameterised tests

`[Arguments]` puts the interesting values in the attribute rather than a loop, so a failure names the
case:

```csharp
[Test]
[Arguments("")]
[Arguments("   ")]
[Arguments("Has Spaces")]
[Arguments("has_underscore")]
public async Task Create_GivenSlugThatIsNotALowercaseSlug_Throws(string value)
{
    // Arrange
    // Act
    Action act = () => CustomerId.Create(value);

    // Assert
    await Assert.That(act).Throws<ArgumentException>();
}
```

## Async all the way down

**Where an API offers an async overload, use it** — `File.ReadAllTextAsync`, not `File.ReadAllText`.
**And the rule reaches through test helpers:** a helper calling such an API is itself converted. Being
a helper is not an exemption.

The build enforces the first half and cannot enforce the second, which is why the second needs saying:

- `VSTHRD103` is an **`error`** and flags a sync call with an `Async`-suffixed equivalent **when the
  call sits inside a `Task`-returning method**. In an `async Task` test, `File.ReadAllText` fails the
  build.
- A **sync helper is not `Task`-returning**, so the identical call inside it is invisible to the
  analyzer. Pushing the call one level down turns a build error into nothing.

It also compounds with the cancellation rule, which keys off whether anything in the chain accepts a
`CancellationToken`: a sync helper accepts none, so it **silently removes the trigger** and the test is
uncancellable while looking compliant. (TUnit also runs tests in parallel, so a blocking read holds a
thread-pool thread an async read would release.)

Converting — three things change together, and the first two are not optional:

```csharp
// Before — invisible to VSTHRD103, and the chain accepts no token
static string Documentation => File.ReadAllText(DocumentationPath);

// After
static async Task<string> ReadDocumentationAsync(CancellationToken cancellationToken) =>
    await File.ReadAllTextAsync(DocumentationPath, cancellationToken);
```

- It returns a `Task`, bringing every call inside it back under `VSTHRD103`.
- It gains the **`Async` suffix** — `VSTHRD200` is an `error`, so the rename is enforced.
- It takes a trailing `CancellationToken`, because the chain now accepts one; its callers declare and
  forward one.

**A property cannot be `async`, so it becomes a method** — that is the conversion, not an exemption.
A read-only property hiding IO is the most common form of this violation.

It genuinely cannot be converted when **no async overload exists** (`File.Exists`, `File.Delete`,
`Directory.EnumerateFiles`; likewise `ArchitecturePolicy.Load()` and `PolicySchemaValidator.Validate()`,
which `Purview.Architecture.Testing` ships synchronous-only) or when **a base type or interface fixes
the signature**. State the reason at the call. Never fake it: `Task.Run` around a blocking call,
`Task.FromResult` around a sync result, or `.Result` / `.GetAwaiter().GetResult()` to flatten a task —
the last two are `VSTHRD002`, an `error`.

## Cancellation

Three parts, triggered by the **call chain** rather than by the test:

1. **If any method the test calls accepts a `CancellationToken`, the test declares one.** Not only the
   method under test — anything in the chain, including a `File.ReadAllTextAsync` or the delegate handed
   to `Assert.That`.
2. **It is the last parameter**, and TUnit supplies the value. With `[Arguments]`, the data binds to the
   leading parameters and the token is supplied on top, so the attribute's positional order still matches
   the signature:

   ```csharp
   [Test]
   [Arguments("build-enforced", true)]
   public async Task ListEnforcedByBuildAsync_GivenAConvention_FiltersIt(
       string id,
       bool enforcedByBuild,
       CancellationToken cancellationToken
   )
   ```

3. **It is forwarded to every call that accepts one.** Never `CancellationToken.None`, never `default`,
   never a token the test creates. TUnit's token is the *test's* token — cancelled on timeout or on an
   aborted run — so a call given `None` cannot be stopped, and **one hop that drops it makes the whole
   chain uncancellable**, which is why partial forwarding is a failure and not a lesser version of the
   rule. It is also the only place that proves the production code threads the token through at all.

A test whose chain accepts no token takes no parameter; an unused token is noise, not compliance.

## Substitutes

TUnit.Mocks is globally imported by the SDK, so no `using` is needed:

```csharp
[Test]
public async Task RegisterAsync_GivenAnAlreadyRegisteredConvention_DoesNotStoreIt(CancellationToken cancellationToken)
{
    // Arrange
    var repository = CreateRepository();
    repository.ExistsAsync(Any(), Any()).Returns(true);
    var catalogue = CreateCatalogue(repository.Object);

    // Act
    await Assert
        .That(async () => await catalogue.RegisterAsync(TestBuilder.EnforcedConvention(), cancellationToken))
        .Throws<InvalidOperationException>();

    // Assert
    repository.AddAsync(Any(), Any()).WasNeverCalled();
}
```

- `Mock.Of<T>()` produces a `Mock<T>`; `.Object` is the instance you pass in.
- `Any()` matches any argument, `Is(value)` a specific one, `AnyArgs()` the whole call.
- `.Returns(value)` sets the result. For a `Task<T>`-returning method, return the `T` — the task is
  unwrapped for you.
- `.WasCalled(Times.Once)` and `.WasNeverCalled()` are the assertions.

The `// Assert` after a `Throws` is not a mistake: that test's point is that the exception was thrown
**and** that nothing was written, and both are assertions about the same act.

## `TestBuilder`

It lives in the shared testing project and is the single means by which a test creates domain
content:

```csharp
public static class TestBuilder
{
    public const int DefaultCount = 3;

    internal static Faker Faker { get; } = new();

    public static Domain.ConventionId ConventionId(string? value = null) =>
        Domain.ConventionId.Create(value ?? $"{Slug()}-{Faker.Random.String2(6, LowercaseAlphabet)}");

    public static Domain.Convention Convention(
        Domain.ConventionId? id = null,
        string? title = null,
        VerificationTier? tier = null,
        bool? enforcedByBuild = null) =>
        Domain.Convention.Create(
            id ?? ConventionId(),
            title ?? Faker.Lorem.Sentence(4),
            tier ?? Faker.Random.Enum<VerificationTier>(),
            enforcedByBuild ?? Faker.Random.Bool());
}
```

Four rules make it work:

- **Omitted arguments are generated; supplied arguments are used exactly.** A test overrides only the
  value it is proving, and everything else is realistic without the test saying so.
- **A collection factory comes with a companion.** `Conventions(int count = DefaultCount)` generates a
  distinct set, so a test needing "some" gets "some" without its own loop.
- **Bogus is a global `using`.** `Faker` is available with no import — which is also why an explicit
  `using Bogus;` in a shared testing project is reported as unnecessary.
- **A value-object test is the one exception.** A test proving what `Create` and `Hydrate` accept and
  reject has to name the rejected input itself, so it constructs the invalid value directly.
  Everything valid still goes through the builder.

## Two constraints that shaped the builder

Both are consequences of SDK rules, not preferences, and both were found by building it.

**The factories are flat, not grouped.** The natural shape is a nested class per feature area
(`TestBuilder.Catalogue.Convention(...)`), and it cannot compile: the SDK ships
`dotnet_diagnostic.CA1034.severity = error`, so a publicly visible nested type fails the build, and
`PRSGD0008` refuses to let a repository add `CA1034` to `NoWarn`. Making the group `internal` does
not help either — the SDK's `InternalsVisibleTo` target runs on non-test projects and grants access
*to* test and shared-testing projects rather than *from* them, so a shared testing project's
internals are not visible to the tests beside it. An area that outgrows a flat builder gets its own
top-level builder class.

**`var` is the house style, with one carve-out.** `csharp_style_var_elsewhere = true:warning` makes
an explicit type on a declaration initialised from a method call a warning (`IDE0007`). The carve-out
is object creation: the `PDS0003` analyzer prefers an explicit type with target-typed `new()`:

```csharp
var id = TestBuilder.ConventionId("build-enforced");   // method call -> var
string title = new('a', Convention.MaxTitleLength);    // new()       -> explicit type
```

## What is enforced, and what is review

| Rule | Enforced by |
| --- | --- |
| TUnit is the only framework | the build — the SDK adds it; a second has to be added by hand |
| Test projects are discovered and categorised | the build (name-suffix detection) |
| A shared testing project is referenced, not executed | the build (`IsSharedTestingProject`) |
| Style, including the `var` carve-out | the build (analyzers, `warning` or above) |
| An acceptance suite is categorised `Acceptance` and kept out of the fast tier | the build (name-suffix detection) and the pipeline's `TestFilter` |
| A container-backed suite fails rather than passes when its backend is missing in CI | the pipeline, when the backend is pinned with `PURVIEW_CONTAINERS_BACKEND` |
| AAA structure, naming, `CreateXXX` helpers, `TestBuilder` usage | **review** |
| Given/When/Then chains end in `.BDTestAsync()` | **review** |
| A real dependency comes from `Purview.Containers`, not a shared instance | **review** |
| An async overload is used inside an `async Task` test | the build — `VSTHRD103` is `error` |
| A converted helper carries the `Async` suffix | the build — `VSTHRD200` is `error` |
| A **sync helper** calling an API with an async overload is converted | **review** — a non-`Task`-returning method is outside `VSTHRD103`'s scope |
| A declared `CancellationToken` is the last parameter | the build — `CA1068` is `warning`, and a clean build has no warnings |
| A test whose chain accepts a token declares one and forwards it everywhere | **review** |

## Acceptance tests and container-backed tests

An acceptance test — a behaviour somebody outside the team asked for, in their words — is written with
**BDTest over TUnit** (`BDTest.TUnit`) in a `<Target>.AcceptanceTests` project, where Given/When/Then
replaces the AAA comments. A suite that needs a real database, queue or emulator gets a throwaway
container from **`Purview.Containers`** in its own `<Target>.IntegrationTests` project, skipping
locally when no backend is usable and failing in CI.

Read `references/acceptance-and-container-tests.md` before writing either: it carries the scenario and
container shapes, and the two traps — a chain that never calls `.BDTestAsync()` runs no steps and still
passes, and a suite that skips in CI turns a missing runtime into a green job.

## Tiers

A test project's **name** carries its tier — the SDK categorises `*UnitTests` as `Unit`, and takes the
word before `Tests` as a pattern, so `*AcceptanceTests` gives `Acceptance` with nothing added to the
pipeline. The `TestFilter` then selects a tier without a hand-written attribute. Split a slow suite
into its own project rather than tagging individual tests.

Filtering uses TUnit's `--treenode-filter`, not `--filter`: these repositories run on
Microsoft.Testing.Platform, where `dotnet test --filter` reports "Zero tests ran".

## At your scale

- **One test project.** TUnit, AAA, the naming convention, `TestBuilder`. That is the whole of it.
- **A shared fixture.** Move it into `SharedTestingInfra` and make the class public — a shared
  testing project is a library, so its fixtures are legitimately public API and `CA1515` does not
  apply.
- **A database or a container.** One `<Target>.IntegrationTests` project and one `Purview.Containers`
  module. Do not build a fixture framework around it until a second suite needs the same container.
- **A behaviour somebody outside the team asked for.** That, and not before, is when BDTest earns its
  place: one `<Target>.AcceptanceTests` project with one story. Reach for the HTML report only once
  somebody will read it.
- **A package with a documented contract.** Add a consumer-verification script; see
  `purview-engineering-principles`.

## Related skills

`purview-engineering-principles` for the rules this implements, and `purview-architecture-testing`
for turning an architectural boundary into a failing build.
