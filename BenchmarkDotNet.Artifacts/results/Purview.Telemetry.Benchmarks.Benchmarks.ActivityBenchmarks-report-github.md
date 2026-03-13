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
| Method                        | Job                | Runtime            | HasListener | Mean          | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |------------------- |------------------- |------------ |--------------:|------:|-------:|----------:|------------:|
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **False**       |     **2.5782 ns** |  **1.74** |      **-** |         **-** |          **NA** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |     1.0311 ns |  0.70 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | False       |     1.4219 ns |  0.96 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | False       |     1.0731 ns |  0.73 |      - |         - |          NA |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | False       |     1.4460 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |     1.8381 ns |  1.27 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | False       |     1.7721 ns |  1.23 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | False       |     1.7975 ns |  1.24 |      - |         - |          NA |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | False       |     0.6967 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |     1.0126 ns |  1.45 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | False       |     1.0158 ns |  1.46 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | False       |     1.4163 ns |  2.03 |      - |         - |          NA |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |    35.7988 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |    50.8424 ns |  1.42 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | False       |    35.5485 ns |  0.99 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False       |    35.8570 ns |  1.00 |      - |         - |          NA |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |    35.8623 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |    36.2793 ns |  1.01 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | False       |    35.5060 ns |  0.99 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False       |    44.7760 ns |  1.25 |      - |         - |          NA |
|                               |                    |                    |             |               |       |        |           |             |
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **True**        |   **458.4777 ns** |  **1.00** | **0.0801** |    **1008 B** |        **1.00** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | True        |   475.7015 ns |  1.04 | 0.0801 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | True        |   461.2753 ns |  1.01 | 0.0730 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | True        |   415.9552 ns |  0.91 | 0.0730 |     920 B |        0.91 |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | True        |   553.5538 ns |  1.00 | 0.0801 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | True        |   528.1172 ns |  0.95 | 0.0801 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | True        |   508.9307 ns |  0.92 | 0.0725 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | True        |   493.7759 ns |  0.89 | 0.0725 |     920 B |        0.91 |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | True        |   485.3905 ns |  1.00 | 0.0801 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | True        |   489.9955 ns |  1.01 | 0.0801 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | True        |   459.5960 ns |  0.95 | 0.0730 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | True        |   454.3257 ns |  0.94 | 0.0730 |     920 B |        0.91 |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,152.0836 ns |  1.00 | 0.2003 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,176.3933 ns |  1.02 | 0.2003 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,102.5113 ns |  0.96 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True        | 1,101.9517 ns |  0.96 | 0.1869 |    1179 B |        0.93 |
|                               |                    |                    |             |               |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,173.9111 ns |  1.00 | 0.2003 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,150.7708 ns |  0.98 | 0.2003 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,093.5545 ns |  0.93 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True        | 1,105.8832 ns |  0.94 | 0.1850 |    1171 B |        0.92 |
