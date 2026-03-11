```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.6649/23H2/2023Update/SunValley3)
Intel Core Ultra 7 155H, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.200
  [Host]     : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.4 (10.0.426.12010), X64 RyuJIT AVX2


```
| Method                                  | Mean      | Ratio | Allocated | Alloc Ratio |
|---------------------------------------- |----------:|------:|----------:|------------:|
| &#39;0 tags (no TagList): histogram record&#39; | 2.8163 ns |  1.00 |         - |          NA |
| &#39;1 tag (no TagList): auto-counter add&#39;  | 0.5598 ns |  0.20 |         - |          NA |
| &#39;3 tags (no TagList): histogram record&#39; | 0.9706 ns |  0.34 |         - |          NA |
| &#39;4 tags (TagList): auto-counter add&#39;    | 4.5737 ns |  1.62 |         - |          NA |
| &#39;5 tags (TagList): auto-counter add&#39;    | 6.7153 ns |  2.38 |         - |          NA |
| &#39;6 tags (TagList): histogram record&#39;    | 7.0297 ns |  2.50 |         - |          NA |
