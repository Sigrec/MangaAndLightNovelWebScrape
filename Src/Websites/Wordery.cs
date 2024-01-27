namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Wordery
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("WorderyLogs");
        private List<string> WorderyLinks = new List<string>();
        private List<EntryModel> WorderyData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Wordery";
        public const Region REGION = Region.America | Region.Canada | Region.Europe | Region.Britain | Region.Australia; 
        private static readonly Dictionary<Region, ushort> CURRENCY_DICTIONARY = new Dictionary<Region, ushort>
        {
            {Region.Europe, 1},
            {Region.America, 2},
            {Region.Australia, 3},
            {Region.Canada, 4}
        };
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='c-book__title']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='c-book__price c-price']/text()[1] | //span[@class='c-book__atb'][contains(text(), 'Unavailable')]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//span[@class='c-book__atb']/text()[1]");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//span[@class='js-pnav-max'])[1]");
        private static readonly XPathExpression CurrencyCheckXPath = XPathExpression.Compile("//div[@class='c-crncy-sel']/form/button");

        [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@",|&nbsp;")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

        [GeneratedRegex(@"(?<=Box Set \d{1,3})[^\d].*", RegexOptions.IgnoreCase)] private static partial Regex BoxSetRegex();

        internal async Task CreateWorderyTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, Region curRegion, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetWorderyData(bookTitle, bookType, curRegion, driver));
            });
        }
    
        internal void ClearData()
        {
            WorderyLinks.Clear();
            WorderyData.Clear();
        }

        internal string GetUrl()
        {
            return WorderyLinks.Count != 0 ? WorderyLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum)
        {
            // https://wordery.com/search?viewBy=grid&resultsPerPage=100&term=jujutsu%20kaisen%20novel&page=1&leadTime[]=any&series[]=jujutsu%20kaisen%20novels
            bookTitle = InternalHelpers.FilterBookTitle(bookTitle);
            string url = $"https://wordery.com/search?viewBy=grid&resultsPerPage=100&term={bookTitle}{(bookType == BookType.LightNovel ? "%20novel" : string.Empty)}&page={pageNum}&leadTime[]=any&languages[]=eng&series[]={bookTitle}{(bookType == BookType.LightNovel ? "%20novels" : string.Empty)}";
            LOGGER.Info("Page {} => {}", pageNum, url);
            WorderyLinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }
            else if (BoxSetRegex().IsMatch(entryTitle))
            {
                entryTitle = BoxSetRegex().Replace(entryTitle, string.Empty);
            }

            StringBuilder curTitle = new StringBuilder(entryTitle);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '-');
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '.');
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');

            if (bookType == BookType.LightNovel && !entryTitle.EndsWith("Novel"))
            {
                curTitle.Insert(curTitle.Length - 1, " Novel");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ");
        }

        private List<EntryModel> GetWorderyData(string bookTitle, BookType bookType, Region curRegion, WebDriver driver)
        {
            try
            {
                ushort pageNum = 1;
                string websiteUrl = GenerateWebsiteUrl(bookTitle, bookType, pageNum);
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new HtmlDocument
                {
                    OptionCheckSyntax = false,
                };

                driver.Navigate().GoToUrl(websiteUrl);
                if (curRegion != Region.Britain)
                {
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath($"(//div[@class='c-crncy-sel']/form/button)[{CURRENCY_DICTIONARY[curRegion]}]"))));
                }
                doc.LoadHtml(driver.PageSource);
                ushort maxPageNum = ushort.Parse(doc.DocumentNode.SelectSingleNode(PageCheckXPath).InnerText.Trim());

                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);

                    bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle); 
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        string stockStatus = stockStatusData[x].InnerText.Trim();
                        if (
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) 
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && !stockStatus.Contains("Unavailable")
                        )
                        {
                            WorderyData.Add(
                                new EntryModel
                                (
                                    ParseTitle(FixVolumeRegex().Replace(TitleParseRegex().Replace(entryTitle, " "), "Vol "), bookTitle, bookType),
                                    curRegion switch
                                    {
                                        Region.Australia or Region.Canada => priceData[x].InnerText.Trim()[2..],
                                        _ => priceData[x].InnerText.Trim()
                                    },
                                    StockStatus.IS,
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    if (pageNum < maxPageNum)
                    {
                        websiteUrl = GenerateWebsiteUrl(bookTitle, bookType, ++pageNum);
                        driver.Navigate().GoToUrl(websiteUrl);
                        doc.LoadHtml(driver.PageSource);
                    }
                    else
                    {
                        driver?.Quit();
                        break;
                    }
                }

                WorderyData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, WorderyData, LOGGER);
            }
            catch (Exception ex)
            {
                driver?.Quit();
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }

            return WorderyData;
        }
    }
}