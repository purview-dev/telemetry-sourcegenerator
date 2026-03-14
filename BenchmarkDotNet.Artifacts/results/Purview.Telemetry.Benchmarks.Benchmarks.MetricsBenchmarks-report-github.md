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
| Method                             | Job                | Runtime            | Mean       | Ratio | Allocated | Alloc Ratio |
|----------------------------------- |------------------- |------------------- |-----------:|------:|----------:|------------:|
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |  0.1723 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 10.0          | .NET 10.0          |  0.3511 ns |  2.04 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |  0.1731 ns |  1.01 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 10.0          | .NET 10.0          |  0.5249 ns |  3.05 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 10.0          | .NET 10.0          |  0.1740 ns |  1.01 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 10.0          | .NET 10.0          |  0.3407 ns |  1.98 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 10.0          | .NET 10.0          |  0.1738 ns |  1.01 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |  0.3503 ns |  2.03 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 10.0          | .NET 10.0          |  0.1732 ns |  1.01 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |  0.5224 ns |  3.03 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 8.0           | .NET 8.0           |  0.1711 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 8.0           | .NET 8.0           |  0.3514 ns |  2.06 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 8.0           | .NET 8.0           |  0.3588 ns |  2.10 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 8.0           | .NET 8.0           |  0.5248 ns |  3.07 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 8.0           | .NET 8.0           |  0.1730 ns |  1.01 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 8.0           | .NET 8.0           |  0.3504 ns |  2.05 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 8.0           | .NET 8.0           |  0.3516 ns |  2.06 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 8.0           | .NET 8.0           |  0.3464 ns |  2.03 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 8.0           | .NET 8.0           |  0.5242 ns |  3.07 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 8.0           | .NET 8.0           |  0.3493 ns |  2.04 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 9.0           | .NET 9.0           |  0.1728 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 9.0           | .NET 9.0           |  0.1825 ns |  1.06 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 9.0           | .NET 9.0           |  0.3470 ns |  2.01 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 9.0           | .NET 9.0           |  0.1734 ns |  1.00 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 9.0           | .NET 9.0           |  0.1700 ns |  0.99 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 9.0           | .NET 9.0           |  0.1797 ns |  1.04 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 9.0           | .NET 9.0           |  0.3478 ns |  2.02 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 9.0           | .NET 9.0           |  0.1739 ns |  1.01 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 9.0           | .NET 9.0           |  0.3481 ns |  2.02 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 9.0           | .NET 9.0           |  0.3503 ns |  2.03 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 14.5226 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | 15.0242 ns |  1.03 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | 17.6109 ns |  1.21 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | 17.7414 ns |  1.22 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.7 | .NET Framework 4.7 | 14.5807 ns |  1.00 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | 14.9923 ns |  1.03 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | 14.5807 ns |  1.00 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 15.1061 ns |  1.04 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | 17.9393 ns |  1.24 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | 18.1634 ns |  1.25 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 14.5683 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 15.0969 ns |  1.04 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | 18.3106 ns |  1.26 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | 17.7005 ns |  1.22 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.8 | .NET Framework 4.8 | 15.5211 ns |  1.07 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | 15.1828 ns |  1.04 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | 14.5580 ns |  1.00 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 15.0912 ns |  1.04 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | 17.6257 ns |  1.21 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | 18.2136 ns |  1.25 |         - |          NA |
