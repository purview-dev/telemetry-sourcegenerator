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
| **&#39;Single-target (generated): start + complete&#39;**                 | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.5433 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 10.0          | .NET 10.0          | False       |  12.4401 ns | 22.91 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 10.0          | .NET 10.0          | False       |  12.2775 ns | 22.61 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 10.0          | .NET 10.0          | False       |  12.2252 ns | 22.51 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 10.0          | .NET 10.0          | False       |  12.1977 ns | 22.46 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 8.0           | .NET 8.0           | False       |   0.7434 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 8.0           | .NET 8.0           | False       |  18.3970 ns | 24.75 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 8.0           | .NET 8.0           | False       |  18.1918 ns | 24.48 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 8.0           | .NET 8.0           | False       |  19.7234 ns | 26.54 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 8.0           | .NET 8.0           | False       |  18.4687 ns | 24.85 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 9.0           | .NET 9.0           | False       |   0.4970 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 9.0           | .NET 9.0           | False       |  17.4147 ns | 35.10 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 9.0           | .NET 9.0           | False       |  16.2279 ns | 32.70 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 9.0           | .NET 9.0           | False       |  16.1905 ns | 32.63 | 0.0013 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 9.0           | .NET 9.0           | False       |  16.1901 ns | 32.63 | 0.0013 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.7 | .NET Framework 4.7 | False       |  16.0388 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.7 | .NET Framework 4.7 | False       |  73.7333 ns |  4.60 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.7 | .NET Framework 4.7 | False       |  70.8653 ns |  4.42 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  91.6982 ns |  5.72 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |  91.4040 ns |  5.70 | 0.0038 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.8304 ns |  1.00 |      - |         - |          NA |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.8 | .NET Framework 4.8 | False       |  73.4440 ns |  4.64 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.8 | .NET Framework 4.8 | False       |  71.3912 ns |  4.51 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  93.1193 ns |  5.88 | 0.0038 |      24 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |  90.7108 ns |  5.73 | 0.0038 |      24 B |          NA |
|                                                               |                    |                    |             |             |       |        |           |             |
| **&#39;Single-target (generated): start + complete&#39;**                 | **.NET 10.0**          | **.NET 10.0**          | **True**        | **218.7470 ns** |  **1.00** | **0.0534** |    **1008 B** |        **1.00** |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 10.0          | .NET 10.0          | True        | 233.6130 ns |  1.07 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 10.0          | .NET 10.0          | True        | 229.2740 ns |  1.05 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 10.0          | .NET 10.0          | True        | 219.6107 ns |  1.01 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 10.0          | .NET 10.0          | True        | 220.0209 ns |  1.01 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 8.0           | .NET 8.0           | True        | 245.6270 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 8.0           | .NET 8.0           | True        | 254.7508 ns |  1.04 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 8.0           | .NET 8.0           | True        | 270.8041 ns |  1.10 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 8.0           | .NET 8.0           | True        | 267.0294 ns |  1.09 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 8.0           | .NET 8.0           | True        | 282.4102 ns |  1.15 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET 9.0           | .NET 9.0           | True        | 226.7737 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET 9.0           | .NET 9.0           | True        | 272.3747 ns |  1.20 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET 9.0           | .NET 9.0           | True        | 251.6312 ns |  1.11 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET 9.0           | .NET 9.0           | True        | 251.5435 ns |  1.11 | 0.0548 |    1032 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET 9.0           | .NET 9.0           | True        | 231.8545 ns |  1.02 | 0.0548 |    1032 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.7 | .NET Framework 4.7 | True        | 523.7702 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.7 | .NET Framework 4.7 | True        | 620.0768 ns |  1.18 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.7 | .NET Framework 4.7 | True        | 635.6409 ns |  1.21 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 653.9821 ns |  1.25 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        | 639.3929 ns |  1.22 | 0.2050 |    1292 B |        1.02 |
|                                                               |                    |                    |             |             |       |        |           |             |
| &#39;Single-target (generated): start + complete&#39;                 | .NET Framework 4.8 | .NET Framework 4.8 | True        | 535.0704 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Multi-target (generated): start + complete&#39;                  | .NET Framework 4.8 | .NET Framework 4.8 | True        | 606.5414 ns |  1.13 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (manual): start + complete&#39;                     | .NET Framework 4.8 | .NET Framework 4.8 | True        | 631.6500 ns |  1.18 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 626.5998 ns |  1.17 | 0.2050 |    1292 B |        1.02 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        | 649.2190 ns |  1.21 | 0.2050 |    1292 B |        1.02 |
