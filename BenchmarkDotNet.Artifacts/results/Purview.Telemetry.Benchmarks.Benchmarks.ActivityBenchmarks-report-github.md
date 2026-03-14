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
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.5075 ns** |  **1.01** |      **-** |         **-** |          **NA** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |   0.5499 ns |  1.09 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | False       |   0.7297 ns |  1.45 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | False       |   0.4813 ns |  0.95 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | False       |   0.6989 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |   0.6744 ns |  0.97 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | False       |   0.9588 ns |  1.37 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | False       |   0.9360 ns |  1.34 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | False       |   0.5264 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |   0.5383 ns |  1.02 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | False       |   0.7020 ns |  1.33 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | False       |   0.6809 ns |  1.29 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.3977 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  22.1166 ns |  1.44 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.2635 ns |  0.99 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False       |  15.0530 ns |  0.98 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |  22.8523 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.7769 ns |  0.69 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.2753 ns |  0.67 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False       |  15.5517 ns |  0.68 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **True**        | **192.3834 ns** |  **1.00** | **0.0534** |    **1008 B** |        **1.00** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 195.9753 ns |  1.02 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | True        | 185.8533 ns |  0.97 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | True        | 186.8529 ns |  0.97 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | True        | 230.4829 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 232.4754 ns |  1.01 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | True        | 228.7053 ns |  0.99 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | True        | 225.5272 ns |  0.98 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | True        | 204.7535 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 208.4428 ns |  1.02 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | True        | 210.5649 ns |  1.03 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | True        | 205.1200 ns |  1.00 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        | 520.9882 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 520.6209 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | True        | 502.7597 ns |  0.97 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True        | 491.6241 ns |  0.94 | 0.1869 |    1179 B |        0.93 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        | 520.3960 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 512.7631 ns |  0.99 | 0.2012 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | True        | 500.6304 ns |  0.96 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True        | 489.9126 ns |  0.94 | 0.1869 |    1179 B |        0.93 |
