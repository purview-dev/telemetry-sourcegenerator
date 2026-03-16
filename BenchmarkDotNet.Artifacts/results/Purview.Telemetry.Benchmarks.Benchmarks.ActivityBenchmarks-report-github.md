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
| Method                        | Job                | Runtime            | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |------------------- |------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.5784 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |   0.5359 ns |  0.93 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | False       |   0.7437 ns |  1.29 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | False       |   0.5580 ns |  0.97 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | False       |   0.7672 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |   0.9231 ns |  1.20 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | False       |   0.9143 ns |  1.19 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | False       |   0.9278 ns |  1.21 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | False       |   0.5706 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |   0.5541 ns |  0.97 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | False       |   0.7391 ns |  1.30 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | False       |   0.7296 ns |  1.28 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.4412 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.8412 ns |  1.03 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.8612 ns |  1.03 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.8348 ns |  1.03 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.9066 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  16.1235 ns |  1.01 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.9997 ns |  1.01 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.8351 ns |  1.00 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **True**        | **220.2907 ns** |  **1.00** | **0.0534** |    **1008 B** |        **1.00** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 221.8544 ns |  1.01 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | True        | 209.8659 ns |  0.95 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | True        | 196.2068 ns |  0.89 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | True        | 246.0644 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 245.1047 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | True        | 236.0602 ns |  0.96 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | True        | 232.7675 ns |  0.95 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | True        | 233.4358 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 223.2543 ns |  0.96 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | True        | 213.2928 ns |  0.91 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | True        | 210.8067 ns |  0.90 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        | 553.1400 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 557.3310 ns |  1.01 | 0.2012 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | True        | 513.8131 ns |  0.93 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True        | 507.1016 ns |  0.92 | 0.1869 |    1179 B |        0.93 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        | 559.9287 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 546.7090 ns |  0.98 | 0.2012 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | True        | 514.7858 ns |  0.92 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True        | 508.8342 ns |  0.91 | 0.1869 |    1179 B |        0.93 |
