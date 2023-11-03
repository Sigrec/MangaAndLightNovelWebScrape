namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class AmazonJapan
    {
        private List<string> AmazonJapanLinks = new List<string>();
        private List<EntryModel> AmazonJapanData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Amazon Japan";
        private static readonly Logger LOGGER = LogManager.GetLogger("AmazonJapanLogs");
        private const Region WEBSITE_REGION = Region.Japan;

        //https://www.amazon.co.jp/-/en/s?k=%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%83%88%E3%83%AA%E3%82%AC%E3%83%BC&i=stripbooks&rh=n%3A466280%2Cp_n_condition-type%3A680578011%2Cp_n_availability%3A2227307051%2Cp_n_shipping_option-bin%3A2493950051&s=date-asc-rank&dc&language=en&crid=2U8NK9ZN4YDXT&qid=1671646219&rnid=2493949051&sprefix=%E3%83%AF%E3%83%BC%E3%83%AB%E3%83%89%E3%83%88%E3%83%AA%E3%82%AC%E3%83%BC%2Cstripbooks%2C78&ref=sr_pg_1

        //https://www.amazon.co.jp/-/en/s?k=%E3%83%80%E3%83%B3%E3%83%80%E3%83%80%E3%83%B3&i=stripbooks&rh=n%3A466280%2Cp_n_availability%3A2227307051%2Cp_n_shipping_option-bin%3A2493950051%2Cp_n_condition-type%3A680578011%2Cp_n_binding_browse-bin%3A86141051%2Cp_lbr_three_browse-bin%3A%E9%BE%8D+%E5%B9%B8%E4%BC%B8&s=date-asc-rank&dc&language=en&qid=1671646500&rnid=2217675051&ref=sr_nr_p_lbr_three_browse-bin_1&ds=v1%3A7c%2FWkJW2w0iyZ8PRRuTqSVyeyT8ej15PaoXbjtB12SE
        //string test = @$"https://www.amazon.co.jp/-/en/s?k={title}&i=stripbooks&rh=n%3A466280%2Cp_n_condition-type%3A680578011%2Cp_n_availability%3A2227307051%2Cp_n_shipping_option-bin%3A2493950051&s=date-asc-rank&dc&language=en&crid=2U8NK9ZN4YDXT&qid=1671646219&rnid=2493949051&sprefix={title}stripbooks%2C78&ref=sr_pg_1";

        internal async Task CreateAmazonJapanTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetAmazonJapanData(bookTitle, bookType, driver));
            });
        }

        internal string GetUrl()
        {
            return AmazonJapanLinks.Count != 0 ? AmazonJapanLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        internal void ClearData()
        {
            AmazonJapanLinks.Clear();
            AmazonJapanData.Clear();
        }

        private List<EntryModel> GetAmazonJapanData(string bookTitle, BookType bookType, WebDriver driver)
        {
            return AmazonJapanData;
        }
    }
}