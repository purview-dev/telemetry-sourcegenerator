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
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |  0.0164 ns |     ? |         - |           ? |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 10.0          | .NET 10.0          |  0.3693 ns |     ? |         - |           ? |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |  0.2134 ns |     ? |         - |           ? |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 10.0          | .NET 10.0          |  0.3627 ns |     ? |         - |           ? |
| &#39;Manual: up-down counter&#39;          | .NET 10.0          | .NET 10.0          |  0.0017 ns |     ? |         - |           ? |
| &#39;Generated: up-down counter&#39;       | .NET 10.0          | .NET 10.0          |  0.4006 ns |     ? |         - |           ? |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 10.0          | .NET 10.0          |  0.0004 ns |     ? |         - |           ? |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 10.0          | .NET 10.0          |  0.3718 ns |     ? |         - |           ? |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 10.0          | .NET 10.0          |  0.1841 ns |     ? |         - |           ? |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 10.0          | .NET 10.0          |  0.3670 ns |     ? |         - |           ? |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 8.0           | .NET 8.0           |  0.4868 ns |  1.02 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 8.0           | .NET 8.0           |  0.3907 ns |  0.82 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 8.0           | .NET 8.0           |  0.3442 ns |  0.72 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 8.0           | .NET 8.0           |  0.3795 ns |  0.79 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 8.0           | .NET 8.0           |  0.4149 ns |  0.87 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 8.0           | .NET 8.0           |  0.5509 ns |  1.15 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 8.0           | .NET 8.0           |  0.3606 ns |  0.75 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 8.0           | .NET 8.0           |  0.3269 ns |  0.68 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 8.0           | .NET 8.0           |  0.3655 ns |  0.76 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 8.0           | .NET 8.0           |  0.5981 ns |  1.25 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET 9.0           | .NET 9.0           |  0.4294 ns |  1.01 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET 9.0           | .NET 9.0           |  0.1828 ns |  0.43 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET 9.0           | .NET 9.0           |  0.3846 ns |  0.90 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET 9.0           | .NET 9.0           |  0.3936 ns |  0.92 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET 9.0           | .NET 9.0           |  0.3386 ns |  0.79 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET 9.0           | .NET 9.0           |  0.1952 ns |  0.46 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET 9.0           | .NET 9.0           |  0.1889 ns |  0.44 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET 9.0           | .NET 9.0           |  0.1714 ns |  0.40 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET 9.0           | .NET 9.0           |  0.3768 ns |  0.88 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET 9.0           | .NET 9.0           |  0.1882 ns |  0.44 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 20.6585 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | 15.6259 ns |  0.76 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | 19.2654 ns |  0.93 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | 18.6556 ns |  0.90 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.7 | .NET Framework 4.7 | 19.1465 ns |  0.93 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | 16.1062 ns |  0.78 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | 15.4466 ns |  0.75 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | 16.1534 ns |  0.78 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | 19.0758 ns |  0.92 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | 19.6967 ns |  0.95 |         - |          NA |
|                                    |                    |                    |            |       |           |             |
| &#39;Manual: auto-counter (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 15.5494 ns |  1.00 |         - |          NA |
| &#39;Generated: auto-counter (0 tags)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | 15.4160 ns |  0.99 |         - |          NA |
| &#39;Manual: auto-counter (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | 18.3244 ns |  1.18 |         - |          NA |
| &#39;Generated: auto-counter (1 tag)&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | 18.2782 ns |  1.18 |         - |          NA |
| &#39;Manual: up-down counter&#39;          | .NET Framework 4.8 | .NET Framework 4.8 | 15.1594 ns |  0.98 |         - |          NA |
| &#39;Generated: up-down counter&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | 15.5524 ns |  1.00 |         - |          NA |
| &#39;Manual: histogram (0 tags)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | 14.8951 ns |  0.96 |         - |          NA |
| &#39;Generated: histogram (0 tags)&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | 15.6076 ns |  1.00 |         - |          NA |
| &#39;Manual: histogram (1 tag)&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | 18.6373 ns |  1.20 |         - |          NA |
| &#39;Generated: histogram (1 tag)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | 19.4532 ns |  1.25 |         - |          NA |
