```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.6649/23H2/2023Update/SunValley3)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2


```
| Method                                                                   | HasLogging | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------------------------------------- |----------- |------------:|------:|-------:|----------:|------------:|
| **&#39;Manual: ILogger.Log — single Info call&#39;**                                 | **False**      |   **0.1890 ns** |  **1.04** |      **-** |         **-** |          **NA** |
| &#39;Manual: LoggerMessage.Define — single Info call&#39;                        | False      |   0.0194 ns |  0.11 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;                 | False      |   0.0608 ns |  0.34 |      - |         - |          NA |
| &#39;Generated v2 (state-based ThreadLocalState) — single Info call&#39;         | False      |   0.1420 ns |  0.78 |      - |         - |          NA |
| &#39;Manual: ILogger.Log — full lifecycle (4 calls)&#39;                         | False      |   0.8401 ns |  4.63 |      - |         - |          NA |
| &#39;Manual: LoggerMessage.Define — full lifecycle (4 calls)&#39;                | False      |   0.7545 ns |  4.16 |      - |         - |          NA |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39;         | False      |   1.3131 ns |  7.24 |      - |         - |          NA |
| &#39;Generated v2 (state-based ThreadLocalState) — full lifecycle (4 calls)&#39; | False      |   1.3252 ns |  7.30 |      - |         - |          NA |
|                                                                          |            |             |       |        |           |             |
| **&#39;Manual: ILogger.Log — single Info call&#39;**                                 | **True**       |  **28.8128 ns** |  **1.00** | **0.0121** |     **152 B** |        **1.00** |
| &#39;Manual: LoggerMessage.Define — single Info call&#39;                        | True       |   6.1272 ns |  0.21 |      - |         - |        0.00 |
| &#39;Generated v1 (LoggerMessage.Define) — single Info call&#39;                 | True       |   6.1731 ns |  0.21 |      - |         - |        0.00 |
| &#39;Generated v2 (state-based ThreadLocalState) — single Info call&#39;         | True       |  16.6159 ns |  0.58 | 0.0019 |      24 B |        0.16 |
| &#39;Manual: ILogger.Log — full lifecycle (4 calls)&#39;                         | True       | 105.6758 ns |  3.67 | 0.0452 |     568 B |        3.74 |
| &#39;Manual: LoggerMessage.Define — full lifecycle (4 calls)&#39;                | True       |  24.2641 ns |  0.84 |      - |         - |        0.00 |
| &#39;Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)&#39;         | True       |  23.9482 ns |  0.83 |      - |         - |        0.00 |
| &#39;Generated v2 (state-based ThreadLocalState) — full lifecycle (4 calls)&#39; | True       |  73.8224 ns |  2.57 | 0.0076 |      96 B |        0.63 |
