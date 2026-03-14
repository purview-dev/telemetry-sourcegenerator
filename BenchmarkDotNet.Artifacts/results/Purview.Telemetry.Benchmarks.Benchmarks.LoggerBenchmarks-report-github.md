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
| Method                                                           | Job                | Runtime            | HasLogging | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------------------------------------- |------------------- |------------------- |----------- |------------:|------:|-------:|----------:|------------:|
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **False**      |   **0.1894 ns** |  **1.02** |      **-** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | False      |   0.1881 ns |  1.02 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | False      |   0.1763 ns |  0.95 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | False      |   0.3521 ns |  1.90 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | False      |   0.7179 ns |  3.88 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | False      |   0.6440 ns |  3.48 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | False      |   0.1726 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | False      |   0.3460 ns |  2.01 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | False      |   0.7087 ns |  4.11 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | False      |   0.8794 ns |  5.10 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | False      |   1.2677 ns |  7.35 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | False      |   3.3236 ns | 19.28 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | False      |   0.1733 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | False      |   0.1750 ns |  1.01 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | False      |   0.5362 ns |  3.10 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | False      |   0.5240 ns |  3.03 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | False      |   1.4125 ns |  8.16 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | False      |   2.6294 ns | 15.19 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | False      |   1.4322 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False      |   1.5678 ns |  1.10 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | False      |   1.7495 ns |  1.22 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False      |   8.0689 ns |  5.64 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False      |   9.8148 ns |  6.86 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False      |   9.7959 ns |  6.84 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | False      |   1.4087 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False      |   1.5921 ns |  1.13 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | False      |   1.7372 ns |  1.23 |      - |         - |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False      |   8.1368 ns |  5.78 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False      |   9.8110 ns |  6.97 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False      |   9.7778 ns |  6.94 |      - |         - |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| **&#39;Manual (LoggerMessage.Define) — single Info call&#39;**               | **.NET 10.0**          | **.NET 10.0**          | **True**       |   **4.0881 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 10.0          | .NET 10.0          | True       |   4.2286 ns |  1.03 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 10.0          | .NET 10.0          | True       |  11.5980 ns |  2.84 | 0.0013 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 10.0          | .NET 10.0          | True       |  16.8506 ns |  4.12 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 10.0          | .NET 10.0          | True       |  16.7257 ns |  4.09 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 10.0          | .NET 10.0          | True       |  51.7753 ns | 12.67 | 0.0051 |      96 B |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 8.0           | .NET 8.0           | True       |   7.8904 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 8.0           | .NET 8.0           | True       |   7.9261 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 8.0           | .NET 8.0           | True       |  16.2818 ns |  2.06 | 0.0013 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 8.0           | .NET 8.0           | True       |  28.7230 ns |  3.64 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 8.0           | .NET 8.0           | True       |  29.8486 ns |  3.78 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 8.0           | .NET 8.0           | True       |  69.5641 ns |  8.82 | 0.0050 |      96 B |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET 9.0           | .NET 9.0           | True       |   6.7171 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET 9.0           | .NET 9.0           | True       |   7.2279 ns |  1.08 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET 9.0           | .NET 9.0           | True       |  15.4979 ns |  2.31 | 0.0013 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET 9.0           | .NET 9.0           | True       |  24.6779 ns |  3.67 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET 9.0           | .NET 9.0           | True       |  27.3735 ns |  4.08 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET 9.0           | .NET 9.0           | True       |  67.6062 ns | 10.07 | 0.0050 |      96 B |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.7 | .NET Framework 4.7 | True       |  16.3008 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True       |  16.6508 ns |  1.02 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.7 | .NET Framework 4.7 | True       |  41.2773 ns |  2.53 | 0.0038 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True       |  66.3620 ns |  4.07 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True       |  68.2500 ns |  4.19 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True       | 164.5308 ns | 10.09 | 0.0153 |      96 B |          NA |
|                                                                  |                    |                    |            |             |       |        |           |             |
| &#39;Manual (LoggerMessage.Define) — single Info call&#39;               | .NET Framework 4.8 | .NET Framework 4.8 | True       |  16.2296 ns |  1.00 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True       |  16.5224 ns |  1.02 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — single Info call&#39;             | .NET Framework 4.8 | .NET Framework 4.8 | True       |  40.8608 ns |  2.52 | 0.0038 |      24 B |          NA |
| &#39;Manual (LoggerMessage.Define) — full lifecycle (4 calls)&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True       |  66.1739 ns |  4.08 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True       |  68.5483 ns |  4.22 |      - |         - |          NA |
| &#39;Generated v2 (ThreadLocalState) — full lifecycle (4 calls)&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True       | 164.3724 ns | 10.13 | 0.0153 |      96 B |          NA |
