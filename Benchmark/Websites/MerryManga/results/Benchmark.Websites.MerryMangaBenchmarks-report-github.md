```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4484)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2
  Job-XDENTF : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2

WarmupCount=5  

```
| Method            | Mean     | Error     | StdDev    | Op/s  | Allocated |
|------------------ |---------:|----------:|----------:|------:|----------:|
| GetMangaBenchmark | 1.880 ms | 0.0931 ms | 0.2731 ms | 531.9 | 856.84 KB |
