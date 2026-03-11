```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22631.6649/23H2/2023Update/SunValley3)
Intel Core Ultra 7 155H 1.40GHz, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.200
  [Host]             : .NET 10.0.4 (10.0.4, 10.0.426.12010), X64 RyuJIT x86-64-v3
  .NET 10.0          : .NET 10.0.4 (10.0.4, 10.0.426.12010), X64 RyuJIT x86-64-v3
  .NET 8.0           : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0           : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET Framework 4.7 : .NET Framework 4.8.1 (4.8.9310.0), X64 RyuJIT VectorSize=256
  .NET Framework 4.8 : .NET Framework 4.8.1 (4.8.9310.0), X64 RyuJIT VectorSize=256


```
| Method                        | Job                | Runtime            | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |------------------- |------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.6949 ns** |  **1.01** |      **-** |         **-** |          **NA** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |   0.7505 ns |  1.09 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | False       |   0.8851 ns |  1.29 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | False       |   0.6064 ns |  0.88 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | False       |   0.8246 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |   1.0101 ns |  1.23 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | False       |   0.8429 ns |  1.02 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | False       |   0.8919 ns |  1.08 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | False       |   0.7966 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |   0.9020 ns |  1.13 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | False       |   0.4331 ns |  0.54 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | False       |   0.7325 ns |  0.92 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |  19.7505 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |  19.0691 ns |  0.97 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | False       |  19.5302 ns |  0.99 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False       |  19.7788 ns |  1.00 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |  29.7620 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |  19.9320 ns |  0.67 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | False       |  19.5105 ns |  0.66 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False       |  19.6746 ns |  0.66 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **True**        | **264.6819 ns** |  **1.00** | **0.0801** |    **1008 B** |        **1.00** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 262.7206 ns |  0.99 | 0.0801 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | True        | 245.8103 ns |  0.93 | 0.0732 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | True        | 246.0237 ns |  0.93 | 0.0730 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | True        | 307.2059 ns |  1.00 | 0.0801 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 312.2104 ns |  1.02 | 0.0801 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | True        | 283.1267 ns |  0.92 | 0.0730 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | True        | 277.3910 ns |  0.90 | 0.0730 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | True        | 269.2554 ns |  1.00 | 0.0801 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 270.8638 ns |  1.01 | 0.0801 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | True        | 255.9540 ns |  0.95 | 0.0730 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | True        | 253.5060 ns |  0.94 | 0.0730 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        | 648.3625 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        | 648.1528 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | True        | 610.4529 ns |  0.94 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True        | 725.5029 ns |  1.12 | 0.1869 |    1179 B |        0.93 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        | 681.7943 ns |  1.00 | 0.2012 |    1268 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        | 660.5009 ns |  0.97 | 0.2012 |    1268 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | True        | 857.9679 ns |  1.26 | 0.1869 |    1179 B |        0.93 |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True        | 636.5179 ns |  0.93 | 0.1869 |    1179 B |        0.93 |
