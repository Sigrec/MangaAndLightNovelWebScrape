using Benchmark.Websites;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using System.Reflection;

public class Program
{
    public static void Main(string[] args)
    {
        // Dictionary to map website names to their corresponding benchmark classes
        Dictionary<string, Type> websiteBenchmarks = new Dictionary<string, Type>
        {
            { "Crunchyroll", typeof(CrunchyrollBenchmarks) },
            { "InStockTrades", typeof(InStockTradesBenchmarks) },
            // Add other benchmarks here as needed
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
            if (websiteBenchmarks.ContainsKey(website))
            {
                RunBenchmarks(website, websiteBenchmarks);
            }
            else
            {
                Console.WriteLine($"Unknown website: {website}. Please provide a valid website name.");
            }
        }
    }

    private static void RunBenchmarks(string? website, Dictionary<string, Type>? websiteBenchmarks)
    {

        // Create a custom configuration for BenchmarkDotNet with an output folder
        var config = ManualConfig.CreateEmpty()
            .WithOptions(ConfigOptions.JoinSummary)
            .AddExporter(MarkdownExporter.GitHub)
            .AddLogger(ConsoleLogger.Default)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .WithArtifactsPath(GetArtifactsPath(website));

        if (website == null)
        {
            // If no specific website, run all benchmarks
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run([], config);
        }
        else
        {
            // If a specific website is given, run that website's benchmark
            Type benchmarkType = websiteBenchmarks[website];
            string filter = $"*{benchmarkType.Name}*";

            // Run the specific benchmark with the filter and output configuration
            BenchmarkSwitcher.FromAssembly(benchmarkType.Assembly).Run(["--filter", filter], config);
        }
    }

    private static string GetArtifactsPath(string? website)
    {
        // If a website is provided, use it to create a folder under 'results'
        // Otherwise, create a generic folder for all results
        string folderName = website ?? "AllBenchmarks";
        Directory.CreateDirectory(folderName); // Ensure the directory exists
        return folderName;
    }
}