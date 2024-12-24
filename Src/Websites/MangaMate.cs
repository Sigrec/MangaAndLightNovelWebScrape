namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class MangaMate
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private List<string> MangaMateLinks = new List<string>();
        private List<EntryModel> MangaMateData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "MangaMate";
        public const Region REGION = Region.Australia;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='grid-product__title grid-product__title--body']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//div[@class='grid-product__price']/text()[3]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='grid-product__content']/div[1]");
        private static readonly XPathExpression StockStatusXPath2 = XPathExpression.Compile("//div[@class='grid-product__image-mask']/div[1]");
        private static readonly XPathExpression EntryLinkXPath = XPathExpression.Compile("//div[@class='grid__item-image-wrapper']/a");
        private static readonly XPathExpression EntryTypeXPath = XPathExpression.Compile("//div[@class='product-block'][4]/div/span/table//tr[4]/td[2]");

        [GeneratedRegex(@"The Manga|\(.*\)|Manga", RegexOptions.IgnoreCase)] private static partial Regex TitleParseRegex();
        [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();

        internal async Task CreateMangaMateTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetMangaMateData(bookTitle, bookType, driver));
            });
        }
    
        internal void ClearData()
        {
            MangaMateLinks.Clear();
            MangaMateData.Clear();
        }

        internal string GetUrl()
        {
            return MangaMateLinks.Count != 0 ? MangaMateLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, ushort pageNum)
        {
            // https://mangamate.shop/search?options%5Bprefix%5D=last&page=2&q=Naruto+manga
            string url = $"https://mangamate.shop/search?options%5Bprefix%5D=last&page={pageNum}&q={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}+{(bookType == BookType.Manga ? "manga" : "novel")}";
            LOGGER.Info("Page {} => {}", pageNum, url);
            MangaMateLinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }
            else
            {
                entryTitle = TitleParseRegex().Replace(entryTitle, string.Empty);
            }
            StringBuilder curTitle = new StringBuilder(entryTitle).Replace(",", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, ":", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Color Edition", "In Color");
            if (bookTitle.Equals("boruto", StringComparison.OrdinalIgnoreCase)) { curTitle.Replace(" Naruto Next Generations", string.Empty); }

            Match findVolNumMatch = MasterScrape.FindVolNumRegex().Match(curTitle.ToString().Trim());
            if (bookType == BookType.Manga && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Vol") && !string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
            {
                curTitle.Insert(findVolNumMatch.Index, "Vol ").TrimEnd();
            }
            else if (bookTitle.Contains("Noragami", StringComparison.OrdinalIgnoreCase) && entryTitle.Contains("Stray Stories") && string.IsNullOrWhiteSpace(findVolNumMatch.Groups[0].Value))
            {
                curTitle.Insert(curTitle.Length, " Vol 1");
            }

            string volNum = findVolNumMatch.Groups[0].Value;
            if (volNum.Length > 1 && volNum.StartsWith('0'))
            {
                curTitle.Replace(volNum, volNum.TrimStart('0'));
            }
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetMangaMateData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                HtmlWeb web = new() { UsingCacheIfExists = true, UseCookies = false };
                HtmlDocument doc = new() { OptionCheckSyntax = false };
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));

                ushort curPageNum = 1;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, curPageNum));
                wait.Until(driver => driver.FindElement(By.XPath("(//div[@class='grid grid--uniform'])[2]")));

                ushort maxPageNum = 1;
                try { maxPageNum = ushort.Parse(driver.FindElement(By.XPath("//span[@class='page'][last()]")).Text.Trim()); }
                catch (NoSuchElementException) {}

                doc.LoadHtml(driver.PageSource);
                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockstatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNodeCollection stockstatusData2 = doc.DocumentNode.SelectNodes(StockStatusXPath2);
                    HtmlNodeCollection entryLinkData = doc.DocumentNode.SelectNodes(EntryLinkXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        if (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && !(
                                bookType == BookType.Manga
                                && (
                                    entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                    ||(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Story")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear")
                                    )
                                )
                            )
                        )
                        {
                            string type = web.Load($"https://mangamate.shop{entryLinkData[x].GetAttributeValue("href", "error")}").DocumentNode.SelectSingleNode(EntryTypeXPath).InnerText.Trim();
                            // LOGGER.Debug("{} | {}", entryTitle, type);

                            if ((bookType == BookType.Manga && (type.Equals("Manga") || type.Equals("Box Set"))) || (bookType == BookType.LightNovel && (type.Equals("Novel") || type.Equals("Box Set"))))
                            {
                                MangaMateData.Add(
                                    new EntryModel
                                    (
                                        ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol"), bookTitle, bookType),
                                        priceData[x].InnerText.Trim(),
                                        stockstatusData2[x].GetAttributeValue("class", "error").Contains("preorder") ? StockStatus.PO : stockstatusData[x].InnerText.Trim() switch
                                        {
                                            "Sold Out" => StockStatus.OOS,
                                            _ => StockStatus.IS
                                        },
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed {}", entryTitle); } 
                        }  
                        else { LOGGER.Info("Removed {}", entryTitle); }    
                    }

                    if (curPageNum < maxPageNum)
                    {
                        driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, ++curPageNum));
                        wait.Until(driver => driver.FindElement(By.XPath("(//div[@class='grid grid--uniform'])[2]")));
                        doc.LoadHtml(driver.PageSource);
                    }
                    else { break; }
                }

                MangaMateData = InternalHelpers.RemoveDuplicateEntries(MangaMateData);
                MangaMateData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, MangaMateData, LOGGER);
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} | {} Does Not Exist @ {} \n{}", bookTitle, bookType, WEBSITE_TITLE, ex);
            }
            finally
            {
                driver?.Quit();
            }
            return MangaMateData;
        }
    }
}