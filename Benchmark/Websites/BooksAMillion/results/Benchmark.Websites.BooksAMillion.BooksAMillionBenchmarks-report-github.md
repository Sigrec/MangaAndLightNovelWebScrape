```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4770/24H2/2024Update/HudsonValley)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.301
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-BZPTTH : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

WarmupCount=3  

```
| Method                | Mean    | Error   | StdDev  | Op/s   | Gen0      | Gen1      | Gen2      | Allocated |
|---------------------- |--------:|--------:|--------:|-------:|----------:|----------:|----------:|----------:|
| GetMangaDataBenchmark | 54.37 s | 1.077 s | 2.602 s | 0.0184 | 7000.0000 | 5000.0000 | 1000.0000 | 159.65 MB |
