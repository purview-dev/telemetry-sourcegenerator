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
| Method                                          | Job                | Runtime            | HasListener | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------------- |------------------- |------------ |----------:|------:|-------:|----------:|------------:|
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **False**       |  **11.90 ns** |  **1.00** | **0.0013** |      **24 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |  11.34 ns |  0.95 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |  31.26 ns |  2.63 | 0.0051 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | False       |  11.35 ns |  0.95 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |  11.49 ns |  0.97 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |  30.38 ns |  2.55 | 0.0051 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |  16.79 ns |  1.41 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |  58.73 ns |  4.94 | 0.0050 |      96 B |        4.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | False       |  16.33 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |  16.70 ns |  1.02 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |  40.56 ns |  2.48 | 0.0051 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | False       |  16.31 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |  17.29 ns |  1.06 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |  39.97 ns |  2.45 | 0.0051 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |  30.24 ns |  1.85 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |  71.83 ns |  4.40 | 0.0050 |      96 B |        4.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | False       |  15.77 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |  15.15 ns |  0.96 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |  37.60 ns |  2.38 | 0.0051 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | False       |  15.75 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |  16.37 ns |  1.04 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |  40.89 ns |  2.59 | 0.0051 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |  26.09 ns |  1.65 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |  63.93 ns |  4.05 | 0.0050 |      96 B |        4.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False       |  67.53 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  68.77 ns |  1.02 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       | 115.80 ns |  1.71 | 0.0153 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False       |  86.11 ns |  1.28 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |  89.68 ns |  1.33 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       | 133.62 ns |  1.98 | 0.0153 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |  69.22 ns |  1.03 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       | 162.15 ns |  2.40 | 0.0153 |      96 B |        4.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False       |  67.25 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  68.86 ns |  1.02 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       | 115.96 ns |  1.72 | 0.0153 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False       |  86.75 ns |  1.29 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |  88.84 ns |  1.32 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       | 129.49 ns |  1.93 | 0.0153 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |  69.23 ns |  1.03 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       | 162.75 ns |  2.42 | 0.0153 |      96 B |        4.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **True**        | **224.02 ns** |  **1.00** | **0.0548** |    **1032 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 218.92 ns |  0.98 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 230.76 ns |  1.03 | 0.0587 |    1104 B |        1.07 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | True        | 210.00 ns |  0.94 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        | 210.11 ns |  0.94 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        | 233.01 ns |  1.04 | 0.0587 |    1104 B |        1.07 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |  17.09 ns |  0.08 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |  54.83 ns |  0.24 | 0.0051 |      96 B |        0.09 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | True        | 246.40 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 248.91 ns |  1.01 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 258.13 ns |  1.05 | 0.0587 |    1104 B |        1.07 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | True        | 247.34 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        | 243.67 ns |  0.99 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        | 265.45 ns |  1.08 | 0.0587 |    1104 B |        1.07 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |  29.23 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |  69.77 ns |  0.28 | 0.0050 |      96 B |        0.09 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | True        | 228.31 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 237.96 ns |  1.04 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 262.22 ns |  1.15 | 0.0587 |    1104 B |        1.07 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | True        | 220.39 ns |  0.97 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 223.93 ns |  0.98 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 249.86 ns |  1.09 | 0.0587 |    1104 B |        1.07 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |  25.08 ns |  0.11 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |  66.60 ns |  0.29 | 0.0050 |      96 B |        0.09 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True        | 575.16 ns |  1.00 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 582.01 ns |  1.01 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 632.07 ns |  1.10 | 0.2165 |    1364 B |        1.06 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True        | 592.29 ns |  1.03 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        | 601.43 ns |  1.05 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        | 652.87 ns |  1.14 | 0.2165 |    1364 B |        1.06 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |  69.08 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        | 165.54 ns |  0.29 | 0.0153 |      96 B |        0.07 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True        | 576.89 ns |  1.00 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 580.12 ns |  1.01 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 633.34 ns |  1.10 | 0.2165 |    1364 B |        1.06 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True        | 591.68 ns |  1.03 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        | 595.25 ns |  1.03 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        | 651.82 ns |  1.13 | 0.2165 |    1364 B |        1.06 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |  68.91 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        | 165.59 ns |  0.29 | 0.0153 |      96 B |        0.07 |
