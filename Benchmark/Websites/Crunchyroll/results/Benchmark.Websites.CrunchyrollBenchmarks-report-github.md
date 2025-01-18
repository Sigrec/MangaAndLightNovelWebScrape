```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  Job-NVZNFH : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

WarmupCount=8  

```
| Method            | Mean     | Error    | StdDev   | Gen0      | Gen1      | Gen2      | Allocated |
|------------------ |---------:|---------:|---------:|----------:|----------:|----------:|----------:|
| GetMangaBenchmark | 802.2 ms | 15.36 ms | 16.43 ms | 6000.0000 | 5000.0000 | 2000.0000 |  92.31 MB |
