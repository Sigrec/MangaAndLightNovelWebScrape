using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.IdentityModel.Tokens;
namespace MangaLightNovelWebScrape.Websites
{
    public partial class Indigo
    {
        public static List<string> IndigoLinks = new();
        public static List<EntryModel> IndigoData = new();
        public const string WEBSITE_TITLE = "Indigo";
        private const decimal PLUM_DISCOUNT = 0.1M;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("IndigoLogs");
        [GeneratedRegex(",|\\.")] private static partial Regex TitleRegex();

        public static void ClearData()
        {
            IndigoLinks.Clear();
            IndigoData.Clear();
        }

        // https://www.indigo.ca/en-ca/search?q=world+trigger&search-button=&lang=en_CA
        // https://www.indigo.ca/en-ca/search?q=jujutsu+kaisen&search-button=&lang=en_CA
        private static string GetUrl(string bookTitle, char bookType)
        {
            string url = $"https://www.indigo.ca/en-ca/search?q={bookTitle.Replace(' ', '+')}&search-button=&lang=en_CA";
            Logger.Debug(url);
            IndigoLinks.Add(url);
            return url;
        }

        private static bool RunClickEvent(WebDriverWait wait, ReadOnlyCollection<IWebElement> elements, string type)
        {
            if (elements.Count == 1)
            {
                Logger.Debug(type);
                wait.Until(driver => elements[0]).Click();
                return true;
            }
            return false;
        }

        private static void RunClickEvent(WebDriverWait wait, IWebElement element, string type)
        {
            Logger.Debug(type);
            wait.Until(driver => element).Click();
        }

        public static List<EntryModel> GetIndigoData(string bookTitle, char bookType, bool isMember)
        {
            Logger.Debug("Indigo Going");
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);

            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                driver.Navigate().GoToUrl(GetUrl(bookTitle, bookType));
                wait.Until(driver => driver.FindElement(By.XPath("/html[@style='position: relative;']")));

                // Check formats to filter entries for Paperback & Hardcover
                if(RunClickEvent(wait, driver.FindElements(By.XPath("//div[@id='refinement-heading']/span[@aria-label='Book Format']")), "Clicking Book Format Tab"))
                {
                    var formatBoxesElements = driver.FindElements(By.XPath("//*[@id='refinement-book-format']/ul/li/button/div/label/div[2]/span[1]"));
                    if (!formatBoxesElements.IsNullOrEmpty())
                    {
                        foreach (IWebElement format in formatBoxesElements)
                        {
                            switch (format.Text)
                            {
                                case "Paperback":
                                    RunClickEvent(wait, format, "Clicking Paperback Format");
                                    break;
                                case "Hardcover":
                                    RunClickEvent(wait, format, "Clicking Hardcover Format");
                                    break;
                            }
                        }
                    }
                }

                // Ensure language is only English
                if(RunClickEvent(wait, driver.FindElements(By.XPath("//div[@id='refinement-heading']/span[@aria-label='Language']")), "Clicking Language Tab"))
                {
                    RunClickEvent(wait, wait.Until(driver => driver.FindElements(By.XPath("//*[@id='refinement-language']/ul/li/button/div/label/div[2]/span[1][contains(text(), 'English')]"))), "Clicking English Language");
                }

                // Sort entries based on Newest Arrivals
                RunClickEvent(wait, driver.FindElements(By.XPath("//*[@id='product-search-results']/div[2]/div[1]/div/div[2]/div[2]/select/option[4]")), "Sort By Newest Arrivals");

                // Load all entries before getting the html page source
                int index = 1;
                var loadMoreElements = driver.FindElements(By.XPath("//button[@class='btn btn-tertiary more']"));
                while (!loadMoreElements.IsNullOrEmpty())
                {
                    RunClickEvent(wait, loadMoreElements, $"Clicking Load More {index}");
                    loadMoreElements = wait.Until(driver => driver.FindElements(By.XPath("//button[@class='btn btn-tertiary more']")));
                    Logger.Debug($"Check{index}");
                    index++;
                    //wait.Until(driver => driver.FindElement(By.XPath("/html[@style='position: relative;']")));
                }

                HtmlDocument doc = new();
                doc.LoadHtml(driver.PageSource);
                Logger.Debug("Finished");
                driver.Close();
                driver.Quit();

                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//a[@class='link secondary']/h3");
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@class='price-wrapper']");
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='mb-0 product-tile-promotion mouse']");

                for(int x = 0; x < titleData.Count; x++)
                {
                    IndigoData.Add(
                        new EntryModel(
                            TitleRegex().Replace(titleData[x].InnerText, ""),
                            isMember ? EntryModel.ApplyDiscount(Convert.ToDecimal(priceData[x].InnerText.Trim()), PLUM_DISCOUNT) : priceData[x].InnerText.Trim(),
                            !stockStatusData[x].InnerText.Contains("Pre-Order") ? "IS" : "PO",
                            WEBSITE_TITLE
                        )
                    );
                }

            }
            catch (Exception ex)
            {
                driver.Close();
                driver.Quit();
                Logger.Error($"{bookTitle} Does Not Exist @ Indigo {ex}");
            }

            IndigoData.Sort(new VolumeSort(bookTitle));

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\IndigoData.txt"))
                {
                    if (IndigoData.Count != 0)
                    {
                        foreach (EntryModel data in IndigoData)
                        {
                            Logger.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        Logger.Debug(bookTitle + " Does Not Exist at Indigo");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at Indigo");
                    }
                } 
            }

            return IndigoData;
        }
    }
}