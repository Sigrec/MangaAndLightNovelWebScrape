using System.Threading;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class AmazonUSA
    {
        private List<string> AmazonUSALinks = new List<string>();
        private List<EntryModel> AmazonUSAData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Amazon USA";
        public const Region REGION = Region.America;
        public const string WEBSITE_LINK = "https://www.amazon.com/Manga-Comics-Graphic-Novels-Books/b?ie=UTF8&node=4367";
        private static readonly Logger LOGGER = LogManager.GetLogger("AmazonUSALogs");
        private static readonly List<string> TitleRemovalStrings = ["Kindle", "Manga Set", "Manga Complete Book Set", "Collection Set", "ESPINAS", "nº", "BOOKS COVER", "Free Comic Book", "v. ", "CN:"];

        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='a-section a-spacing-none puis-padding-right-small s-title-instructions-style']/h2/a/span");
        private static readonly XPathExpression TopPriceXPath = XPathExpression.Compile("//div[@data-cy='price-recipe']");
        private static readonly XPathExpression BottomPriceXPath = XPathExpression.Compile("//div[@class='puisg-col-inner']//div[@class='puisg-col-inner']/div[last()]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='a-section a-spacing-none a-spacing-top-micro puis-price-instructions-style' or @class='a-section a-spacing-small puis-padding-left-small puis-padding-right-small']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//span[@class='s-pagination-item s-pagination-disabled'] | //span[@class='s-pagination-strip']/a[(last() - 1)]");

        [GeneratedRegex(@"\(.*?\)|―The Manga", RegexOptions.IgnoreCase)] internal static partial Regex FormatMangaEntryTitleRegex();
        [GeneratedRegex(@"(?:\$\d{1,3}\.\d{1,2})")] internal static partial Regex GetPriceRegex();
        [GeneratedRegex(@"Save\s(\$\d{1,3}\.\d{1,2})", RegexOptions.IgnoreCase)] internal static partial Regex GetCouponRegex();
        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FormatVolumeRegex();
        [GeneratedRegex(@"\d{1,3}-\d{1,3}-(\d{1,3})")] private static partial Regex OmnibusCheckRegex();
        [GeneratedRegex(@"\(Omnibus Edition\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
        [GeneratedRegex(@":.*")] private static partial Regex ColonFixRegex();
        [GeneratedRegex(@"\((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s\d{1,3}\s\d{4}\)", RegexOptions.IgnoreCase)] private static partial Regex ContainsDateRegex();

        protected internal async Task CreateAmazonUSATask(string bookTitle, BookType book, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetAmazonUSAData(bookTitle, book, driver));
            });
        }

        protected internal string GetUrl()
        {
            return AmazonUSALinks.Count != 0 ? AmazonUSALinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }
        
        protected internal void ClearData()
        {
            AmazonUSALinks.Clear();
            AmazonUSAData.Clear();
        }

        // Manga
        // https://www.amazon.com/s?k=world+trigger&i=stripbooks&rh=n%3A4367%2Cp_n_condition-type%3A1294423011%2Cp_n_availability%3A2661601011%2Cp_n_feature_nine_browse-bin%3A3291437011&dc&qid=1713630168&rnid=3291435011&ref=sr_pg_1

        // https://www.amazon.com/s?k=world+trigger&i=stripbooks&rh=n%3A4367%2Cp_n_condition-type%3A1294423011%2Cp_n_availability%3A2661601011%2Cp_n_feature_nine_browse-bin%3A3291437011&dc&page=2&qid=1713632732&rnid=3291435011&ref=sr_pg_2
        private string GenerateWebsiteUrl(BookType bookType, uint curPage, string bookTitle)
        {
            string url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A4367%2Cp_n_condition-type%3A1294423011%2Cp_n_availability%3A2661601011%2Cp_n_feature_nine_browse-bin%3A3291437011&dc&page={curPage}&qid=1713630168&rnid=3291435011&ref=sr_pg_{curPage}";
            LOGGER.Debug(url);
            AmazonUSALinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, BookType bookType, string bookTitle)
        {
            string insertString = string.Empty;
            if (OmnibusCheckRegex().IsMatch(entryTitle))
            {
                insertString = $" Omnibus Vol {Convert.ToInt32(OmnibusCheckRegex().Match(entryTitle).Groups[1].Value) / 3}";
                entryTitle = entryTitle.Insert(entryTitle.IndexOf(':'), insertString);
            }
            else if (entryTitle.Contains("Omnibus"))
            {
                entryTitle = OmnibusFixRegex().Replace(entryTitle, "Omnibus");
            }

            if (bookType == BookType.Manga)
            {
                entryTitle = FormatMangaEntryTitleRegex().Replace(entryTitle, string.Empty);
            }

            if (!bookTitle.Contains(':') && entryTitle.Contains(':') && !entryTitle.Substring(entryTitle.IndexOf(':')).Contains("Vol ") && entryTitle.Any(char.IsDigit))
            {
                entryTitle = ColonFixRegex().Replace(entryTitle, string.Empty);
            }

            StringBuilder parsedTitle = new StringBuilder(entryTitle);
            parsedTitle.Replace(",", "");
            InternalHelpers.RemoveCharacterFromTitle(ref parsedTitle, bookTitle, ':');

            if (char.IsDigit(parsedTitle.ToString()[parsedTitle.Length - 1]) && !entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set"))
            {
                parsedTitle = parsedTitle.Insert(MasterScrape.FindVolNumRegex().Match(parsedTitle.ToString()).Index, "Vol ");
            }

            parsedTitle.TrimEnd();
            if (entryTitle.Contains("Box Set") && !char.IsDigit(parsedTitle[parsedTitle.Length - 1]))
            {
                parsedTitle.Append(" 1");
            }

            if (bookTitle.Contains("Adventure of Dai"))
            {
                parsedTitle.Replace(" Disciples of Avan", string.Empty);
            }
            parsedTitle.TrimEnd([ ':' ]);

            LOGGER.Info("AFTER = {}", MasterScrape.MultipleWhiteSpaceRegex().Replace(parsedTitle.ToString(), " ").Trim());
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(parsedTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetAmazonUSAData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                uint curPage = 1;
                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, curPage, bookTitle));
                wait.Until(driver => driver.FindElement(By.XPath("/html/body/div[1]/div[1]/div[1]/div[1]/div/span[1]/div[1]")));
                // Thread.Sleep(100000);

                HtmlDocument doc = new() { OptionCheckSyntax = true, OptionFixNestedTags = true };
                doc.LoadHtml(driver.PageSource);

                HtmlNodeCollection pageNums = doc.DocumentNode.SelectNodes(PageCheckXPath);
                uint maxPage = pageNums != null ? (pageNums.Count == 2 ? Convert.ToUInt32(pageNums[1].InnerText.Trim()) : Convert.ToUInt32(pageNums[0].InnerText.Trim())) : 0;

                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                while (true)
                {
                    driver.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection topPriceData = doc.DocumentNode.SelectNodes(TopPriceXPath);
                    HtmlNodeCollection bottomPriceData = doc.DocumentNode.SelectNodes(BottomPriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                    // LOGGER.Debug("{} | {} | {} | {}", titleData.Count, topPriceData.Count, bottomPriceData.Count, stockStatusData.Count); 

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        string stockStatus = stockStatusData[x].InnerText;
                        string topPrice = topPriceData[x].InnerText.Trim();
                        string bottomPrice = bottomPriceData[x].InnerText.Trim();
                        LOGGER.Info("BEFORE = {} | {} | {} | {}", entryTitle, stockStatus, topPrice, bottomPrice);
                        if (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && stockStatus.Contains('$')
                            && !ContainsDateRegex().IsMatch(entryTitle)
                            && !entryTitle.ContainsAny(TitleRemovalStrings)
                            && !((topPrice.Contains("Kindle") || topPrice.Contains("Available instantly")) && (bottomPrice.Contains("Kindle") || bottomPrice.Contains("Available instantly")))
                            && !(
                                bookType == BookType.Manga
                                && (
                                     InternalHelpers.RemoveUnintendedVolumes(bookTitle, "one piece", entryTitle, "joy boy")
                                )
                            )
                        )
                        {
                            string price = string.Empty;
                            if (topPrice.Contains("Paperback") || topPrice.Contains("Hardcover"))
                            {
                                price = GetPriceRegex().Match(topPrice).Value.Trim();
                            }
                            else if (bottomPrice.Contains("Paperback") || bottomPrice.Contains("Hardcover"))
                            {
                                price = GetPriceRegex().Match(bottomPrice).Value.Trim();
                            }
                            else
                            {
                                LOGGER.Error("No Valid Price, Top Price = {} | Bottom Price = {}", topPrice, bottomPrice);
                                continue;
                            }

                            if (stockStatus.Contains("Save $"))
                            {
                                decimal coupon = Convert.ToDecimal(GetCouponRegex().Match(stockStatus).Groups[1].Value.TrimStart('$'));
                                LOGGER.Info("Applying Coupon {} to {} for {}", coupon, entryTitle, price.TrimStart('$'));
                                price = $"${InternalHelpers.ApplyCoupon(Convert.ToDecimal(price.TrimStart('$')), coupon)}";
                            }
                            AmazonUSAData.Add(
                                new EntryModel
                                    (
                                        ParseTitle(FormatVolumeRegex().Replace(entryTitle, "Vol"), bookType, bookTitle),
                                        price,
                                        stockStatus.Contains("Pre-order") ? StockStatus.PO : StockStatus.IS,
                                        WEBSITE_TITLE
                                    )
                                );
                        }
                        else
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                        }
                    }

                    if (curPage < maxPage)
                    {
                        driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookType, ++curPage, bookTitle));
                        wait.Until(driver => driver.FindElement(By.CssSelector("div[class='s-main-slot s-result-list s-search-results sg-row']")));
                        doc.LoadHtml(driver.PageSource);
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
                AmazonUSAData.Sort(EntryModel.VolumeSort);
                AmazonUSAData.RemoveExtras();
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, AmazonUSAData, LOGGER);
            }
            return AmazonUSAData;
        }
    }
}