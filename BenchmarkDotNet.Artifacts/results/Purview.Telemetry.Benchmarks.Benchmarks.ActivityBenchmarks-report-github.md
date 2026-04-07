```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i9-13900KF 3.00GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3


```
| Method                        | Job                | Runtime            | HasListener | Mean        | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |------------------- |------------------- |------------ |------------:|------:|-------:|----------:|------------:|
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **False**       |   **0.5619 ns** |  **1.00** |      **-** |         **-** |          **NA** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | False       |   0.5529 ns |  0.99 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | False       |   0.7231 ns |  1.29 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | False       |   0.5207 ns |  0.93 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | False       |   0.7059 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | False       |   0.7146 ns |  1.01 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | False       |   0.9147 ns |  1.30 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | False       |   0.9031 ns |  1.28 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | False       |   0.5421 ns |  1.00 |      - |         - |          NA |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | False       |   0.5476 ns |  1.01 |      - |         - |          NA |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | False       |   0.7345 ns |  1.36 |      - |         - |          NA |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | False       |   0.7043 ns |  1.30 |      - |         - |          NA |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | False       |          NA |     ? |     NA |        NA |           ? |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | False       |          NA |     ? |     NA |        NA |           ? |
|                               |                    |                    |             |             |       |        |           |             |
| **&#39;Manual: start + complete&#39;**    | **.NET 10.0**          | **.NET 10.0**          | **True**        | **217.7505 ns** |  **1.00** | **0.0534** |    **1008 B** |        **1.00** |
| &#39;Generated: start + complete&#39; | .NET 10.0          | .NET 10.0          | True        | 204.0318 ns |  0.94 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 10.0          | .NET 10.0          | True        | 198.4338 ns |  0.91 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 10.0          | .NET 10.0          | True        | 189.2585 ns |  0.87 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 8.0           | .NET 8.0           | True        | 241.1073 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 8.0           | .NET 8.0           | True        | 250.4936 ns |  1.04 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 8.0           | .NET 8.0           | True        | 223.8695 ns |  0.93 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 8.0           | .NET 8.0           | True        | 222.1394 ns |  0.92 | 0.0486 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET 9.0           | .NET 9.0           | True        | 216.8368 ns |  1.00 | 0.0534 |    1008 B |        1.00 |
| &#39;Generated: start + complete&#39; | .NET 9.0           | .NET 9.0           | True        | 214.2963 ns |  0.99 | 0.0534 |    1008 B |        1.00 |
| &#39;Manual: start + fail&#39;        | .NET 9.0           | .NET 9.0           | True        | 200.4337 ns |  0.92 | 0.0489 |     920 B |        0.91 |
| &#39;Generated: start + fail&#39;     | .NET 9.0           | .NET 9.0           | True        | 222.1402 ns |  1.03 | 0.0489 |     920 B |        0.91 |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + complete&#39; | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.7 | .NET Framework 4.7 | True        |          NA |     ? |     NA |        NA |           ? |
|                               |                    |                    |             |             |       |        |           |             |
| &#39;Manual: start + complete&#39;    | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + complete&#39; | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Manual: start + fail&#39;        | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |
| &#39;Generated: start + fail&#39;     | .NET Framework 4.8 | .NET Framework 4.8 | True        |          NA |     ? |     NA |        NA |           ? |

Benchmarks with issues:
  ActivityBenchmarks.'Manual: start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  ActivityBenchmarks.'Generated: start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  ActivityBenchmarks.'Manual: start + fail': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  ActivityBenchmarks.'Generated: start + fail': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=False]
  ActivityBenchmarks.'Manual: start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  ActivityBenchmarks.'Generated: start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  ActivityBenchmarks.'Manual: start + fail': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  ActivityBenchmarks.'Generated: start + fail': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=False]
  ActivityBenchmarks.'Manual: start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  ActivityBenchmarks.'Generated: start + complete': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  ActivityBenchmarks.'Manual: start + fail': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  ActivityBenchmarks.'Generated: start + fail': .NET Framework 4.7(Runtime=.NET Framework 4.7) [HasListener=True]
  ActivityBenchmarks.'Manual: start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  ActivityBenchmarks.'Generated: start + complete': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  ActivityBenchmarks.'Manual: start + fail': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
  ActivityBenchmarks.'Generated: start + fail': .NET Framework 4.8(Runtime=.NET Framework 4.8) [HasListener=True]
