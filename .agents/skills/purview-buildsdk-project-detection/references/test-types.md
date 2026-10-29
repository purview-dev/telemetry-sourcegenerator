# The shipped `TestType` group

These 53 names drive **`InternalsVisibleTo` generation**, not test detection. A non-test project
grants friend access to `{AssemblyName}.{TestType}Tests` for each name here.

Detection is the pattern `^.*\.([\w|_]\w*)Tests?$`, so a suffix outside this group is still detected,
still runs and still gets its category — it simply cannot reach the target project's `internal`
members. Prefer a name from this group unless the test type genuinely needs its own word.

```text
Unit             Integration      E2E              EndToEnd         Acceptance
Functional       Performance      Load             Smoke            Stress
Regression       Security         Chaos            Scenario         System
Threat           BlackBox         WhiteBox         GrayBox          GreyBox
Accessibility    Usability        Visual           Interactive      Localization
Globalization    Environment      Configuration    Deployment       Architecture
Contract         Component        Sanity           Property         Fuzz
Mutation         Snapshot         Approval         Benchmark        Capacity
Concurrency      Endurance        Soak             Spike            Failover
Recovery         Resilience       Compatibility    Interoperability Migration
Upgrade          Penetration      Compliance
```

## The five to reach for first

`Unit`, `Integration`, `E2E`, `Functional`, `Contract`. Everything else is available, but a reader
has to ask what it means — use one only when the test type itself carries operational weight.

## What a suffix buys you

| | detected as a test project | gets a category | gets a runner and coverage | sees `internal` members |
| -- | -- | -- | -- | -- |
| `Api.UnitTests` | yes | `Unit` | yes | yes |
| `Api.GoldenTests` | yes | `Golden` | yes | **no** |
| `UnitTests` *(no dot)* | **no** | no | **no** | no |
