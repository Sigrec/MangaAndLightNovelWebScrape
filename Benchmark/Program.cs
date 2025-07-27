using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using System.Reflection;
using BenchmarkDotNet.Diagnosers;
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
    public static void Main(string[] args)
    {
        // Dictionary to map website names to their corresponding benchmark classes
        Dictionary<string[], Type> websiteBenchmarks = new()
        {
            { [ "Crunchyroll", "CR" ], typeof(CrunchyrollBenchmarks) },
            { [ "InStockTrades", "IST" ], typeof(InStockTradesBenchmarks) },
            { [ "RobertsAnimeCornerStore", "ROB" ], typeof(RobertsAnimeCornerStoreBenchmarks) },
            { [ "BooksAMillion", "BAM" ], typeof(BooksAMillionBenchmarks) },
            { [ "KinokuniyaUSA", "KINOUS" ], typeof(KinokuniyaUSABenchmarks) },
            { [ "SciFier", "SF" ], typeof(SciFierBenchmarks) },
            { [ "MerryManga", "MERRY" ], typeof(MerryMangaBenchmarks) },
            { [ "MangaMart", "MART" ], typeof(MangaMartBenchmarks) },
        };


        // Check if an argument is given, if not, run all benchmarks
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments provided. Running all benchmarks...");
            RunBenchmarks(null, null);
        }
        else
        {
            // If an argument is given, run the specific benchmark
            string website = args[0]; // Assume the first argument is the website name
            if (websiteBenchmarks.Keys.Any(keys => keys.Contains(website)))
            {
                RunBenchmarks(website, websiteBenchmarks);
            }
            else
            {
                Console.WriteLine($"Unknown website: {website}. Please provide a valid website name.");
            }
        }
    }

    private static void RunBenchmarks(string? website, Dictionary<string[], Type>? websiteBenchmarks)
    {

        // Create a custom configuration for BenchmarkDotNet with an output folder
        ManualConfig config = ManualConfig.CreateEmpty()
            .WithOptions(ConfigOptions.JoinSummary)
            .AddExporter(MarkdownExporter.GitHub)
            .AddLogger(ConsoleLogger.Default)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddColumn(StatisticColumn.OperationsPerSecond)
            .WithArtifactsPath(GetArtifactsPath(websiteBenchmarks!
                .FirstOrDefault(entry => entry.Key.Contains(website))
                .Key.FirstOrDefault())
            );

        if (website == null)
        {
            // If no specific website, run all benchmarks
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run([], config);
        }
        else
        {
            // If a specific website is given, run that website's benchmark
            Type benchmarkType = websiteBenchmarks!
                .FirstOrDefault(entry => entry.Key.Contains(website))
                .Value;
            string filter = $"*{benchmarkType.Name}*";

            // Run the specific benchmark with the filter and output configuration
            BenchmarkSwitcher.FromAssembly(benchmarkType.Assembly).Run(["--filter", filter], config);
        }
    }

    private static string GetArtifactsPath(string? website)
    {
        // If a website is provided, use it to create a folder under 'results'
        // Otherwise, create a generic folder for all results
        string folderName = @$"Websites/{website}" ?? "AllBenchmarks";
        Directory.CreateDirectory(@$"Websites/{folderName}"); // Ensure the directory exists
        return folderName;
    }
}