namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class InStockTrades
    {
        private List<string> InStockTradesLinks = new();
        private List<EntryModel> InStockTradesData = new();
        public const string WEBSITE_TITLE = "InStockTrades";
        private static readonly Logger LOGGER = LogManager.GetLogger("InStockTradesLogs");
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("/html/body/div[2]/div/div[3]/div/div[2][not(div[@class='damage'])]/div/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("/html/body/div[2]/div/div[3]/div/div[2][not(div[@class='damage'])]/div/div[1]/div[2]");
        private static readonly XPathExpression PageCheckXPath= XPathExpression.Compile("/html/body/div[2]/div/div[4]/span/input");

        [GeneratedRegex(@" GN| TP| HC| Manga|(?<=Vol).*|(?<=Box Set).*")]  private static partial Regex TitleRegex();
        [GeneratedRegex(@" GN| TP| HC| Manga|(?<=Vol \d{1,3})[^\d{1,3].*|(?<=Box Set).*")]  private static partial Regex FixTitleTwoRegex();
        [GeneratedRegex(@"(?:Vol|GN)\s+(\d+)|Box Set\s+(\d+)|Box Set\s+Part\s+(\d+)")] private static partial Regex VolNumberRegex();
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
            LOGGER.Info(url);
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

        // TODO Recheck Toilet Bound scrape
        private static string TitleParse(string bookTitle, string entryTitle, BookType bookType)
        {
            string volGroup;
            StringBuilder curTitle = new StringBuilder(entryTitle);
            if (entryTitle.Contains("Box Set")) 
            { 
                curTitle.Replace("Vol ", string.Empty);
                Match match = VolNumberRegex().Match(curTitle.ToString());
                if (!string.IsNullOrWhiteSpace(match.Groups[3].Value))
                {
                    volGroup = match.Groups[3].Value;
                }
                else
                {
                    volGroup = match.Groups[2].Value;
                }

                if (entryTitle.Contains("Season"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Box Set"), $"Part {volGroup.TrimStart('0')} ");
                }
            }
            else
            {
                volGroup = VolNumberRegex().Match(curTitle.ToString()).Groups[1].Value;
                if (bookTitle.Equals("Overlord", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains(" Og "))
                {
                    curTitle.Replace("Og", "Oh");
                }

                if (entryTitle.Contains("Special Ed") && !entryTitle.Contains("Vol"))
                {
                    curTitle.Insert(VolNumberRegex().Match(curTitle.ToString()).Index, "Vol ");
                }

                if (bookType == BookType.Manga)
                {
                    // LOGGER.Debug("{} | {} | {}", curTitle.ToString(), volGroup, string.IsNullOrWhiteSpace(volGroup));
                    if (!entryTitle.Contains("Vol") || string.IsNullOrWhiteSpace(volGroup))
                    {
                        curTitle.Append(" Vol 1");
                    }
                }
                else if (bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
                {
                    curTitle.Append(" Novel");
                }
            }

            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "One", "1");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color HC Ed", "In Color");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, " Ann", " Anniversary Edition");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Light Novel", "Novel");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Deluxe Edition", "Deluxe");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Vol GN", "Vol");

            if (!curTitle.ToString().Contains("Special Edition") && (entryTitle.Contains("Special Ed") || entryTitle.Contains("Sp Ed")))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Special Edition ");
            }
            if (entryTitle.EndsWith(" Sc") || entryTitle.Contains(" Sc "))
            {
                curTitle.Remove(entryTitle.LastIndexOf(" Sc"), 3);
            }

            return System.Net.WebUtility.HtmlDecode($"{(!string.IsNullOrWhiteSpace(volGroup) ? TitleRegex().Replace(OmnibusRegex().Replace(curTitle.ToString(), "Omnibus"), string.Empty) : FixTitleTwoRegex().Replace(OmnibusRegex().Replace(curTitle.ToString(), "Omnibus"), string.Empty))} {(!entryTitle.Contains("Season") ? volGroup.TrimStart('0') : string.Empty)}".Replace("Ed Vol", "Edition Vol").Trim());
        }

        private List<EntryModel> GetInStockTradesData(string bookTitle, BookType bookType, byte currPageNum)
        {
            ushort maxPages = 0;
            bool oneShotCheck = false;
            try
            {
                HtmlWeb web = new HtmlWeb() { UsingCacheIfExists = true };
                HtmlDocument doc = new HtmlDocument();
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                
                while (true)
                {
                    doc = web.Load(GetUrl(currPageNum, bookTitle));

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    oneShotCheck = titleData.Count == 1 && !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol") || title.InnerText.Contains("Box Set") || title.InnerText.Contains("Manga"));
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    if (maxPages == 0)
                    {
                        HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                        if (pageCheck != null)
                        {
                            maxPages = Convert.ToUInt16(pageCheck.GetAttributeValue("data-max", "Page Num Error"));
                        }
                    }

                    string entryTitle;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        entryTitle = !bookTitle.Contains("Adv") ? entryTitle = titleData[x].InnerText.Replace(" Adv ", " Adventure ") : titleData[x].InnerText;
                        LOGGER.Debug("{} | {}", entryTitle, oneShotCheck);

                        if (
                            (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && !MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && (   
                                (
                                    bookType == BookType.Manga 
                                    && ( // Ensure manga entry contains valid indentifier
                                            oneShotCheck
                                            || 
                                            (!oneShotCheck
                                                && 
                                                (
                                                    entryTitle.Contains("Vol") 
                                                    || entryTitle.Contains("Box Set") 
                                                    || entryTitle.Contains("Manga")
                                                    || entryTitle.Contains("Special Ed")
                                                )
                                            )
                                        )
                                    && !entryTitle.Contains(" Novel", StringComparison.OrdinalIgnoreCase)
                                    && !( // Remove unintended volumes from specific series
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle.ToString(), "of Gluttony")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle.ToString(), "Boruto")
                                        )
                                )
                                || 
                                ( // Ensure novel entry doesn't contain Manga & Contains Novel or doesn't contain "Vol" identifier
                                    bookType == BookType.LightNovel 
                                    && !entryTitle.Contains("Manga") 
                                    && (
                                            entryTitle.Contains(" Novel", StringComparison.OrdinalIgnoreCase) 
                                            || !entryTitle.Contains("Vol")
                                        )
                                    && !(
                                            InternalHelpers.RemoveUnintendedVolumes(bookTitle, "fullmetal alchemist", entryTitle.ToString(), "Fullmetal Alchemist 20th Annv Book HC")
                                        )
                                )
                            )
                        )
                        {
                            InStockTradesData.Add(
                                new EntryModel
                                (
                                    TitleParse(bookTitle, entryTitle, bookType),
                                    priceData[x].InnerText.Trim(), 
                                    StockStatus.IS, 
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else { LOGGER.Info("Removed {}", entryTitle); }
                    }

                    if (maxPages != 0 && currPageNum != maxPages)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        InStockTradesData.Sort(EntryModel.VolumeSort);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Debug($"{bookTitle} Does Not Exist @ {WEBSITE_TITLE} \n{e}");
            }

            //Print data to a txt file
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, InStockTradesData, LOGGER);

            return InStockTradesData;
        }
    }
}