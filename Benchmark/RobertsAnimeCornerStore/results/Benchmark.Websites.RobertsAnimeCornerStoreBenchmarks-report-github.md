```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  Job-VZLDHT : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

WarmupCount=20  

```
| Method            | Mean     | Error   | StdDev   | Gen0      | Allocated |
|------------------ |---------:|--------:|---------:|----------:|----------:|
| GetMangaBenchmark | 379.1 ms | 8.77 ms | 25.46 ms | 8000.0000 |  128.7 MB |
