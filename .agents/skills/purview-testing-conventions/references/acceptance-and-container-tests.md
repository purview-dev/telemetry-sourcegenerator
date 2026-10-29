# Acceptance tests and container-backed tests

Two shapes that sit outside the unit tier. Both keep TUnit as the runner, and both live in their own
test project so the tier comes from the project name rather than an attribute.

## Acceptance tests: BDTest

An acceptance test states a behaviour somebody outside the team asked for, in their words. Use
**BDTest over TUnit through the `BDTest.TUnit` adapter**, in a `<Target>.AcceptanceTests` project
(the SDK emits `Category=Acceptance` for it).

Not Gherkin, SpecFlow or Reqnroll, for two reasons:

- **BDTest is code-only.** No `.feature` file and no generated test body, so there is no second
  artefact that can drift from the step implementing it. That drift is the failure that makes a BDD
  suite stop being trusted.
- **One runner stays one runner.** `BDTest.TUnit` is an adapter over TUnit, so the SDK's wiring, the
  emitted category and `--treenode-filter` are all unchanged. A BDD framework with its own runner is
  exactly what the one-framework rule refuses.

```csharp
[Story(AsA = "account holder", IWant = "to withdraw money", SoThat = "I can use my savings")]
public class WithdrawalTests : TUnitBDTestBase<AccountContext>
{
    AccountSteps Account => new(Context);

    [Test]
    [ScenarioText("Withdrawing money reduces the balance")]
    public async Task WithdrawMoney() =>
        await Given(() => Account.CreateAnAccount(100m))
            .When(() => Account.Withdraw(25m))
            .Then(() => Account.TheBalanceIs(75m))
            .BDTestAsync();
}

public class AccountSteps(AccountContext context)
{
    [StepText("I have an account with a balance of {0}")]
    public void CreateAnAccount(decimal balance) => context.Balance = balance;

    [StepText("I withdraw {0}")]
    public void Withdraw(decimal amount) => context.Balance -= amount;

    [StepText("the account balance is {0}")]
    public async Task TheBalanceIs(decimal expected) => await Assert.That(context.Balance).IsEqualTo(expected);
}
```

Five rules, and the first is the one that bites:

- **`.BDTestAsync()` runs the scenario.** A chain stopping at `.Then(...)` compiles, passes, and
  executes none of its steps — the same failure mode as an un-awaited `Assert.That`.
- **Given/When/Then replaces Arrange/Act/Assert, and only here.** The chain already names the three
  phases. Everywhere else the AAA comments stay.
- **`Context` is the only state.** Steps take it through the constructor and hold no fields of their
  own; BDTest gives each test its own context, which is what keeps parallel and repeated runs honest.
- **`[StepText]` is written for the report's reader.** `{0}` is replaced with the argument's actual
  value at run time, so the report states what the scenario did, not what it was called.
- **Domain content still comes from `TestBuilder`.** The vocabulary changes at the step; the test-data
  rule does not.

Assertions inside a step are ordinary awaited TUnit assertions. `BDTest.Reporting` produces the
single-file HTML report — publish it as a build artifact from the job that ran the suite.

## A test that needs a real dependency: `Purview.Containers`

A database, queue, blob store or emulator comes from `Purview.Containers`: a throwaway Linux container
behind one API over Microsoft WSL Containers or Docker, with typed modules for SQL Server, PostgreSQL,
Redis, MySQL, RabbitMQ, Azurite and NATS.

The two refused alternatives:

- **A shared instance** (a local SQL Server, a team database) makes tests order-dependent and failures
  unreproducible — the state a test found was left by whatever ran before it, on whatever machine.
- **Testcontainers directly** ties the suite to a Docker installation, which on Windows is an install
  and a licensing question per developer and per runner. The WSL backend removes it, and the same test
  code runs on either.

```csharp
await using var postgres = new PostgreSqlBuilder()
    .WithDatabase("tests")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

await postgres.StartAsync();

await using var connection = new NpgsqlConnection(postgres.GetConnectionString());
await connection.OpenAsync();
```

`Build()` validates the configuration, `StartAsync()` returns only once every wait strategy is
satisfied — there is no sleep to tune — and `await using` disposes the container at end of scope.

Three rules:

- **Its own `<Target>.IntegrationTests` project**, so the tier is in the name and the suite stays out
  of the pull-request filter without anybody remembering an attribute.
- **Skip locally when no backend is usable; fail in CI.** Two different situations, so never one
  outcome. Locally, probe and skip. In CI, pin the backend with `PURVIEW_CONTAINERS_BACKEND` — a named
  backend never falls back, so a missing runtime fails loudly instead of letting a green job mean
  nothing ran:

  ```csharp
  [Before(Class)]
  public static async Task EnsureABackendIsAvailable()
  {
      var backends = await ContainerBackends.ProbeAllAsync();

      Skip.Unless(backends.Any(backend => backend.IsUsable), "No container backend is available.");
  }
  ```

  Left unpinned, automatic selection probes in priority order and prefers WSL Containers over Docker.
- **Pin the version exactly.** `Purview.Containers` is **experimental and prerelease** — API, defaults
  and packaging rules can change between releases. The packages are a family, so one property in
  `Directory.Packages.props` governs all of them and a bump cannot be applied to half.
