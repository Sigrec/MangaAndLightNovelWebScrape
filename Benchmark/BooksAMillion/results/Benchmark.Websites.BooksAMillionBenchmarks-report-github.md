```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  Job-OSRLRZ : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

WarmupCount=1  

```
| Method            | Mean     | Error     | StdDev    | Allocated |
|------------------ |---------:|----------:|----------:|----------:|
| GetMangaBenchmark | 1.310 ms | 0.0250 ms | 0.0532 ms | 825.66 KB |
