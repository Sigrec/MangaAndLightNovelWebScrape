using System;
using System.Text;
using System.Threading.Tasks;

namespace MangaLightNovelWebScrape.Websites
{
    public partial class RobertsAnimeCornerStore
    {
        private static List<string> RobertsAnimeCornerStoreLinks = new();
        private static List<EntryModel> RobertsAnimeCornerStoreData = new();
        public const string WEBSITE_TITLE = "RobertsAnimeCornerStore";
        private static readonly Logger Logger = LogManager.GetLogger("RobertsAnimeCornerStoreLogs");
        private static readonly Dictionary<string, string> URL_MAP_DICT = new()
        { 
            {"mangrapnovag", @"^[a-bA-B\d]"},
            {"mangrapnovhp", @"^[c-dC-D]"},
            {"mangrapnovqz", @"^[e-gE-G]"},
            {"magrnomo", @"^[h-kH-K]"},
            {"magrnops", @"^[l-nL-N]"},
            {"magrnotz", @"^[o-qO-Q]"},
            {"magrnors", @"^[r-sR-S]"},
            {"magrnotv", @"^[t-vT-V]"},
            {"magrnowz", @"^[w-zW-Z]"}
        };

        [GeneratedRegex("-|\\s+")] private static partial Regex FilterBookTitleRegex();
        [GeneratedRegex(",|#| Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex TitleFilterRegex();
        [GeneratedRegex(",| #\\d+-\\d+| #\\d+|Graphic Novel| :|\\(.*?\\)|\\[Novel\\]")] private static partial Regex OmnibusTitleFilterRegex();
        [GeneratedRegex("-(\\d+)")] private static partial Regex OmnibusVolNumberRegex();
        [GeneratedRegex("\\s+|[^a-zA-Z0-9]")] private static partial Regex FindTitleRegex();

        public static string GetUrl()
        {
            return RobertsAnimeCornerStoreLinks[RobertsAnimeCornerStoreLinks.Count - 1];
        }
        
        private static string GetUrl(string htmlString, bool pageExists)
        {
            string url = "";
            if (!pageExists) // Gets the starting page based on first letter and checks if we are looking for the 1st webpage (false) or 2nd webpage containing the actual item data (true)
            {
                Parallel.ForEach(URL_MAP_DICT, (link, state) =>
                {
                    if (new Regex(link.Value).Match(htmlString).Success)
                    {
                        url = $"https://www.animecornerstore.com/{link.Key}.html";
                        RobertsAnimeCornerStoreLinks.Add(url);
                        state.Stop();
                    }
                });
            }
            else
            { //Gets the actual page that houses the data the user is looking for
                url = $"https://www.animecornerstore.com/{htmlString}";
                RobertsAnimeCornerStoreLinks.Add(url);
            }
            Logger.Debug(url);
            return url;
        }

        public static void ClearData()
        {
            RobertsAnimeCornerStoreLinks.Clear();
            RobertsAnimeCornerStoreData.Clear();
        }

        /**
         * TODO: Figure out a way to when checking for title for it to ignore case
         */
        private static string GetPageData(WebDriver driver, string bookTitle, Book book, HtmlDocument doc, WebDriverWait wait)
        {
            string link = "";
            driver.Navigate().GoToUrl(GetUrl(bookTitle, false));
            wait.Until(e => e.FindElement(By.XPath($"//b//a[1]")));
            doc.LoadHtml(driver.PageSource);

            HtmlNodeCollection seriesTitle = doc.DocumentNode.SelectNodes($"//b//a[1]");
            string seriesText;
            foreach (HtmlNode series in seriesTitle)
            {
                seriesText = series.InnerText;
                if (MasterScrape.TitleContainsBookTitle(bookTitle, seriesText))
                {
                    link = GetUrl(series.Attributes["href"].Value, true);
                    break;
                }
            }
            return link;
        }

        private static string TitleParse(string titleText, Book book)
        {
            StringBuilder curTitle;
            if (titleText.Contains("Omnibus"))
            {
                uint volNum = Convert.ToUInt32(OmnibusVolNumberRegex().Match(titleText).Groups[1].Value);
                curTitle = new StringBuilder(OmnibusTitleFilterRegex().Replace(titleText, "").Trim());
                curTitle.Replace("Omnibus Edition", "Omnibus");
                if (!curTitle.ToString().Contains(" Vol"))
                {
                    curTitle.Append(' ');
                    curTitle.Append("Vol");
                }
                curTitle.Append(' ');
                curTitle.Append(volNum / 3);
            }
            else
            {
                titleText = TitleFilterRegex().Replace(titleText, "").Trim();
                curTitle = new StringBuilder(titleText);
                curTitle.Replace("Deluxe Edition", "Deluxe Vol");
                if (titleText.Contains("Box Set") && !titleText.Any(char.IsDigit))
                {
                    curTitle.Append(' ');
                    curTitle.Append('1');
                }
            }
            return book == Book.Manga ? MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ") : MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.Replace("Vol", "Novel Vol").ToString(), " ");
        }
        
        public static List<EntryModel> GetRobertsAnimeCornerStoreData(string bookTitle, Book book)
        {
            WebDriver driver = MasterScrape.SetupBrowserDriver(false);
            WebDriverWait wait = new(driver, TimeSpan.FromMinutes(1));

            // Initialize the html doc for crawling
            HtmlDocument doc = new();

            string linkPage = GetPageData(driver, FilterBookTitleRegex().Replace(bookTitle, ""), book, doc, wait);
            string errorMessage;
            if (string.IsNullOrEmpty(linkPage))
            {
                errorMessage = "Error! Invalid Series Title";
                Logger.Error(errorMessage);
                driver.Close();
                driver.Quit();
            }
            else
            {
                try
                {
                    // Start scraping the URL where the data is found
                    driver.Navigate().GoToUrl(linkPage);
                    wait.Until(e => e.FindElement(By.XPath("//font[@face='dom bold, arial, helvetica']/b")));

                    // Get the html doc for crawling
                    doc.LoadHtml(driver.PageSource);

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//font[@face='dom bold, arial, helvetica']/b");

                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//form[@method='POST'][contains(text()[2], '$')]//font[@color='#ffcc33'][2]"); // //form[@method='POST'][contains(text()[2], '$')]/text()[2] | //font[2][@color='#ffcc33']
                    driver.Close();
                    driver.Quit();
                    string titleText;

                    // Remove All Empty Titles
                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = titleData[x].InnerText;
                        if (string.IsNullOrWhiteSpace(titleText))
                        {
                            titleData.RemoveAt(x);
                        }
                    }

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        titleText = titleData[x].InnerText;
                        if (
                                string.IsNullOrWhiteSpace(titleText) 
                                || titleText.Contains("Poster") 
                                || (
                                        titleText.Contains("[Novel]") 
                                        && book == Book.Manga
                                    ) 
                                || (
                                        titleText.Contains("Graphic") 
                                        && book == Book.LightNovel
                                    )
                            ) // If the user is looking for manga and the page returned a novel data set and vice versa skip that data set
                        {
                            continue;
                        }

                        RobertsAnimeCornerStoreData.Add(
                            new EntryModel(
                                TitleParse(titleText, book),
                                priceData[x].InnerText.Trim(),
                                titleText switch
                                {
                                    string curTitle when curTitle.Contains("Pre Order") => "PO",
                                    string curTitle when curTitle.Contains("Backorder") => "OOS",
                                    _ => "IS"
                                },
                                WEBSITE_TITLE
                            )
                        );
                    }

                    RobertsAnimeCornerStoreData.Sort(new VolumeSort());
                }
                catch(Exception ex)
                {
                    driver.Close();
                    driver.Quit();
                    Logger.Error(bookTitle + " Does Not Exist at RobertsAnimeCornerStore\n" + ex);
                }

                if (MasterScrape.IsDebugEnabled)
                {
                    using (StreamWriter outputFile = new(@"Data\RobertsAnimeCornerStoreData.txt"))
                    {
                        if (RobertsAnimeCornerStoreData.Count != 0)
                        {
                            foreach (EntryModel data in RobertsAnimeCornerStoreData)
                            {
                                Logger.Debug(data.ToString());
                                outputFile.WriteLine(data.ToString());
                            }
                        }
                        else
                        {
                            errorMessage = bookTitle + " Does Not Exist at RobertsAnimeCornerStore";
                            outputFile.WriteLine(errorMessage);
                        }
                    } 
                }
            }
            
            return RobertsAnimeCornerStoreData;
        }
    }
}