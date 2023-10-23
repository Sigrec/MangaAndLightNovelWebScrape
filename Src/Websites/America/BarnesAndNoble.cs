using System.Net;

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
        

        [GeneratedRegex(@"Manga| Complete|(?:Vol).*|(?<=Box Set \d{1,3})[^\d{1,3}.]+.*|\,|:| \([^()]*\)")] private static partial Regex ParseBoxSetTitleRegex();
        [GeneratedRegex(@"(?<=Vol \d{1,3})[^\d{1,3}.]+.*|\,|:| \([^()]*\)")]  private static partial Regex ParseTitleRegex();
        [GeneratedRegex("\\(Omnibus Edition\\)|\\(3-in-1 Edition\\)|\\(2-in-1 Edition\\)")] private static partial Regex OmnibusTitleRegex();
        [GeneratedRegex(@"\?Ns=.*")] private static partial Regex UrlFixRegex();
        internal async Task CreateBarnesAndNobleTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetBarnesAndNobleData(bookTitle, book, isMember, 1));
            });
        }

        internal string GetUrl()
        {
            return BarnesAndNobleLinks.Count != 0 ? BarnesAndNobleLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        internal void ClearData()
        {
            BarnesAndNobleLinks.Clear();
            BarnesAndNobleData.Clear();
        }

        private Uri GetUrl(BookType bookType, string bookTitle, bool check)
        {
            string url = string.Empty;
            if (bookType == BookType.Manga)
            {
                if (!check)
                {
                    // https://www.barnesandnoble.com/s/one+piece/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1
                    url = $"https://www.barnesandnoble.com/s/{MasterScrape.FilterBookTitle(bookTitle)}/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1";
                    LOGGER.Info($"Initial Manga Url = {url}");
                }
                else
                {
                    // https://www.barnesandnoble.com/s/classroom+of+the+elite+manga/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1
                    url = $"https://www.barnesandnoble.com/s/{MasterScrape.FilterBookTitle(bookTitle)}+manga/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1";
                    LOGGER.Info($"Dif Manga Url = {url}");
                }
            }
            else if (bookType == BookType.LightNovel)
            {
                // https://www.barnesandnoble.com/s/overlord+novel/_/N-1z141wbZ8q8/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{MasterScrape.FilterBookTitle(bookTitle)}+novel/_/N-1z141wbZ8q8/?Nrpp=40&Ns=P_Display_Name%7C0&page=1";
                LOGGER.Info($"Initial Novel Url = {url}");
            }
            BarnesAndNobleLinks.Add(url);
            return new Uri(url);
        }

        private static string TitleParse(string titleText, BookType bookType, string inputTitle, bool oneShotCheck)
        {
            titleText = MasterScrape.FixVolumeRegex().Replace(titleText, "Vol");
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

            if (!oneShotCheck && bookType == BookType.Manga && !titleText.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !titleText.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(titleText))
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
        }

        private List<EntryModel> GetBarnesAndNobleData(string bookTitle, BookType bookType, bool memberStatus, byte currPageNum)
        {
            try
            {
                bool hardcoverCheck = false, secondCheck = false, oneShotCheck = false;
                Uri originalUrl = GetUrl(bookType, bookTitle, secondCheck);
                HtmlDocument doc = new HtmlDocument();
                HtmlWeb web = new HtmlWeb
                {
                    UserAgent = string.Empty
                };

                CheckOther:
                List<KeyValuePair<Uri, string>> ValidUrls = new List<KeyValuePair<Uri, string>>();
                int validUrlCount = 0;
                doc = web.Load(originalUrl);

                if (doc.DocumentNode.SelectSingleNode("//div[@id='productDetail-container']") != null)
                {
                    oneShotCheck = true;
                    LOGGER.Info("One Shot Series");
                }
                else
                {
                    HtmlNodeCollection formats = doc.DocumentNode.SelectNodes("(//div[@class='sidebar__section refinements']/span/h2[contains(text(), 'Format')])[1]/ancestor::div[1]/ul//a[contains(text(), 'Paperback') or contains(text(), 'Hardcover') or contains(text(), 'BN Exclusive')]");
                    if (formats.Count != 0)
                    {
                        // After https://www.barnesandnoble.com/s/one+piece/_/N-1z141tjZ8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1
                        // Before https://www.barnesandnoble.com/s/one+piece/_/N-1z141tjZ8q8Z2y35?Ns=P_Display_Name%7C0
                        LOGGER.Debug("Found Formats");
                        foreach(HtmlNode format in formats)
                        {
                            string innerText = format.InnerText.Trim();
                            string url = $"https://www.barnesandnoble.com/{format.GetAttributeValue("href", "No Url")}";
                            LOGGER.Debug("{} Url -> {}", innerText, url);
                            if (!hardcoverCheck && innerText.Equals("Hardcover", StringComparison.OrdinalIgnoreCase))
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri($"{UrlFixRegex().Replace(url, "")}/?Nrpp=40&Ns=P_Display_Name%7C0&page=1"), "Hardcover"));
                            }
                            else if (!hardcoverCheck && innerText.Equals("BN Exclusive", StringComparison.OrdinalIgnoreCase))
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri(url), "OneShot"));
                            }
                            else
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri($"{UrlFixRegex().Replace(url, "")}/?Nrpp=40&Ns=P_Display_Name%7C0&page=1"), "Paperback"));
                            }
                        }
                    }
                }

                HtmlNode pageCheck = null;
                Uri nextPage = !oneShotCheck ? ValidUrls[validUrlCount].Key : originalUrl;
                while (oneShotCheck || (validUrlCount <= ValidUrls.Count))
                {
                    if (!oneShotCheck && pageCheck == null)
                    {
                        nextPage = ValidUrls[validUrlCount].Key;
                        hardcoverCheck = ValidUrls[validUrlCount].Value.Equals("Hardcover");
                        oneShotCheck = ValidUrls[validUrlCount].Value.Equals("OneShot");
                        LOGGER.Info($"{ValidUrls[validUrlCount].Value} Url = {ValidUrls[validUrlCount].Key}");
                        validUrlCount++;
                    }
                    else if (oneShotCheck)
                    {
                        validUrlCount++;
                    }

                    if (!doc.ParsedText.Contains("The page you requested can't be found") && !doc.ParsedText.Contains("Sorry, we couldn't find what you're looking for"))
                    {
                        LOGGER.Debug("Valid Page");
                        doc = web.Load(nextPage);
                        if (!oneShotCheck && doc.DocumentNode.SelectSingleNode("//div[@id='productDetail-container']") != null)
                        {
                            oneShotCheck = true;
                        }
                    }
                    else
                    {
                        LOGGER.Debug("Invalid Page");
                        goto Quit;
                    }

                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(!oneShotCheck ? NotOneShotTitleXPath : OneShotTitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(!oneShotCheck ? NotOneShotPriceXPath : OneShotPriceXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(!oneShotCheck ? NotOneShotStockStatusXPath : OneShotStockStatusXPath);
                    pageCheck = doc.DocumentNode.SelectSingleNode(PaginationCheckXPath);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string curTitle = !oneShotCheck ? titleData[x].GetAttributeValue("title", "Title Error") : titleData[x].InnerText;
                        if (
                            MasterScrape.EntryRemovalRegex().IsMatch(curTitle)
                            || (
                                !oneShotCheck
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
                            )
                        {
                            LOGGER.Debug($"Removed {curTitle}");
                            continue;
                        }

                        curTitle = TitleParse(curTitle, bookType, bookTitle, oneShotCheck);
                        if (!hardcoverCheck || !BarnesAndNobleData.Exists(entry => entry.Entry.Equals(curTitle)))
                        {
                            decimal price = !oneShotCheck ? decimal.Parse(priceData[x].InnerText.Trim()[1..]) : decimal.Parse(WebUtility.HtmlDecode(priceData[x].InnerText).Trim()[1..]);
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
                        else
                        {
                            LOGGER.Debug($"Removed {curTitle}");
                        }
                    }

                    Quit:
                    if (pageCheck != null)
                    {
                        // driver.ExecuteScript("arguments[0].click();", wait.Until(driver => driver.FindElement(By.ClassName("next-button"))));
                        // wait.Until(driver => driver.FindElement(By.CssSelector(".product-view-section")));
                        nextPage = new Uri(doc.DocumentNode.SelectSingleNode("//a[@class='next-button']").GetAttributeValue("href", "No Url"));
                        LOGGER.Info($"Next Page {nextPage}");
                    }
                    else if (!oneShotCheck && !secondCheck && BarnesAndNobleData.Count == 0 && validUrlCount <= ValidUrls.Count)
                    {
                        secondCheck = true;
                        originalUrl = GetUrl(bookType, bookTitle, secondCheck);
                        LOGGER.Info("Checking Other");
                        goto CheckOther;
                    }
                    else if (oneShotCheck || validUrlCount == ValidUrls.Count)
                    {
                        LOGGER.Debug("Finished");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ Barnes & Noble \n{e}");
            }
            BarnesAndNobleData.Sort(MasterScrape.VolumeSort);

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