```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8037/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i9-12900H 2.50GHz, 1 CPU, 14 logical and 14 physical cores
.NET SDK 10.0.200
  [Host]             : .NET 10.0.4 (10.0.4, 10.0.426.12010), X64 RyuJIT x86-64-v3
  .NET 10.0          : .NET 10.0.4 (10.0.4, 10.0.426.12010), X64 RyuJIT x86-64-v3
  .NET 8.0           : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0           : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET Framework 4.7 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256
  .NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256


```
| Method                             | Job                | Runtime            | Mean        | Ratio | Allocated | Alloc Ratio |
|----------------------------------- |------------------- |------------------- |------------:|------:|----------:|------------:|
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |   0.0000 ns |     ? |         - |           ? |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 10.0          | .NET 10.0          |   5.7670 ns |     ? |         - |           ? |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |   1.2463 ns |     ? |         - |           ? |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 10.0          | .NET 10.0          |   1.0129 ns |     ? |         - |           ? |
| &#39;Manual: up-down counter&#39;          | .NET 10.0          | .NET 10.0          |   2.4884 ns |     ? |         - |           ? |
| &#39;Generated: up-down counter&#39;       | .NET 10.0          | .NET 10.0          |  12.5985 ns |     ? |         - |           ? |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 10.0          | .NET 10.0          |   0.6942 ns |     ? |         - |           ? |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |   2.9321 ns |     ? |         - |           ? |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 10.0          | .NET 10.0          |   0.2152 ns |     ? |         - |           ? |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |   1.0002 ns |     ? |         - |           ? |
|                                    |                    |                    |             |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |   0.2005 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 10.0          | .NET 10.0          |   3.3341 ns | 16.64 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |   0.2490 ns |  1.24 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 10.0          | .NET 10.0          |   1.0089 ns |  5.04 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 10.0          | .NET 10.0          |   0.2213 ns |  1.10 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 10.0          | .NET 10.0          |   3.3945 ns | 16.94 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 10.0          | .NET 10.0          |   0.2024 ns |  1.01 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |   4.6844 ns | 23.38 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 10.0          | .NET 10.0          |   0.1989 ns |  0.99 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |   1.0278 ns |  5.13 |         - |          NA |
|                                    |                    |                    |             |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 8.0           | .NET 8.0           |   0.2052 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 8.0           | .NET 8.0           |   4.5278 ns | 22.10 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 8.0           | .NET 8.0           |   0.6041 ns |  2.95 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 8.0           | .NET 8.0           |   1.0825 ns |  5.28 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 8.0           | .NET 8.0           |   0.1753 ns |  0.86 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 8.0           | .NET 8.0           |   5.3643 ns | 26.18 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 8.0           | .NET 8.0           |   0.6836 ns |  3.34 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 8.0           | .NET 8.0           |   4.5635 ns | 22.27 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 8.0           | .NET 8.0           |   1.0090 ns |  4.92 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 8.0           | .NET 8.0           |   0.6115 ns |  2.98 |         - |          NA |
|                                    |                    |                    |             |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 9.0           | .NET 9.0           |   0.6400 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 9.0           | .NET 9.0           |   5.3938 ns |  8.43 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 9.0           | .NET 9.0           |   0.6317 ns |  0.99 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 9.0           | .NET 9.0           |   0.6392 ns |  1.00 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 9.0           | .NET 9.0           |   0.6316 ns |  0.99 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 9.0           | .NET 9.0           |   3.3667 ns |  5.26 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 9.0           | .NET 9.0           |   0.2296 ns |  0.36 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 9.0           | .NET 9.0           |   5.7749 ns |  9.03 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 9.0           | .NET 9.0           |   0.6403 ns |  1.00 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 9.0           | .NET 9.0           |   0.2096 ns |  0.33 |         - |          NA |
|                                    |                    |                    |             |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |  36.5453 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.7 | .NET Framework 4.7 |  58.3213 ns |  1.60 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 |  41.6624 ns |  1.14 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.7 | .NET Framework 4.7 |  42.5525 ns |  1.16 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.7 | .NET Framework 4.7 |  36.8169 ns |  1.01 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.7 | .NET Framework 4.7 |  58.7831 ns |  1.61 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 |  34.3161 ns |  0.94 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 |  58.7402 ns |  1.61 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.7 | .NET Framework 4.7 |  41.6366 ns |  1.14 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | 111.8771 ns |  3.06 |         - |          NA |
|                                    |                    |                    |             |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 |  88.3332 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 148.4742 ns |  1.68 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | 112.2207 ns |  1.27 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | 113.2573 ns |  1.28 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.8 | .NET Framework 4.8 |  88.5448 ns |  1.00 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | 147.9799 ns |  1.68 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 |  86.3522 ns |  0.98 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 147.0063 ns |  1.66 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | 110.5966 ns |  1.25 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | 115.4011 ns |  1.31 |         - |          NA |
