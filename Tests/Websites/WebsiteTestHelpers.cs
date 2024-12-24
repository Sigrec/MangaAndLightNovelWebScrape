namespace Tests.Websites
{
    public partial class WebsiteTestHelpers
    {
        [GeneratedRegex("\\[|\\]")] private static partial Regex RemoveBracketsRegex();

        public static List<EntryModel> ImportDataToList(string path)
        {
            IEnumerable<EntryModel> entryModels = File.ReadLines(path)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => 
                {
                    string[] lineSplit = RemoveBracketsRegex().Replace(line, string.Empty).Split(',', StringSplitOptions.TrimEntries);
                    return new EntryModel(
                        lineSplit[0], 
                        lineSplit[1], 
                        Helpers.GetStockStatusFromString(lineSplit[2]), 
                        lineSplit[3]
                    );
                });

            return entryModels.ToList();
        }
    }
}