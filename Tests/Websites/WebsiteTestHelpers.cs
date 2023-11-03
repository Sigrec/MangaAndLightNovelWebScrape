namespace Tests.Websites
{
    public partial class WebsiteTestHelpers
    {
        [GeneratedRegex("\\[|\\]")]  private static partial Regex RemoveBracketsRegex();

        public static List<EntryModel> ImportDataToList(string path)
        {
            List<EntryModel> dataList = new();
            string[] lineSplit;
            foreach (string line in File.ReadLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lineSplit = RemoveBracketsRegex().Replace(line, "").Split(',', StringSplitOptions.TrimEntries);
                    dataList.Add(new EntryModel(lineSplit[0], lineSplit[1], Helpers.GetStockStatusFromString(lineSplit[2]), lineSplit[3]));
                }
            }
            return dataList;
        }
    }
}