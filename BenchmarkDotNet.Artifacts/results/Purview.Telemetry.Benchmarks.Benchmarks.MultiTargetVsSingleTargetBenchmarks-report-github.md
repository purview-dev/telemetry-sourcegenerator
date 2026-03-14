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
| Method                                                        | Job                | Runtime            | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------------------- |------------------- |------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Single-target (generated): start + complete&#39;**                 | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.5278 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 10.0          | .NET 10.0          | False       |  32.4139 ns | 61.46 | 0.0051 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 10.0          | .NET 10.0          | False       |  11.1402 ns | 21.12 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 10.0          | .NET 10.0          | False       |  31.1795 ns | 59.12 | 0.0051 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 10.0          | .NET 10.0          | False       |  11.7568 ns | 22.29 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 8.0           | .NET 8.0           | False       |   0.6965 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 8.0           | .NET 8.0           | False       |  40.7226 ns | 58.47 | 0.0051 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 8.0           | .NET 8.0           | False       |  16.3603 ns | 23.49 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 8.0           | .NET 8.0           | False       |  41.3527 ns | 59.37 | 0.0051 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 8.0           | .NET 8.0           | False       |  16.5292 ns | 23.73 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 9.0           | .NET 9.0           | False       |   0.5241 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 9.0           | .NET 9.0           | False       |  39.6487 ns | 75.67 | 0.0051 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 9.0           | .NET 9.0           | False       |  15.1125 ns | 28.84 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 9.0           | .NET 9.0           | False       |  37.8235 ns | 72.19 | 0.0051 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 9.0           | .NET 9.0           | False       |  16.0761 ns | 30.68 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.7 | .NET Framework 4.7 | False       |  14.5993 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.7 | .NET Framework 4.7 | False       | 117.0178 ns |  8.02 | 0.0153 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.7 | .NET Framework 4.7 | False       |  67.6556 ns |  4.63 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       | 131.8141 ns |  9.03 | 0.0153 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |  85.5416 ns |  5.86 | 0.0038 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.1056 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.8 | .NET Framework 4.8 | False       | 116.8985 ns |  7.74 | 0.0153 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.8 | .NET Framework 4.8 | False       |  67.8095 ns |  4.49 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       | 131.6717 ns |  8.72 | 0.0153 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |  85.7925 ns |  5.68 | 0.0038 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| **&#39;Single-target (generated): start + complete&#39;**                 | **.NET 10.0**          | **.NET 10.0**          | **True**        | **195.7798 ns** |  **1.00** | **0.0534** |    **1008 B** |        **1.00** |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 10.0          | .NET 10.0          | True        | 224.8798 ns |  1.15 | 0.0587 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 10.0          | .NET 10.0          | True        | 221.2457 ns |  1.13 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 10.0          | .NET 10.0          | True        | 249.7377 ns |  1.28 | 0.0587 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 10.0          | .NET 10.0          | True        | 207.7807 ns |  1.06 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 8.0           | .NET 8.0           | True        | 232.6755 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 8.0           | .NET 8.0           | True        | 268.5589 ns |  1.15 | 0.0587 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 8.0           | .NET 8.0           | True        | 246.4678 ns |  1.06 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 8.0           | .NET 8.0           | True        | 267.8886 ns |  1.15 | 0.0587 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 8.0           | .NET 8.0           | True        | 250.7895 ns |  1.08 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 9.0           | .NET 9.0           | True        | 210.9063 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 9.0           | .NET 9.0           | True        | 256.2940 ns |  1.22 | 0.0587 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 9.0           | .NET 9.0           | True        | 232.5587 ns |  1.10 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 9.0           | .NET 9.0           | True        | 250.5832 ns |  1.19 | 0.0587 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 9.0           | .NET 9.0           | True        | 224.5278 ns |  1.06 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.7 | .NET Framework 4.7 | True        | 504.2353 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.7 | .NET Framework 4.7 | True        | 632.6156 ns |  1.25 | 0.2165 |    1364 B |        1.08 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.7 | .NET Framework 4.7 | True        | 571.5370 ns |  1.13 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 650.8462 ns |  1.29 | 0.2165 |    1364 B |        1.08 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        | 589.8619 ns |  1.17 | 0.2031 |    1284 B |        1.01 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.8 | .NET Framework 4.8 | True        | 504.0475 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.8 | .NET Framework 4.8 | True        | 651.3672 ns |  1.29 | 0.2174 |    1372 B |        1.08 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.8 | .NET Framework 4.8 | True        | 575.2488 ns |  1.14 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 646.4075 ns |  1.28 | 0.2165 |    1364 B |        1.08 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        | 588.5810 ns |  1.17 | 0.2050 |    1292 B |        1.02 |
