namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class SpeedyHen
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private List<string> SpeedyHenLinks = new List<string>();
        private List<EntryModel> SpeedyHenData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "SpeedyHen";
        public const Region REGION = Region.Britain;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//h3[@class='search-item__title']/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//p[@class='price search-item__purchase-price']/text()");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='search-item__availability']/p/span[1]/text() | //div[@class='search-item__availability']/p[@class='availabilityLink comingSoon' or @class='availabilityLink unavailable']/text()");
        private static readonly XPathExpression FormatXPath = XPathExpression.Compile("//div[@class='search-item__name']/*[last()]");
        private static readonly XPathExpression OneShotTitleXPath = XPathExpression.Compile("//h1[@itemprop='name']/text()");
        private static readonly XPathExpression OneShotPriceXPath = XPathExpression.Compile("(//p[@class='sitePrice'])[1]/text()");
        private static readonly XPathExpression OneShotStockStatusXPath = XPathExpression.Compile("(//span[@class='status'])[1]/text() | (//div[@class='availability'])[1]/a/p/text()");
        private static readonly XPathExpression OneShotFormatXPath = XPathExpression.Compile("(//li[@class='format'])[1]/span[2]/text()");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("(//nav[@class='mainPagination'])[1]/ul/li[@class='pageItem last']");
        private static readonly XPathExpression FormatCheckXPath = XPathExpression.Compile("//h2[@class='series']/a/text() | //div[@class='productInfoWrapGrid']/div/ul//li[@class='category']/ul/li"); // Also the OneShotCheck

        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@"The Manga|\s+Manga|(?<=Vol (?:\d{1,3}|\d{1,3}.\d{1}))[^\d{1,3}.]+.*|(?<=Box Set \d{1,3}).*|\(.*?\)")] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|Omnibus\s+(\d{1,3}).*", RegexOptions.IgnoreCase)] private static partial Regex OmnibusParseRegex();
        [GeneratedRegex(@"Box Set!", RegexOptions.IgnoreCase)] private static partial Regex BoxSetParseRegex();
        

        internal void ClearData()
        {
            SpeedyHenLinks.Clear();
            SpeedyHenData.Clear();
        }

        internal async Task CreateSpeedyHenTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetSpeedyHenData(bookTitle, bookType));
            });
        }

        internal string GetUrl()
        {
            return SpeedyHenLinks.Count != 0 ? SpeedyHenLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum)
        {
            // https://www.speedyhen.com/Search/Books/Comics-and-Graphic-Novels?Keyword=07-ghost&ipp=40&fq=01120-121612
            string url;

            if (bookType == BookType.Manga)
            {
                url = $"https://www.speedyhen.com/Search/Books/Comics-and-Graphic-Novels?Keyword={bookTitle.Replace(" ", "+")}&fq=01120-121612&ipp=40&pg={pageNum}";
            }
            else
            {
                url = $"https://www.speedyhen.com/Search/Keyword?keyword={InternalHelpers.FilterBookTitle(bookTitle)}%20novel&productType=0&pg={pageNum}";
            }
            LOGGER.Info("Page {} => {}", pageNum, url);
            SpeedyHenLinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            if (OmnibusParseRegex().IsMatch(entryTitle))
            {
                entryTitle = OmnibusParseRegex().Replace(entryTitle, "Omnibus $1");
            }
            else if (BoxSetParseRegex().IsMatch(entryTitle))
            {
                entryTitle = BoxSetParseRegex().Replace(entryTitle, "Box Set");
            }

            StringBuilder curTitle = new StringBuilder(TitleParseRegex().Replace(entryTitle, string.Empty)).Replace(",", "");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, ":", " ");

            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase)) { curTitle.Replace(" Naruto Next Generations", ""); }

            if (curTitle.ToString().Contains("Special Edition"))
            {
                int index = curTitle.ToString().IndexOf("Special Edition");
                curTitle.Remove(index, curTitle.Length - index);
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim()).Index, "Special Edition ");
            }

            entryTitle = curTitle.ToString().Trim();
            if (bookType == BookType.Manga && !entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Comics") && MasterScrape.FindVolNumRegex().IsMatch(entryTitle))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(entryTitle).Index, "Vol ");
            }
            else if (bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
            {
                int index = MasterScrape.FindVolWithNumRegex().Match(entryTitle).Index;
                curTitle.Insert(index > 0 ? index : curTitle.Length, " Novel ");
            }

            if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && !curTitle.ToString().Contains("Omnibus")  && !curTitle.ToString().Contains("Stray Stories") && !curTitle.ToString().Contains("Stray God"))
            {
                curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Stray God ");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetSpeedyHenData(string bookTitle, BookType bookType)
        {
            try
            {
                HtmlWeb web = new HtmlWeb() { UsingCacheIfExists = true };
                HtmlDocument doc = new HtmlDocument() { OptionCheckSyntax = false, };
                ushort curPageNum = 1;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                doc = web.Load(GenerateWebsiteUrl(bookTitle, bookType, curPageNum));
                bool oneShotCheck = doc.DocumentNode.SelectNodes(FormatCheckXPath) != null;
                LOGGER.Debug("IsOneShot? {}", oneShotCheck);

                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(!oneShotCheck ? TitleXPath : OneShotTitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(!oneShotCheck ? PriceXPath : OneShotPriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(!oneShotCheck ? StockStatusXPath : OneShotStockStatusXPath);
                    HtmlNodeCollection formatData = doc.DocumentNode.SelectNodes(!oneShotCheck ? FormatXPath : OneShotFormatXPath);
                    HtmlNode pageCheck = !oneShotCheck ? doc.DocumentNode.SelectSingleNode(PageCheckXPath) : null;

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        string format = formatData[x].InnerText.Trim();
                        string stockStatus = stockStatusData[x].InnerText.Trim();

                        if (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && (format.Contains("Paperback") || format.Contains("Hardback"))
                            && !stockStatus.Equals("Unavailable") 
                            && !(
                                bookType == BookType.Manga
                                && (entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase)
                                    || (
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Darkness Ink")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Unbound")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                    )
                                )
                            )
                        )        
                        {
                            entryTitle = System.Net.WebUtility.HtmlDecode(entryTitle);
                            string formatCheck = string.Empty;
                            string href = !oneShotCheck ? titleData[x].GetAttributeValue("href", "error") : "oneshot";
                            HtmlNodeCollection formatCheckData = null;
                            if (!href.Equals("error"))
                            {
                                formatCheckData = !oneShotCheck ? web.Load($"https://www.speedyhen.com{href}").DocumentNode.SelectNodes(FormatCheckXPath) : doc.DocumentNode.SelectNodes(FormatCheckXPath);
                                formatCheck = formatCheckData != null ? string.Join(" ", formatCheckData.Select(item => item.InnerText.Trim())) : string.Empty;
                            }

                            // LOGGER.Debug("{} | {} | {}", System.Net.WebUtility.HtmlDecode(entryTitle), priceData[x].InnerText.Trim().Replace("&#163;", "£"), formatCheck);
                            if (
                                (bookType == BookType.LightNovel 
                                    && (entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase) || formatCheck.Contains("Novel"))) 
                                || (bookType == BookType.Manga
                                    && (!formatCheckData[0].InnerText.Contains("Novel") || formatCheck.Contains("Graphic Novel", StringComparison.OrdinalIgnoreCase))
                                    && (entryTitle.Contains("manga", StringComparison.OrdinalIgnoreCase) || formatCheck.Contains("Graphic Novel", StringComparison.OrdinalIgnoreCase) || formatCheck.Contains("Manga") || entryTitle.Contains("Color Edition") || entryTitle.Contains("The Undead King Oh", StringComparison.OrdinalIgnoreCase))))
                            {
                                SpeedyHenData.Add(
                                    new EntryModel
                                    (
                                        ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol").Trim(), bookTitle, bookType),
                                        priceData[x].InnerText.Trim().Replace("&#163;", "£"),
                                        stockStatus switch
                                        {
                                            string curStatus when curStatus.Contains("In Stock") => StockStatus.IS,
                                            "Out of Stock" => StockStatus.OOS,
                                            string curStatus when curStatus.Contains("Pre-Order") || curStatus.Equals("Coming Soon") => StockStatus.PO,
                                            string curStatus when curStatus.Contains("We are unable to provide an estimated availability date") => StockStatus.BO,
                                            _ => StockStatus.NA,
                                        },
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed (2) {}", entryTitle); }
                        }
                        else { LOGGER.Info("Removed (1) {}", entryTitle); }
                    }

                    if (pageCheck != null) { doc = web.Load(GenerateWebsiteUrl(bookTitle, bookType, ++curPageNum)); }
                    else { break; }
                }

                SpeedyHenData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, bookType, SpeedyHenData, LOGGER);
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} ({}) Does Not Exist @ {} \n{}", bookTitle, bookType, WEBSITE_TITLE, ex);
            }
            return SpeedyHenData;
        }
    }
}