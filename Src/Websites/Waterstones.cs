namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class Waterstones
    {
        private List<string> WaterstonesLinks = new List<string>();
        private List<EntryModel> WaterstonesData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Waterstones";
        private static readonly Logger LOGGER = LogManager.GetLogger("WaterstonesLogs");
        public const Region REGION = Region.Britain;

        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//a[contains(@class, 'title link-invert')]");
        private static readonly XPathExpression OneShotTitleXPath = XPathExpression.Compile("//span[@id='scope_book_title']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price']");
        private static readonly XPathExpression OneShotPriceXPath = XPathExpression.Compile("//b[@itemprop='price']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='book-price']/span[1]");
        private static readonly XPathExpression OneShotStockStatusXPath = XPathExpression.Compile("//span[@id='scope_offer_availability']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//div[@class='pager']/span[2]");
        private static readonly XPathExpression OneShotCheckXPath = XPathExpression.Compile("//div[@class='span12']/h2");

        [GeneratedRegex(@",|The Manga|(?<=Vol \d{1,3})[^\d{1,3}.].*|(?<=Vol \d{1,3}.\d{1})[^\d{1,3}.]+.*|(?<=Special Edition).*|(?<=Omnibus \d{1,3})[^\d{1,3}].*| \(Paperback\)| - .*", RegexOptions.IgnoreCase)] private static partial Regex FixTitleRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex FixOmnibusRegex();
        [GeneratedRegex(@"(?<=Box Set \d{1}).*", RegexOptions.IgnoreCase)] private static partial Regex FixBoxSetRegex();
        [GeneratedRegex(@"\(.*\)", RegexOptions.IgnoreCase)] private static partial Regex FixTitleTwoRegex();
        [GeneratedRegex(@"(?:Vol \d{1,3})$", RegexOptions.IgnoreCase)] private static partial Regex FullTitleCheckRegex();
        [GeneratedRegex(@"Vol\.|Volume|v\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

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

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum, bool isOneShot)
        {
            // https://www.waterstones.com/books/search/term/overlord+novel
            string url;
            if (!isOneShot)
            {
                // https://www.waterstones.com/books/search/term/jujutsu+kaisen/category/394/facet/347/sort/pub-date-asc/page/1
                url = $"{(bookType == BookType.Manga ? $"https://www.waterstones.com/books/search/term/{bookTitle}/category/394/facet/347/sort/pub-date-asc/page1//page/{pageNum}" : $"https://www.waterstones.com/books/search/term/{bookTitle}+novel")}";
                LOGGER.Info($"Url Page {pageNum} = {url}");
            }
            else
            {
                // https://www.waterstones.com/books/search/term/Goodbye+Eri
                url = $"https://www.waterstones.com/books/search/term/{bookTitle}";
                LOGGER.Info($"OneShot Url = {url}");
            }
            WaterstonesLinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            StringBuilder curTitle;
            if (FixOmnibusRegex().IsMatch(entryTitle))
            {
                curTitle = new StringBuilder(FixOmnibusRegex().Replace(entryTitle, "Omnibus"));
            }
            else if (FixBoxSetRegex().IsMatch(entryTitle))
            {
                curTitle = new StringBuilder(FixBoxSetRegex().Replace(entryTitle, ""));
            }
            else
            {
                curTitle = new StringBuilder(entryTitle);
            }
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "—", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "–", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Manga", string.Empty);

            if (entryTitle.Contains(" Special Edition", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(" Special Edition", string.Empty);
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Special Edition ");
            }

            if (curTitle.ToString().Contains('('))
            {
                curTitle = new StringBuilder(FixTitleTwoRegex().Replace(curTitle.ToString(), string.Empty));
            }

            if (!entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(curTitle.ToString().Trim()))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Vol ");
            }

            if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel"))
            {
                if (entryTitle.Contains("Vol"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }
            else if (bookType == BookType.Manga)
            {
                if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Naruto Next Generations", string.Empty);
                }
            }
            
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetWaterstonesData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                ushort pageNum = 1, maxPageNum = 1;
                bool isOneShot = false;

                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" " , "+"), bookType, pageNum, isOneShot));
                HtmlDocument doc = new HtmlDocument
                {
                    OptionCheckSyntax = false
                };
                doc.LoadHtml(driver.PageSource);

                HtmlNode oneShotCheckNode = doc.DocumentNode.SelectSingleNode(OneShotCheckXPath);
                if (oneShotCheckNode != null && oneShotCheckNode.InnerText.Contains("No results"))
                {
                    isOneShot = true;
                    driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" " , "+"), bookType, pageNum, isOneShot));
                    doc.LoadHtml(driver.PageSource);
                }
                else
                {
                    // Get the total number of pages
                    HtmlNode pageCheckNode = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                    maxPageNum = pageCheckNode != null ? (ushort)char.GetNumericValue(pageCheckNode.InnerText.TrimEnd()[^1]) : (ushort)1;
                    LOGGER.Debug("Entry has {} Page(s)", maxPageNum);
                }

                bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);
                while (true)
                {
                    // Get page data
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(!isOneShot ? TitleXPath : OneShotTitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(!isOneShot ? PriceXPath : OneShotPriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(!isOneShot ? StockStatusXPath : OneShotStockStatusXPath);
                    // LOGGER.Debug("{} | {} | {}", titleData == null, priceData == null, stockStatusData == null);
                    // LOGGER.Debug("{} | {} | {}", titleData.Count, priceData.Count, stockStatusData.Count);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        // LOGGER.Debug("{} | {}", titleData[x].InnerText, FixVolumeRegex().Replace(titleData[x].InnerText, " Vol").Trim());
                        string entryTitle = FixVolumeRegex().Replace(titleData[x].InnerText, " Vol").Trim();
                        if (!isOneShot && ((entryTitle.EndsWith('…') && !FullTitleCheckRegex().IsMatch(entryTitle[..entryTitle.IndexOf('…')])) || !entryTitle.Contains("Vol"))) // Check to see if title is cutoff
                        {
                            string oldTitle = entryTitle;
                            driver.Navigate().GoToUrl($"https://www.waterstones.com/{titleData[x].GetAttributeValue("href", "Error")}");
                            entryTitle = FixVolumeRegex().Replace(wait.Until(driver => driver.FindElement(By.Id("scope_book_title"))).Text, " Vol");
                            // LOGGER.Debug("Replaced {} w/ {}", oldTitle, entryTitle);
                        }

                        if (
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && !(
                                bookType == BookType.Manga
                                && (
                                        (
                                            entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) 
                                            && !bookTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                        )
                                        ||
                                        (
                                            entryTitle.Contains("NoVoll", StringComparison.OrdinalIgnoreCase) 
                                            && !bookTitle.Contains("NoVoll", StringComparison.OrdinalIgnoreCase)
                                        )
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Bleachers")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "attack on titan", entryTitle, "Kuklo Unbound")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Flame Dragon Knight")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    )
                                )
                            )
                        {
                            string price = priceData[x].InnerText.Trim();
                            WaterstonesData.Add(
                                new EntryModel
                                (
                                    ParseTitle(FixTitleRegex().Replace(entryTitle, string.Empty), bookTitle, bookType),
                                    price.Substring(price.IndexOf('£')),
                                    stockStatusData[x].InnerText.Trim() switch
                                    {
                                        string status when status.Contains("In stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                        string status when status.Contains("Pre-order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                        _ => StockStatus.OOS,
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }
                    
                    if (!isOneShot && pageNum < maxPageNum)
                    {
                        pageNum++;
                        driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.Replace(",", string.Empty).Replace(" " , "+"), bookType, pageNum, isOneShot));
                        doc.LoadHtml(driver.PageSource);
                    }
                    else
                    {
                        driver.Quit();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                driver?.Quit();
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }

            WaterstonesData.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, WaterstonesData, LOGGER);
            return WaterstonesData;
        }
    }
}