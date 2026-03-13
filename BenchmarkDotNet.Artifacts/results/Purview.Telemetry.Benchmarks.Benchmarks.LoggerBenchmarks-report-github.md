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
| Method                                                           | Job                | Runtime            | HasLogging | Mean        | Ratio  | Gen0   | Allocated | Alloc Ratio |
|----------------------------------------------------------------- |------------------- |------------------- |----------- |------------:|-------:|-------:|----------:|------------:|
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **False**      |   **0.7715 ns** |   **1.01** |      **-** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | False      |   0.2187 ns |   0.29 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | False      |   0.1982 ns |   0.26 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | False      |   1.1994 ns |   1.57 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | False      |   2.2975 ns |   3.01 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | False      |   2.0801 ns |   2.73 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | False      |   0.2341 ns |  1.001 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | False      |   0.0000 ns |  0.000 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | False      |   1.4157 ns |  6.053 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | False      |   2.0337 ns |  8.696 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | False      |   2.7106 ns | 11.590 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | False      |   8.7497 ns | 37.411 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | False      |   0.2016 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | False      |   0.2031 ns |   1.01 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | False      |   1.0428 ns |   5.18 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | False      |   1.8417 ns |   9.15 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | False      |   3.0679 ns |  15.24 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | False      |   6.7245 ns |  33.41 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | False      |   5.2671 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False      |   4.2696 ns |   0.81 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | False      |   4.6589 ns |   0.88 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False      |  16.2226 ns |   3.08 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False      |  20.0970 ns |   3.82 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False      |  20.0458 ns |   3.81 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | False      |   3.8573 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False      |   4.2897 ns |   1.11 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | False      |   4.6646 ns |   1.21 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False      |  16.1279 ns |   4.18 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False      |  20.1477 ns |   5.22 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False      |  20.0966 ns |   5.21 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **True**       |  **10.3028 ns** |   **1.00** |      **-** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | True       |   9.9389 ns |   0.96 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | True       |  27.4102 ns |   2.66 | 0.0019 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | True       |  39.6126 ns |   3.84 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | True       |  86.6112 ns |   8.41 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | True       | 232.2812 ns |  22.55 | 0.0076 |      96 B |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | True       |  32.9812 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | True       |  33.2624 ns |   1.01 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | True       |  66.3701 ns |   2.01 | 0.0019 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | True       | 127.5765 ns |   3.87 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | True       | 129.4090 ns |   3.93 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | True       | 279.2304 ns |   8.47 | 0.0076 |      96 B |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | True       |  12.9622 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | True       |  14.5380 ns |   1.12 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | True       |  36.7851 ns |   2.84 | 0.0019 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | True       |  54.1419 ns |   4.18 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | True       |  55.1882 ns |   4.26 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | True       | 143.4742 ns |  11.07 | 0.0076 |      96 B |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | True       |  37.4942 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True       |  38.1463 ns |   1.02 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | True       |  89.1042 ns |   2.38 | 0.0038 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True       | 148.3694 ns |   3.96 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True       | 153.8677 ns |   4.10 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True       | 358.4055 ns |   9.56 | 0.0153 |      96 B |          NA |
|                                                                  |                    |                    |            |             |        |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | True       |  37.3695 ns |   1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True       |  38.3003 ns |   1.02 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | True       |  89.5365 ns |   2.40 | 0.0038 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True       | 149.2832 ns |   3.99 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True       | 153.7015 ns |   4.11 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True       | 358.3366 ns |   9.59 | 0.0153 |      96 B |          NA |
