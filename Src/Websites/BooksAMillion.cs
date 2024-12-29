using System.Threading;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class BooksAMillion
    {
        private List<string> BooksAMillionLinks = new();
        private List<EntryModel> BooksAMillionData = new();
        public const string WEBSITE_TITLE = "Books-A-Million";
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='search-item-title']/a");
        private static readonly XPathExpression BookQualityXPath = XPathExpression.Compile("//div[@class='productInfoText']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='our-price']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='availability_search_results']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//ul[@class='search-page-list']//a[@title='Next']");

        [GeneratedRegex(@"(?<=Box Set).*|:|\!|,|Includes.*|--The Manga|The Manga|\d+-\d+|\(Manga\) |(?<=Omnibus\s\d{1,3})[^\d.].*|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex MangaFilterTitleRegex();
        [GeneratedRegex(@":|\!|,|Includes.*|\d+-\d+|\d+, \d+ \& \d+", RegexOptions.IgnoreCase)] private static partial Regex NovelFilterTitleRegex();
        [GeneratedRegex(@"(?<=Vol\s+\d+)[^\d\.].*|\(.*?\)$|\[.*?\]|Manga ", RegexOptions.IgnoreCase)] private static partial Regex CleanFilterTitleRegex();
        [GeneratedRegex(@"Box Set (\d+)", RegexOptions.IgnoreCase)] private static partial Regex VolNumberRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|3-In-1 V\d+|Vols\.|\d{1,3}-In-\d{1,3}|\d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"3-In-1 V(\d+)|\d{1,3}-In-\d{1,3}|(?:\d{1,3}-(\d{1,3}))$|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMatchRegex();
        [GeneratedRegex(@"Vol\.|Volumes|Volume|Vols", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

        internal async Task CreateBooksAMillionTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetBooksAMillionData(bookTitle, book, isMember, driver));
            });
        }

        internal string GetUrls()
        {
            return string.Join(" , ", BooksAMillionLinks);
        }

        internal void ClearData()
        {
            BooksAMillionLinks.Clear();
            BooksAMillionData.Clear();
        }

        private string GenerateWebsiteUrl(string bookTitle, bool boxsetCheck, BookType bookType, int pageNum, bool skipUrlAdd = false)
        {
            // Initialize a StringBuilder
            var stringBuilder = new StringBuilder("https://www.booksamillion.com/search?");
            
            // Directly append the query and filter components
            stringBuilder.Append($"query={InternalHelpers.FilterBookTitle(bookTitle)}");
            stringBuilder.Append("&sort=date");

            if (bookType == BookType.LightNovel)
            {
                // https://www.booksamillion.com/search?filter=product_type%3Abooks;query=classroom%20of%20the%20elite%20novel;sort=date;page=2&id=9253104384644
                stringBuilder.Append($"{(boxsetCheck ? "+novel+box+set" : "+novel")}&page={pageNum}");
            }
            else if (!boxsetCheck)
            {
                // https://booksamillion.com/search?query=jujutsu%20kaisen&filter=product_type%3Abooks%7Cbook_categories%3ACGN&sort=date
                // https://booksamillion.com/search?query=jujutsu%20kaisen;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=2&id=9253104384644
                stringBuilder.Append($"&filter=product_type%3Abooks%7Cbook_categories%3ACGN&page={pageNum}");
            }
            else
            {
                // https://booksamillion.com/search?filter=product_type%3Abooks&query=bleach+box+set
                stringBuilder.Append($"{(boxsetCheck ? "+box+set" : string.Empty)}");
            }

            // Convert StringBuilder to string
            string url = stringBuilder.ToString();

            if (!skipUrlAdd)
            {
                BooksAMillionLinks.Add(url);  // No need for ToString() here, `url` is already a string
            }

            return url;
        }

        private static string CleanAndParseTitle(string entryTitle, BookType bookType, string bookTitle)
        {
            StringBuilder curTitle;
            if (bookType == BookType.LightNovel)
            {
                entryTitle = CleanFilterTitleRegex().Replace(NovelFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty);
                curTitle = new StringBuilder(entryTitle).Replace("(Novel)", "Novel").Replace("(Light Novel)", "Novel");
                entryTitle = curTitle.ToString();
                if (!entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase))
                {
                    int volIndex = entryTitle.IndexOf("Vol");
                    int boxSetIndex = entryTitle.IndexOf("Box Set");
                    if (volIndex != -1)
                    {
                        curTitle.Insert(volIndex, "Novel ");
                    }
                    else if (boxSetIndex != -1)
                    {
                        curTitle.Insert(boxSetIndex, "Novel ");
                    }
                    else
                    {
                        curTitle.Append(" Novel");
                    }
                }
            }
            else
            {
                if (entryTitle.Contains("Omnibus") || 
                    entryTitle.Contains("3-in-1", StringComparison.CurrentCultureIgnoreCase) || 
                    entryTitle.Contains("2-in-1", StringComparison.CurrentCultureIgnoreCase))
                {
                    entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
                }
                
                curTitle = new StringBuilder(CleanFilterTitleRegex().Replace(MangaFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty));
                entryTitle = curTitle.ToString().Trim();

                if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Naruto Next Generations", string.Empty);
                }

                if (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || 
                    entryTitle.Contains("BOXSET", StringComparison.OrdinalIgnoreCase))
                {
                    if (bookTitle.Equals("Noruto", StringComparison.OrdinalIgnoreCase) && 
                        entryTitle.Equals(entryTitle.ToUpper()))
                    {
                        curTitle.Replace("NARUTO BOXSET V01-V27", "Naruto Box Set 1");
                    }
                    else
                    {
                        string boxSetNum = VolNumberRegex().Match(entryTitle).Groups[1].Value;
                        if (!bookTitle.ContainsAny(["attack on titan"]))
                        {
                            curTitle.AppendFormat(" {0}", !string.IsNullOrWhiteSpace(boxSetNum) ? boxSetNum : '1');
                        }
                    }
                }
                else if (OmnibusMatchRegex().IsMatch(entryTitle))
                {
                    GroupCollection omnibusMatch = OmnibusMatchRegex().Match(entryTitle).Groups;
                    string firstOmniNum = omnibusMatch[1].Value.TrimStart('0');
                    string secondOmniNum = omnibusMatch[2].Value;
                    string thirdOmniNum = omnibusMatch[3].Value;

                    if (!entryTitle.Contains(" Omnibus ", StringComparison.OrdinalIgnoreCase))
                    {
                        curTitle.Insert(entryTitle.IndexOf("Vol", StringComparison.OrdinalIgnoreCase), "Omnibus ");
                    }
                    
                    if (!entryTitle.Contains(" Vol ", StringComparison.OrdinalIgnoreCase))
                    {
                        curTitle.Append("Vol ");
                    }
                    
                    if (!char.IsDigit(curTitle.ToString().Trim()[^1]))
                    {
                        if (!string.IsNullOrWhiteSpace(firstOmniNum))
                        {
                            curTitle.Append(firstOmniNum);
                        }
                        else if (!string.IsNullOrWhiteSpace(secondOmniNum))
                        {
                            curTitle.Append(Math.Ceiling(Convert.ToDecimal(secondOmniNum) / 3));
                        }
                        else if (!string.IsNullOrWhiteSpace(thirdOmniNum))
                        {
                            curTitle.Append(Math.Ceiling(Convert.ToDecimal(thirdOmniNum) / 3));
                        }
                    }
                }
                else if (!entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) &&
                         !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) &&
                         MasterScrape.FindVolNumRegex().IsMatch(entryTitle))
                {
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(entryTitle).Index, "Vol ");
                }

                if (entryTitle.Contains("Stall", StringComparison.OrdinalIgnoreCase) && 
                    !bookTitle.Contains("Stall", StringComparison.OrdinalIgnoreCase))
                {
                    int volIndex = entryTitle.LastIndexOf("Vol", StringComparison.OrdinalIgnoreCase);
                    if (volIndex != -1)
                    {
                        curTitle.Remove(volIndex, curTitle.Length - volIndex);
                    }
                }
            }

            entryTitle = curTitle.ToString();
            if (entryTitle.Contains("vols.", StringComparison.OrdinalIgnoreCase))
            {
                int index = entryTitle.IndexOf("vols.", StringComparison.OrdinalIgnoreCase);
                curTitle.Remove(index, curTitle.Length - index);
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
        }

        internal List<EntryModel> GetBooksAMillionData(string bookTitle, BookType bookType, bool memberStatus, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));

                HtmlDocument doc = new HtmlDocument();
                bool boxsetCheck = false, boxsetValidation = false;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                int pageNum = 1;
                string curUrl = GenerateWebsiteUrl(bookTitle, boxsetCheck, bookType, pageNum);
                LOGGER.Info($"Initial Url {curUrl}");
                driver.Navigate().GoToUrl(curUrl);

                // Check for promotion popup and clear them if it exist
                if (driver.FindElements(By.ClassName("ltkpopup-container")).Count != 0)
                {
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("ltkpopup-close"))));
                }
                
                while(true)
                {
                    wait.Until(e => e.FindElement(By.Id("content")));

                    // Initialize the html doc for crawling
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection bookQuality = doc.DocumentNode.SelectNodes(BookQualityXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode(PageCheckXPath);

                    for(int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = titleData[x].InnerText;
                        if (!boxsetCheck && (entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || entryTitle.Contains("BOXSET", StringComparison.OrdinalIgnoreCase)))
                        {
                            boxsetValidation = true;
                            continue;
                        }

                        if ((!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || 
                            BookTitleRemovalCheck || 
                            entryTitle.Contains("With Poster", StringComparison.OrdinalIgnoreCase)) &&
                            InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) &&
                            !bookQuality[x].InnerText.Contains("Library Binding") &&
                            (titleData.Count == 1 && !boxsetCheck || 
                            !(bookType == BookType.Manga &&
                                (
                                    entryTitle.Contains("(Light Novel", StringComparison.OrdinalIgnoreCase) ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto", "Itachi's Story", "Team 7 Character") ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony") ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Attack on Titan", entryTitle, "Adventure") ||
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Heroines") ||
                                    (!entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) &&
                                    entryTitle.Any(char.IsDigit) &&
                                    !bookTitle.Any(char.IsDigit))
                                )
                            ))
                        )
                        {
                            entryTitle = CleanAndParseTitle(
                                FixVolumeRegex().Replace(System.Net.WebUtility.HtmlDecode(entryTitle), "Vol"), 
                                bookType, 
                                bookTitle
                            );
                            
                            if (!BooksAMillionData.Exists(entry => entry.Entry.Equals(entryTitle)))
                            {
                                decimal priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim()[1..]);

                                ReadOnlySpan<char> stockText = stockStatusData[x].InnerText.AsSpan();
                                StockStatus stockStatus = stockText.Contains("In Stock", StringComparison.OrdinalIgnoreCase) ? StockStatus.IS : 
                                stockText.Contains("Preorder", StringComparison.OrdinalIgnoreCase) ? StockStatus.PO : 
                                stockText.Contains("On Order", StringComparison.OrdinalIgnoreCase) ? StockStatus.BO : 
                                StockStatus.OOS;

                                BooksAMillionData.Add(
                                    new EntryModel
                                    (
                                        entryTitle,
                                        $"${(memberStatus ? EntryModel.ApplyDiscount(priceVal, MEMBERSHIP_DISCOUNT) : priceVal.ToString())}",
                                        stockStatus,
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

                    if (pageCheck != null)
                    {
                        // driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath($"//ul[@class='search-page-list']//a[@title='Next']"))));
                        // wait.Until(e => e.FindElement(By.Id("content")));
                        curUrl = GenerateWebsiteUrl(bookTitle, boxsetCheck, bookType, ++pageNum, true);
                        driver.Navigate().GoToUrl(curUrl);
                        LOGGER.Info($"Next Page {driver.Url}");
                    }
                    else
                    {
                        if (boxsetValidation && !boxsetCheck)
                        {
                            boxsetCheck = true;
                            curUrl = GenerateWebsiteUrl(bookTitle, boxsetCheck, bookType, pageNum);
                            driver.Navigate().GoToUrl(curUrl);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error($"{bookTitle} | {bookType} Does Not Exist @ BooksAMillion\n{ex}");
            }
            finally
            {
                driver?.Quit();
                BooksAMillionData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, BooksAMillionData, LOGGER);
            }
            return BooksAMillionData;
        }
    }
}