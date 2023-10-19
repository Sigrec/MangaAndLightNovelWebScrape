namespace MangaLightNovelWebScrape.Websites.America
{
    public partial class InStockTrades
    {
        private List<string> InStockTradesLinks = new();
        private List<EntryModel> InStockTradesData = new();
        public const string WEBSITE_TITLE = "InStockTrades";
        private static readonly Logger LOGGER = LogManager.GetLogger("InStockTradesLogs");
        private const Region WEBSITE_REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("/html/body/div[2]/div/div[3]/div/div[2][not(div[@class='damage'])]/div/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("/html/body/div[2]/div/div[3]/div/div[2][not(div[@class='damage'])]/div/div[1]/div[2]");
        private static readonly XPathExpression PageCheckXPath= XPathExpression.Compile("/html/body/div[2]/div/div[4]/span/input");

        [GeneratedRegex(@" GN| TP| HC| Manga|(?<=Vol).*|(?<=Box Set).*")]  private static partial Regex TitleRegex();
        [GeneratedRegex(@"Vol (\d+)|Box Set (\d+)|Box Set Part (\d+)")] private static partial Regex VolNumberRegex();
        [GeneratedRegex(@"3In1 Ed|3In1")] private static partial Regex OmnibusRegex();

        internal async Task CreateInStockTradesTask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetInStockTradesData(bookTitle, book, 1));
            });
        }

        //https://www.instocktrades.com/search?term=world+trigger
        //https://www.instocktrades.com/search?pg=1&title=World+Trigger&publisher=&writer=&artist=&cover=&ps=true
        // https://www.instocktrades.com/search?title=overlord+novel&publisher=&writer=&artist=&cover=&ps=true
        private string GetUrl(byte currPageNum, string bookTitle)
        {
            string url = $"https://www.instocktrades.com/search?pg={currPageNum}&title={bookTitle.Replace(' ', '+')}&publisher=&writer=&artist=&cover=&ps=true";
            InStockTradesLinks.Add(url);
            LOGGER.Debug(url);
            return url;
        }

        internal string GetUrl()
        {
            return InStockTradesLinks.Count != 0 ? InStockTradesLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        internal void ClearData()
        {
            InStockTradesLinks.Clear();
            InStockTradesData.Clear();
        }

        private static string TitleParse(string bookTitle, string titleText, BookType bookType)
        {
            Group volGroup;
            StringBuilder curTitle = new StringBuilder(titleText);
            if (titleText.Contains("Box Set")) 
            { 
                curTitle.Replace("Vol ", "");
                Match match = VolNumberRegex().Match(curTitle.ToString());
                if (match.Groups[3] != null)
                {
                    volGroup = match.Groups[3];
                }
                else
                {
                    volGroup = match.Groups[2];
                }
            }
            else
            {
                if (bookTitle.Equals("Overlord", StringComparison.OrdinalIgnoreCase) && titleText.Contains(" Og "))
                {
                    curTitle.Replace("Og", "Oh");
                }

                if (titleText.EndsWith(" Sc") || titleText.Contains(" Sc "))
                {
                    curTitle.Remove(titleText.LastIndexOf(" Sc"), 3);
                }

                curTitle.Replace(" Ann", " Anniversary");
                curTitle.Replace("Light Novel", "Novel");
                curTitle.Replace("Deluxe Edition", "Deluxe");

                if (bookType == BookType.Manga && !titleText.Contains("Vol"))
                {
                    curTitle.Append(" Vol 1");
                }
                else if (bookType == BookType.LightNovel && !titleText.Contains("Novel"))
                {
                    curTitle.Append(" Novel");
                }
                volGroup = VolNumberRegex().Match(curTitle.ToString()).Groups[1];
            }

            if (!bookTitle.Contains("One", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace("One", "1");
            }

            return System.Net.WebUtility.HtmlDecode($"{TitleRegex().Replace(OmnibusRegex().Replace(curTitle.ToString(), "Omnibus"), "")} {volGroup.Value.TrimStart('0')}".Trim());
        }

        private List<EntryModel> GetInStockTradesData(string bookTitle, BookType bookType, byte currPageNum)
        {
            ushort maxPages = 0;
            bool oneShotCheck = false;
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = new HtmlDocument();
                while (true)
                {
                    // driver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle));
                    // wait.Until(e => e.FindElement(By.XPath("/html/body/div[2]")));

                    // // Initialize the html doc for crawling
                    // HtmlDocument doc = new();
                    // doc.LoadHtml(driver.PageSource);
                    doc = web.Load(GetUrl(currPageNum, bookTitle));

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    oneShotCheck = !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol") || title.InnerText.Contains("Box Set") || title.InnerText.Contains("Manga"));
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    if (maxPages == 0)
                    {
                        HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                        if (pageCheck != null)
                        {
                            maxPages = Convert.ToUInt16(pageCheck.GetAttributeValue("data-max", "Page Num Error"));
                        }
                    }

                    string titleText;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = titleData[x].InnerText;
                        if (
                            !titleText.Contains("Artbook")
                            && !titleText.Contains("Character Bk")
                            && (   
                                (
                                    bookType == BookType.Manga 
                                    && ( // Ensure manga entry contains valid indentifier
                                            oneShotCheck
                                            || (
                                                    !oneShotCheck
                                                    && (
                                                            titleText.Contains("Vol") 
                                                            || titleText.Contains("Box Set") 
                                                            || titleText.Contains("Manga")
                                                        )
                                                )
                                        )
                                    && !titleText.Contains(" Novel", StringComparison.OrdinalIgnoreCase)
                                    && !( // Remove unintended volumes from specific series
                                            MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText.ToString(), "of Gluttony")
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText.ToString(), "Boruto")
                                        )
                                )
                                || 
                                ( // Ensure novel entry doesn't contain Manga & Contains Novel or doesn't contain "Vol" identifier
                                    bookType == BookType.LightNovel 
                                    && !titleText.Contains("Manga") 
                                    && (
                                            titleText.Contains(" Novel", StringComparison.OrdinalIgnoreCase) 
                                            || !titleText.Contains("Vol")
                                        )
                                )
                            )
                        )
                        {
                            InStockTradesData.Add(
                                new EntryModel
                                (
                                    TitleParse(bookTitle, titleText, bookType),
                                    priceData[x].InnerText.Trim(), 
                                    StockStatus.IS, 
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    if (maxPages != 0 && currPageNum != maxPages)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        InStockTradesData.Sort(new VolumeSort());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Debug($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }

            //Print data to a txt file
            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\InStockTradesData.txt"))
                {
                    if (InStockTradesData.Count != 0)
                    {
                        foreach (EntryModel data in InStockTradesData)
                        {
                            LOGGER.Debug(data);
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        LOGGER.Debug($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                    }
                }
            }

            return InStockTradesData;
        }
    }
}