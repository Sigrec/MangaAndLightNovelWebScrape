namespace MangaLightNovelWebScrape.Websites.Britain
{
    public class Waterstones
    {
        private List<string> WaterstonesLinks = new List<string>();
        private List<EntryModel> WaterstonesData = new();
        public const string WEBSITE_TITLE = "Waterstones";
        private static readonly Logger LOGGER = LogManager.GetLogger("WaterstonesLogs");
        private const Region WEBSITE_REGION = Region.Britain;
    }
}