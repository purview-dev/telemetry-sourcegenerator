```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.6649/23H2/2023Update/SunValley3)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2


```
| Method                                          | HasListener | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------ |------------ |----------:|------:|-------:|----------:|------------:|
| **&#39;Single-target (generated v2): full lifecycle&#39;**  | **False**       |  **73.45 ns** |  **1.00** | **0.0076** |      **96 B** |        **1.00** |
| &#39;Single-target (generated v1): full lifecycle&#39;  | False       |  24.71 ns |  0.34 |      - |         - |        0.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | False       |  44.14 ns |  0.60 | 0.0076 |      96 B |        1.00 |
| &#39;Multi-target (generated v1): start + complete&#39; | False       |  17.12 ns |  0.23 | 0.0019 |      24 B |        0.25 |
| &#39;Multi-target (manual): start + complete&#39;       | False       |  18.41 ns |  0.25 | 0.0019 |      24 B |        0.25 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | False       |  46.27 ns |  0.63 | 0.0076 |      96 B |        1.00 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | False       |  17.63 ns |  0.24 | 0.0019 |      24 B |        0.25 |
| &#39;Multi-target (manual): full lifecycle&#39;         | False       |  15.50 ns |  0.21 | 0.0019 |      24 B |        0.25 |
|                                                 |             |           |       |        |           |             |
| **&#39;Single-target (generated v2): full lifecycle&#39;**  | **True**        |  **66.56 ns** |  **1.00** | **0.0076** |      **96 B** |        **1.00** |
| &#39;Single-target (generated v1): full lifecycle&#39;  | True        |  23.55 ns |  0.35 |      - |         - |        0.00 |
| &#39;Multi-target (generated v2): start + complete&#39; | True        | 304.22 ns |  4.58 | 0.0877 |    1104 B |       11.50 |
| &#39;Multi-target (generated v1): start + complete&#39; | True        | 304.07 ns |  4.57 | 0.0820 |    1032 B |       10.75 |
| &#39;Multi-target (manual): start + complete&#39;       | True        | 316.11 ns |  4.76 | 0.0820 |    1032 B |       10.75 |
| &#39;Multi-target (generated v2): full lifecycle&#39;   | True        | 303.94 ns |  4.57 | 0.0877 |    1104 B |       11.50 |
| &#39;Multi-target (generated v1): full lifecycle&#39;   | True        | 270.62 ns |  4.07 | 0.0820 |    1032 B |       10.75 |
| &#39;Multi-target (manual): full lifecycle&#39;         | True        | 272.24 ns |  4.10 | 0.0820 |    1032 B |       10.75 |
