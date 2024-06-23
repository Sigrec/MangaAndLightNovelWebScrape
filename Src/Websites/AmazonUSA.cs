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
        private static readonly List<string> TitleRemovalStrings = [ "Kindle", "Manga Set", "Book Set", "Books Set", "Collection Set", "Novels Set", "books Collection", "Novel Set", "ESPINAS", "nº", "BOOKS COVER", "Free Comic Book", "n.", "v. ", "CN:", "Reedición", "Català" ];
        private static readonly List<string> EntryPriceRemovalStrings = [ "Kindle", "Available instantly", "No featured offers available", "Unknown Binding", "DVD", "Blu-ray" ];
        private static readonly List<string> ValidPriceStrings = [ "Paperback", "Hardcover" ];

        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("(//div[@class='a-section a-spacing-none puis-padding-right-small s-title-instructions-style'] | //div[@data-cy='title-recipe'])/h2/a/span");
        private static readonly XPathExpression TopPriceXPath = XPathExpression.Compile("//div[@data-cy='price-recipe']");
        private static readonly XPathExpression BottomPriceXPath = XPathExpression.Compile("//div[@class='puisg-row']/div[2]/div/div/div[@class='puisg-row']/div[1]/div/div[last()]");
        private static readonly XPathExpression NovelStockStatusXPath = XPathExpression.Compile("//div[@data-cy='delivery-recipe']");
        private static readonly XPathExpression MangaStockStatusXPath = XPathExpression.Compile("//div[@class='a-section a-spacing-none a-spacing-top-micro puis-price-instructions-style' or @class='a-section a-spacing-small puis-padding-left-small puis-padding-right-small']/..");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//span[@class='s-pagination-item s-pagination-disabled'] | //span[@class='s-pagination-strip']/a[(last() - 1)]");

        [GeneratedRegex(@"\(.*?\)$|―The Manga|The Manga| Manga", RegexOptions.IgnoreCase)] internal static partial Regex FormatMangaTitleRegex();
        [GeneratedRegex(@"\(Light Novel\)|\(Novel\)|\(.* Novel(?:\)|s\))", RegexOptions.IgnoreCase)] internal static partial Regex FormatNovelTitleRegex();
        [GeneratedRegex(@"(?:\$\d{1,3}\.\d{1,2})")] internal static partial Regex GetPriceRegex();
        [GeneratedRegex(@"Save\s(\$\d{1,3}\.\d{1,2})", RegexOptions.IgnoreCase)] internal static partial Regex GetCouponRegex();
        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FormatVolumeRegex();
        [GeneratedRegex(@"\d{1,3}-\d{1,3}-(\d{1,3})|\d{1,3}-(\d{1,3})")] private static partial Regex OmnibusCheckRegex();
        [GeneratedRegex(@"\((?:Omnibus|\d{1}-in-\d{1}) Edition\)|\d{1}-in-\d{1} Edition|\d{1}-in-\d{1}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusFixRegex();
        [GeneratedRegex(@":.*")] private static partial Regex ColonFixRegex();
        [GeneratedRegex(@"\((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,3}\s\d{4}\)|\(\s+\d{4}\s+\)|\d{4}-\d{1,2}-\d{1,2}", RegexOptions.IgnoreCase)] private static partial Regex ContainsDateRegex();
        [GeneratedRegex(@"\((.*)\)|:(.*)", RegexOptions.IgnoreCase)] private static partial Regex ExtractTextRegex();
        [GeneratedRegex(@"(?:Korean|German|Japanese|Spanish|French|Italian) Edition", RegexOptions.IgnoreCase)] private static partial Regex LangCheckRegex();
        [GeneratedRegex(@"(\d{1,3}) Special Edition.*", RegexOptions.IgnoreCase)] private static partial Regex SpecialEditionRegex();

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

        //Novel
        // https://www.amazon.com/s?k=classroom+of+the+elite+light+novel&i=stripbooks&rh=n%3A283155%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page=2&crid=37IOUUTCBCGQW&qid=1719069790&rnid=1294421011&ref=sr_pg_2
        private string GenerateWebsiteUrl(BookType bookType, uint curPage, string bookTitle)
        {
            string url = string.Empty;
            if (bookType == BookType.Manga)
            {
                url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}&i=stripbooks&rh=n%3A4367%2Cp_n_condition-type%3A1294423011%2Cp_n_availability%3A2661601011%2Cp_n_feature_nine_browse-bin%3A3291437011&dc&page={curPage}&qid=1713630168&rnid=3291435011&ref=sr_pg_{curPage}";
            }
            else if (bookType == BookType.LightNovel)
            {
                url = $"https://www.amazon.com/s?k={bookTitle.Replace(" ", "+")}+light+novel&i=stripbooks&rh=n%3A283155%2Cp_n_feature_nine_browse-bin%3A3291437011%2Cp_n_condition-type%3A1294423011&dc&page={curPage}&crid=37IOUUTCBCGQW&qid=1719069790&rnid=1294421011&ref=sr_pg_{curPage}";
            }

            LOGGER.Info(url);
            AmazonUSALinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, BookType bookType, string bookTitle)
        {
            string insertString = string.Empty;
            bool omniCheck = OmnibusCheckRegex().IsMatch(entryTitle);
            if (OmnibusFixRegex().IsMatch(entryTitle))
            {
                LOGGER.Debug("ENTER {}", entryTitle);
                entryTitle = OmnibusFixRegex().Replace(entryTitle, "Omnibus");
            }
            else if (!entryTitle.Contains("Box Set") && !entryTitle.Contains("Omnibus") && omniCheck)
            {
                GroupCollection omniVol = OmnibusCheckRegex().Match(entryTitle).Groups;
                int volNum = -1;
                for(int i = 1; i < omniVol.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(omniVol[i].Value))
                    {
                        volNum = Convert.ToInt32(omniVol[i].Value) / 3;
                        break;
                    }
                }
                insertString = $" Omnibus Vol {volNum}";
                if (entryTitle.Contains(':'))
                {
                    entryTitle = entryTitle.Insert(entryTitle.IndexOf(':'), insertString);
                }
                else
                {
                    entryTitle = entryTitle[..entryTitle.IndexOf("Vol")] + insertString;
                }
            }
            
            LOGGER.Debug("CHECK 0 - {}", entryTitle);

            if (bookType == BookType.Manga)
            {
                if (entryTitle.Contains("Naruto Chapter Book"))
                {
                    entryTitle = ExtractTextRegex().Match(entryTitle).Groups[1].Value;
                }
                else if (entryTitle.Contains(':') && char.IsDigit(entryTitle[^1]) && !entryTitle.Contains("Includes") && !entryTitle[..entryTitle.IndexOf(':')].Contains("Vol"))
                {
                    entryTitle = entryTitle.Insert(entryTitle.LastIndexOf(':'), ExtractTextRegex().Match(entryTitle).Groups[2].Value);
                }
                entryTitle = FormatMangaTitleRegex().Replace(entryTitle, string.Empty);
            }
            else if (bookType == BookType.LightNovel)
            {
                entryTitle = FormatNovelTitleRegex().Replace(entryTitle, "Novel");
            }
            LOGGER.Debug("CHECK 1 - {}", entryTitle);
            int colonIndex = entryTitle.IndexOf(':');
            if (!bookTitle.Contains(':') && entryTitle.Contains(':') && (!entryTitle[colonIndex..].Contains("Vol ") || entryTitle[..colonIndex].Contains("Vol ")) && entryTitle.Any(char.IsDigit))
            {
                entryTitle = ColonFixRegex().Replace(entryTitle, string.Empty);
            }

            if (entryTitle.Contains("Special Edition"))
            {
                entryTitle = SpecialEditionRegex().Replace(entryTitle, "Vol $1 Special Edition");
            }

            StringBuilder parsedTitle = new StringBuilder(entryTitle).Replace(",", "");
            InternalHelpers.ReplaceTextInEntryTitle(ref parsedTitle, bookTitle, "Books", "Book");
            InternalHelpers.RemoveCharacterFromTitle(ref parsedTitle, bookTitle, ':');
            // InternalHelpers.RemoveCharacterFromTitle(ref parsedTitle, bookTitle, '-');
            InternalHelpers.ReplaceTextInEntryTitle(ref parsedTitle, bookTitle, "―", " ");
            InternalHelpers.ReplaceTextInEntryTitle(ref parsedTitle, bookTitle, "-", " ");
            parsedTitle.TrimEnd();

            if (entryTitle.Contains("Deluxe") && !entryTitle.Contains("Deluxe Edition") && !bookTitle.Contains("Deluxe"))
            {
                parsedTitle.Replace("Deluxe", "Deluxe Edition");
            }

            if (bookTitle.Contains("Boruto", StringComparison.CurrentCultureIgnoreCase))
            {
                parsedTitle.Replace(" Naruto Next Generations", string.Empty);
            }

            if (char.IsDigit(parsedTitle.ToString()[parsedTitle.Length - 1]) && !parsedTitle.ToString().Contains("Vol") && !parsedTitle.ToString().Contains("Box Set"))
            {
                parsedTitle = parsedTitle.Insert(MasterScrape.FindVolNumRegex().Match(parsedTitle.ToString()).Index, "Vol ");
            }

            parsedTitle.TrimEnd();
            if (entryTitle.Contains("Box Set") && !entryTitle.Contains("Season") && !entryTitle.Contains("Part") && !entryTitle.Contains("Compelte Box Set") && !char.IsDigit(parsedTitle[parsedTitle.Length - 1]))
            {
                parsedTitle.Append(" 1");
            }
            LOGGER.Debug("CHECK 2 - {}", parsedTitle.ToString());

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
                    // driver.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection topPriceData = doc.DocumentNode.SelectNodes(TopPriceXPath);
                    HtmlNodeCollection bottomPriceData = null;
                    if (bookType == BookType.Manga) bottomPriceData = doc.DocumentNode.SelectNodes(BottomPriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(bookType == BookType.Manga ? MangaStockStatusXPath : NovelStockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                    LOGGER.Debug("{} | {} | {} | {}", titleData != null ? titleData.Count : "null", stockStatusData != null ? stockStatusData.Count : "null", topPriceData != null ? topPriceData.Count : "null", bottomPriceData != null ? bottomPriceData.Count : "null");

                    int entryCount = bookType == BookType.Manga ? Math.Min(titleData.Count, Math.Min(stockStatusData.Count, Math.Min(topPriceData.Count, bottomPriceData.Count))) : Math.Min(titleData.Count, Math.Min(stockStatusData.Count, topPriceData.Count));
                    for (int x = 0; x < entryCount; x++)
                    {
                        string entryTitle = titleData[x].InnerText.Trim();
                        string stockStatus = stockStatusData[x].InnerText;
                        string topPrice = topPriceData[x].InnerText.Trim();
                        string bottomPrice = string.Empty;
                        if (bookType == BookType.Manga) bottomPrice = bottomPriceData[x].InnerText.Trim();
                        LOGGER.Info("BEFORE = {} | {} | {} | {}", entryTitle, stockStatus, topPrice, bottomPrice);
                        if (InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && (!stockStatus.Contains("No featured offers available") || stockStatus.ContainsAny(ValidPriceStrings))
                            && !entryTitle.ContainsAny(TitleRemovalStrings)
                            && !(topPrice.ContainsAny(EntryPriceRemovalStrings) && bottomPrice.ContainsAny(EntryPriceRemovalStrings))
                            && (((topPrice.ContainsAny(ValidPriceStrings) || !topPrice.Contains("Library Binding", StringComparison.CurrentCultureIgnoreCase)) && topPrice.Contains('$')) || ((bottomPrice.ContainsAny(ValidPriceStrings) || !bottomPrice.Contains("Library Binding", StringComparison.CurrentCultureIgnoreCase)) && bottomPrice.Contains('$')))
                            && !ContainsDateRegex().IsMatch(entryTitle)
                            && !LangCheckRegex().IsMatch(entryTitle)
                            && (
                                (
                                    bookType == BookType.Manga
                                    && (!entryTitle.Contains("Novel", StringComparison.CurrentCultureIgnoreCase) || entryTitle.Contains("Graphic Novel", StringComparison.CurrentCultureIgnoreCase))
                                    && (!entryTitle.Contains("Adventure") || (entryTitle.Contains("Adventure") && !entryTitle[..entryTitle.IndexOf(':')].Contains("Adventure")))
                                    && !(
                                        InternalHelpers.RemoveUnintendedVolumes(bookTitle, "naruto", entryTitle, "boruto")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "one piece", entryTitle, "joy boy")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "bleach", entryTitle, "maximum bleach")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "overlord", entryTitle, "starfall")
                                        || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "berserk", entryTitle, [ "gluttony", "flame dragon knight", "darkness ink" ])
                                    )
                                )
                                ||
                                (
                                    bookType == BookType.LightNovel
                                    && !entryTitle.Contains("(Manga)", StringComparison.CurrentCultureIgnoreCase)
                                    && (entryTitle.Contains("(Novel)", StringComparison.CurrentCultureIgnoreCase) || entryTitle.Contains("Light Novel", StringComparison.CurrentCultureIgnoreCase) || entryTitle.Contains("Novels", StringComparison.CurrentCultureIgnoreCase))
                                )
                            )
                        )
                        {
                            string price = string.Empty;
                            if (topPrice.Contains('$') && (topPrice.Contains("Paperback") && !topPrice.ContainsAny(EntryPriceRemovalStrings) || topPrice.Contains("Hardcover")))
                            {
                                price = GetPriceRegex().Match(topPrice).Value.Trim();
                            }
                            else if (bottomPrice.Contains('$') && (bottomPrice.Contains("Paperback") && !bottomPrice.ContainsAny(EntryPriceRemovalStrings) || bottomPrice.Contains("Hardcover")))
                            {
                                price = GetPriceRegex().Match(bottomPrice).Value.Trim();
                            }
                            else if (stockStatus.Contains('$') && stockStatus.Contains("Paperback") && !stockStatus.ContainsAny(EntryPriceRemovalStrings) || stockStatus.Contains("Hardcover"))
                            {
                                price = GetPriceRegex().Match(stockStatus).Value.Trim();
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
                                        stockStatus switch
                                        {
                                            string curStatus when curStatus.Contains("Temporarily out of stock", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                            string curStatus when curStatus.Contains("Pre-order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                            _ => StockStatus.IS,
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
                AmazonUSAData.RemoveDuplicates(LOGGER);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, AmazonUSAData, LOGGER);
            }
            return AmazonUSAData;
        }
    }
}