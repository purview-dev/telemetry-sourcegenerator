# Performance document shapes

The exact shapes the two performance documents use today. Preserve them: a consumer reads the same
table layout release after release, and a diff should show changed numbers, not a reshaped page.

## `docs/wiki/Performance.md`

Order of sections:

1. `# Performance` - one sentence: what the page is and which project produced it.
2. `## Reproducing` - the `dotnet run` command, the artifacts path and file types, and the
   `[!IMPORTANT]` Release-only callout.
3. `## Environment` - a fenced block copied verbatim from a `-report-github.md` header, then any caveat
   notes that are still true.
4. One `##` section per family, in this order: `Activities`, `Logging`, `Multi-target`,
   `Logger multi-target`, `Metrics`, `Generator model overhead`, `Observable instruments`.
   `Generator model overhead` has no table yet — write it on the first refresh that includes a
   `GeneratorModelBenchmarks` report.
5. `## Raw results` - only what is actually published; the artifacts folder is gitignored, so this
   section must not claim a GitHub link to it exists.
6. `## See also` - links to the README summary and the related wiki pages.

Each family section:

```markdown
## Activities

**Source:** `ActivityBenchmarks`

One paragraph: what is compared against what, and what the parameter does.

| Method | Runtime | HasListener | Mean | Ratio | Allocated |
| --- | --- | --- | --- | --- | --- |
| **Manual: start + complete** | **.NET 10.0** | **True** | **220.37 ns** | **1.00** | **1008 B** |
| Generated: start + complete | .NET 10.0 | True | 216.69 ns | 0.98 | 1008 B |
...

**Interpretation:** the four-part analysis - comparison, delta, mechanism, caveat.
```

Conventions in the tables:

- Column order: `Method | Runtime | <parameters> | Mean | Ratio | Allocated`, plus `Alloc Ratio` where
  allocations differ, and `Gen0`/`Gen1` where the report shows them.
- Rows are grouped by runtime, with the `.NET 10.0` block first, then `.NET 11.0`, then
  `.NET Framework 4.8`; within a block the parameter groups of one class sit together.
- The baseline row of each block is bold, including its bold `1.00` ratio.
- Means carry the report's unit and precision (`ns`, `µs`, `ms`); allocations carry `B` or `-`.
- Method names are the report's `Description` strings, shortened only where the report already
  shortened them.

Family-specific parameters:

| Section | Parameters | Columns |
| --- | --- | --- |
| Activities | `HasListener` | `Mean | Ratio | Allocated` |
| Logging | `HasLogging` | `Mean | Ratio | Allocated` |
| Multi-target | `HasListener` | `Mean | Ratio | Allocated | Alloc Ratio` |
| Logger multi-target | `HasListener` | `Mean | Ratio | Allocated` |
| Metrics | - (two tables: instruments, then tag counts) | `Mean` per scenario |
| Generator model overhead | `MethodCount` | `Mean | Ratio | Gen0 | Gen1 | Allocated | Alloc Ratio` |

The Metrics section is the one deliberate exception: it is two scenario tables (`MetricsBenchmarks`
generated-vs-manual, then `TagListBenchmarks` by tag count) followed by the tag-count threshold
explanation, because a column-per-implementation table would be 3x wider than the page.

## `README.md` section

```markdown
## Performance

Benchmarked on <CPU>, .NET SDK <version>. See the [Performance](https://purview.dev/docs/telemetry-sourcegenerator/performance/) page for full cross-runtime results.

### Activities (.NET 10.0)

| Scenario | HasListener | Manual | Generated | Ratio |
| --- | --- | --- | --- | --- |
| start + complete | False | 0.53 ns | 0.58 ns | 1.11x |
...
```

- The heading carries the headline runtime in parentheses. It must match the rows beneath it.
- Columns squeeze the wiki table into scenario rows: a scenario column, the parameter column, one column
  per implementation, and `Ratio`.
- Allocations are folded into the cell when they matter (`220 ns / 1008 B`), not given their own column.
- One short sentence per family states the conclusion a consumer needs; it is not a shortened copy of
  the wiki's interpretation.
- Metrics keeps its `Notes` column for the `TagList` threshold.

## Environment header

Copy it, do not compose it:

```
BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
Memory: 63.77 GB Total, 35.44 GB Available
.NET SDK 11.0.100
  [Host]     : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3
  Job-GVKUBM : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-IHFIKV : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3
  Job-UGENHH : .NET Framework 4.8 (4.8.10008.0), X64 RyuJIT VectorSize=256
```

Drop the `Job-XXXXX` ids if you prefer, but keep the exact runtime versions, the SDK version, the CPU and
the OS build - those are what make two runs comparable. The README's one-line environment must agree with
this block's CPU and SDK.

## Interpretation paragraph checklist

- [ ] Names the baseline and the alternatives.
- [ ] Gives the time delta **and** the allocation delta for the production path.
- [ ] Names the mechanism when it is known (framework cost, inlining, `IsEnabled` short-circuit, tag
      threshold).
- [ ] States the caveat (machine/SDK) or the boundary of the claim.
- [ ] Contains no number that is absent from the report.
- [ ] Would still be true if the reader only ever saw the table, not the report.
