using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class KinokuniyaUSA
    {
        private static List<string> KinokuniyaUSALinks = new();
        private static List<EntryModel> KinokuniyaUSAData = new();
        public const string WEBSITE_TITLE = "Kinokuniya USA";
        private static readonly Logger Logger = LogManager.GetLogger("KinokuniyaUSALogs");
        private static readonly int STATUS_START_INDEX = "Availability Status : ".Length;
        [GeneratedRegex("Official|Character Book|Guide|Art of |Illustration|Chapter Book", RegexOptions.IgnoreCase)] private static partial Regex TitleRemovalRegex();
        [GeneratedRegex("\\(Omnibus Edition\\)|\\(3-In-1 Edition\\)|\\(2-In-1 Edition\\)", RegexOptions.IgnoreCase)] private static partial Regex OmnibusRegex();
        [GeneratedRegex(@"\(Light Novel\)|Novel", RegexOptions.IgnoreCase)] private static partial Regex NovelRegex();
        [GeneratedRegex("Volume|Vol\\.", RegexOptions.IgnoreCase)] private static partial Regex VolumeRegex();
        [GeneratedRegex(@"(Vol \d{1,3}\.\d{1})?(?(?<=Vol \d{1,3}\.\d{1})[^\d].*|(?<=Vol \d{1,3})[^\d].*)|(?<=Box Set \d{1,3}).*|\(+.*?\)+|(?<=\d{1,3} :).*|,|:|<.*?>", RegexOptions.IgnoreCase)] private static partial Regex TitleFixRegex();

        // Manga English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=world+trigger&taxon=2&x=39&y=4&page=1&per_page=100&form_taxon=109
        // https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=Skeleton+Knight+in+Another+World&taxon=2&x=39&y=11&page=1&per_page=100

        // Light Novel English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=overlord+novel&taxon=&x=33&y=8&per_page=100&form_taxon=109
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite&taxon=&x=33&y=8&per_page=100&form_taxon=109
        private static string GetUrl(Book book, string titleText){
            string url = $"https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={titleText.Replace(" ", "+")}{(book == Book.LightNovel ? "+novel" : "")}&taxon=2&x=39&y=11&page=1&per_page=100";
            Logger.Debug(url);
            KinokuniyaUSALinks.Add(url);
            return url;
        }

        public static string GetUrl()
        {
            return KinokuniyaUSALinks[0];
        }
        
        public static void ClearData()
        {
            KinokuniyaUSALinks.Clear();
            KinokuniyaUSAData.Clear();
        }

        public static string TitleParse(string titleText, Book book, string bookTitle, string entryDesc, bool oneShotCheck)
        {
            if (!oneShotCheck)
            {
                if (book == Book.LightNovel)
                {
                    titleText = NovelRegex().Replace(titleText, "Novel");
                }
                titleText = TitleFixRegex().Replace(OmnibusRegex().Replace(VolumeRegex().Replace(titleText, "Vol"), "Omnibus"), "$1");
                StringBuilder curTitle = new StringBuilder(titleText);

                if (book == Book.LightNovel && !titleText.Contains("Novel"))
                {
                    int index = titleText.IndexOf("Vol");
                    if (index != -1)
                    {
                        curTitle.Insert(index, "Novel ");
                    }
                    else
                    {
                        curTitle.Insert(curTitle.Length - 1, " Novel");
                    }
                }
                else if (book == Book.Manga && !titleText.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
                {
                    if (titleText.AsParallel().Any(char.IsDigit) && !bookTitle.AsParallel().Any(char.IsDigit))
                    {
                        curTitle.Insert(MasterScrape.FindVolNumRegex().Match(titleText).Index, "Vol ");
                    }
                    else if (entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase) && entryDesc.Contains("volumes", StringComparison.OrdinalIgnoreCase))
                    {
                        curTitle.Insert(curTitle.Length, " Box Set");
                    }
                }
                return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(TitleFixRegex().Replace(VolumeRegex().Replace(titleText, "Vol"), "$1").Trim(), " ");
        }

        private static bool RunClickEvent(string xPath, WebDriver driver, WebDriverWait wait, string type)
        {
            var elements = driver.FindElements(By.XPath(xPath));
            if (!elements.IsNullOrEmpty())
            {
                Logger.Debug(type);
                wait.Until(driver => driver.FindElement(By.XPath(xPath))).Click();
                wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                return true;
            }
            Logger.Debug($"{type} Failed");
            return false;
        }
        
        public static List<EntryModel> GetKinokuniyaUSAData(string bookTitle, Book book, bool memberStatus)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(true);
            int maxPageCount = -1, curPageNum = 1;
            bool oneShotCheck = false;
            string titleText, entryDesc;
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));
                driver.Navigate().GoToUrl(GetUrl(book, bookTitle));
                wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                if (book == Book.Manga)
                {
                    driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//p[contains(text(), 'English Books')]/following-sibling::ul//a[contains(text(), 'Manga')]"))));
                    wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                    Logger.Debug("Clicked Manga Button");
                }

                // Click the list display mode so it shows stock status data with entry
                wait.Until(driver => driver.FindElement(By.XPath("//ul[@class='sortMenu']//li//a[contains(text(), 'List')]"))).Click();
                wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                Logger.Debug("Clicked List Mode");

                while(true)
                {
                    // Initialize the html doc for crawling
                    HtmlDocument doc = new();
                    doc.LoadHtml(driver.PageSource);

                    // Get the page data from the HTML doc
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//span[@class='underline']");
                    oneShotCheck = !titleData.AsParallel().Any(title => title.InnerText.Contains("Vol", StringComparison.OrdinalIgnoreCase)); // Determine if the series is a one shot or not
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(memberStatus ? "//li[@class='price'][2]/span" : "//li[@class='price'][1]/span");
                    HtmlNodeCollection descData = doc.DocumentNode.SelectNodes("//p[@class='description']");
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//li[@class='status']");
                    if (maxPageCount == -1)
                    {
                        maxPageCount = Convert.ToInt32(doc.DocumentNode.SelectSingleNode("//div[@class='categoryPager']/ul/li[last()]/a").InnerText);
                    }

                    // Remove all of the novels from the list if user is searching for manga
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = System.Net.WebUtility.HtmlDecode(titleData[x].InnerText);
                        entryDesc = descData[x].InnerText;
                        if (
                            !TitleRemovalRegex().IsMatch(titleText)
                            && MasterScrape.TitleContainsBookTitle(bookTitle, titleText) 
                            && (
                                    (
                                        book == Book.Manga
                                        && (
                                                titleText.Contains("graphic novel", StringComparison.OrdinalIgnoreCase) 
                                                || !titleText.Contains("light novel", StringComparison.OrdinalIgnoreCase)
                                                || !titleText.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                            )
                                        && ( // If it's not a one shot series check to see if it contains 'Vol' or has a volume number string if not skip
                                            oneShotCheck
                                            || (
                                                    !oneShotCheck
                                                    && (
                                                            VolumeRegex().IsMatch(titleText)
                                                            || (
                                                                    entryDesc.Contains("Collection", StringComparison.OrdinalIgnoreCase) 
                                                                    && entryDesc.Contains("volumes", StringComparison.OrdinalIgnoreCase)
                                                                )
                                                            || (
                                                                    titleText.AsParallel().Any(char.IsDigit)
                                                                    && !bookTitle.AsParallel().Any(char.IsDigit)
                                                                )
                                                        )
                                                )
                                            )
                                        && !(
                                                MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", titleText, "of Gluttony") 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Bleach", titleText, "Can't Fear Your Own World") 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Itachi's Story") 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Naruto", titleText, "Boruto")
                                            )
                                    )
                                    || 
                                    (
                                        book == Book.LightNovel
                                        && !titleText.Contains("graphic novel", StringComparison.OrdinalIgnoreCase) 
                                        && (
                                            titleText.Contains("light novel", StringComparison.OrdinalIgnoreCase) 
                                            || titleText.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                            )
                                    )
                                )
                            && !(
                                    !bookTitle.Contains("Color", StringComparison.OrdinalIgnoreCase) 
                                    && titleText.Contains("Color", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                        {
                            KinokuniyaUSAData.Add(
                                new EntryModel(
                                    TitleParse(titleText, book, bookTitle, entryDesc, oneShotCheck), 
                                    priceData[x].InnerText.Trim(), 
                                    stockStatusData[x].InnerText.Trim().AsSpan(STATUS_START_INDEX) switch
                                    {
                                        "In stock at the Fulfilment Center." or "Available for order from suppliers." => "IS",
                                        "Available for Pre Order" => "PO",
                                        "Out of stock." => "OOS",
                                        _ => "Error"
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    if (curPageNum != maxPageCount)
                    {
                        curPageNum++;
                        RunClickEvent("//p[@class='pagerArrowR']", driver, wait, "Clicking Next Page");
                    }
                    else
                    {
                        driver.Close();
                        driver.Quit();
                        KinokuniyaUSAData.Sort(new VolumeSort());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ Kinokuniya USA \n{e}");
            }

            //Print data to a txt file
            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\KinokuniyaUSAData.txt"))
                {
                    if (KinokuniyaUSAData.Count != 0)
                    {
                        foreach (EntryModel data in KinokuniyaUSAData)
                        {
                            outputFile.WriteLine(data);
                            Logger.Debug(data);
                        }
                    }
                    else
                    {
                        Logger.Error($"{bookTitle} Does Not Exist at Kinokuniya USA");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at Kinokuniya USA");
                    }
                } 
            }
            return KinokuniyaUSAData;
        }
    }
}