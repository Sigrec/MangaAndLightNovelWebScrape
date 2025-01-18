```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4751/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.102
  [Host]     : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2
  Job-SVNRTB : .NET 9.0.1 (9.0.124.61010), X64 RyuJIT AVX2

WarmupCount=10  

```
| Method            | Mean    | Error    | StdDev   | Gen0       | Gen1      | Gen2      | Allocated |
|------------------ |--------:|---------:|---------:|-----------:|----------:|----------:|----------:|
| GetMangaBenchmark | 2.253 s | 0.0520 s | 0.1507 s | 19000.0000 | 6000.0000 | 2000.0000 | 282.56 MB |
