```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3


```
| Method                                  | Job                | Runtime            | Mean      | Ratio | Allocated | Alloc Ratio |
|---------------------------------------- |------------------- |------------------- |----------:|------:|----------:|------------:|
| &#39;0 tags (no TagList): histogram record&#39; | .NET 10.0          | .NET 10.0          | 0.3443 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 10.0          | .NET 10.0          | 0.3747 ns |  1.09 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 10.0          | .NET 10.0          | 0.8679 ns |  2.53 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 10.0          | .NET 10.0          | 4.1941 ns | 12.22 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 10.0          | .NET 10.0          | 5.1786 ns | 15.08 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 10.0          | .NET 10.0          | 6.5114 ns | 18.97 |         - |          NA |
|                                         |                    |                    |           |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET 8.0           | .NET 8.0           | 0.5448 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 8.0           | .NET 8.0           | 0.3681 ns |  0.68 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 8.0           | .NET 8.0           | 0.7316 ns |  1.34 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 8.0           | .NET 8.0           | 3.7765 ns |  6.94 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 8.0           | .NET 8.0           | 4.1910 ns |  7.70 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 8.0           | .NET 8.0           | 4.1711 ns |  7.67 |         - |          NA |
|                                         |                    |                    |           |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET 9.0           | .NET 9.0           | 0.1915 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 9.0           | .NET 9.0           | 0.3585 ns |  1.87 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 9.0           | .NET 9.0           | 0.6797 ns |  3.55 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 9.0           | .NET 9.0           | 3.6203 ns | 18.93 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 9.0           | .NET 9.0           | 3.7791 ns | 19.76 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 9.0           | .NET 9.0           | 3.8767 ns | 20.27 |         - |          NA |
|                                         |                    |                    |           |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;3 tags (no TagList): histogram record&#39; | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
| &#39;6 tags (TagList): histogram record&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |        NA |     ? |        NA |           ? |
|                                         |                    |                    |           |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;3 tags (no TagList): histogram record&#39; | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |
| &#39;6 tags (TagList): histogram record&#39;    | .NET Framework 4.8 | .NET Framework 4.8 |        NA |     ? |        NA |           ? |

Benchmarks with issues:
  TagListBenchmarks.'0 tags (no TagList): histogram record': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  TagListBenchmarks.'1 tag (no TagList): auto-counter add': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  TagListBenchmarks.'3 tags (no TagList): histogram record': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  TagListBenchmarks.'4 tags (TagList): auto-counter add': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  TagListBenchmarks.'5 tags (TagList): auto-counter add': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  TagListBenchmarks.'6 tags (TagList): histogram record': .NET Framework 4.7(Runtime=.NET Framework 4.7)
  TagListBenchmarks.'0 tags (no TagList): histogram record': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TagListBenchmarks.'1 tag (no TagList): auto-counter add': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TagListBenchmarks.'3 tags (no TagList): histogram record': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TagListBenchmarks.'4 tags (TagList): auto-counter add': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TagListBenchmarks.'5 tags (TagList): auto-counter add': .NET Framework 4.8(Runtime=.NET Framework 4.8)
  TagListBenchmarks.'6 tags (TagList): histogram record': .NET Framework 4.8(Runtime=.NET Framework 4.8)
