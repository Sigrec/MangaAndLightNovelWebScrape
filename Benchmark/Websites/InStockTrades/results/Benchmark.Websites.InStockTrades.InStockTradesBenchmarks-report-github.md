```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4770/24H2/2024Update/HudsonValley)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-RNFBGW : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

WarmupCount=20  

```
| Method            | Mean     | Error    | StdDev   | Median   | Op/s  | Gen0     | Allocated |
|------------------ |---------:|---------:|---------:|---------:|------:|---------:|----------:|
| GetMangaBenchmark | 71.41 ms | 2.391 ms | 6.424 ms | 69.05 ms | 14.00 | 125.0000 |   3.79 MB |
