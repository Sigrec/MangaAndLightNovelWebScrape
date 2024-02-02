using Tsundoku.Helpers;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class CDJapan
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("CDJapanLogs");
        public const Region WEBSITE_REGION = Region.Japan;
        public const string WEBSITE_TITLE = "CDJapan";
        private List<string> CDJapanLinks = new List<string>();
        private List<EntryModel> CDJapanData = new List<EntryModel>();
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@id='search-result']//span[@class='title-text']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@id='search-result']//span[@class='price-jp-yen']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@id='search-result']//span[@class='status']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//a[@class='next']");

        [GeneratedRegex(@"(\d{1,3}|\d{1,3}.\d{1})\s+(?:\(.*?\).*|\[.*?\])")] private static partial Regex TitleParseVolRegex();
        [GeneratedRegex(@" \(.*?\)| \[.*?\]")] private static partial Regex TitleRemovalRegex();
        // [GeneratedRegex(@"GN|Graphic Novel|:\s+Volumes|Volumes|:\s+Volume|Volume|Vol\.|:\s+Volumr|Volumr|Volume(\d{1,3})", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

        internal async Task CreateCDJapanTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList)
        {
            string altTitle = await TranslateAPI.ToEnglish(bookTitle, bookType == BookType.Manga ? "MANGA" : "NOVEL") ?? await TranslateAPI.ToRomaji(bookTitle, bookType == BookType.Manga ? "MANGA" : "NOVEL");
            LOGGER.Info("Alt Title = {}", altTitle);

            await Task.Run(() => 
            {
                MasterDataList.Add(GetCDJapanData(bookTitle, altTitle, bookType));
            });
        }
        
        internal void ClearData()
        {
            CDJapanLinks.Clear();
            CDJapanData.Clear();
        }

        internal string GetUrl()
        {
            return CDJapanLinks.Count != 0 ? CDJapanLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private  string GetUrl(string bookTitle, int pageNum, BookType bookType)
        {
            string url = $"https://www.cdjapan.co.jp/searchuni?page={pageNum}&fq.category={(bookType == BookType.Manga ? "UD%3A14" : "UD%3A11")}&q={bookTitle.Replace(" ", "%20")}&order=relasc&opt.exclude_eoa=on&opt.exclude_prx=on";
            CDJapanLinks.Add(url);
            LOGGER.Info("Url = {}", url);
            return url;
        }

        private static string TitleParse(string bookTitle, string entryTitle, string altTitle, BookType bookType)
        {
            entryTitle = TitleRemovalRegex().Replace(TitleParseVolRegex().Replace(entryTitle, $"{(bookType == BookType.LightNovel ? "Novel " : string.Empty)}Vol $1"), string.Empty);

            StringBuilder curTitle = new StringBuilder(entryTitle);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            if (entryTitle.Contains("ようこそ実力至上主義の教室へ"))
            {
                InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "2 Nen Sei Hen", "2年生編");
                InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "2 Nensei", "2年生編");
                InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "2 Nen Sei", "2年生編");
            }
            if (entryTitle.StartsWith("Vol") && !bookTitle.StartsWith("Vol"))
            {
                curTitle.Remove(0, 3);
            }

            return curTitle.ToString().Trim();
        }

        internal List<EntryModel> GetCDJapanData(string bookTitle, string altTitle, BookType bookType)
        {
            try
            {
                // Initialize the html doc for crawling
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = new HtmlDocument();
                int currPageNum = 1;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);

                while (true)
                {
                    doc = web.Load(GetUrl(bookTitle, currPageNum, bookType));
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

                    // In stock
                    // In Stock at Supplier:Usually ships in 2-4 days
                    // Backorder:Usually ships in 1-3 weeks
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string titleText = titleData[x].InnerText.Replace(altTitle, bookTitle, StringComparison.OrdinalIgnoreCase);
                        if (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, titleText) 
                        && (!MasterScrape.EntryRemovalRegex().IsMatch(titleText) || BookTitleRemovalCheck) 
                        && !titleText.Contains("Manga Set"))
                        {
                            CDJapanData.Add(
                                new EntryModel
                                (
                                    TitleParse(bookTitle, titleText, altTitle, bookType),
                                    priceData[x].InnerText.Replace("yen", "¥").Trim(),
                                    stockStatusData[x].InnerText.Trim() switch
                                    {
                                        string curStatus when curStatus.Contains("In Stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                        string curStatus when curStatus.Contains("Out of Stock", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                        string curStatus when curStatus.Contains("Backorder", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                        string curStatus when curStatus.Contains("Pre-Order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                        _ => StockStatus.NA,
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else    
                        {
                            LOGGER.Debug("Removed {}", titleText);
                        }
                    }

                    if (doc.DocumentNode.SelectSingleNode(PageCheckXPath) != null)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        break;
                    }
                }

                CDJapanData.Sort(EntryModel.VolumeSort);
            }
            catch (Exception e)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }
            
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, CDJapanData, LOGGER);
            return CDJapanData;
        }
    }
}