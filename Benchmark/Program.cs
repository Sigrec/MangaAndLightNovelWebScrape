using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Validators;
using System.Reflection;
using Benchmark.Websites.Crunchyroll;
using Benchmark.Websites.RobertsAnimeCornerStore;
using Benchmark.Websites.InStockTrades;
using Benchmark.Websites.KinokuniyaUSA;
using Benchmark.Websites.MangaMart;
using Benchmark.Websites.MerryManga;
using Benchmark.Websites.SciFier;
using Benchmark.Websites.BooksAMillion;

namespace Benchmark;

public class Program
{
    // Single source of truth: maps every alias (full name + short code) to its benchmark Type
    // via case-insensitive string lookup. Previous shape used Dictionary<string[], Type>, which
    // is keyed by array *reference* rather than contents — every lookup had to fall back to a
    // linear scan with .Keys.Any(keys => keys.Contains(...)).
    private static readonly Dictionary<string, Type> Benchmarks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Crunchyroll"] = typeof(CrunchyrollBenchmarks),
        ["CR"]          = typeof(CrunchyrollBenchmarks),

        ["InStockTrades"] = typeof(InStockTradesBenchmarks),
        ["IST"]           = typeof(InStockTradesBenchmarks),

        ["RobertsAnimeCornerStore"] = typeof(RobertsAnimeCornerStoreBenchmarks),
        ["ROB"]                     = typeof(RobertsAnimeCornerStoreBenchmarks),

        ["BooksAMillion"] = typeof(BooksAMillionBenchmarks),
        ["BAM"]           = typeof(BooksAMillionBenchmarks),

        ["KinokuniyaUSA"] = typeof(KinokuniyaUSABenchmarks),
        ["KINOUS"]        = typeof(KinokuniyaUSABenchmarks),

        ["SciFier"] = typeof(SciFierBenchmarks),
        ["SF"]      = typeof(SciFierBenchmarks),

        ["MerryManga"] = typeof(MerryMangaBenchmarks),
        ["MERRY"]      = typeof(MerryMangaBenchmarks),

        ["MangaMart"] = typeof(MangaMartBenchmarks),
        ["MART"]      = typeof(MangaMartBenchmarks),
    };

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Running all benchmarks...");
            RunBenchmarks(null);
            return;
        }

        string requested = args[0];
        if (!Benchmarks.ContainsKey(requested))
        {
            Console.WriteLine($"Unknown website: {requested}. Valid aliases: {string.Join(", ", Benchmarks.Keys)}");
            return;
        }

        RunBenchmarks(requested);
    }

    private static void RunBenchmarks(string? website)
    {
        ManualConfig config = ManualConfig.CreateEmpty()
            .WithOptions(ConfigOptions.JoinSummary)
            .AddExporter(MarkdownExporter.GitHub)
            .AddLogger(ConsoleLogger.Default)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddColumn(StatisticColumn.OperationsPerSecond)
            // FailOnError on the JIT optimizations validator catches accidental Debug runs
            // before they pollute the results with non-optimized numbers.
            .AddValidator(JitOptimizationsValidator.FailOnError)
            .WithArtifactsPath(GetArtifactsPath(website));

        if (website is null)
        {
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run([], config);
        }
        else
        {
            Type benchmarkType = Benchmarks[website];
            string filter = $"*{benchmarkType.Name}*";
            BenchmarkSwitcher.FromAssembly(benchmarkType.Assembly).Run(["--filter", filter], config);
        }
    }

    private static string GetArtifactsPath(string? website)
    {
        // One bucket per benchmark target; the "AllBenchmarks" bucket holds the no-arg run.
        string folderName = $"Websites/{website ?? "AllBenchmarks"}";
        Directory.CreateDirectory(folderName);
        return folderName;
    }
}
