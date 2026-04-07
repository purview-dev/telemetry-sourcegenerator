```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3


```
| Method                             | Job                | Runtime            | Mean      | Ratio | Allocated | Alloc Ratio |
|----------------------------------- |------------------- |------------------- |----------:|------:|----------:|------------:|
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 10.0          | .NET 10.0          | 0.0062 ns |     ? |         - |           ? |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 10.0          | .NET 10.0          | 0.3716 ns |     ? |         - |           ? |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 10.0          | .NET 10.0          | 0.1711 ns |     ? |         - |           ? |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 10.0          | .NET 10.0          | 0.3659 ns |     ? |         - |           ? |
| &#39;Manual: up-down counter&#39;          | .NET 10.0          | .NET 10.0          | 0.0033 ns |     ? |         - |           ? |
| &#39;Generated: up-down counter&#39;       | .NET 10.0          | .NET 10.0          | 0.3522 ns |     ? |         - |           ? |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 10.0          | .NET 10.0          | 0.0058 ns |     ? |         - |           ? |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 10.0          | .NET 10.0          | 0.3631 ns |     ? |         - |           ? |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 10.0          | .NET 10.0          | 0.1682 ns |     ? |         - |           ? |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 10.0          | .NET 10.0          | 0.3614 ns |     ? |         - |           ? |
|                                    |                    |                    |           |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 8.0           | .NET 8.0           | 0.3671 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 8.0           | .NET 8.0           | 0.3651 ns |  0.99 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 8.0           | .NET 8.0           | 0.3711 ns |  1.01 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 8.0           | .NET 8.0           | 0.3603 ns |  0.98 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 8.0           | .NET 8.0           | 0.3568 ns |  0.97 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 8.0           | .NET 8.0           | 0.5436 ns |  1.48 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 8.0           | .NET 8.0           | 0.3670 ns |  1.00 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 8.0           | .NET 8.0           | 0.3774 ns |  1.03 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 8.0           | .NET 8.0           | 0.3579 ns |  0.98 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 8.0           | .NET 8.0           | 0.5460 ns |  1.49 |         - |          NA |
|                                    |                    |                    |           |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 9.0           | .NET 9.0           | 0.3654 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 9.0           | .NET 9.0           | 0.1773 ns |  0.49 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 9.0           | .NET 9.0           | 0.3647 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 9.0           | .NET 9.0           | 0.3596 ns |  0.98 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 9.0           | .NET 9.0           | 0.3470 ns |  0.95 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 9.0           | .NET 9.0           | 0.1822 ns |  0.50 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 9.0           | .NET 9.0           | 0.1824 ns |  0.50 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 9.0           | .NET 9.0           | 0.1747 ns |  0.48 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 9.0           | .NET 9.0           | 0.3749 ns |  1.03 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 9.0           | .NET 9.0           | 0.1915 ns |  0.52 |         - |          NA |
|                                    |                    |                    |           |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
|                                    |                    |                    |           |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |

Benchmarks with issues:
  MetricsBenchmarks.'Manual: auto-counter (0 tags)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Generated: auto-counter (0 tags)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Manual: auto-counter (1 tag)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Generated: auto-counter (1 tag)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Manual: up-down counter': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Generated: up-down counter': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Manual: histogram (0 tags)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Generated: histogram (0 tags)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Manual: histogram (1 tag)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Generated: histogram (1 tag)': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  MetricsBenchmarks.'Manual: auto-counter (0 tags)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Generated: auto-counter (0 tags)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Manual: auto-counter (1 tag)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Generated: auto-counter (1 tag)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Manual: up-down counter': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Generated: up-down counter': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Manual: histogram (0 tags)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Generated: histogram (0 tags)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Manual: histogram (1 tag)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  MetricsBenchmarks.'Generated: histogram (1 tag)': .NET Framework 4.8(Runtime=.NET Framework 4.8)
