```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3


```
| Method                                                        | Job                | Runtime            | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------------------- |------------------- |------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Single-target (generated): start + complete&#39;**                 | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.5495 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 10.0          | .NET 10.0          | False       |  11.4622 ns | 20.88 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 10.0          | .NET 10.0          | False       |  11.7645 ns | 21.43 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 10.0          | .NET 10.0          | False       |  12.1595 ns | 22.15 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 10.0          | .NET 10.0          | False       |  11.8310 ns | 21.55 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 8.0           | .NET 8.0           | False       |   0.7331 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 8.0           | .NET 8.0           | False       |  16.0671 ns | 21.92 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 8.0           | .NET 8.0           | False       |  16.4451 ns | 22.43 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 8.0           | .NET 8.0           | False       |  16.8955 ns | 23.05 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 8.0           | .NET 8.0           | False       |  15.4756 ns | 21.11 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 9.0           | .NET 9.0           | False       |   0.5479 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 9.0           | .NET 9.0           | False       |  14.3090 ns | 26.13 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 9.0           | .NET 9.0           | False       |  13.9639 ns | 25.50 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 9.0           | .NET 9.0           | False       |  15.1283 ns | 27.63 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 9.0           | .NET 9.0           | False       |  14.9992 ns | 27.39 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
|                                                               |                    |                    |             |             |       |        |           |             |
| **&#39;Single-target (generated): start + complete&#39;**                 | **.NET 10.0**          | **.NET 10.0**          | **True**        | **203.3330 ns** |  **1.00** | **0.0534** |    **1008 B** |        **1.00** |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 10.0          | .NET 10.0          | True        | 229.9145 ns |  1.13 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 10.0          | .NET 10.0          | True        | 233.4837 ns |  1.15 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 10.0          | .NET 10.0          | True        | 224.2663 ns |  1.10 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 10.0          | .NET 10.0          | True        | 217.2379 ns |  1.07 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 8.0           | .NET 8.0           | True        | 242.2897 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 8.0           | .NET 8.0           | True        | 257.9860 ns |  1.06 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 8.0           | .NET 8.0           | True        | 254.4583 ns |  1.05 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 8.0           | .NET 8.0           | True        | 253.3515 ns |  1.05 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 8.0           | .NET 8.0           | True        | 261.4624 ns |  1.08 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 9.0           | .NET 9.0           | True        | 219.2015 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 9.0           | .NET 9.0           | True        | 237.4226 ns |  1.08 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 9.0           | .NET 9.0           | True        | 242.8769 ns |  1.11 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 9.0           | .NET 9.0           | True        | 235.9753 ns |  1.08 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 9.0           | .NET 9.0           | True        | 221.2808 ns |  1.01 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |

Benchmarks with issues:
  MultiTargetVsSingleTargetBenchmarks.'Single-target (generated): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete + record latency': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete + record latency': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Single-target (generated): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete + record latency': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete + record latency': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  MultiTargetVsSingleTargetBenchmarks.'Single-target (generated): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete + record latency': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete + record latency': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Single-target (generated): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (generated): start + complete + record latency': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  MultiTargetVsSingleTargetBenchmarks.'Multi-target (manual): start + complete + record latency': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
