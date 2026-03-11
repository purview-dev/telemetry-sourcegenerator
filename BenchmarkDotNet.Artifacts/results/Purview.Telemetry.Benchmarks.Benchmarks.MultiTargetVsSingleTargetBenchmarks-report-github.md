```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.6649/23H2/2023Update/SunValley3)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2


```
| Method                                                        | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Single-target (generated): start + complete&#39;**                 | **False**       |   **0.5871 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Multi-target (generated): start + complete&#39;                  | False       |  37.5346 ns | 63.94 | 0.0076 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete&#39;                     | False       |  16.1119 ns | 27.45 | 0.0019 |      24 B |          NA |
| &#39;Multi-target (generated): start + complete + record latency&#39; | False       |  40.1067 ns | 68.32 | 0.0076 |      96 B |          NA |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | False       |  15.2997 ns | 26.06 | 0.0019 |      24 B |          NA |
|                                                               |             |             |       |        |           |             |
| **&#39;Single-target (generated): start + complete&#39;**                 | **True**        | **264.4486 ns** |  **1.00** | **0.0801** |    **1008 B** |        **1.00** |
| &#39;Multi-target (generated): start + complete&#39;                  | True        | 293.5554 ns |  1.11 | 0.0877 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete&#39;                     | True        | 302.9957 ns |  1.15 | 0.0820 |    1032 B |        1.02 |
| &#39;Multi-target (generated): start + complete + record latency&#39; | True        | 302.3526 ns |  1.14 | 0.0877 |    1104 B |        1.10 |
| &#39;Multi-target (manual): start + complete + record latency&#39;    | True        | 267.5486 ns |  1.01 | 0.0820 |    1032 B |        1.02 |
