namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class KinokuniyaUSA
    {
        private List<string> KinokuniyaUSALinks = new();
        private List<EntryModel> KinokuniyaUSAData = new();
        public const string WEBSITE_TITLE = "Kinokuniya USA";
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private static readonly int STATUS_START_INDEX = "Availability Status : ".Length;
        public const Region REGION = Region.America;
        private static readonly string[] SkipBookTitles = { "Attack on Titan" };
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//span[@class='underline']");
        private static readonly XPathExpression MemberPriceXPath = XPathExpression.Compile("//li[@class='price'][2]/span");
        private static readonly XPathExpression NonMemberPriceXPath = XPathExpression.Compile("//li[@class='price'][1]/span");
        private static readonly XPathExpression DescXPath = XPathExpression.Compile("//p[@class='description']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//li[@class='status']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//div[@class='categoryPager']/ul/li[last()]/a");
        
        [GeneratedRegex(@"\((?:Omnibus |3\s*In\s*1 |2\s*In\s*1 )Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"\(Light Novel\)|Light Novel|Novel", RegexOptions.IgnoreCase)] private static partial Regex NovelRegex();
        [GeneratedRegex(@"\((.*?)\)+", RegexOptions.IgnoreCase)] private static partial Regex TitleCaptureRegex();
        [GeneratedRegex(@"^[^\(]+", RegexOptions.IgnoreCase)] private static partial Regex CleanInFrontTitleRegex();
        [GeneratedRegex(@"(?<=\d{1,3})[^\d{1,3}].*", RegexOptions.IgnoreCase)] private static partial Regex CleanBehindTitleRegex();
        [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|<.*?>|w/DVD|<|>|(?<=\d{1,3})\s+:.*", RegexOptions.IgnoreCase)] private static partial Regex MangaTitleFixRegex();
        [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\({1,}.*?\){1,}|(?<=\d{1,3})\s?:.*|<.*?[^\d+]>|w/DVD|<|>", RegexOptions.IgnoreCase)] private static partial Regex NovelTitleFixRegex();
        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] private static partial Regex FixVolumeRegex();
        [GeneratedRegex(@"\d{1,3}\.\d{1,3}|\d{1,3}")] internal static partial Regex FindVolNumRegex();

        // Manga English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=world+trigger&taxon=2&x=39&y=4&page=1&per_page=100&form_taxon=109
        // https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=Skeleton+Knight+in+Another+World&taxon=2&x=39&y=11&page=1&per_page=100

        // Light Novel English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=overlord+novel&taxon=&x=33&y=8&per_page=100&form_taxon=109
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite&taxon=&x=33&y=8&per_page=100&form_taxon=109

        internal async Task CreateKinokuniyaUSATask(string bookTitle, BookType bookType, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetKinokuniyaUSAData(bookTitle, bookType, isMember, driver));
            });
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType)
        {
            string url = $"https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={bookTitle.Replace(" ", "+")}{(bookType == BookType.LightNovel ? "+novel" : string.Empty)}&taxon=2&x=39&y=11&page=1&per_page=100";
            LOGGER.Info($"Url = {url}");
            KinokuniyaUSALinks.Add(url);
            return url;
        }

        internal string GetUrl()
        {
            return KinokuniyaUSALinks.Count != 0 ? KinokuniyaUSALinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        internal void ClearData()
        {
            KinokuniyaUSALinks.Clear();
            KinokuniyaUSAData.Clear();
        }

        private static void WaitForPageLoad(WebDriverWait wait)
        {
            wait.Until(d =>
            {
                try
                {
                    IWebElement element = d.FindElement(By.Id("loading"));
                    string style = element.GetDomAttribute("style");
                    return style != null && style.Contains("display: none;");
                }
                catch (NoSuchElementException)
                {
                    LOGGER.Warn("Loading Failed");
                    return true;
                }
            });
        }

        private static string ParseAndCleanTitle(string entryTitle, BookType bookType, string bookTitle, string entryDesc, bool oneShotCheck)
        {
            if (!bookTitle.Contains('-'))
            {
                entryTitle = entryTitle.Replace("-", " ");
            }
            
            string parseCheckTitle = TitleCaptureRegex().Match(entryTitle).Groups[1].Value;
            string checkBeforeText = CleanInFrontTitleRegex().Match(entryTitle).Value;
            if (!SkipBookTitles.Contains(bookTitle, StringComparer.OrdinalIgnoreCase) && parseCheckTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) && !checkBeforeText.Contains(bookTitle, StringComparison.OrdinalIgnoreCase) && entryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit))
            {
                // LOGGER.Debug("{} | {} | {} | {} | {}", checkBeforeText, parseCheckTitle, entryTitle, CleanInFrontTitleRegex().Replace(entryTitle, string.Empty), CleanInFrontTitleRegex().Replace(entryTitle, string.Empty).Insert(0, $"{parseCheckTitle} "));
                entryTitle = CleanInFrontTitleRegex().Replace(entryTitle, string.Empty).Insert(0, $"{parseCheckTitle} ");
            }

            if (!oneShotCheck)
            {
                string newEntryTitle;
                if (bookType == BookType.LightNovel)
                {
                    entryTitle = NovelRegex().Replace(entryTitle, "Novel");
                    newEntryTitle = NovelTitleFixRegex().Replace(OmnibusRegex().Replace(FixVolumeRegex().Replace(entryTitle, "Vol"), "Omnibus"), "$1");
                }
                else
                {
                    newEntryTitle = MangaTitleFixRegex().Replace(OmnibusRegex().Replace(FixVolumeRegex().Replace(entryTitle, "Vol"), "Omnibus"), "$1");
                }

                StringBuilder curTitle = new StringBuilder(newEntryTitle).Replace(",", string.Empty);
                InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
                newEntryTitle = curTitle.ToString().Trim();

                if (bookType == BookType.LightNovel)
                {
                    if (!newEntryTitle.Contains(bookTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        curTitle.Insert(0, $"{char.ToUpper(bookTitle[0])}{bookTitle.AsSpan(1)} ");
                    }

                    newEntryTitle = curTitle.ToString();
                    bool containsNovel = newEntryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase);
                    bool containsVol = newEntryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase);
                    if (!containsNovel && !containsVol)
                    {
                        curTitle = new StringBuilder(CleanBehindTitleRegex().Replace(newEntryTitle, string.Empty));
                    }
                    else if (!containsNovel && containsVol)
                    {
                        curTitle.Insert(newEntryTitle.IndexOf("Vol"), "Novel ");
                    }

                    newEntryTitle = curTitle.ToString();
                    if (!containsNovel && !newEntryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit))
                    {
                        curTitle.Insert(curTitle.Length, " Novel");
                    }
                }
                else if (bookType == BookType.Manga && !newEntryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !newEntryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                {
                    if (MasterScrape.FindVolNumRegex().IsMatch(newEntryTitle) && !bookTitle.AsParallel().Any(char.IsDigit))
                    {
                        curTitle.Insert(MasterScrape.FindVolNumRegex().Match(newEntryTitle).Index, "Vol ");
                    }
                    else if (entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase) && entryDesc.Contains("volumes", StringComparison.OrdinalIgnoreCase))
                    {
                        curTitle.Insert(curTitle.Length, " Box Set");
                    }
                }

                if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Naruto Next Generations", string.Empty);
                }

                if (entryTitle.AsParallel().Any(char.IsDigit) && !curTitle.ToString().Contains("Vol") && !entryTitle.Contains("Box Set")  && !entryTitle.Contains("Anniversary"))
                {
                    Match volNum = FindVolNumRegex().Match(curTitle.ToString());
                    if (!string.IsNullOrWhiteSpace(volNum.Value))
                    {
                        curTitle.Remove(volNum.Index, volNum.Value.Length);
                        curTitle.AppendFormat("{0} Vol {1}", bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel") ? " Novel" : string.Empty, volNum.Value);
                    }
                }

                if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Omnibus")  && !curTitle.ToString().Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Stray God ");
                }
                
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Manga", string.Empty).ToString().Trim(), " ");
            }

            if (bookType == BookType.Manga)
            {
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(MangaTitleFixRegex().Replace(FixVolumeRegex().Replace(entryTitle.Replace("Manga", string.Empty).Replace(",", string.Empty), "Vol"), "$1").Trim(), " ");
            }
            else
            {
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(NovelTitleFixRegex().Replace(FixVolumeRegex().Replace(entryTitle.Replace("Manga", string.Empty).Replace(",", string.Empty), "Vol"), "$1").Trim(), " ");
            }
        }
        
        internal List<EntryModel> GetKinokuniyaUSAData(string bookTitle, BookType bookType, bool memberStatus, WebDriver driver)
        {
            try
            {
                int maxPageCount = -1, curPageNum = 1;
                bool oneShotCheck = false;
                string entryTitle, entryDesc;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                HtmlDocument doc = new HtmlDocument();

                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType));
                WaitForPageLoad(wait);

                // Click the list display mode so it shows stock status data with entry
                driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.LinkText("List"))));
                WaitForPageLoad(wait);
                LOGGER.Info("Clicked List Mode");

                if (bookType == BookType.Manga)
                {
                    // Click the Manga
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.LinkText("Manga"))));
                    WaitForPageLoad(wait);
                    LOGGER.Info("Clicked Manga");
                }

                while(true)
                {
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    oneShotCheck = curPageNum == 1 && titleData.Count == 1 && !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase)); // Determine if the series is a one shot or not
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(memberStatus ? MemberPriceXPath : NonMemberPriceXPath);
                    HtmlNodeCollection descData = doc.DocumentNode.SelectNodes(DescXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    if (maxPageCount == -1) { maxPageCount = Convert.ToInt32(doc.DocumentNode.SelectSingleNode(PageCheckXPath).InnerText); }
                    
                    // LOGGER.Debug("{} | {} | {} | {}", titleData.Count, priceData.Count, descData.Count, stockStatusData.Count);

                    // Remove all of the novels from the list if user is searching for manga
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        entryTitle = System.Net.WebUtility.HtmlDecode(titleData[x].InnerText);
                        entryDesc = descData[x].InnerText;

                        if (
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && (
                                    (
                                        bookType == BookType.Manga
                                        && (
                                            InternalHelpers.RemoveEntryTitleCheck(bookTitle, entryTitle, "Novel")
                                            || entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                            )
                                        && !entryTitle.Contains("Chapter Book" ,StringComparison.OrdinalIgnoreCase)
                                        && (
                                            oneShotCheck || 
                                            FixVolumeRegex().IsMatch(entryTitle) || 
                                            entryDesc.ContainsAny(["Collection", "volumes", "color edition"]) || 
                                            (entryTitle.Any(char.IsDigit) && !bookTitle.Any(char.IsDigit)))
                                        && !(
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World") ||
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, ["Itachi's Story", "Boruto"]) ||
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", entryTitle, ["Kuklo", "end of the world"]) ||
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Overlord", entryTitle, "Unimplemented")
                                        )
                                    )
                                    || 
                                    (
                                        bookType == BookType.LightNovel
                                        && !entryTitle.Contains("graphic novel", StringComparison.OrdinalIgnoreCase) 
                                        && entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                    )
                                )
                            )
                        {
                            entryTitle = ParseAndCleanTitle(entryTitle, bookType, bookTitle, entryDesc, oneShotCheck);
                            if (!KinokuniyaUSAData.Any(entry => entry.Entry.Equals(entryTitle, StringComparison.OrdinalIgnoreCase)))
                            {
                                KinokuniyaUSAData.Add(
                                    new EntryModel(
                                        entryTitle, 
                                        priceData[x].InnerText.Trim(), 
                                        stockStatusData[x].InnerText.Trim().AsSpan(STATUS_START_INDEX) switch
                                        {
                                            "In stock at the Fulfilment Center." => StockStatus.IS,
                                            "Available for Pre Order" => StockStatus.PO,
                                            "Out of stock." => StockStatus.OOS,
                                            "Available for order from suppliers." => StockStatus.BO,
                                            _ => StockStatus.NA
                                        },
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else
                            {
                                LOGGER.Info("Removed (2) {}", entryTitle);
                            }
                        }
                        else
                        {
                            LOGGER.Info("Removed (1) {}", entryTitle);
                        }
                    }
                    if (curPageNum != maxPageCount)
                    {
                        curPageNum++;
                        LOGGER.Debug("Going to Page {}", curPageNum);
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("pagerArrowR"))));
                        WaitForPageLoad(wait);
                        LOGGER.Info("Page {} = {}", curPageNum, driver.Url);
                    }
                    else
                    {
                        break;
                    }
                }

                KinokuniyaUSAData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, bookType, KinokuniyaUSAData, LOGGER);
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} ({}) Error @ {} \n{}", bookTitle, bookType, WEBSITE_TITLE, ex);
            }
            finally
            {
                if (!MasterScrape.IsWebDriverPersistent)
                {
                    driver?.Quit();
                }
                else 
                { 
                    driver?.Close(); 
                }
            }

            return KinokuniyaUSAData;
        }
    }
}