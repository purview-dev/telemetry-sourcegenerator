```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26220.5770)
13th Gen Intel Core i9-13900KF, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.100-preview.7.25380.108
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  Job-SZTKAZ : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2

IterationCount=5  WarmupCount=1  

```
| Method            | Methods | Mean       | Error     | StdDev   | Gen0   | Gen1   | Allocated |
|------------------ |-------- |-----------:|----------:|---------:|-------:|-------:|----------:|
| **LegacyBuilder**     | **5**       |   **280.9 ns** |  **28.93 ns** |  **7.51 ns** | **0.2689** | **0.0033** |   **4.95 KB** |
| CodeWriterAdapter | 5       | 2,072.6 ns |  77.43 ns | 11.98 ns | 1.8158 | 0.0458 |  33.42 KB |
| **LegacyBuilder**     | **25**      |   **388.1 ns** |  **46.61 ns** | **12.11 ns** | **0.3047** | **0.0033** |    **5.6 KB** |
| CodeWriterAdapter | 25      | 2,271.8 ns |  41.28 ns |  6.39 ns | 1.8692 | 0.0458 |  34.39 KB |
| **LegacyBuilder**     | **75**      |   **614.4 ns** |  **25.67 ns** |  **6.67 ns** | **0.5474** | **0.0124** |  **10.08 KB** |
| CodeWriterAdapter | 75      | 2,861.0 ns | 202.31 ns | 52.54 ns | 2.2240 | 0.0763 |  40.91 KB |
