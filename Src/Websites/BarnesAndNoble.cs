using System.Collections.ObjectModel;
using Microsoft.IdentityModel.Tokens;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class BarnesAndNoble
    {
        public static List<string> BarnesAndNobleLinks = new();
        public static List<EntryModel> BarnesAndNobleData = new();
        public const string WEBSITE_TITLE = "Barnes & Noble";
        private static bool novelCheck = false;
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("BarnesAndNobleLogs");

        [GeneratedRegex("(?<=\\d{1,3})[^\\d{1,3}.]+.*|\\,|:| \\([^()]*\\)")] private static partial Regex ParseBoxSetTitleRegex();
        [GeneratedRegex("Vol\\.|Volume")] private static partial Regex VolTitleFixRegex(); 
        [GeneratedRegex("(?<=Vol \\d{1,3})[^\\d{1,3}.]+.*|\\,|:| \\([^()]*\\)")]  private static partial Regex ParseTitleRegex();

        // https://www.barnesandnoble.com/s/Classroom+of+the+Elite/_/N-1z141tjZ8q8Z1gvk
        //light novel
        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = "Error";
            if (bookType == 'M')
            {
                // https://www.barnesandnoble.com/s/jujutsu+kaisen/_/N-1z141tjZucb/?Nrpp=40&page=1
                // https://www.barnesandnoble.com/s/classroom+of+the+elite/_/N-1z141tjZucb/?Nrpp=40&page=1
                // https://www.barnesandnoble.com/s/world+trigger/_/N-1z141tjZ8q8Zucb/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(" ", "%20")}/_/N-1z141tjZucb/?Nrpp=40&page={currPageNum}";
            }
            else if (bookType == 'N')
            {
                // https://www.barnesandnoble.com/s/classroom+of+the+elite+novel/_/N-1z141tjZucb/?Nrpp=40&page=1
                // https://www.barnesandnoble.com/s/overlord+novel?Nrpp=40&page=1
                //url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(" ", "%20")}+novel/_/N-1z141tjZucb/?Nrpp=40&page={currPageNum}";
                // https://www.barnesandnoble.com/s/overlord%20novel?Nrpp=40&page=1
                // https://www.barnesandnoble.com/s/overlord+novel/_/N-1z141wb/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{bookTitle.Replace(" ", "+")}+novel/_/N-1z141wb/?Nrpp=40&page={currPageNum}";
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

        public static string TitleParse(string currTitle, char bookType, string inputTitle)
        {
            currTitle = VolTitleFixRegex().Replace(currTitle, "Vol").Replace("(Omnibus Edition)", "Omnibus");
            string parsedTitle = currTitle.Contains("Box Set") ? ParseBoxSetTitleRegex().Replace(currTitle, "") : ParseTitleRegex().Replace(currTitle, "");

            if (bookType == 'M' && !parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !parsedTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(MasterScrape.FindVolNumRegex().Match(parsedTitle).Index, "Vol ");
            }

            return parsedTitle.Trim();
        }

        public static List<EntryModel> GetBarnesAndNobleData(string bookTitle, char bookType, bool memberStatus, byte currPageNum)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(true);
            WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));

            restart:
            try
            {
                string curTitle = "";
                while(true)
                {
                    driver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle));
                    wait.Until(e => e.FindElement(By.XPath("//div[@class='product-shelf-title product-info-title pt-xs']/a")));
                    
                    // if (bookType == 'N' && !novelCheck)
                    // {
                    //     ReadOnlyCollection<IWebElement> novelCheckElements = driver.FindElements(By.XPath("//div[@class='product-shelf-title product-info-title pt-xs']//a[contains(@title, 'Novel')]"));
                    //     if (novelCheckElements.IsNullOrEmpty() && !novelCheck)
                    //     {
                    //         Logger.Debug("Trying 2nd URL");
                    //         novelCheck = true;
                    //         goto restart; 
                    //     }
                    // }

                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-shelf-title product-info-title pt-xs')]/a");
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//div[@class='product-shelf-pricing mt-xs']//div//a//span[2]");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//p[@class='ml-xxs bopis-badge-message mt-0 mb-0' and (contains(text(), 'Online') or contains(text(), 'Pre-order'))]");
                    HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//li[@class='pagination__next ']");

                    decimal price;
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        curTitle = titleData[x].GetAttributeValue("title", "Title Error");
                        if ((bookType == 'M' && (curTitle.Contains("Vol") || curTitle.Contains("Box Set"))) || bookType == 'N')
                        {
                            curTitle = TitleParse(curTitle, bookType, bookTitle);
                            Logger.Debug(curTitle);
                            if (!curTitle.Contains("Artbook") && MasterScrape.RemoveNonWordsRegex().Replace(curTitle.ToLower(), "").Contains(MasterScrape.RemoveNonWordsRegex().Replace(bookTitle.ToLower(), "")))
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
                    }

                    if (pageCheck != null)
                    {
                        currPageNum++;
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!novelCheck)
                {
                    Logger.Debug($"Trying 2nd URL #2 ? {ex}");
                    novelCheck = true;
                    goto restart; 
                }
                driver.Close();
                driver.Quit();
            }

            BarnesAndNobleData.Sort(new VolumeSort());

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\BarnesAndNobleData.txt"))
                {
                    if (BarnesAndNobleData.Count != 0)
                    {
                        foreach (EntryModel data in BarnesAndNobleData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Warn(bookTitle + " Does Not Exist at BarnesAndNoble");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at BarnesAndNoble");
                    }
                }
            }  

            return BarnesAndNobleData;
        }
    }
}