```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4751/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.102
  [Host]     : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-NJMEYD : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2

WarmupCount=5  

```
| Method            | Mean     | Error    | StdDev   | Allocated |
|------------------ |---------:|---------:|---------:|----------:|
| GetMangaBenchmark | 294.9 μs | 11.16 μs | 32.39 μs |  72.47 KB |
