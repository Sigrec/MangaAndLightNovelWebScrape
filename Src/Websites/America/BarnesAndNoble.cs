using System.Collections.ObjectModel;

namespace MangaLightNovelWebScrape.Websites.America
{
    public partial class BarnesAndNoble
    {
        private List<string> BarnesAndNobleLinks = new();
        private List<EntryModel> BarnesAndNobleData = new();
        public const string WEBSITE_TITLE = "Barnes & Noble";
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        private static readonly Logger LOGGER = LogManager.GetLogger("BarnesAndNobleLogs");
        private const Region WEBSITE_REGION = Region.America;
        private static readonly XPathExpression NotOneShotTitleXPath = XPathExpression.Compile("//div[contains(@class, 'product-shelf-title product-info-title pt-xs')]/a");
        private static readonly XPathExpression OneShotTitleXPath = XPathExpression.Compile("//div[@id='commerce-zone']//h1[@itemprop='name']");
        private static readonly XPathExpression NotOneShotPriceXPath = XPathExpression.Compile("//div[@class='product-shelf-pricing mt-xs']//div//a//span[2]");
        private static readonly XPathExpression OneShotPriceXPath = XPathExpression.Compile("(//span[@id='pdp-cur-price'])[1]");
        private static readonly XPathExpression NotOneShotStockStatusXPath = XPathExpression.Compile("//p[@class='ml-xxs bopis-badge-message mt-0 mb-0' and (contains(text(), 'Online') or contains(text(), 'Pre-order'))]");
        private static readonly XPathExpression OneShotStockStatusXPath = XPathExpression.Compile("//span[@class='shipping-message-text mt-0 mb-0']/span");
        private static readonly XPathExpression PaginationCheckXPath = XPathExpression.Compile("//li[@class='pagination__next ']");
        

        [GeneratedRegex(@"(?:Vol).*|(?<=\d{1,3})[^\d{1,3}.]+.*|\,|:| \([^()]*\)| Complete")] private static partial Regex ParseBoxSetTitleRegex();
        [GeneratedRegex("Vol\\.|Volume")] private static partial Regex VolTitleFixRegex(); 
        [GeneratedRegex("(?<=Vol \\d{1,3})[^\\d{1,3}.]+.*|\\,|:| \\([^()]*\\)")]  private static partial Regex ParseTitleRegex();
        [GeneratedRegex("\\(Omnibus Edition\\)|\\(3-in-1 Edition\\)|\\(2-in-1 Edition\\)")]  private static partial Regex OmnibusTitleRegex();
        internal async Task CreateBarnesAndNobleTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetBarnesAndNobleData(bookTitle, book, isMember, 1, driver));
            });
        }

        private string GetUrl(BookType bookType, string bookTitle, bool check)
        {
            string url = string.Empty;
            if (bookType == BookType.Manga)
            {
                if (!check)
                {
                    // https://www.barnesandnoble.com/s/overlord/_/N-1z141tjZucb/?Nrpp=40&Ns=P_Publication_Date%7C0&page=1
                    // https://www.barnesandnoble.com/s/overlord/_/N-8q8Zucc/?Nrpp=40&page=1
                    // https://www.barnesandnoble.com/s/overlord/_/N-8q8Zucb/?Nrpp=40&page=1
                    // https://www.barnesandnoble.com/s/world+trigger/_/N-8q8Zucb/?Nrpp=40&page=1
                    url = $"https://www.barnesandnoble.com/s/{MasterScrape.FilterBookTitle(bookTitle)}/_/N-8q8Zucb/?Nrpp=40";
                    LOGGER.Info($"Initial Url = {url}");
                }
                else
                {
                    url = $"https://www.barnesandnoble.com/s/{MasterScrape.FilterBookTitle(bookTitle)}+manga/_/N-8q8Zucb/?Nrpp=40";
                    LOGGER.Info($"Dif Manga Url = {url}");
                }
            }
            else if (bookType == BookType.LightNovel)
            {
                // https://www.barnesandnoble.com/s/overlord+novel/_/N-1z141wbZ8q8/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{MasterScrape.FilterBookTitle(bookTitle)}+novel/_/N-1z141wbZ8q8/?Nrpp=40";
                LOGGER.Info($"Initial Url = {url}");
            }
            BarnesAndNobleLinks.Add(url);
            return url;
        }

        internal string GetUrl()
        {
            return BarnesAndNobleLinks.Count != 0 ? BarnesAndNobleLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        internal void ClearData()
        {
            if (this != null)
            {
                BarnesAndNobleLinks.Clear();
                BarnesAndNobleData.Clear();
            }
        }

        private static string TitleParse(string titleText, BookType bookType, string inputTitle, bool oneShotCheck)
        {
            titleText = VolTitleFixRegex().Replace(titleText, "Vol");
            if (titleText.Contains("Box Set"))
            {
                titleText = ParseBoxSetTitleRegex().Replace(titleText, "");
            }
            else
            {
                if (bookType == BookType.LightNovel)
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
            if (!inputTitle.Contains('-'))
            {
                curTitle.Replace('-', ' ');
            }
            if (titleText.Contains("Toilet-bound Hanako-kun First Stall"))
            {
                curTitle.Append(" Box Set");
            }
            titleText = curTitle.ToString();

            if (!oneShotCheck && bookType == BookType.Manga && !titleText.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(titleText).Index, "Vol ");
            }
            else if (!oneShotCheck && bookType == BookType.LightNovel && !titleText.Contains("Novel"))
            {
                if (titleText.Contains("Vol"))
                {
                    curTitle.Insert(titleText.IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(titleText.Length, " Novel");
                }
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
            // return MasterScrape.MultipleWhiteSpaceRegex().Replace(ParseTitleRegex().Replace(VolTitleFixRegex().Replace(titleText, "Vol"), ""), " ").Trim();
        }

        private List<EntryModel> GetBarnesAndNobleData(string bookTitle, BookType bookType, bool memberStatus, byte currPageNum, WebDriver driver)
        {
            try
            {
                string curTitle = string.Empty;
                string paperbackUrl = string.Empty, hardcoverUrl = string.Empty, curUrl = string.Empty;
                bool hardcoverCheck = false, secondCheck = false, oneShotCheck = false;
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new();

                CheckOther:
                List<KeyValuePair<Uri, string>> ValidUrls = new List<KeyValuePair<Uri, string>>();
                int validUrlCount = 0;
                driver.Navigate().GoToUrl(GetUrl(bookType, bookTitle, secondCheck));
                wait.Until(driver => driver.FindElement(By.XPath("//div[@class='product-view-section pl-lg-l p-sm-0'] | //div[@id='productDetail']")));

                var elements = driver.FindElements(By.Id("productDetail-container"));
                if (elements != null && elements.Any())
                {
                    oneShotCheck = true;
                    LOGGER.Info("One Shot Series");
                }
                else
                {
                    ReadOnlyCollection<IWebElement> formats = driver.FindElements(By.XPath("(//div[@class='sidebar__section refinements']/span/h2[contains(text(), 'Format')])[1]/ancestor::div[1]/ul//a[contains(text(), 'Paperback') or contains(text(), 'Hardcover') or contains(text(), 'BN Exclusive')]"));
                    if (formats.Count != 0)
                    {
                        LOGGER.Debug("Found Formats");
                        foreach(IWebElement format in formats)
                        {
                            if (!hardcoverCheck && format.GetAttribute("innerText").Contains("Hardcover", StringComparison.OrdinalIgnoreCase))
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri($"{MasterScrape.RemoveJSessionIDRegex().Replace(format.GetAttribute("href"), "")}/?Nrpp=40"), "Hardcover"));
                            }
                            else if (!hardcoverCheck && format.GetAttribute("innerText").Contains("BN Exclusive", StringComparison.OrdinalIgnoreCase))
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri(MasterScrape.RemoveJSessionIDRegex().Replace(format.GetAttribute("href"), "")), "OneShot"));
                            }
                            else
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri($"{MasterScrape.RemoveJSessionIDRegex().Replace(format.GetAttribute("href"), "")}/?Nrpp=40"), "Paperback"));
                            }
                        }
                    }
                }

                HtmlNode pageCheck = null;
                while (oneShotCheck || (validUrlCount <= ValidUrls.Count))
                {
                    if (!oneShotCheck && pageCheck == null)
                    {
                        driver.Navigate().GoToUrl(ValidUrls[validUrlCount].Key);
                        hardcoverCheck = ValidUrls[validUrlCount].Value.Equals("Hardcover");
                        oneShotCheck = ValidUrls[validUrlCount].Value.Equals("OneShot");
                        LOGGER.Debug($"{ValidUrls[validUrlCount].Value} Url = {ValidUrls[validUrlCount].Key}");
                        validUrlCount++;
                    }
                    else if (oneShotCheck)
                    {
                        validUrlCount++;
                    }

                    string pageSource = driver.PageSource;
                    if (!pageSource.Contains("The page you requested can't be found") && !pageSource.Contains("Sorry, we couldn't find what you're looking for"))
                    {
                        LOGGER.Debug("Valid Page");
                        doc.LoadHtml(pageSource);
                    }
                    else
                    {
                        LOGGER.Debug("Invalid Page");
                        goto Quit;
                    }

                    if (!oneShotCheck && driver.FindElements(By.Id("productDetail-container")).Count != 0)
                    {
                        oneShotCheck = true;
                    }

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(!oneShotCheck ? NotOneShotTitleXPath : OneShotTitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(!oneShotCheck ? NotOneShotPriceXPath : OneShotPriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(!oneShotCheck ? NotOneShotStockStatusXPath : OneShotStockStatusXPath);
                    pageCheck = doc.DocumentNode.SelectSingleNode(PaginationCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        curTitle = !oneShotCheck ? titleData[x].GetAttributeValue("title", "Title Error") : titleData[x].InnerText;
                        if (
                            MasterScrape.EntryRemovalRegex().IsMatch(curTitle)
                            || !oneShotCheck
                            && (
                                !MasterScrape.TitleContainsBookTitle(bookTitle, curTitle)
                                || (
                                        bookType == BookType.Manga
                                        && (
                                                (
                                                    !curTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) 
                                                    && !curTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) 
                                                    && !curTitle.Contains("Toilet-bound Hanako-kun: First Stall")
                                                    && !(
                                                            curTitle.AsParallel().Any(char.IsDigit) 
                                                            && !bookTitle.AsParallel().Any(char.IsDigit)
                                                        ) 
                                                ) 
                                                || curTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                                || (
                                                    bookTitle.Equals("Naruto", StringComparison.OrdinalIgnoreCase) 
                                                    && (
                                                            curTitle.Contains("Boruto") || curTitle.Contains("Itachi's Story")
                                                        )
                                                ) 
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", curTitle, "Gluttony")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Attack On Titan", curTitle, "No Regrets")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Attack On Titan", curTitle, "Lost Girls")
                                                || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Attack On Titan", curTitle, "The Harsh Mistress of the City")
                                        )
                                    )
                                )
                            )
                        {
                            LOGGER.Info($"Removed {curTitle}");
                            continue;
                        }

                        curTitle = TitleParse(curTitle, bookType, bookTitle, oneShotCheck);
                        if (!hardcoverCheck || !BarnesAndNobleData.Exists(entry => entry.Entry.Equals(curTitle)))
                        {
                            decimal price = !oneShotCheck ? decimal.Parse(priceData[x].InnerText.Trim()[1..]) : decimal.Parse(System.Net.WebUtility.HtmlDecode(priceData[x].InnerText).Trim()[1..]);
                            BarnesAndNobleData.Add(
                                new EntryModel
                                (
                                    curTitle,
                                    $"${(memberStatus ? EntryModel.ApplyDiscount(price, MEMBERSHIP_DISCOUNT) : price)}",
                                    stockStatusData[x].InnerText.Contains("Pre-order", StringComparison.OrdinalIgnoreCase) ? StockStatus.PO : StockStatus.IS, 
                                    WEBSITE_TITLE
                                )
                            );
                        }
                    }

                    Quit:
                    LOGGER.Debug($"{validUrlCount} | {ValidUrls.Count}");
                    if (pageCheck != null)
                    {
                        driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("next-button"))));
                        wait.Until(driver => driver.FindElement(By.CssSelector(".product-view-section")));
                        LOGGER.Info($"Next Page {driver.Url}");
                    }
                    else if (!oneShotCheck && !secondCheck && validUrlCount <= ValidUrls.Count && BarnesAndNobleData.Count == 0)
                    {
                        secondCheck = true;
                        goto CheckOther;
                    }
                    else if (validUrlCount == ValidUrls.Count)
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
                LOGGER.Error($"{bookTitle} Does Not Exist @ Barnes & Noble \n{e}");
            }

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\BarnesAndNobleData.txt"))
                {
                    if (BarnesAndNobleData != null && BarnesAndNobleData.Any())
                    {
                        foreach (EntryModel data in BarnesAndNobleData)
                        {
                            LOGGER.Info(data.ToString());
                            outputFile.WriteLine(data.ToString());
                        }
                    }
                    else
                    {
                        LOGGER.Error(bookTitle + " Does Not Exist at BarnesAndNoble");
                        outputFile.WriteLine(bookTitle + " Does Not Exist at BarnesAndNoble");
                    }
                }
            }  

            return BarnesAndNobleData;
        }
    }
}