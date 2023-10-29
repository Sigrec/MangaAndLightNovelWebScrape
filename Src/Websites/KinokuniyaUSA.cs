namespace MangaLightNovelWebScrape.Websites
{
    public partial class KinokuniyaUSA
    {
        private List<string> KinokuniyaUSALinks = new();
        private List<EntryModel> KinokuniyaUSAData = new();
        public const string WEBSITE_TITLE = "Kinokuniya USA";
        private static readonly Logger LOGGER = LogManager.GetLogger("KinokuniyaUSALogs");
        private static readonly int STATUS_START_INDEX = "Availability Status : ".Length;
        private const Region WEBSITE_REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//span[@class='underline']");
        private static readonly XPathExpression MemberPriceXPath = XPathExpression.Compile("//li[@class='price'][2]/span");
        private static readonly XPathExpression NonMemberPriceXPath = XPathExpression.Compile("//li[@class='price'][1]/span");
        private static readonly XPathExpression DescXPath = XPathExpression.Compile("//p[@class='description']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//li[@class='status']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//div[@class='categoryPager']/ul/li[last()]/a");
        
        [GeneratedRegex(@"\((?:Omnibus |3\s*In\s*1 |2\s*In\s*1 )Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"\(Light Novel\)|Novel", RegexOptions.IgnoreCase)] private static partial Regex NovelRegex();
        [GeneratedRegex(@",|(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\(+.*?\)+|(?<=\d{1,3} :).*|:|<.*?>|w/DVD", RegexOptions.IgnoreCase)] private static partial Regex TitleFixRegex();

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

        private string GetUrl(BookType bookType, string titleText)
        {
            string url = $"https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={titleText.Replace(" ", "+")}{(bookType == BookType.LightNovel ? "+novel" : "")}&taxon=2&x=39&y=11&page=1&per_page=100";
            LOGGER.Debug(url);
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

        private static string TitleParse(string titleText, BookType bookType, string bookTitle, string entryDesc, bool oneShotCheck)
        {
            if (!bookTitle.Contains('-'))
            {
                titleText = titleText.Replace("-", " ");
            }

            if (!oneShotCheck)
            {
                if (bookType == BookType.LightNovel)
                {
                    titleText = NovelRegex().Replace(titleText, "Novel");
                }

                string newTitleText = TitleFixRegex().Replace(OmnibusRegex().Replace(MasterScrape.FixVolumeRegex().Replace(titleText, "Vol"), "Omnibus"), "$1");
                StringBuilder curTitle = new StringBuilder(newTitleText);
                curTitle.Replace(",", "");

                if (bookType == BookType.LightNovel && !newTitleText.Contains("Novel"))
                {
                    int index = newTitleText.IndexOf("Vol");
                    if (index != -1)
                    {
                        curTitle.Insert(index, "Novel ");
                    }
                    else
                    {
                        curTitle.Insert(curTitle.Length - 1, " Novel");
                    }
                }
                else if (bookType == BookType.Manga && !newTitleText.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !newTitleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                {
                    newTitleText = newTitleText.Trim();
                    if (MasterScrape.FindVolNumRegex().IsMatch(newTitleText) && !bookTitle.AsParallel().Any(char.IsDigit))
                    {
                        curTitle.Insert(MasterScrape.FindVolNumRegex().Match(newTitleText).Index, "Vol ");
                    }
                    else if (entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase) && entryDesc.Contains("volumes", StringComparison.OrdinalIgnoreCase))
                    {
                        curTitle.Insert(curTitle.Length, " Box Set");
                    }
                }

                if (titleText.AsParallel().Any(char.IsDigit) && !curTitle.ToString().Contains("Vol") && !titleText.Contains("Box Set")  && !titleText.Contains("Anniversary"))
                {
                    Regex findVolRegex = new Regex(@"\d+");
                    Match volNum = findVolRegex.Match(titleText);
                    if (string.IsNullOrWhiteSpace(volNum.Value))
                    {
                        volNum = findVolRegex.Match(newTitleText);
                    }
                    if (volNum.Index < curTitle.Length)
                    {
                        curTitle.Remove(volNum.Index, volNum.Value.Length);
                    }
                    curTitle.AppendFormat(" Vol {0}", volNum.Value);
                }
                
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Manga", string.Empty).ToString().Trim(), " ");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(TitleFixRegex().Replace(MasterScrape.FixVolumeRegex().Replace(titleText.Replace("Manga", string.Empty), "Vol"), "$1").Trim(), " ");
        }
        
        private List<EntryModel> GetKinokuniyaUSAData(string bookTitle, BookType bookType, bool memberStatus, WebDriver driver)
        {
            int maxPageCount = -1, curPageNum = 1;
            bool oneShotCheck = false;
            string titleText, entryDesc;
            StockStatus stockStatus;
            HtmlDocument doc = new HtmlDocument();
            bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                driver.Navigate().GoToUrl(GetUrl(bookType, bookTitle));
                wait.Until(driver => driver.FindElement(By.CssSelector("#loading[style='display: none;']")));
                if (bookType == BookType.Manga)
                {
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.LinkText("Manga"))));
                    wait.Until(driver => driver.FindElement(By.CssSelector("#loading[style='display: none;']")));
                    LOGGER.Info("Clicked Manga Button");
                }

                // Click the list display mode so it shows stock status data with entry
                driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.LinkText("List"))));
                wait.Until(driver => driver.FindElement(By.CssSelector("#loading[style='display: none;']")));
                LOGGER.Info("Clicked List Mode");

                while(true)
                {
                    // Initialize the html doc for crawling
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    oneShotCheck = titleData.Count == 1 && !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase)); // Determine if the series is a one shot or not
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(memberStatus ? MemberPriceXPath : NonMemberPriceXPath);
                    HtmlNodeCollection descData = doc.DocumentNode.SelectNodes(DescXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    if (maxPageCount == -1)
                    {
                        maxPageCount = Convert.ToInt32(doc.DocumentNode.SelectSingleNode(PageCheckXPath).InnerText);
                    }

                    // Remove all of the novels from the list if user is searching for manga
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = System.Net.WebUtility.HtmlDecode(titleData[x].InnerText);
                        entryDesc = descData[x].InnerText;
                        if (
                            MasterScrape.TitleContainsBookTitle(bookTitle, titleText)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(titleText) || BookTitleRemovalCheck)
                            && (
                                    (
                                        bookType == BookType.Manga
                                        && (
                                            (!titleText.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                            && !bookTitle.Contains("Novel"))
                                            || titleText.Contains("graphic novel", StringComparison.OrdinalIgnoreCase)
                                            )
                                        && !titleText.Contains("Chapter Book" ,StringComparison.OrdinalIgnoreCase)
                                        && ( // If it's not a one shot series check to see if it contains 'Vol' or has a volume number string if not skip
                                            oneShotCheck
                                            || (
                                                    !oneShotCheck
                                                    && (
                                                            MasterScrape.FixVolumeRegex().IsMatch(titleText)
                                                            || (
                                                                    entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase) 
                                                                    && entryDesc.Contains("volumes", StringComparison.OrdinalIgnoreCase)
                                                                )
                                                            || (
                                                                    titleText.AsParallel().Any(char.IsDigit)
                                                                    && !bookTitle.AsParallel().Any(char.IsDigit)
                                                                )
                                                        )
                                                )
                                            )
                                        && !(
                                                MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText, "of Gluttony") 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Bleach", titleText, "Can't Fear Your Own World") 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Itachi's Story") 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Boruto")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", titleText, "Kuklo")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", titleText, "end of the world")
                                            )
                                    )
                                    || 
                                    (
                                        bookType == BookType.LightNovel
                                        && !titleText.Contains("graphic novel", StringComparison.OrdinalIgnoreCase) 
                                        && titleText.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                    )
                                )
                            && !(
                                    !bookTitle.Contains("Color", StringComparison.OrdinalIgnoreCase) 
                                    && titleText.Contains("Color", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                        {
                            KinokuniyaUSAData.Add(
                                new EntryModel(
                                    TitleParse(titleText, bookType, bookTitle, entryDesc, oneShotCheck), 
                                    priceData[x].InnerText.Trim(), 
                                    stockStatusData[x].InnerText.Trim().AsSpan(STATUS_START_INDEX) switch
                                    {
                                        "In stock at the Fulfilment Center." or "Available for order from suppliers." => StockStatus.IS,
                                        "Available for Pre Order" => StockStatus.PO,
                                        "Out of stock." => StockStatus.OOS,
                                        _ => StockStatus.NA
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", titleText);
                        }
                    }
                    if (curPageNum != maxPageCount)
                    {
                        curPageNum++;
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("pagerArrowR"))));
                        wait.Until(driver => driver.FindElement(By.CssSelector("#loading[style='display: none;']")));
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        KinokuniyaUSAData.Sort(MasterScrape.VolumeSort);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ Kinokuniya USA \n{e.StackTrace}");
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\KinokuniyaUSAData.txt"))
                {
                    if (KinokuniyaUSAData.Count != 0)
                    {
                        foreach (EntryModel data in KinokuniyaUSAData)
                        {
                            outputFile.WriteLine(data);
                            LOGGER.Debug(data);
                        }
                    }
                    else
                    {
                        LOGGER.Error($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                    }
                } 
            }
            return KinokuniyaUSAData;
        }
    }
}