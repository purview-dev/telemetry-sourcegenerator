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
| Method                                                           | Job                | Runtime            | HasLogging | Mean       | Ratio | Allocated | Alloc Ratio |
|----------------------------------------------------------------- |------------------- |------------------- |----------- |-----------:|------:|----------:|------------:|
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **False**      |  **0.1709 ns** |  **1.00** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | False      |  0.1925 ns |  1.13 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | False      |  0.1974 ns |  1.16 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | False      |  0.3366 ns |  1.97 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | False      |  0.7852 ns |  4.60 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | False      |  0.7722 ns |  4.52 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | False      |  0.1795 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | False      |  0.3441 ns |  1.92 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | False      |  0.3797 ns |  2.12 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | False      |  0.9708 ns |  5.43 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | False      |  1.3025 ns |  7.28 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | False      |  1.4316 ns |  8.01 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | False      |  0.1733 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | False      |  0.1705 ns |  0.98 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | False      |  0.1934 ns |  1.12 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | False      |  0.6721 ns |  3.88 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | False      |  1.0975 ns |  6.34 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | False      |  1.4155 ns |  8.18 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | False      |  1.5203 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False      |  1.7568 ns |  1.16 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | False      |  1.8118 ns |  1.19 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False      |  8.4475 ns |  5.56 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False      | 10.2276 ns |  6.73 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False      | 10.6178 ns |  6.99 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | False      |  1.5137 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False      |  1.8478 ns |  1.22 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | False      |  1.7834 ns |  1.18 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False      |  8.9035 ns |  5.90 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False      | 10.2581 ns |  6.79 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False      | 10.3777 ns |  6.87 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **True**       |  **4.3921 ns** |  **1.00** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | True       |  4.4712 ns |  1.02 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | True       |  4.1579 ns |  0.95 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | True       | 17.2970 ns |  3.94 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | True       | 17.7772 ns |  4.05 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | True       | 19.1010 ns |  4.35 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | True       |  8.3116 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | True       |  8.3427 ns |  1.00 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | True       |  7.9456 ns |  0.96 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | True       | 30.3376 ns |  3.65 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | True       | 30.2259 ns |  3.64 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | True       | 31.4344 ns |  3.78 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | True       |  6.8289 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | True       |  7.5779 ns |  1.11 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | True       |  7.3687 ns |  1.08 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | True       | 26.0813 ns |  3.82 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | True       | 27.3537 ns |  4.01 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | True       | 27.3429 ns |  4.01 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | True       | 17.6071 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True       | 16.6134 ns |  0.94 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | True       | 17.7473 ns |  1.01 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True       | 69.9885 ns |  3.98 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True       | 70.2332 ns |  3.99 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True       | 71.3750 ns |  4.06 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | True       | 17.2275 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True       | 17.1368 ns |  0.99 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | True       | 16.9819 ns |  0.99 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True       | 70.1832 ns |  4.07 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True       | 70.4340 ns |  4.09 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True       | 69.9433 ns |  4.06 |         - |          NA |
