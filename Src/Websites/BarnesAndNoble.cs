using System.Collections.ObjectModel;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class BarnesAndNoble
    {
        public static List<string> BarnesAndNobleLinks = new();
        public static List<EntryModel> BarnesAndNobleData = new();
        public const string WEBSITE_TITLE = "Barnes & Noble";
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        private static readonly Logger Logger = LogManager.GetLogger("BarnesAndNobleLogs");

        [GeneratedRegex("(?<=\\d{1,3})[^\\d{1,3}.]+.*|\\,|:| \\([^()]*\\)| Complete")] private static partial Regex ParseBoxSetTitleRegex();
        [GeneratedRegex("Vol\\.|Volume")] private static partial Regex VolTitleFixRegex(); 
        [GeneratedRegex("(?<=Vol \\d{1,3})[^\\d{1,3}.]+.*|\\,|:| \\([^()]*\\)")]  private static partial Regex ParseTitleRegex();
        [GeneratedRegex("\\(Omnibus Edition\\)|\\(3-in-1 Edition\\)|\\(2-in-1 Edition\\)")]  private static partial Regex OmnibusTitleRegex();

        private static string GetUrl(Book book, byte currPageNum, string bookTitle, bool check){
            string url = "Error";
            if (book == Book.Manga)
            {
                if (!check)
                {
                    // https://www.barnesandnoble.com/s/overlord/_/N-1z141tjZucb/?Nrpp=40&Ns=P_Publication_Date%7C0&page=1
                    // https://www.barnesandnoble.com/s/overlord/_/N-8q8Zucc/?Nrpp=40&page=1
                    // https://www.barnesandnoble.com/s/overlord/_/N-8q8Zucb/?Nrpp=40&page=1
                    // https://www.barnesandnoble.com/s/world+trigger/_/N-8q8Zucb/?Nrpp=40&page=1
                    url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(' ', '+')}/_/N-8q8Zucb/?Nrpp=40&page={currPageNum}";
                }
                else
                {
                    url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(' ', '+')}+manga/_/N-8q8Zucb/?Nrpp=40&page={currPageNum}";
                }
            }
            else if (book == Book.LightNovel)
            {
                // https://www.barnesandnoble.com/s/overlord+novel/_/N-1z141wbZ8q8/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(' ', '+')}+novel/_/N-1z141wbZ8q8/?Nrpp=40&page={currPageNum}";
            }
            Logger.Debug(url);
            BarnesAndNobleLinks.Add(url);
            return url;
        }

        public static void ClearData()
        {
            BarnesAndNobleLinks.Clear();
            BarnesAndNobleData.Clear();
        }

        private static string TitleParse(string titleText, Book book, string inputTitle)
        {
            titleText = VolTitleFixRegex().Replace(titleText, "Vol");
            if (titleText.Contains("Box Set"))
            {
                titleText = ParseBoxSetTitleRegex().Replace(titleText, "");
            }
            else
            {
                if (book == Book.LightNovel)
                {
                    titleText = titleText.Replace("(Light Novel)", "Novel");
                }
                else if (titleText.Contains("Edition"))
                {
                    titleText = OmnibusTitleRegex().Replace(titleText, "Omnibus");
                }
                titleText = ParseTitleRegex().Replace(titleText, "");
            }

            StringBuilder curTitle = new StringBuilder(titleText);
            if (titleText.Contains("Toilet-bound Hanako-kun First Stall"))
            {
                curTitle.Append(" Box Set");
            }
            titleText = curTitle.ToString();

            if (book == Book.Manga && !titleText.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(titleText).Index, "Vol ");
            }
            else if (book == Book.LightNovel && !titleText.Contains("Novel"))
            {
                if (titleText.IndexOf("Vol") != -1)
                {
                    curTitle.Insert(titleText.IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(titleText.Length, " Novel");
                }
            }

            return curTitle.ToString().Trim();
        }

        private static bool RunClickEvent(string xPath, WebDriver driver, WebDriverWait wait, string type)
        {
            var elements = driver.FindElements(By.XPath(xPath));
            if (!elements.IsNullOrEmpty())
            {
                Logger.Debug(type);
                wait.Until(driver => driver.FindElement(By.XPath("(//span[@class='augHeader']/h2[contains(text(), 'Format')]/ancestor::span)[1]"))).Click();
                IWebElement element = driver.FindElement(By.XPath(xPath));
                wait.Until(driver => element.Displayed);
                wait.Until(driver => element).Click();
                return true;
            }
            Logger.Debug($"{type} Failed");
            return false;
        }

        public static List<EntryModel> GetBarnesAndNobleData(string bookTitle, Book book, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(true);
            WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
            try
            {
                string curTitle = string.Empty;
                string pageSource = string.Empty;
                string paperbackUrl = string.Empty, hardcoverUrl = string.Empty, curUrl = string.Empty;
                bool paperbackCheck = false, hardcoverCheck = false, secondCheck = false;
                HtmlDocument doc = new();

                CheckOther:
                string originalUrl = GetUrl(book, currPageNum, bookTitle, secondCheck);
                driver.Navigate().GoToUrl(originalUrl);
                wait.Until(e => e.FindElement(By.XPath("//div[@class='product-view-section pl-lg-l p-sm-0']")));
                if (!paperbackCheck && RunClickEvent("(//span[@class='augHeader']/h2[contains(text(), 'Format')]/ancestor::span/following-sibling::ul//a[contains(text(), 'Paperback')])[1]", driver, wait, "Clicking Paperback"))
                {
                    curUrl = driver.Url;
                    paperbackUrl = $"{(curUrl.IndexOf(';') != -1 ? curUrl[..curUrl.IndexOf(';')] : curUrl)}/?Nrpp=40&page=1";
                }

                driver.Navigate().GoToUrl(originalUrl);
                wait.Until(e => e.FindElement(By.XPath("//div[@class='product-view-section pl-lg-l p-sm-0']")));
                if (!hardcoverCheck && RunClickEvent("(//span[@class='augHeader']/h2[contains(text(), 'Format')]/ancestor::span/following-sibling::ul//a[contains(text(), 'Hardcover')])[1]", driver, wait, "Clicking Hardcover"))
                {
                    curUrl = driver.Url;
                    hardcoverUrl = $"{(curUrl.IndexOf(';') != -1 ? curUrl[..curUrl.IndexOf(';')] : curUrl)}/?Nrpp=40&page=1";
                }

                FormatCheck:
                if (!string.IsNullOrWhiteSpace(paperbackUrl))
                {
                    Logger.Debug($"Going To Paperback Url {paperbackUrl}");
                    driver.Navigate().GoToUrl(paperbackUrl);
                    paperbackCheck = true;
                }
                else if (!string.IsNullOrWhiteSpace(hardcoverUrl))
                {
                    Logger.Debug($"Going To Hardcover Url {hardcoverUrl}");
                    driver.Navigate().GoToUrl(hardcoverUrl);
                    hardcoverCheck = true;
                }

                while(true)
                {
                    HtmlNodeCollection titleData = null;
                    HtmlNodeCollection priceData = null;
                    HtmlNodeCollection stockStatusData = null;
                    HtmlNode pageCheck = null;
                    
                    pageSource = driver.PageSource;
                    if (!pageSource.Contains("The page you requested can't be found") && !pageSource.Contains("Sorry, we couldn't find what you're looking for"))
                    {
                        Logger.Debug("Valid page");
                        doc.LoadHtml(pageSource);
                        wait.Until(e => e.FindElement(By.XPath("//div[@class='product-view-section pl-lg-l p-sm-0']")));
                    }
                    else
                    {
                        Logger.Debug("Invalid Page");
                        goto Quit;
                    }

                    titleData = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-shelf-title product-info-title pt-xs')]/a");
                    priceData = doc.DocumentNode.SelectNodes("//div[@class='product-shelf-pricing mt-xs']//div//a//span[2]");
                    stockStatusData = doc.DocumentNode.SelectNodes("//p[@class='ml-xxs bopis-badge-message mt-0 mb-0' and (contains(text(), 'Online') or contains(text(), 'Pre-order'))]");
                    pageCheck = doc.DocumentNode.SelectSingleNode("//li[@class='pagination__next ']");

                    decimal price;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        curTitle = titleData[x].GetAttributeValue("title", "Title Error");
                        if (
                            !MasterScrape.TitleContainsBookTitle(bookTitle, curTitle) || 
                            curTitle.Contains(" Art of ") || 
                            curTitle.Contains("Artbook", StringComparison.OrdinalIgnoreCase) || 
                            curTitle.Contains("Character Book") || 
                            curTitle.Contains("Illustration") || 
                            (book == Book.Manga && 
                                (
                                    (!(curTitle.AsParallel().Any(char.IsDigit) && !bookTitle.AsParallel().Any(char.IsDigit)) && !curTitle.Contains("Vol") && !curTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && !curTitle.Contains("Toilet-bound Hanako-kun: First Stall")) ||
                                    curTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase) || 
                                    (
                                        bookTitle.Equals("Naruto", StringComparison.OrdinalIgnoreCase) && 
                                        (
                                            curTitle.Contains("Boruto") || curTitle.Contains("Itachi's Story")
                                        )
                                    ) || 
                                    MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", curTitle, "of Gluttony")
                                )
                            )
                            )
                        {
                            Logger.Debug($"Removed {curTitle}");
                            continue;
                        }

                        curTitle = TitleParse(curTitle, book, bookTitle);
                        if (!BarnesAndNobleData.Exists(entry => entry.Entry.Equals(curTitle)))
                        {
                            price = decimal.Parse(priceData[x].InnerText.Trim()[1..]);
                            BarnesAndNobleData.Add(
                                new EntryModel
                                (
                                    curTitle,
                                    $"${(memberStatus ? EntryModel.ApplyDiscount(price, MEMBERSHIP_DISCOUNT) : price.ToString())}",
                                    stockStatusData[x].InnerText.Contains("Pre-order") ? "PO" : "IS", WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    Quit:
                    if (pageCheck == null)
                    {
                        Logger.Debug("No More Pages");
                        if (paperbackCheck)
                        {
                            paperbackUrl = string.Empty;
                            paperbackCheck = false;
                        }
                        else if (hardcoverCheck)
                        {
                            hardcoverUrl = string.Empty;
                            hardcoverCheck = false;
                        }
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                        if (paperbackCheck)
                        {
                            Logger.Debug($"Next Paperback Page {paperbackUrl[..^1]}{currPageNum}");
                            driver.Navigate().GoToUrl($"{paperbackUrl[..^1]}{currPageNum}");
                        }
                        else if (hardcoverCheck)
                        {
                            Logger.Debug($"Next Hardcover Page {hardcoverUrl[..^1]}{currPageNum}");
                            driver.Navigate().GoToUrl($"{hardcoverUrl[..^1]}{currPageNum}");
                        }
                    }
                    else if ((string.IsNullOrWhiteSpace(paperbackUrl) && !string.IsNullOrWhiteSpace(hardcoverUrl)) || (!string.IsNullOrWhiteSpace(paperbackUrl) && string.IsNullOrWhiteSpace(hardcoverUrl)))
                    {
                        goto FormatCheck;
                    }
                    else if (!secondCheck && BarnesAndNobleData.IsNullOrEmpty())
                    {
                        Logger.Debug("Checking Dif Url for Manga");
                        secondCheck = true;
                        goto CheckOther;
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        break;
                    }
                }

                BarnesAndNobleData.Sort(new VolumeSort());
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ Barnes & Noble \n{e}");
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\BarnesAndNobleData.txt"))
                {
                    if (!BarnesAndNobleData.IsNullOrEmpty())
                    {
                        foreach (EntryModel data in BarnesAndNobleData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Error(bookTitle + " Does Not Exist at BarnesAndNoble");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at BarnesAndNoble");
                    }
                }
            }  

            return BarnesAndNobleData;
        }
    }
}