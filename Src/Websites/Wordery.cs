namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Wordery
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("WorderyLogs");
        private List<string> WorderyLinks = new List<string>();
        private List<EntryModel> WorderyData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Wordery";
        public const Region REGION = Region.America | Region.Canada | Region.Europe | Region.Britain | Region.Australia; 
        private static readonly Dictionary<Region, Tuple<string, string>> CURRENCY_DICTIONARY = new Dictionary<Region, Tuple<string, string>>
        {
            {Region.Europe, new Tuple<string, string>(" Euro ", "EUR")},
            {Region.Britain, new Tuple<string, string>(" British Pound ", "GBP")},
            {Region.America, new Tuple<string, string>(" US Dollar ", "USD")},
            {Region.Australia, new Tuple<string, string>(" Australian Dollar ", "AUD")},
            {Region.Canada, new Tuple<string, string>(" Canadian Dollar ", "CAD")}
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
        [GeneratedRegex(@"(?<=Vol\s+(?:\d{1,3}|\d{1,3}.\d{1,2})[^\d]).*", RegexOptions.IgnoreCase)] private static partial Regex FindVolWithNumRegex();

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

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum, bool secondNovelCheck)
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
                if (secondNovelCheck)
                {
                    bookTitle = InternalHelpers.FilterBookTitle(bookTitle);
                    url = $"https://wordery.com/search?viewBy=grid&resultsPerPage=100&term={bookTitle}%20novel&page=1&leadTime[]=any&languages[]=eng&series[]={bookTitle.ToLower()}%20novels";
                }
                else
                {
                    url = $"https://wordery.com/search?page={pageNum}&term={bookTitle.Replace(" ", "+")}+light+novel";
                }
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
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "?", " ");
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            if (bookTitle.Equals("boruto", StringComparison.OrdinalIgnoreCase)) { curTitle.Replace(" : Naruto Next Generations", string.Empty); }
            if (!MultiVolNumCheck().IsMatch(entryTitle))
            {
                InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '.');
            }

            if (entryTitle.Contains("Special Edition"))
            {
                int index = curTitle.ToString().IndexOf("Special Edition");
                curTitle.Remove(index, curTitle.Length - index);
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, " Special Edition ");
            }
            curTitle.TrimEnd();

            string volNum = MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Groups[0].Value.Trim();
            if (volNum.StartsWith('0') && volNum.Length > 1)
            {
                curTitle.Replace(volNum, volNum.TrimStart('0'));
            }

            if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel"))
            {
                int index = MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index;
                if (index != 0)
                {
                    curTitle.Insert(index - 5, "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
            else if (bookType == BookType.Manga && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Vol") && !entryTitle.Contains("Anniversary", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(curTitle.ToString().Trim()) && !entryTitle.Contains("Part") && !bookTitle.Contains("Part"))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Vol ");
            }

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

            if (bookType == BookType.Manga && curTitle.ToString().Contains("Vol") && !char.IsDigit(curTitle.ToString()[curTitle.Length - 1]))
            {
                Match match = FindVolWithNumRegex().Match(curTitle.ToString());
                // LOGGER.Debug("Fix {} | {} | {} | {}", curTitle.ToString(), match.Index, match.Value, curTitle.ToString()[match.Index]);
                curTitle.Remove(match.Index, match.Length);
            }

            if (curTitle.ToString().Contains("Box Set") && curTitle.ToString().Contains("Vol"))
            {
                curTitle.Replace("Vol", string.Empty);
            }
            else if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Omnibus")  && !curTitle.ToString().Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
            {
                curTitle.Insert(curTitle.ToString().Trim().AsSpan().IndexOf("Vol"), "Stray God ");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetWorderyData(string bookTitle, BookType bookType, Region curRegion, WebDriver driver)
        {
            try
            {
                ushort pageNum = 1;
                bool secondNovelCheck = false;
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new HtmlDocument
                {
                    OptionCheckSyntax = false,
                };
                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, pageNum, secondNovelCheck));

                // Check to see whether to use alternative url for light novel entry
                if (bookType == BookType.LightNovel && driver.FindElements(By.ClassName("c-zero-results__help")).Count != 0)
                {
                    secondNovelCheck = true;
                    LOGGER.Info("Checking Alternative Url for Light Novel");
                    driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, pageNum, secondNovelCheck));
                }
                wait.Until(driver => driver.FindElement(By.XPath("//div[@class='js-search-results']")));
                
                // Check to see if we should change the region
                if (!driver.FindElement(By.XPath("//*[@id='page']/nav[2]/div[1]/div/ul/li[1]/button/span")).Text.Equals(CURRENCY_DICTIONARY[curRegion].Item2))
                {
                    LOGGER.Info("Changing Region to {}", curRegion.ToString());
                    wait.Until(driver => driver.FindElement(By.XPath("//div[@id='results']/div[2]/ul")));
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath($"//div[@class='c-crncy-sel']/form/button[text()='{CURRENCY_DICTIONARY[curRegion].Item1}']"))));
                }
                wait.Until(driver => driver.FindElement(By.XPath("//div[@id='results']/div[2]/ul")));

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

                    bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle); 
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
                            string price = priceData[x].InnerText.Trim();
                            if (curRegion != Region.Britain && curRegion != Region.Europe)
                            {
                                if (price.IndexOf('$') == 2)
                                {
                                    price = price[2..];
                                }
                                else if (price.IndexOf('$') == 1)
                                {
                                    price = price[1..];
                                }
                            }

                            entryTitle = ParseTitle(FixVolumeRegex().Replace(entryTitle.Trim(), "Vol "), bookTitle, bookType, formatAndLang);
                            // LOGGER.Debug("{} | {} | {} | {}", entryTitle, stockStatus, formatAndLang, staff);
                            if (!WorderyData.Exists(entry => entry.Entry.Equals(entryTitle)))
                            {
                                WorderyData.Add(
                                    new EntryModel
                                    (
                                        entryTitle,
                                        price,
                                        StockStatus.IS,
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else
                            {
                                LOGGER.Info("Removed {} | {}", entryTitle, formatAndLang);
                            }
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }

                    if (pageNum < maxPageNum)
                    {
                        driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, ++pageNum, secondNovelCheck));
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
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }
            finally
            {
                driver?.Quit();
                WorderyData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, WorderyData, LOGGER);
            }
            return WorderyData;
        }
    }
}