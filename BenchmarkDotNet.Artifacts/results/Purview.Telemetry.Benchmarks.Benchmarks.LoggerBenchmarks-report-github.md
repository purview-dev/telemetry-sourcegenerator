```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3


```
| Method                                                           | Job                | Runtime            | HasLogging | Mean       | Ratio | Allocated | Alloc Ratio |
|----------------------------------------------------------------- |------------------- |------------------- |----------- |-----------:|------:|----------:|------------:|
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **False**      |  **0.2067 ns** |  **1.01** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | False      |  0.1827 ns |  0.89 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | False      |  0.2060 ns |  1.00 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | False      |  0.3706 ns |  1.80 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | False      |  0.7469 ns |  3.63 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | False      |  0.7632 ns |  3.71 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | False      |  0.1800 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | False      |  0.3866 ns |  2.15 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | False      |  0.3662 ns |  2.04 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | False      |  0.9202 ns |  5.12 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | False      |  1.2819 ns |  7.13 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | False      |  1.3383 ns |  7.44 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | False      |  0.1814 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | False      |  0.1789 ns |  0.99 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | False      |  0.1658 ns |  0.91 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | False      |  0.5420 ns |  2.99 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | False      |  1.4932 ns |  8.23 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | False      |  1.4658 ns |  8.08 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | False      |         NA |     ? |        NA |           ? |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False      |         NA |     ? |        NA |           ? |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | False      |         NA |     ? |        NA |           ? |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False      |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False      |         NA |     ? |        NA |           ? |
|                                                                  |                    |                    |            |            |       |           |             |
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **True**       |  **4.2946 ns** |  **1.00** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | True       |  4.2436 ns |  0.99 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | True       |  4.1993 ns |  0.98 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | True       | 17.7290 ns |  4.13 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | True       | 19.5227 ns |  4.55 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | True       | 18.8097 ns |  4.38 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | True       |  7.5734 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | True       |  7.3350 ns |  0.97 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | True       |  7.2616 ns |  0.96 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | True       | 28.7313 ns |  3.79 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | True       | 29.8984 ns |  3.95 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | True       | 29.8537 ns |  3.94 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | True       |  6.0953 ns |  1.00 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | True       |  6.2176 ns |  1.02 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | True       |  6.2526 ns |  1.03 |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | True       | 23.8761 ns |  3.92 |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | True       | 24.5516 ns |  4.03 |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | True       | 24.7461 ns |  4.06 |         - |          NA |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | True       |         NA |     ? |        NA |           ? |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True       |         NA |     ? |        NA |           ? |
|                                                                  |                    |                    |            |            |       |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | True       |         NA |     ? |        NA |           ? |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True       |         NA |     ? |        NA |           ? |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True       |         NA |     ? |        NA |           ? |

Benchmarks with issues:
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — single Info call': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=False]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — single Info call': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=False]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — single Info call': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=False]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=False]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=False]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — full lifecycle (4 calls)': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=False]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — single Info call': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=False]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — single Info call': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=False]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — single Info call': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=False]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=False]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=False]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — full lifecycle (4 calls)': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=False]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — single Info call': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=True]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — single Info call': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=True]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — single Info call': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=True]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=True]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=True]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — full lifecycle (4 calls)': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasLogging=True]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — single Info call': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=True]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — single Info call': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=True]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — single Info call': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=True]
  LoggerBenchmarks.'Manual (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=True]
  LoggerBenchmarks.'Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=True]
  LoggerBenchmarks.'Generated v2 (ThreadLocalState) — full lifecycle (4 calls)': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasLogging=True]
