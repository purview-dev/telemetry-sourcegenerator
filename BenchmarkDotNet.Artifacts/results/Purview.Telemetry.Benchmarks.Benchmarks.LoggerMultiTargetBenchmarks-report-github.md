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
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **False**       |  **11.27 ns** |  **1.00** | **0.0013** |      **24 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |  11.03 ns |  0.98 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |  11.64 ns |  1.03 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | False       |  12.48 ns |  1.11 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |  12.10 ns |  1.07 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | False       |  12.99 ns |  1.15 | 0.0013 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |  17.96 ns |  1.59 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | False       |  17.80 ns |  1.58 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | False       |  18.09 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |  18.87 ns |  1.04 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |  19.10 ns |  1.06 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | False       |  18.88 ns |  1.04 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |  19.43 ns |  1.07 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | False       |  19.36 ns |  1.07 | 0.0013 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |  31.61 ns |  1.75 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | False       |  32.10 ns |  1.77 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | False       |  16.15 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |  17.38 ns |  1.08 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |  16.50 ns |  1.02 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | False       |  17.26 ns |  1.07 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |  16.51 ns |  1.02 | 0.0013 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | False       |  16.13 ns |  1.00 | 0.0013 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |  27.20 ns |  1.68 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | False       |  26.75 ns |  1.66 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | False       |  73.46 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  73.69 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  75.84 ns |  1.03 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | False       |  94.32 ns |  1.28 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |  93.37 ns |  1.27 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | False       |  94.21 ns |  1.28 | 0.0038 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |  72.47 ns |  0.99 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | False       |  72.13 ns |  0.98 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | False       |  72.37 ns |  1.00 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  73.25 ns |  1.01 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  73.74 ns |  1.02 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | False       |  90.78 ns |  1.25 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |  98.78 ns |  1.37 | 0.0038 |      24 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | False       |  95.74 ns |  1.32 | 0.0038 |      24 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |  72.89 ns |  1.01 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | False       |  72.45 ns |  1.00 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| **&#39;Multi-target (manual): start + complete&#39;**       | **.NET 10.0**          | **.NET 10.0**          | **True**        | **237.26 ns** |  **1.00** | **0.0548** |    **1032 B** |        **1.00** |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 229.61 ns |  0.97 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 235.24 ns |  0.99 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 10.0          | .NET 10.0          | True        | 220.70 ns |  0.93 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        | 229.56 ns |  0.97 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 10.0          | .NET 10.0          | True        | 230.89 ns |  0.97 | 0.0548 |    1032 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |  17.83 ns |  0.08 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 10.0          | .NET 10.0          | True        |  17.77 ns |  0.07 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 8.0           | .NET 8.0           | True        | 274.42 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 274.83 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 267.53 ns |  0.98 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 8.0           | .NET 8.0           | True        | 266.80 ns |  0.97 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        | 271.30 ns |  0.99 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 8.0           | .NET 8.0           | True        | 271.51 ns |  0.99 | 0.0548 |    1032 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |  30.73 ns |  0.11 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 8.0           | .NET 8.0           | True        |  30.96 ns |  0.11 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET 9.0           | .NET 9.0           | True        | 254.49 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 255.51 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 242.20 ns |  0.95 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET 9.0           | .NET 9.0           | True        | 243.63 ns |  0.96 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 255.47 ns |  1.00 | 0.0548 |    1032 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET 9.0           | .NET 9.0           | True        | 260.15 ns |  1.02 | 0.0548 |    1032 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |  28.71 ns |  0.11 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET 9.0           | .NET 9.0           | True        |  26.95 ns |  0.11 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.7 | .NET Framework 4.7 | True        | 634.23 ns |  1.00 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 605.31 ns |  0.95 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 624.15 ns |  0.98 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.7 | .NET Framework 4.7 | True        | 636.16 ns |  1.00 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        | 643.34 ns |  1.01 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.7 | .NET Framework 4.7 | True        | 645.00 ns |  1.02 | 0.2050 |    1292 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |  73.52 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.7 | .NET Framework 4.7 | True        |  73.02 ns |  0.12 |      - |         - |        0.00 |
|                                                 |                    |                    |             |           |       |        |           |             |
| &#39;Multi-target (manual): start + complete&#39;       | .NET Framework 4.8 | .NET Framework 4.8 | True        | 631.25 ns |  1.00 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 638.79 ns |  1.01 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 621.33 ns |  0.98 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (manual): full lifecycle&#39;         | .NET Framework 4.8 | .NET Framework 4.8 | True        | 665.51 ns |  1.05 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        | 656.73 ns |  1.04 | 0.2050 |    1292 B |        1.00 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | .NET Framework 4.8 | .NET Framework 4.8 | True        | 649.40 ns |  1.03 | 0.2050 |    1292 B |        1.00 |
| &#39;Single-target (generated v1): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |  72.64 ns |  0.12 |      - |         - |        0.00 |
| &#39;Single-target (generated v2): full lifecycle&#39;  | .NET Framework 4.8 | .NET Framework 4.8 | True        |  72.41 ns |  0.11 |      - |         - |        0.00 |
