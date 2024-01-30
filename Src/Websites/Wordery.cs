namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Wordery
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("WorderyLogs");
        private List<string> WorderyLinks = new List<string>();
        private List<EntryModel> WorderyData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Wordery";
        public const Region REGION = Region.America | Region.Canada | Region.Europe | Region.Britain | Region.Australia; 
        private static readonly Dictionary<Region, string> CURRENCY_DICTIONARY = new Dictionary<Region, string>
        {
            {Region.Europe, " Euro "},
            {Region.Britain, " British Pound "},
            {Region.America, " US Dollar "},
            {Region.Australia, " Australian Dollar "},
            {Region.Canada, " Canadian Dollar "}
        };
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[@class='c-book__title']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='c-book__price c-price']/text()[1] | //span[@class='c-book__atb'][contains(text(), 'Unavailable')]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//span[@class='c-book__atb']/text()[1]");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//span[@class='js-pnav-max'])[1]");
        private static readonly XPathExpression BookFormatXPath = XPathExpression.Compile("//small[@class='c-book__meta']");
        private static readonly XPathExpression StaffXPath = XPathExpression.Compile("//span[@class='c-book__by']");

        [GeneratedRegex(@"\d{1,3}.\d{1}", RegexOptions.IgnoreCase)] internal static partial Regex MultiVolNumCheck();
        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@",|&nbsp;|The Manga|Manga|\(.*\)| HARDCOVER", RegexOptions.IgnoreCase)] private static partial Regex TitleParseRegex();
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
            string url = string.Empty;
            if (bookType == BookType.Manga)
            {
                bookTitle = InternalHelpers.FilterBookTitle(bookTitle);
                url = $"https://wordery.com/search?viewBy=grid&resultsPerPage=100&term={bookTitle}&page={pageNum}&leadTime[]=any&languages[]=eng&series[]={bookTitle}";
            }
            else if (bookType == BookType.LightNovel)
            {
                url = $"https://wordery.com/search?page={pageNum}&term={bookTitle.Replace(" ", "+")}+light+novel";
            }
            LOGGER.Info("Page {} => {}", pageNum, url);
            WorderyLinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType, string formatAndLang)
        {
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }
            else if (BoxSetRegex().IsMatch(entryTitle))
            {
                entryTitle = BoxSetRegex().Replace(entryTitle, string.Empty);
            }

            StringBuilder curTitle = new StringBuilder(TitleParseRegex().Replace(entryTitle, " "));
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            if (!MultiVolNumCheck().IsMatch(entryTitle))
            {
                InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '.');
            }

            if (entryTitle.Contains("Special Edition"))
            {
                int index = curTitle.ToString().IndexOf("Special Edition");
                curTitle.Remove(index, curTitle.Length - index);
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, " Special Edition ");
            }

            string volNum = MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Groups[0].Value.Trim();
            if (volNum.StartsWith('0') && volNum.Length > 1)
            {
                curTitle.Replace(volNum, volNum.TrimStart('0'));
            }

            //LOGGER.Debug("{} | {}", curTitle.ToString(), !curTitle.ToString().Contains("Novel"));
            if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel"))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index - 5, "Novel ");
            }
            else if (bookType == BookType.Manga && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Vol") && !entryTitle.Contains("Anniversary", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(curTitle.ToString()) && !entryTitle.Contains("Part") && !bookTitle.Contains("Part"))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Vol ");
            }

            // LOGGER.Debug("{} | {} | {} | {} | {}", curTitle.ToString(), formatAndLang, bookTitle.Equals("fullmetal alchemist", StringComparison.OrdinalIgnoreCase), formatAndLang.Contains("Board book", StringComparison.OrdinalIgnoreCase), !bookTitle.Equals("Fullmetal Edition", StringComparison.OrdinalIgnoreCase));
            if (bookTitle.Equals("fullmetal alchemist", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Anniversary") && formatAndLang.Contains("Board book", StringComparison.OrdinalIgnoreCase) && !bookTitle.Equals("Fullmetal Edition", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Insert(MasterScrape.FindVolWithNumRegex().Match(curTitle.ToString().Trim()).Index, " Fullmetal Edition ");
            }
            else if (bookTitle.Equals("berserk", StringComparison.OrdinalIgnoreCase) && formatAndLang.Contains("Board book", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Deluxe", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Insert(MasterScrape.FindVolWithNumRegex().Match(curTitle.ToString().Trim()).Index, " Deluxe ");
            }
            else if (bookTitle.Equals("attack on titan", StringComparison.OrdinalIgnoreCase))
            {
                if (entryTitle.Contains("The Final Season") && entryTitle.Contains("Part 1"))
                {
                    curTitle = new StringBuilder("Attack on Titan The Final Season Part 1 Box Set");
                }
                else if (entryTitle.Contains("The Final Season") && !entryTitle.Contains("Box Set"))
                {
                    curTitle.Insert(curTitle.Length, " Box Set");
                }
            }

            if (curTitle.ToString().Contains("Box Set") && curTitle.ToString().Contains("Vol"))
            {
                curTitle.Replace("Vol", string.Empty);
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetWorderyData(string bookTitle, BookType bookType, Region curRegion, WebDriver driver)
        {
            try
            {
                ushort pageNum = 1;
                string websiteUrl = GenerateWebsiteUrl(bookTitle, bookType, pageNum);
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(10));
                HtmlDocument doc = new HtmlDocument
                {
                    OptionCheckSyntax = false,
                };

                driver.Navigate().GoToUrl(websiteUrl);
                wait.Until(driver => driver.FindElement(By.XPath("//div[@id='results']")));
                if (curRegion != Region.Britain)
                {
                    LOGGER.Info("Changing Region to {}", curRegion.ToString());
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath($"//div[@class='c-crncy-sel']/form/button[text()='{CURRENCY_DICTIONARY[curRegion]}']"))));
                }
                doc.LoadHtml(driver.PageSource);
                ushort maxPageNum = ushort.Parse(doc.DocumentNode.SelectSingleNode(PageCheckXPath).InnerText.Trim());
                bool oneShotCheck = false;

                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNodeCollection staffData = doc.DocumentNode.SelectNodes(StaffXPath);
                    HtmlNodeCollection bookFormatAndLangData = doc.DocumentNode.SelectNodes(BookFormatXPath);

                    bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle); 
                    OneShotRetry:
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        string stockStatus = stockStatusData[x].InnerText.Trim();
                        string formatAndLang = bookFormatAndLangData[x].InnerText.Trim();
                        string staff = staffData[x].InnerText.Trim();
                        if (
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) 
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && !formatAndLang.Contains("Foam Book")
                            && formatAndLang.Contains("English", StringComparison.OrdinalIgnoreCase)
                            && !stockStatus.Contains("Unavailable", StringComparison.OrdinalIgnoreCase)
                            && (
                                !(
                                    bookType == BookType.Manga
                                    && !entryTitle.Contains("First Stall")
                                    && (
                                        entryTitle.Contains("light novel", StringComparison.OrdinalIgnoreCase)
                                        || staff.Contains("Illustrator", StringComparison.OrdinalIgnoreCase)
                                        || staff.Contains("artist", StringComparison.OrdinalIgnoreCase)
                                        || (
                                            !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase)
                                            && !entryTitle.Contains("Comics", StringComparison.OrdinalIgnoreCase)
                                            && !bookTitle.AsParallel().Any(char.IsDigit)
                                            && !entryTitle.AsParallel().Any(char.IsDigit)
                                        )
                                    )
                                )
                                ||
                                oneShotCheck
                            )
                        )
                        {
                            entryTitle = ParseTitle(FixVolumeRegex().Replace(entryTitle.Trim(), "Vol "), bookTitle, bookType, formatAndLang);
                            // LOGGER.Debug("{} | {} | {}", entryTitle, formatAndLang.Contains("Hardcover"), WorderyData.Exists(entry => entry.Entry.Equals(entryTitle)));
                            // LOGGER.Debug("{} | {} | {} | {}", entryTitle, stockStatus, formatAndLang, staff);
                            //  || !(formatAndLang.Contains("Hardback", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Special Edition") && WorderyData.Exists(entry => entry.Entry.Equals(entryTitle))
                            if (!WorderyData.Exists(entry => entry.Entry.Equals(entryTitle)))
                            {
                                WorderyData.Add(
                                    new EntryModel
                                    (
                                        entryTitle,
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
                            else
                            {
                                LOGGER.Info("Removed2 {} {}", entryTitle, formatAndLang);
                            }
                        }
                        else
                        {
                            LOGGER.Info("Removed1 {}", entryTitle);
                        }
                    }

                    if (pageNum < maxPageNum)
                    {
                        websiteUrl = GenerateWebsiteUrl(bookTitle, bookType, ++pageNum);
                        driver.Navigate().GoToUrl(websiteUrl);
                        doc.LoadHtml(driver.PageSource);
                    }
                    else if (!oneShotCheck && WorderyData.Count == 0)
                    {
                        oneShotCheck = true;
                        LOGGER.Info("Retry for OneShot");
                        goto OneShotRetry;
                    }
                    else
                    {
                        break;
                    }
                }

                WorderyData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, WorderyData, LOGGER);
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }
            driver?.Quit();

            return WorderyData;
        }
    }
}