using System.Runtime.Intrinsics.X86;
using System;
using System.Web;

namespace MangaLightNovelWebScrape.Websites
{
    public class KinokuniyaUSA
    {
        public static List<string> KinokuniyaUSALinks = new List<string>(); //List of links used to get data from KinokuniyaUSA
        private static List<string[]> KinokuniyaUSADataList = new List<string[]>(); //List of all the series data from KinokuniyaUSA

        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("KinokuniyaUSALogs");
        // Manga English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=world+trigger&taxon=2&x=39&y=4&page=1&per_page=100&form_taxon=109
        // https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=Skeleton+Knight+in+Another+World&taxon=2&x=39&y=11&page=1&per_page=100

        // Light Novel English Search
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=overlord+novel&taxon=&x=33&y=8&per_page=100&form_taxon=109
        //https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords=classroom+of+the+elite&taxon=&x=33&y=8&per_page=100&form_taxon=109
        private static string GetUrl(char bookType, byte currPageNum, string bookTitle){
            string url = $"https://united-states.kinokuniya.com/products?utf8=%E2%9C%93&is_searching=true&restrictBy%5Bavailable_only%5D=1&keywords={bookTitle.Replace(" ", "+")}{(bookType == 'N' ? "+novel" : "")}&taxon=2&x=39&y=11&page={currPageNum}&per_page=100";
            Logger.Debug(url);
            KinokuniyaUSALinks.Add(url);
            return url;
        }

        public static string TitleParse(string bookTitle, char bookType, string inputTitle)
        {
            string parsedTitle = Regex.Replace(bookTitle, @"(?<=\d{1,3})[^\d{1,3}]+.*|,| \([^()]*\)|LIGHT NOVEL |NOVEL", "");

            if (parsedTitle[parsedTitle.Length - 1])
            if (parsedTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = Regex.Replace(parsedTitle, "MANGA|Manga", "");
            }
            else if (parsedTitle.Contains("Novel"), StringComparison.OrdinalIgnoreCase)
            {
                parsedTitle = Regex.Replace(parsedTitle, "NOVEL|novel", "");
            }
            

            if (!parsedTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase))
            {
                parsedTitle = parsedTitle.Insert(Regex.Match(parsedTitle, @"\d{1,3}").Index, "Vol ");
                // Logger.Debug("Parsed Title = " + parsedTitle);
            }
            else
            {
                parsedTitle = parsedTitle.Replace("Vol.", "Vol");
            }

            if (bookType == 'N')
            {
                parsedTitle = parsedTitle.Insert(parsedTitle.IndexOf("Vol"), "Novel ");
                Logger.Debug("Adding Novel = " + parsedTitle + " Index = " + (parsedTitle.IndexOf("Novel") - 1) + " Substring Title = " + parsedTitle.Substring(0, parsedTitle.IndexOf("Novel") - 1));
                if (!MasterScrape.Similar(parsedTitle.Substring(0, parsedTitle.IndexOf("Novel") - 1), inputTitle))
                {
                    // Logger.Debug("Not SImilar");
                    parsedTitle = parsedTitle.Remove(0, parsedTitle.IndexOf("Novel")).Insert(0, char.ToUpper(inputTitle[0]) + inputTitle.ToLower().Substring(1) + " ");
                    // Logger.Debug("Fixed Title = " + parsedTitle);
                }
            }
            else if (bookType == 'M' && !MasterScrape.Similar(parsedTitle.Substring(0, parsedTitle.IndexOf("Vol") - 1), inputTitle))
            {
                parsedTitle = parsedTitle.Remove(0, parsedTitle.IndexOf("Vol")).Insert(0, char.ToUpper(inputTitle[0]) + inputTitle.ToLower().Substring(1) + " ");
            }

            return parsedTitle.Trim();
        }

        public static List<string[]> GetKinokuniyaUSAData(string bookTitle, char bookType, bool memberStatus, byte currPageNum, EdgeOptions edgeOptions)
        {
            WebDriver dummyDriver = new EdgeDriver(@"DriverExecutables/Edge", edgeOptions);
            edgeOptions.AddArgument("user-agent=" + dummyDriver.ExecuteScript("return navigator.userAgent").ToString().Replace("Headless", ""));
            dummyDriver.Quit();

            EdgeDriver edgeDriver = new EdgeDriver(@"DriverExecutables/Edge", edgeOptions);
            WebDriverWait wait = new WebDriverWait(edgeDriver, TimeSpan.FromSeconds(10));
            edgeDriver.Navigate().GoToUrl(GetUrl(bookType, currPageNum, bookTitle.Replace("-", "")));

            if (bookType == 'M')
            {
                // Click the Manga button so it only shows manga and wait for DOM to fully load
                wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
                edgeDriver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.XPath("//p[contains(text(), 'English Books')]/following-sibling::ul//a[contains(text(), 'Manga')]"))));
            }
            wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));
            wait.Until(driver => driver.FindElement(By.XPath("//ul[@class='sortMenu']//li//a[contains(text(), 'List')]"))).Click();
            wait.Until(driver => driver.FindElement(By.XPath("//div[@id='loading' and @style='display: none;']")));

             // Initialize the html doc for crawling
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(edgeDriver.PageSource);
            edgeDriver.Quit();

            // Get the page data from the HTML doc
            HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//span[@class='underline']");
            HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//li[@class='price']/span");
            HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//li[@class='status']");

            // Remove all of the novels from the list if user is searching for manga
            if (titleData != null)
            {
                for(int x = 0; x < titleData.Count; x++)
                {
                    if (titleData[x].InnerText.Contains("Novel", StringComparison.OrdinalIgnoreCase) && bookType == 'M')
                    {
                        //Logger.Debug($"Removed Novel {titleData[x].InnerText}");
                        titleData.RemoveAt(x);
                        stockStatusData.RemoveAt(x);
                        priceData.RemoveAt(x);
                        priceData.RemoveAt(x + 1);
                        x--;
                    }
                }
            }
            else
            {
                Logger.Warn($"{bookTitle} Does Not Exist at KinokuniyaUSA");
            }

            HtmlNode pageCheck = doc.DocumentNode.SelectSingleNode("//p[@class='pagerArrowR']");

            if (titleData.Count == 0)
            {
                Logger.Warn($"{bookTitle} Does not Exist at KinokuniyaUSA");
            }

            string stockStatus = "";
            if (memberStatus) // If the user is a member only take the discounted prices (10%)
            {
                for (int x = 1; x < priceData.Count; x+=2)
                {
                    if (stockStatusData[x / 2].InnerText.Contains("Out of stock"))
                    {
                        stockStatus = "OOS";
                    }
                    else if (stockStatusData[x / 2].InnerText.Contains("Pre Order"))
                    {
                        stockStatus = "PO";
                    }
                    else if (stockStatusData[x / 2].InnerText.Contains("In stock"))
                    {
                        stockStatus = "IS";
                    }
                    KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType, bookTitle), priceData[x].InnerText.Trim(), stockStatus, "KinokuniyaUSA"});

                    Logger.Debug("[" + KinokuniyaUSADataList[x / 2][0] + ", " + KinokuniyaUSADataList[x / 2][1] + ", " + KinokuniyaUSADataList[x / 2][2] + ", " + KinokuniyaUSADataList[x / 2][3] + "]");
                }
            }
            else
            {
                for (int x = 0; x < priceData.Count; x+=2)
                {
                    if (stockStatusData[x / 2].InnerText.Contains("Out of stock"))
                    {
                        stockStatus = "OOS";
                    }
                    else if (stockStatusData[x / 2].InnerText.Contains("Pre Order"))
                    {
                        stockStatus = "PO";
                    }
                    else if (stockStatusData[x / 2].InnerText.Contains("In stock"))
                    {
                        stockStatus = "IS";
                    }
                    
                    // if (titleData[x / 2].InnerText.Contains("23"))
                    // {
                    //     KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType), "$4.00", stockStatus, "KinokuniyaUSA"});
                    //     continue;
                    // }
                    KinokuniyaUSADataList.Add(new string[]{TitleParse(titleData[x / 2].InnerText, bookType, bookTitle), priceData[x].InnerText.Trim(), stockStatus, "KinokuniyaUSA"});

                    Logger.Debug("[" + KinokuniyaUSADataList[x / 2][0] + ", " + KinokuniyaUSADataList[x / 2][1] + ", " + KinokuniyaUSADataList[x / 2][2] + ", " + KinokuniyaUSADataList[x / 2][3] + "]");
                }
            }

            // if (pageCheck != null)
            // {
            //     //edgeDriver.FindElement(By.XPath("//p[@class='pagerArrowR']")).Click();
            //     //GetKinokuniyaUSAData(bookTitle, bookType, memberStatus, currPageNum, edgeOptions);
            // }
            //edgeDriver.Quit();

            KinokuniyaUSADataList.Sort(new VolumeSort(bookTitle));
            Logger.Debug("Finished Sorting");

            //Print data to a txt file
            using (StreamWriter outputFile = new StreamWriter(@"Data\KinokuniyaUSAData.txt"))
            {
                if (KinokuniyaUSADataList.Count != 0)
                {
                    foreach (string[] data in KinokuniyaUSADataList)
                    {
                        outputFile.WriteLine("[" + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3] + "]");
                    }
                }
                else{
                    outputFile.WriteLine(bookTitle + " Does Not Exist at KinokuniyaUSA");
                }
            } 
            return KinokuniyaUSADataList;
        }
    }
}