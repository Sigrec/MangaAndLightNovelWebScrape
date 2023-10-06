namespace MangaLightNovelWebScrape.Websites.Britain
{
    public class Waterstones
    {
        private List<string> WaterstonesLinks = new List<string>();
        private List<EntryModel> WaterstonesData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Waterstones";
        private static readonly Logger LOGGER = LogManager.GetLogger("WaterstonesLogs");
        private const Region WEBSITE_REGION = Region.Britain;

        // https://forbiddenplanet.com/catalog/comics-and-graphic-novels/manga/?utm_source=mainnav&utm_campaign=comics-and-graphic-novels&page=1

        internal async Task CreateWaterstonesTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetWaterstonesData(bookTitle, bookType, driver));
            });
        }

        internal void ClearData()
        {
            WaterstonesLinks.Clear();
            WaterstonesData.Clear();
        }

        internal string GetUrl()
        {
            return WaterstonesLinks.Count != 0 ? WaterstonesLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private List<EntryModel> GetWaterstonesData(string bookTitle, BookType bookType, WebDriver driver)
        {
            return WaterstonesData;
        }
    }
}