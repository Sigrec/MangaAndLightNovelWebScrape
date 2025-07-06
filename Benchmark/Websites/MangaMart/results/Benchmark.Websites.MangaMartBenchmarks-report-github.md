```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4484/24H2/2024Update/HudsonValley)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2
  Job-AJFIWO : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2

WarmupCount=5  

```
| Method            | Mean     | Error     | StdDev    | Median   | Op/s  | Allocated |
|------------------ |---------:|----------:|----------:|---------:|------:|----------:|
| GetMangaBenchmark | 1.460 ms | 0.0340 ms | 0.0936 ms | 1.431 ms | 684.9 |    854 KB |
