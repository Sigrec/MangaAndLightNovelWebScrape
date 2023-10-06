namespace MangaLightNovelWebScrape.Websites.Canada
{
    public partial class Indigo
    {
        public List<string> IndigoLinks = new();
        public List<EntryModel> IndigoData = new();
        public const string WEBSITE_TITLE = "Indigo";
        private const decimal PLUM_DISCOUNT = 0.1M;
        private static readonly Logger LOGGER = LogManager.GetLogger("IndigoLogs");
        private const Region WEBSITE_REGION = Region.Canada;

        [GeneratedRegex(@",|\.")] private static partial Regex TitleRegex();

        internal async Task CreateIndigoTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetIndigoData(bookTitle, book, isMember, driver));
            });
        }

        internal void ClearData()
        {
            if (this != null)
            {
                IndigoLinks.Clear();
                IndigoData.Clear();
            }
        }

        internal string GetUrl()
        {
            return IndigoLinks.Count != 0 ? IndigoLinks[0] : $"{WEBSITE_TITLE} Has no Link"; 
        }

        // https://www.indigo.ca/en-ca/search?q=world+trigger&search-button=&lang=en_CA
        // https://www.indigo.ca/en-ca/search?q=jujutsu+kaisen&search-button=&lang=en_CA
        private string GetUrl(string bookTitle, BookType bookType)
        {
            string url = $"https://www.indigo.ca/en-ca/search?q={bookTitle.Replace(' ', '+')}&search-button=&lang=en_CA";
            LOGGER.Debug(url);
            IndigoLinks.Add(url);
            return url;
        }

        private static bool RunClickEvent(string xPath, WebDriver driver, WebDriverWait wait, string type)
        {
            var elements = driver.FindElements(By.XPath(xPath));
            if (elements != null && elements.Count != 0)
            {
                LOGGER.Debug(type);
                wait.Until(driver => driver.FindElement(By.XPath(xPath))).Click();
                return true;
            }
            LOGGER.Debug($"{type} Failed");
            return false;
        }

        internal List<EntryModel> GetIndigoData(string bookTitle, BookType bookType, bool isMember, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                driver.Navigate().GoToUrl(GetUrl(bookTitle, bookType));
                // wait.Until(driver => driver.FindElement(By.XPath("/html[@style='position: relative;']")));

                // Check formats to filter entries for Paperback & Hardcover
                if(RunClickEvent("//div[@id='refinement-heading']/span[@aria-label='Book Format']", driver, wait, "Clicking Book Format Tab"))
                {
                    wait.Until(driver => driver.FindElement(By.XPath("//div[contains(@class, 'Book Format active')]")));
                    if(RunClickEvent("//div[@id='refinement-book-format']//span[contains(text(), 'Paperback')]", driver, wait, "Clicking Paperback"))
                    {
                        wait.Until(driver => driver.FindElement(By.XPath("//div[@id='refinement-book-format']//span[contains(text(), 'Paperback')]/ancestor::div[@class='custom-control custom-checkbox form-group']/input[@checked]")));
                    }

                    wait.Until(driver => driver.FindElement(By.XPath("//div[contains(@class, 'Book Format active')]"))); 
                    if (RunClickEvent("//div[@id='refinement-book-format']//span[contains(text(), 'Hardcover')]", driver, wait, "Clicking Hardcover"))
                    {
                        wait.Until(driver => driver.FindElement(By.XPath("//div[@id='refinement-book-format']//span[contains(text(), 'Hardcover')]/ancestor::div[@class='custom-control custom-checkbox form-group']/input[@checked]")));
                    }

                    // Ensure language is only English
                    //wait.Until(driver => driver.FindElement(By.XPath(@"//div[@data-refinement-name='Language']")));
                    if(RunClickEvent("//div[@id='refinement-heading']/span[@aria-label='Language']", driver, wait, "Clicking Language Tab"))
                    {
                        wait.Until(driver => driver.FindElement(By.XPath("//div[contains(@class, 'Language active')]")));
                        if (RunClickEvent("//div[@id='refinement-language']//span[1][contains(text(), 'English')]", driver, wait, "Clicking English Language"))
                        {
                            wait.Until(driver => driver.FindElement(By.XPath("//div[@id='refinement-language']//span[contains(text(), 'English')]/ancestor::div[@class='custom-control custom-checkbox form-group']/input[@checked]")));
                        }
                    }
                }

                // Load all entries before getting the html page source
                int index = 1;
                var loadMoreElements = driver.FindElements(By.XPath("//button[@class='btn btn-tertiary more']"));
                while (loadMoreElements.Count != 0)
                {
                    // RunClickEvent("//button[@class='btn btn-tertiary more']", driver, wait, $"Clicking Load More {index}");
                    LOGGER.Debug($"Clicking Load More {index}");
                    wait.Until(driver => driver.FindElements(By.XPath("//button[@class='btn btn-tertiary more']"))[0]).Click();
                    loadMoreElements = driver.FindElements(By.XPath("//button[@class='btn btn-tertiary more']"));
                    index++;
                }

                HtmlDocument doc = new();
                doc.LoadHtml(driver.PageSource);
                driver.Close();
                driver.Quit();

                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//a[@class='link secondary']/h3/text()");
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@class='price-wrapper']/span/span");
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='mb-0 product-tile-promotion mouse']");

                string price = string.Empty;
                for(int x = 0; x < titleData.Count; x++)
                {
                    price = priceData[x].InnerText.Trim();
                    IndigoData.Add(
                        new EntryModel(
                            TitleRegex().Replace(titleData[x].InnerText, ""),
                            isMember ? EntryModel.ApplyDiscount(Convert.ToDecimal(price), PLUM_DISCOUNT) : price,
                            !stockStatusData[x].InnerText.Contains("Pre-Order") ? StockStatus.IS : StockStatus.PO,
                            WEBSITE_TITLE
                        )
                    );
                }

            }
            catch (Exception ex)
            {
                // driver.Close();
                // driver.Quit();
                LOGGER.Error($"{bookTitle} Does Not Exist @ Indigo {ex}");
            }

            IndigoData.Sort(new VolumeSort());

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\IndigoData.txt"))
                {
                    if (IndigoData.Count != 0)
                    {
                        foreach (EntryModel data in IndigoData)
                        {
                            LOGGER.Debug(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        LOGGER.Debug(bookTitle + " Does Not Exist at Indigo");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at Indigo");
                    }
                } 
            }

            return IndigoData;
        }
    }
}