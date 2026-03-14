```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8037/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.200
  [Host]             : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0          : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0           : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0           : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET Framework 4.7 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256
  .NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256


```
| Method                                  | Job                | Runtime            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------------- |------------------- |------------------- |-----------:|------:|----------:|------------:|
| &#39;0 tags (no TagList): histogram record&#39; | .NET 10.0          | .NET 10.0          |  0.3490 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 10.0          | .NET 10.0          |  0.5210 ns |  1.49 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 10.0          | .NET 10.0          |  0.8236 ns |  2.36 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 10.0          | .NET 10.0          |  4.1999 ns | 12.04 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 10.0          | .NET 10.0          |  5.3628 ns | 15.37 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 10.0          | .NET 10.0          |  5.8919 ns | 16.89 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET 8.0           | .NET 8.0           |  0.3503 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 8.0           | .NET 8.0           |  0.3497 ns |  1.00 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 8.0           | .NET 8.0           |  0.7031 ns |  2.01 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 8.0           | .NET 8.0           |  3.6129 ns | 10.32 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 8.0           | .NET 8.0           |  4.0323 ns | 11.52 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 8.0           | .NET 8.0           |  4.5053 ns | 12.87 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET 9.0           | .NET 9.0           |  0.1715 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 9.0           | .NET 9.0           |  0.3537 ns |  2.06 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 9.0           | .NET 9.0           |  0.6664 ns |  3.89 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 9.0           | .NET 9.0           |  3.3120 ns | 19.32 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 9.0           | .NET 9.0           |  3.8826 ns | 22.65 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 9.0           | .NET 9.0           |  4.1868 ns | 24.43 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET Framework 4.7 | .NET Framework 4.7 | 14.7722 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | 18.0124 ns |  1.22 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET Framework 4.7 | .NET Framework 4.7 | 28.4549 ns |  1.93 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 48.4072 ns |  3.28 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 56.4414 ns |  3.82 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 56.1476 ns |  3.80 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 14.8460 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | 18.0325 ns |  1.21 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 28.4483 ns |  1.92 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 48.4274 ns |  3.26 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 56.4846 ns |  3.80 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 55.5627 ns |  3.74 |         - |          NA |
