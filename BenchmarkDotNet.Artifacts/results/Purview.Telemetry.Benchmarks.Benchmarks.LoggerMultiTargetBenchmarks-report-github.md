```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3


```
| Method                                          | Job                | Runtime            | HasListener | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------------- |------------------- |------------ |----------:|------:|-------:|----------:|------------:|
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **False**       |  **11.87 ns** |  **1.00** | **0.0013** |      **24 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |  27.85 ns |  2.35 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |  26.67 ns |  2.25 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | False       |  23.67 ns |  1.99 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |  18.29 ns |  1.54 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |  12.27 ns |  1.03 | 0.0013 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |  17.84 ns |  1.50 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |  17.66 ns |  1.49 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | False       |  17.55 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |  18.64 ns |  1.06 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |  18.81 ns |  1.07 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | False       |  15.90 ns |  0.91 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |  17.38 ns |  0.99 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |  16.92 ns |  0.96 | 0.0013 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |  31.09 ns |  1.77 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |  31.25 ns |  1.78 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | False       |  14.51 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |  14.67 ns |  1.01 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |  14.60 ns |  1.01 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | False       |  14.34 ns |  0.99 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |  14.66 ns |  1.01 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |  14.34 ns |  0.99 | 0.0013 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |  24.22 ns |  1.67 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |  24.32 ns |  1.68 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |        NA |     ? |     NA |        NA |           ? |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |        NA |     ? |     NA |        NA |           ? |
|                                                 |                    |                    |             |           |       |        |           |             |
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **True**        | **243.70 ns** |  **1.00** | **0.0548** |    **1032 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 226.39 ns |  0.93 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 228.01 ns |  0.94 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | True        | 219.11 ns |  0.90 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        | 220.89 ns |  0.91 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        | 220.03 ns |  0.90 | 0.0548 |    1032 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |  19.27 ns |  0.08 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |  17.73 ns |  0.07 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | True        | 259.01 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 265.32 ns |  1.02 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 263.96 ns |  1.02 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | True        | 252.01 ns |  0.97 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        | 261.70 ns |  1.01 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        | 252.52 ns |  0.98 | 0.0548 |    1032 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |  31.17 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |  30.51 ns |  0.12 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | True        | 238.41 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 243.77 ns |  1.02 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 240.78 ns |  1.01 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | True        | 234.18 ns |  0.98 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 233.81 ns |  0.98 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 243.42 ns |  1.02 | 0.0548 |    1032 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |  27.69 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |  26.07 ns |  0.11 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |        NA |     ? |     NA |        NA |           ? |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |        NA |     ? |     NA |        NA |           ? |

Benchmarks with issues:
  LoggerMultiTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Single-target (generated v1): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Single-target (generated v2): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Single-target (generated v1): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Single-target (generated v2): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Single-target (generated v1): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Single-target (generated v2): full lifecycle': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (manual): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v1): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Multi-target (generated v2): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Single-target (generated v1): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  LoggerMultiTargetBenchmarks.'Single-target (generated v2): full lifecycle': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
