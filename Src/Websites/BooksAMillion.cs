using System.Text.RegularExpressions;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class BooksAMillion
    {
        private static List<string> BooksAMillionLinks = new();
        private static List<EntryModel> BooksAMillionData = new();
        public const string WEBSITE_TITLE = "Books-A-Million";
        private static readonly Logger Logger = LogManager.GetLogger("BooksAMillionLogs");
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        [GeneratedRegex(@"Vol\.|Volume")] private static partial Regex ParseTitleVolRegex();
        [GeneratedRegex(@"(?<=Box Set).*|:|\!|,|Includes.*|--The Manga|\d+-\d+|\(Manga\) |\d+, \d+ \& \d+")] private static partial Regex MangaFilterTitleRegex();
        [GeneratedRegex(@":|\!|,|Includes.*|\d+-\d+|\d+, \d+ \& \d+")] private static partial Regex NovelFilterTitleRegex();
        [GeneratedRegex(@"(?<=Vol \d+)[^\d\.].*")] private static partial Regex CleanFilterTitleRegex();
        [GeneratedRegex("Box Set (\\d+)")] private static partial Regex VolNumberRegex();
        [GeneratedRegex("\\(Omnibus Edition\\)|\\(3-In-1 Edition\\)|\\(2-In-1 Edition\\)|3-In-1 V\\d+|Vols\\.|Vols|3-In-1")] private static partial Regex OmnibusRegex();
        [GeneratedRegex("3-In-1 V(\\d+)|\\d+-(\\d+)|\\d+, \\d+ \\& (\\d+)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusMatchRegex();
        [GeneratedRegex("Compendium|Anniversary Book|Art of |\\(Osi\\)|Character|Guide|Illustration|Anime|Advertising", RegexOptions.IgnoreCase)] private static partial Regex TitleRemovalRegex();

        public static string GetUrl()
        {
            return BooksAMillionLinks[0];
        }

        public static void ClearData()
        {
            BooksAMillionLinks.Clear();
            BooksAMillionData.Clear();
        }

        private static string GetUrl(string bookTitle, bool boxsetCheck, Book book){
            StringBuilder url = new StringBuilder();
            if (book == Book.LightNovel)
            {
                // https://www.booksamillion.com/search?filter=product_type%3Abooks&query=naruto+novel&sort=date&page=1
                url.AppendFormat("https://www.booksamillion.com/search?filter=product_type%3Abooks&query={0}{1}&sort=date", MasterScrape.FilterBookTitle(bookTitle), boxsetCheck ? "+novel+box+set" : "+novel");
                //
            }
            else if (!boxsetCheck)
            {
                // https://booksamillion.com/search?query=naruto;filter=product_type%3Abooks%7Cbook_categories%3ACGN;sort=date;page=1%7Clanguage%3AENG"
                url.AppendFormat("https://booksamillion.com/search?query={0}&filter=product_type%3Abooks%7Cbook_categories%3ACGN&sort=date", MasterScrape.FilterBookTitle(bookTitle));
            }
            else
            {
                // https://booksamillion.com/search?filter=product_type%3Abooks&query=bleach+box+set
                url.AppendFormat("https://booksamillion.com/search?filter=product_type%3Abooks&query={0}{1}", MasterScrape.FilterBookTitle(bookTitle), boxsetCheck ? "+box+set" : "");
            }

            Logger.Debug(url);
            BooksAMillionLinks.Add(url.ToString());
            return url.ToString();
        }

        public static string TitleParse(string textTitle, Book book, string inputTitle)
        {
            string boxSetNum = string.Empty;
            StringBuilder curTitle;
            if (book == Book.LightNovel)
            {
                textTitle = CleanFilterTitleRegex().Replace(NovelFilterTitleRegex().Replace(textTitle, ""), "");
                curTitle = new StringBuilder(textTitle).Replace("(Novel)", "Novel").Replace("(Light Novel)", "Novel");
                if (!textTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase))
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
                if (textTitle.Contains("Box Set"))
                {
                    boxSetNum = VolNumberRegex().Match(textTitle).Groups[1].Value;
                }
                GroupCollection omnibusMatch = OmnibusMatchRegex().Match(textTitle).Groups;
                string firstOmniNum = omnibusMatch[1].Value.TrimStart('0');
                string secondOmniNum = omnibusMatch[2].Value;
                string thirdOmniNum = omnibusMatch[3].Value;
                curTitle = new StringBuilder(CleanFilterTitleRegex().Replace(MangaFilterTitleRegex().Replace(OmnibusRegex().Replace(textTitle, "Omnibus"), ""), ""));

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
                else if (!textTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !textTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && textTitle.AsParallel().Any(char.IsDigit))
                {
                    curTitle.Insert(MasterScrape.FindVolNumRegex().Match(textTitle).Index, "Vol ");
                }
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
        }

        private static bool RunClickEvent(string xPath, WebDriver driver, WebDriverWait wait, string type)
        {
            var elements = driver.FindElements(By.XPath(xPath));
            if (!elements.IsNullOrEmpty())
            {
                Logger.Debug(type);
                wait.Until(driver => driver.FindElement(By.XPath(xPath))).Click();
                return true;
            }
            Logger.Debug($"{type} Failed");
            return false;
        }

        public static List<EntryModel> GetBooksAMillionData(string bookTitle, Book book, bool memberStatus)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(true);
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));

                string curTitle;
                decimal priceVal;
                HtmlDocument doc = new HtmlDocument();
                bool boxsetCheck = false, boxsetValidation = false;
                HtmlNodeCollection titleData, priceData, stockStatusData, bookQuality;
                HtmlNode pageCheck;
                driver.Navigate().GoToUrl(GetUrl(bookTitle, boxsetCheck, book));
                
                while(true)
                {
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='search-item-title']/a")));

                    // Initialize the html doc for crawling
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    titleData = doc.DocumentNode.SelectNodes("//div[@class='search-item-title']/a");
                    bookQuality = doc.DocumentNode.SelectNodes("//div[@class='productInfoText']");
                    priceData = doc.DocumentNode.SelectNodes("//span[@class='our-price']");
                    stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='availability_search_results']");
                    pageCheck = doc.DocumentNode.SelectSingleNode("//ul[@class='search-page-list']//a[@title='Next']");

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
                            !TitleRemovalRegex().IsMatch(curTitle)
                            && MasterScrape.TitleContainsBookTitle(bookTitle, curTitle.ToString())
                            && !bookQuality[x].InnerText.Contains("Library Binding")
                            && (
                                (
                                    titleData.Count == 1
                                    && !boxsetCheck
                                )
                                || !(
                                    book == Book.Manga 
                                    && (
                                            curTitle.Contains("(Light Novel") 
                                            || (
                                                !curTitle.Contains("Vol")
                                                && !(
                                                        curTitle.AsParallel().Any(char.IsDigit)
                                                        && !bookTitle.AsParallel().Any(char.IsDigit)
                                                    )
                                                )
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Boruto") 
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", curTitle.ToString(), "Itachi's Story") 
                                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", curTitle.ToString(), "of Gluttony")
                                        )
                                )
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
                        RunClickEvent("//ul[@class='search-page-list']//a[@title='Next']", driver, wait, "Clicking Next Page");
                    }
                    else
                    {
                        if (boxsetValidation && !boxsetCheck)
                        {
                            boxsetCheck = true;
                            driver.Navigate().GoToUrl(GetUrl(bookTitle, boxsetCheck, book));
                        }
                        else
                        {
                            driver.Close();
                            driver.Quit();
                            break;
                        }
                    }
                }
                BooksAMillionData.Sort(new VolumeSort());
            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ BooksAMillion\n{ex}");
            }

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