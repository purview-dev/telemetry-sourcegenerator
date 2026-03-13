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
| Method                                          | Job                | Runtime            | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------------- |------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **False**       |    **56.25 ns** |  **1.00** | **0.0019** |      **24 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |    53.75 ns |  0.96 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |   132.77 ns |  2.36 | 0.0076 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | False       |    24.88 ns |  0.44 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |    27.79 ns |  0.49 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |    67.67 ns |  1.20 | 0.0076 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |    37.99 ns |  0.68 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |   126.56 ns |  2.25 | 0.0076 |      96 B |        4.00 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | False       |    36.39 ns |  1.00 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |    36.90 ns |  1.01 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |    85.54 ns |  2.35 | 0.0076 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | False       |    35.52 ns |  0.98 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |    37.55 ns |  1.03 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |    85.12 ns |  2.34 | 0.0076 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |    64.76 ns |  1.78 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |   148.52 ns |  4.08 | 0.0076 |      96 B |        4.00 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | False       |    34.53 ns |  1.00 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |    35.27 ns |  1.02 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |    83.61 ns |  2.42 | 0.0076 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | False       |    32.00 ns |  0.93 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |    33.55 ns |  0.97 | 0.0019 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |    87.29 ns |  2.53 | 0.0076 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |    56.72 ns |  1.64 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |   139.89 ns |  4.05 | 0.0076 |      96 B |        4.00 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False       |   158.59 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |   161.55 ns |  1.02 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |   260.35 ns |  1.64 | 0.0153 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False       |   201.33 ns |  1.27 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |   206.99 ns |  1.31 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |   294.34 ns |  1.86 | 0.0153 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |   157.15 ns |  0.99 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |   356.31 ns |  2.25 | 0.0153 |      96 B |        4.00 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False       |   158.22 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |   161.51 ns |  1.02 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |   260.58 ns |  1.65 | 0.0153 |      96 B |        4.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False       |   202.05 ns |  1.28 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |   206.94 ns |  1.31 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |   567.52 ns |  3.59 | 0.0153 |      96 B |        4.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |   156.83 ns |  0.99 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |   355.45 ns |  2.25 | 0.0153 |      96 B |        4.00 |
|                                                 |                    |                    |             |             |       |        |           |             |
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **True**        |   **523.98 ns** |  **1.00** | **0.0820** |    **1032 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        |   528.11 ns |  1.01 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        |   547.85 ns |  1.05 | 0.0877 |    1104 B |        1.07 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | True        |   489.29 ns |  0.93 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        |   490.56 ns |  0.94 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        |   575.96 ns |  1.10 | 0.0877 |    1104 B |        1.07 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |    37.99 ns |  0.07 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |   116.99 ns |  0.22 | 0.0076 |      96 B |        0.09 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | True        |   568.03 ns |  1.00 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        |   582.80 ns |  1.03 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        |   614.12 ns |  1.08 | 0.0877 |    1104 B |        1.07 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | True        |   567.64 ns |  1.00 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        |   567.41 ns |  1.00 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        |   651.88 ns |  1.15 | 0.0877 |    1104 B |        1.07 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |    65.05 ns |  0.11 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |   148.09 ns |  0.26 | 0.0076 |      96 B |        0.09 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | True        |   535.06 ns |  1.00 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        |   544.46 ns |  1.02 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        |   581.11 ns |  1.09 | 0.0877 |    1104 B |        1.07 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | True        |   909.31 ns |  1.70 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        |   890.24 ns |  1.66 | 0.0820 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 1,004.02 ns |  1.88 | 0.0877 |    1104 B |        1.07 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |   110.79 ns |  0.21 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |   259.61 ns |  0.49 | 0.0076 |      96 B |        0.09 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,314.67 ns |  1.00 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 3,353.63 ns |  2.55 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,439.97 ns |  1.10 | 0.2155 |    1364 B |        1.06 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True        | 2,729.57 ns |  2.08 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        | 2,881.26 ns |  2.19 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,475.26 ns |  1.12 | 0.2155 |    1364 B |        1.06 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |   156.93 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |   359.52 ns |  0.27 | 0.0153 |      96 B |        0.07 |
|                                                 |                    |                    |             |             |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,361.33 ns |  1.00 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,309.05 ns |  0.96 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,450.71 ns |  1.07 | 0.2155 |    1364 B |        1.06 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,356.70 ns |  1.00 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,364.44 ns |  1.00 | 0.2041 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,473.92 ns |  1.08 | 0.2155 |    1364 B |        1.06 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |   156.41 ns |  0.11 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |   357.95 ns |  0.26 | 0.0153 |      96 B |        0.07 |
