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
| &#39;0 tags (no TagList): histogram record&#39; | .NET 10.0          | .NET 10.0          |  0.3622 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 10.0          | .NET 10.0          |  0.3525 ns |  0.97 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 10.0          | .NET 10.0          |  0.7345 ns |  2.03 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 10.0          | .NET 10.0          |  4.1003 ns | 11.33 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 10.0          | .NET 10.0          |  5.2885 ns | 14.62 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 10.0          | .NET 10.0          |  5.9376 ns | 16.41 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET 8.0           | .NET 8.0           |  0.5407 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 8.0           | .NET 8.0           |  0.3574 ns |  0.66 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 8.0           | .NET 8.0           |  0.7202 ns |  1.33 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 8.0           | .NET 8.0           |  3.6848 ns |  6.82 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 8.0           | .NET 8.0           |  4.4160 ns |  8.17 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 8.0           | .NET 8.0           |  4.3526 ns |  8.06 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET 9.0           | .NET 9.0           |  0.1674 ns |  1.01 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET 9.0           | .NET 9.0           |  0.3702 ns |  2.22 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET 9.0           | .NET 9.0           |  0.4979 ns |  2.99 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET 9.0           | .NET 9.0           |  3.6140 ns | 21.72 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET 9.0           | .NET 9.0           |  4.0370 ns | 24.26 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET 9.0           | .NET 9.0           |  3.9937 ns | 24.00 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET Framework 4.7 | .NET Framework 4.7 | 15.1750 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | 18.7807 ns |  1.24 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET Framework 4.7 | .NET Framework 4.7 | 29.7604 ns |  1.96 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 52.3936 ns |  3.45 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 60.0563 ns |  3.96 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 58.2202 ns |  3.84 |         - |          NA |
|                                         |                    |                    |            |       |           |             |
| &#39;0 tags (no TagList): histogram record&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 15.3467 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | 18.7864 ns |  1.22 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 29.3580 ns |  1.91 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 49.5865 ns |  3.23 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 60.5989 ns |  3.95 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 59.1921 ns |  3.86 |         - |          NA |
