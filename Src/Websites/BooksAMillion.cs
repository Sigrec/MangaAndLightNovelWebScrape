using OpenQA.Selenium.Edge;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class BooksAMillion
    {
        private List<string> BooksAMillionLinks = new();
        private List<EntryModel> BooksAMillionData = new();
        public const string WEBSITE_TITLE = "Books-A-Million";
        private static readonly Logger LOGGER = LogManager.GetLogger("BooksAMillionLogs");
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='search-item-title']/a");
        private static readonly XPathExpression BookQualityXPath = XPathExpression.Compile("//div[@class='productInfoText']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='our-price']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='availability_search_results']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//ul[@class='search-page-list']//a[@title='Next']");

        [GeneratedRegex(@"(?<=Box Set).*|:|\!|,|Includes.*|--The Manga|The Manga|\d+-\d+|\(Manga\) |\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex MangaFilterTitleRegex();
        [GeneratedRegex(@":|\!|,|Includes.*|\d+-\d+|\d+, \d+ \& \d+", RegexOptions.IgnoreCase)] private static partial Regex NovelFilterTitleRegex();
        [GeneratedRegex(@"(?<=Vol\s+\d+)[^\d\.].*|\(.*?\)$|\[.*?\]|Manga ", RegexOptions.IgnoreCase)] private static partial Regex CleanFilterTitleRegex();
        [GeneratedRegex(@"Box Set (\d+)", RegexOptions.IgnoreCase)] private static partial Regex VolNumberRegex();
        [GeneratedRegex(@"\(Omnibus Edition\)|\(3-In-1 Edition\)|\(2-In-1 Edition\)|3-In-1 V\d+|Vols\.|Vols|3-In-1", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"3-In-1 V(\d+)|(?:\d{1,3}-(\d{1,3}))$|\d{1,3},\s+\d{1,3}\s+\&\s+(\d{1,3})|\d{1,3}\s+\d{1,3}\s+\&\s+(\d{1,3})", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMatchRegex();
        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

        internal async Task CreateBooksAMillionTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetBooksAMillionData(bookTitle, book, isMember, driver));
            });
        }

        internal string GetUrl()
        {
            return BooksAMillionLinks.Count != 0 ? BooksAMillionLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        internal void ClearData()
        {
            BooksAMillionLinks.Clear();
            BooksAMillionData.Clear();
        }

        private string GetUrl(string bookTitle, bool boxsetCheck, BookType bookType){
            StringBuilder url = new StringBuilder();
            if (bookType == BookType.LightNovel)
            {
                // https://www.booksamillion.com/search?filter=product_type%3Abooks&query=naruto+novel&sort=date&page=1
                url.AppendFormat("https://www.booksamillion.com/search?filter=product_type%3Abooks&query={0}{1}&sort=date", InternalHelpers.FilterBookTitle(bookTitle), boxsetCheck ? "+novel+box+set" : "+novel");
                LOGGER.Info($"Initial Novel Url {url}");
            }
            else if (!boxsetCheck)
            {
                // https://booksamillion.com/search?query=naruto;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1%7Clanguage%3AENG"
                url.AppendFormat("https://booksamillion.com/search?query={0}&filter=product_type%3Abooks%7Cbook_categories%3ACGN&sort=date", InternalHelpers.FilterBookTitle(bookTitle));
                LOGGER.Info($"Initial Manga Url {url}");
            }
            else
            {
                // https://booksamillion.com/search?filter=product_type%3Abooks&query=bleach+box+set
                url.AppendFormat("https://booksamillion.com/search?filter=product_type%3Abooks&query={0}{1}", InternalHelpers.FilterBookTitle(bookTitle), boxsetCheck ? "+box+set" : string.Empty);
                LOGGER.Info($"Initial Box Set Url {url}");
            }
            BooksAMillionLinks.Add(url.ToString());
            return url.ToString();
        }

        private static string TitleParse(string entryTitle, BookType bookType, string bookTitle)
        {
            StringBuilder curTitle;
            if (bookType == BookType.LightNovel)
            {
                entryTitle = CleanFilterTitleRegex().Replace(NovelFilterTitleRegex().Replace(entryTitle, string.Empty), string.Empty);
                curTitle = new StringBuilder(entryTitle).Replace("(Novel)", "Novel").Replace("(Light Novel)", "Novel");
                if (!entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase))
                {
                    int volIndex = curTitle.ToString().IndexOf("Vol");
                    int boxSetIndex = curTitle.ToString().IndexOf("Box Set");
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
                        curTitle.Insert(curTitle.Length, " Novel");
                    }
                }
            }
            else
            {
                curTitle = new StringBuilder(CleanFilterTitleRegex().Replace(MangaFilterTitleRegex().Replace(OmnibusRegex().Replace(entryTitle, "Omnibus"), string.Empty), string.Empty));
                string newEntryTitle = curTitle.ToString().Trim();

                if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Naruto Next Generations", string.Empty);
                }

                if (newEntryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || newEntryTitle.Contains("BOXSET", StringComparison.OrdinalIgnoreCase))
                {
                    if (bookTitle.Equals("Noruto", StringComparison.OrdinalIgnoreCase) && newEntryTitle.Equals(newEntryTitle.ToUpper()))
                    {
                        curTitle.Replace("NARUTO BOXSET V01-V27", "Naruto Box Set 1");
                    }
                    else
                    {
                        string boxSetNum = VolNumberRegex().Match(entryTitle).Groups[1].Value;
                        curTitle.AppendFormat(" {0}", !string.IsNullOrWhiteSpace(boxSetNum) ? boxSetNum : '1');
                    }
                }
                else if (OmnibusMatchRegex().IsMatch(entryTitle))
                {
                    GroupCollection omnibusMatch = OmnibusMatchRegex().Match(entryTitle).Groups;
                    string firstOmniNum = omnibusMatch[1].Value.TrimStart('0');
                    string secondOmniNum = omnibusMatch[2].Value;
                    string thirdOmniNum = omnibusMatch[3].Value;

                    if (!newEntryTitle.Contains("Omnibus"))
                    {
                        curTitle.Insert(newEntryTitle.IndexOf("Vol"), "Omnibus ");
                    }
                    
                    if (!newEntryTitle.Contains("Vol"))
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
                else if (!newEntryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !newEntryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(newEntryTitle))
                {
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(newEntryTitle).Index, "Vol ");
                }

                if (newEntryTitle.Contains("First Stall"))
                {
                    curTitle.Remove(newEntryTitle.LastIndexOf("Vol"), curTitle.Length - newEntryTitle.LastIndexOf("Vol"));
                }
            }

            if (curTitle.ToString().Contains("vols.", StringComparison.OrdinalIgnoreCase))
            {
                int index = curTitle.ToString().IndexOf("vols.", StringComparison.OrdinalIgnoreCase);
                curTitle.Remove(index, curTitle.Length - index);
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
        }

        private List<EntryModel> GetBooksAMillionData(string bookTitle, BookType bookType, bool memberStatus, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));

                HtmlDocument doc = new HtmlDocument();
                bool boxsetCheck = false, boxsetValidation = false;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                driver.Navigate().GoToUrl(GetUrl(bookTitle, boxsetCheck, bookType));
                
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
                        string curTitle = titleData[x].InnerText;
                        if ((curTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) || curTitle.Contains("BOXSET", StringComparison.OrdinalIgnoreCase)) && !boxsetCheck)
                        {
                            boxsetValidation = true;
                            LOGGER.Info("Found Boxset");
                            continue;
                        }

                        if (
                            (!MasterScrape.EntryRemovalRegex().IsMatch(curTitle) || BookTitleRemovalCheck)
                            && InternalHelpers.BookTitleContainsEntryTitle(bookTitle, curTitle.ToString())
                            && !bookQuality[x].InnerText.Contains("Library Binding")
                            && (
                                (
                                    titleData.Count == 1
                                    && !boxsetCheck
                                )
                                || !(
                                    bookType == BookType.Manga 
                                    && (
                                            curTitle.Contains("(Light Novel") 
                                            || (
                                                !curTitle.Contains("Vol")
                                                && !(
                                                        curTitle.AsParallel().Any(char.IsDigit)
                                                        && !bookTitle.AsParallel().Any(char.IsDigit)
                                                    )
                                                )
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Boruto") 
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Itachi's Story")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Team 7 Character") 
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", curTitle.ToString(), "of Gluttony")
                                        )
                                )
                            )
                        )
                        {
                            curTitle = TitleParse(FixVolumeRegex().Replace(System.Net.WebUtility.HtmlDecode(curTitle), "Vol"), bookType, bookTitle);
                            if (!BooksAMillionData.Exists(entry => entry.Entry.Equals(curTitle)))
                            {
                                decimal priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim()[1..]);
                                BooksAMillionData.Add(
                                    new EntryModel
                                    (
                                        curTitle,
                                        $"${(memberStatus ? EntryModel.ApplyDiscount(priceVal, MEMBERSHIP_DISCOUNT) : priceVal.ToString())}",
                                        stockStatusData[x].InnerText switch
                                        {
                                            string curStatus when curStatus.Contains("In Stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                            string curStatus when curStatus.Contains("Preorder", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                            _ => StockStatus.OOS,
                                        },
                                        WEBSITE_TITLE
                                    )
                                );
                            }
                            else { LOGGER.Info("Removed {}", curTitle); }
                        }
                        else { LOGGER.Info("Removed {}", curTitle); }
                    }

                    if (pageCheck != null)
                    {
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath($"//ul[@class='search-page-list']//a[@title='Next']"))));
                        wait.Until(e => e.FindElement(By.Id("content")));
                        LOGGER.Info($"Next Page {driver.Url}");
                    }
                    else
                    {
                        if (boxsetValidation && !boxsetCheck)
                        {
                            boxsetCheck = true;
                            driver.Navigate().GoToUrl(GetUrl(bookTitle, boxsetCheck, bookType));
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                BooksAMillionData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, BooksAMillionData, LOGGER);
            }
            catch (Exception ex)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ BooksAMillion\n{ex}");
            }
            finally
            {
                driver?.Quit();
            }
            return BooksAMillionData;
        }
    }
}