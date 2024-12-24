```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.101
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  Job-NDJULS : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

WarmupCount=20  

```
| Method            | Mean     | Error    | StdDev   | Gen0       | Gen1     | Allocated |
|------------------ |---------:|---------:|---------:|-----------:|---------:|----------:|
| GetMangaBenchmark | 294.8 ms | 22.58 ms | 65.88 ms | 23250.0000 | 500.0000 | 371.58 MB |
