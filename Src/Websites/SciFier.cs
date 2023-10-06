namespace MangaLightNovelWebScrape.Websites
{
    public class SciFier
    {
        private List<string> SciFierLinks = new List<string>();
        private List<EntryModel> SciFierData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "SciFier";
        private static readonly Logger LOGGER = LogManager.GetLogger("SciFierLogs");
        private const Region WEBSITE_REGION = Region.America | Region.Europe | Region.Britain | Region.Canada;

        internal async Task CreateSciFierTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetSciFierData(bookTitle, bookType, driver));
            });
        }

        // Has issues where the search is not very strict unforunate
        private string GetUrl(BookType bookType, string titleText)
        {
            // https://scifier.com/search.php?search_query_adv=jujutsu+kaisen&searchsubs=ON&brand=&price_from=&price_to=&featured=&category%5B%5D=2060&section=product&limit=100
            string url = $"";
            LOGGER.Debug(url);
            SciFierLinks.Add(url);
            return url;
        }

        internal string GetUrl()
        {
            return SciFierLinks.Count != 0 ? SciFierLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        internal void ClearData()
        {
            SciFierLinks.Clear();
            SciFierData.Clear();
        }

        private static string TitleParse(string titleText)
        {
            StringBuilder curTitle = new StringBuilder(titleText);
            return curTitle.ToString();
        }

        private List<EntryModel> GetSciFierData(string bookTitle, BookType bookType, WebDriver driver)
        {
            return SciFierData;
        }
    }
}