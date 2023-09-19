using System.Text.RegularExpressions;
using System.Text;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class BooksAMillion
    {
        private static List<string> BooksAMillionLinks = new();
        private static List<EntryModel> BooksAMillionData = new();
        public const string WEBSITE_TITLE = "Books-A-Million";
        private static readonly Logger Logger = LogManager.GetLogger("BooksAMillionLogs");
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        [GeneratedRegex("Vol\\.|Volume")] private static partial Regex ParseTitleVolRegex();
        [GeneratedRegex("(?<=Box Set).*|:|\\!|,|(?<=Vol \\d+)[^\\d\\.].*|Includes.*|--The Manga|\\d+-\\d+|\\(Manga\\) |\\(Light Novel\\) |\\d+, \\d+ \\& \\d+")] private static partial Regex FilterTitleRegex();
        [GeneratedRegex("Box Set (\\d+)")] private static partial Regex VolNumberRegex();
        [GeneratedRegex("\\(Omnibus Edition\\)|\\(3-In-1 Edition\\)|\\(2-In-1 Edition\\)|3-In-1 V\\d+|Vols\\.|Vols|3-In-1")] private static partial Regex OmnibusRegex();
        [GeneratedRegex("3-In-1 V(\\d+)|\\d+-(\\d+)|\\d+, \\d+ \\& (\\d+)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMatchRegex();
        [GeneratedRegex("Compendium|Anniversary Book|Art of |\\(Osi\\)|Character Book|Guide", RegexOptions.IgnoreCase)] private static partial Regex TitleRemovalRegex();
        
        private static string FilterBookTitle(string bookTitle, Book book){
            char[] trimedChars = {' ', '\'', '!', '-'};
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), $"%{Convert.ToByte(var).ToString("x2")}");
            }
            return book == Book.LightNovel ? $"{bookTitle}+novel" : bookTitle;
        }

        public static string GetUrl()
        {
            return BooksAMillionLinks[0];
        }

        public static void ClearData()
        {
            BooksAMillionLinks.Clear();
            BooksAMillionData.Clear();
        }

        private static string GetUrl(byte currPageNum, string bookTitle, bool boxsetCheck, Book book){
            string url;
            if (!boxsetCheck)
            {
                // https://booksamillion.com/search?query=naruto;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1%7Clanguage%3AENG"
                url = $"https://booksamillion.com/search?query={FilterBookTitle(bookTitle, book)};filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page={currPageNum}%7Clanguage%3AENG";
            }
            else
            {
                // https://booksamillion.com/search?query=naruto%20box%20set&filter=product_type%3Abooks
                url = $"https://booksamillion.com/search?query={FilterBookTitle(bookTitle, book)}%20box%20set&filter=product_type%3Abooks&page=1%7Clanguage%3AENG&sort=date";
            }
            Logger.Debug(url);
            BooksAMillionLinks.Add(url);
            return url;
        }

        public static string TitleParse(string textTitle, Book book, string inputTitle)
        {
            string boxSetNum = string.Empty;
            if (textTitle.Contains("Box Set"))
            {
                boxSetNum = VolNumberRegex().Match(textTitle).Groups[1].Value;
            }
            GroupCollection omnibusMatch = OmnibusMatchRegex().Match(textTitle).Groups;
            string firstOmniNum = omnibusMatch[1].Value.TrimStart('0');
            string secondOmniNum = omnibusMatch[2].Value;
            string thirdOmniNum = omnibusMatch[3].Value;
            StringBuilder curTitle = new StringBuilder(FilterTitleRegex().Replace(OmnibusRegex().Replace(textTitle, "Omnibus"), ""));

            if (curTitle.ToString().Contains("Omnibus"))
            {
                if (!string.IsNullOrWhiteSpace(firstOmniNum))
                {
                    curTitle.Append(" Vol ").Append(firstOmniNum);
                }
                else if (!textTitle.Contains("Box Set") && !string.IsNullOrWhiteSpace(secondOmniNum))
                {
                    curTitle.Append(" Vol ").Append(Convert.ToUInt16(secondOmniNum) / 3);
                }
                else if (!string.IsNullOrWhiteSpace(thirdOmniNum) && !curTitle.ToString().Contains("Vol"))
                {
                    curTitle.Append(" Vol ").Append(Convert.ToUInt16(thirdOmniNum) / 3);
                }
            }
            else if (textTitle.Contains("Box Set"))
            {
                curTitle.Append(' ').Append(!string.IsNullOrWhiteSpace(boxSetNum) ? boxSetNum : '1');
            }
            else if (!textTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !textTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && textTitle.Any(char.IsDigit))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(textTitle).Index, "Vol ");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
        }

        public static List<EntryModel> GetBooksAMillionData(string bookTitle, Book book, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(true);
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));

                string curTitle;
                decimal priceVal;
                HtmlDocument doc;
                bool boxsetCheck = false, boxsetValidation = false;
                HtmlNodeCollection titleData, priceData, stockStatusData;
                while(true)
                {
                    driver.Navigate().GoToUrl(GetUrl(currPageNum, bookTitle, boxsetCheck, book));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='search-item-title']//a")));

                    // Initialize the html doc for crawling
                    doc = new HtmlDocument();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    titleData = doc.DocumentNode.SelectNodes("//div[@class='search-item-title']//a");
                    priceData = doc.DocumentNode.SelectNodes("//span[@class='our-price']");
                    stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='availability_search_results']");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//ul[@class='search-page-list']//a[@title='Next']");

                    for(int x = 0; x < titleData.Count; x++)
                    {
                        curTitle = titleData[x].InnerText;
                        if (curTitle.Contains("Box Set") && !boxsetCheck)
                        {
                            boxsetValidation = true;
                            Logger.Debug("Found Boxset");
                            continue;
                        }
                               
                        if (
                            !TitleRemovalRegex().IsMatch(curTitle) && 
                            MasterScrape.TitleContainsBookTitle(bookTitle, curTitle.ToString()) && 
                            !(
                                book == Book.Manga 
                                && (
                                        curTitle.Contains("(Light Novel") 
                                        || !curTitle.Contains("Vol")
                                        // || !curTitle.Any(char.IsDigit)
                                    )
                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Boruto") 
                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Itachi's Story") 
                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", curTitle.ToString(), "of Gluttony")
                            )
                        )
                        {
                            priceVal = Convert.ToDecimal(priceData[x].InnerText.Trim()[1..]);
                            BooksAMillionData.Add(
                                new EntryModel
                                (
                                    TitleParse(ParseTitleVolRegex().Replace(System.Net.WebUtility.HtmlDecode(curTitle), "Vol"), book, bookTitle),
                                    $"${(memberStatus ? EntryModel.ApplyDiscount(priceVal, MEMBERSHIP_DISCOUNT) : priceVal.ToString())}",
                                    stockStatusData[x].InnerText switch
                                    {
                                        string curStatus when curStatus.Contains("In Stock") => "IS",
                                        string curStatus when curStatus.Contains("Preorder") => "PO",
                                        _ => "OOS",
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                        //wait.Until(driver => driver.FindElement(By.XPath("//ul[@class='search-page-list']//a[@title='Next']"))).Click();
                    }
                    else
                    {
                        if (boxsetValidation && !boxsetCheck)
                        {
                            boxsetCheck = true;
                        }
                        else
                        {
                            driver.Close();
                            driver.Quit();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ BooksAMillion\n{ex}");
            }

            BooksAMillionData.Sort(new VolumeSort());

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\BooksAMillionData.txt"))
                {
                    if (BooksAMillionData.Count != 0)
                    {
                        foreach (EntryModel data in BooksAMillionData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Debug(bookTitle + " Does Not Exist at BooksAMillion");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at BooksAMillion");
                    }
                }
            }

            return BooksAMillionData;
        }
    }
}