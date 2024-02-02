using System.Net;

namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class BarnesAndNoble
    {
        private List<string> BarnesAndNobleLinks = new();
        private List<EntryModel> BarnesAndNobleData = new();
        public const string WEBSITE_TITLE = "Barnes & Noble";
        private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        private static readonly Logger LOGGER = LogManager.GetLogger("BarnesAndNobleLogs");
        public const Region REGION = Region.America;
        private static readonly XPathExpression NotOneShotTitleXPath = XPathExpression.Compile("//div[contains(@class, 'product-shelf-title product-info-title pt-xs')]/a");
        private static readonly XPathExpression OneShotTitleXPath = XPathExpression.Compile("//div[@id='commerce-zone']//h1[@itemprop='name']");
        private static readonly XPathExpression NotOneShotPriceXPath = XPathExpression.Compile("//div[@class='product-shelf-pricing mt-xs']//div//a//span[2]");
        private static readonly XPathExpression OneShotPriceXPath = XPathExpression.Compile("(//span[@id='pdp-cur-price'])[1]");
        private static readonly XPathExpression NotOneShotStockStatusXPath = XPathExpression.Compile("//p[@class='ml-xxs bopis-badge-message mt-0 mb-0' and (contains(text(), 'Online') or contains(text(), 'Pre-order'))]");
        private static readonly XPathExpression OneShotStockStatusXPath = XPathExpression.Compile("//div[@class='ship-this-item-qualify']");
        private static readonly XPathExpression PaginationCheckXPath = XPathExpression.Compile("//li[@class='pagination__next ']");
        

        [GeneratedRegex(@"Manga| Complete|(?:Vol).*|(?<=Box Set \d{1,3})[^\d{1,3}.]+.*|,| \([^()]*\)")] private static partial Regex ParseBoxSetTitleRegex();
        [GeneratedRegex(@"(?<=Vol \d{1,3})[^\d{1,3}.]+.*|,| \([^()]*\)|The Manga|Manga")]  private static partial Regex ParseTitleRegex();
        [GeneratedRegex(@"\((?:Omnibus|\d{1}-in-\d{1}) Edition\)")] private static partial Regex OmnibusTitleRegex();
        [GeneratedRegex(@"\?Ns=.*")] private static partial Regex UrlFixRegex();

        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        protected internal async Task CreateBarnesAndNobleTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetBarnesAndNobleData(bookTitle, book, isMember, 1));
            });
        }

        protected internal string GetUrl()
        {
            return BarnesAndNobleLinks.Count != 0 ? BarnesAndNobleLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        protected internal void ClearData()
        {
            BarnesAndNobleLinks.Clear();
            BarnesAndNobleData.Clear();
        }

        private Uri GenerateWebsiteUrl(BookType bookType, string bookTitle, bool check)
        {
            string url = string.Empty;
            if (bookType == BookType.Manga)
            {
                if (!check)
                {
                    // https://www.barnesandnoble.com/s/one+piece/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1
                    url = $"https://www.barnesandnoble.com/s/{InternalHelpers.FilterBookTitle(bookTitle)}/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1";
                    LOGGER.Info($"Initial Manga Url = {url}");
                }
                else
                {
                    // https://www.barnesandnoble.com/s/classroom+of+the+elite+manga/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1
                    url = $"https://www.barnesandnoble.com/s/{InternalHelpers.FilterBookTitle(bookTitle)}+manga/_/N-8q8Z2y35/?Nrpp=40&Ns=P_Display_Name%7C0&page=1";
                    LOGGER.Info($"Dif Manga Url = {url}");
                }
            }
            else if (bookType == BookType.LightNovel)
            {
                // https://www.barnesandnoble.com/s/overlord+novel/_/N-1z141wbZ8q8/?Nrpp=40&page=1
                url = $"https://www.barnesandnoble.com/s/{InternalHelpers.FilterBookTitle(bookTitle)}+novel/_/N-1z141wbZ8q8/?Nrpp=40&Ns=P_Display_Name%7C0&page=1";
                LOGGER.Info($"Initial Novel Url = {url}");
            }
            BarnesAndNobleLinks.Add(url);
            return new Uri(url);
        }

        private static string TitleParse(string entryTitle, BookType bookType, string bookTitle, bool oneShotCheck)
        {
            bool titleParseCheck = false;
            string volNum = string.Empty;
            if (entryTitle.Contains("Box Set"))
            {
                entryTitle = ParseBoxSetTitleRegex().Replace(entryTitle, string.Empty);
            }
            else
            {
                if (bookType == BookType.LightNovel)
                {
                    entryTitle = entryTitle.Replace("(Light Novel)", "Novel");
                }
                else if (entryTitle.Contains("Edition") && !entryTitle.Contains("Exclusive"))
                {
                    entryTitle = OmnibusTitleRegex().Replace(entryTitle, "Omnibus");
                }
                
                if (entryTitle.Contains("(B&N Exclusive Edition)"))
                {
                    entryTitle = entryTitle.Insert(entryTitle.IndexOf("Vol"), "B&N Exclusive Edition ");
                }

                volNum = ParseTitleRegex().Match(entryTitle).Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(volNum))
                {
                    titleParseCheck = true;
                }
                entryTitle = ParseTitleRegex().Replace(entryTitle, string.Empty);
            }
            
            StringBuilder curTitle = new StringBuilder(entryTitle);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "-", " ");
            if (titleParseCheck)
            {
                curTitle.AppendFormat(" Vol {0}", volNum);
            }
            if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
            {
                InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "Naruto Next Generations", string.Empty);
            }
            entryTitle = curTitle.ToString();

            if (entryTitle.Contains("Special Edition", StringComparison.OrdinalIgnoreCase))
            {
                int startIndex = entryTitle.IndexOf("Special Edition", StringComparison.OrdinalIgnoreCase);
                curTitle.Remove(startIndex, entryTitle.Length - startIndex).TrimEnd();
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Special Edition Vol ");
            }

            if (!oneShotCheck && bookType == BookType.Manga && !entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) && MasterScrape.FindVolNumRegex().IsMatch(entryTitle))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(entryTitle).Index, "Vol ");
            }
            else if (!oneShotCheck && bookType == BookType.LightNovel && !entryTitle.Contains("Novel"))
            {
                if (entryTitle.Contains("Vol"))
                {
                    curTitle.Insert(curTitle.ToString().IndexOf("Vol"), "Novel ");
                }
                else
                {
                    curTitle.Insert(curTitle.Length, " Novel");
                }
            }

            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString(), " ").Trim();
        }

        private List<EntryModel> GetBarnesAndNobleData(string bookTitle, BookType bookType, bool memberStatus, byte currPageNum)
        {
            try
            {
                bool hardcoverCheck = false, secondCheck = false, oneShotCheck = false;
                Uri originalUrl = GenerateWebsiteUrl(bookType, bookTitle, secondCheck);
                HtmlDocument doc = new HtmlDocument();
                HtmlWeb web = new HtmlWeb { UserAgent = string.Empty };

                CheckOther:
                List<KeyValuePair<Uri, string>> ValidUrls = new List<KeyValuePair<Uri, string>>();
                int validUrlCount = 0;
                doc = web.Load(originalUrl);

                if (doc.DocumentNode.SelectSingleNode("//div[@id='productDetail-container']") != null)
                {
                    oneShotCheck = true;
                    LOGGER.Info("Single Entry/Oneshot Url");
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
                            // LOGGER.Debug("{} Url -> {}", innerText, url);
                            if (!hardcoverCheck && innerText.Equals("Hardcover", StringComparison.OrdinalIgnoreCase))
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri($"{UrlFixRegex().Replace(url, string.Empty)}/?Nrpp=40&Ns=P_Display_Name%7C0&page=1"), "Hardcover"));
                            }
                            else if (!hardcoverCheck && innerText.Equals("BN Exclusive", StringComparison.OrdinalIgnoreCase))
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri(url), "OneShot"));
                            }
                            else
                            {
                                ValidUrls.Add(new KeyValuePair<Uri, string>(new Uri($"{UrlFixRegex().Replace(url, string.Empty)}/?Nrpp=40&Ns=P_Display_Name%7C0&page=1"), "Paperback"));
                            }
                        }
                    }
                }

                HtmlNode pageCheck = null;
                Uri nextPage = !oneShotCheck ? ValidUrls[validUrlCount].Key : originalUrl;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
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
                        string entryTitle = !oneShotCheck ? titleData[x].GetAttributeValue("title", "Title Error") : titleData[x].InnerText;
                        
                        if (!oneShotCheck
                            && (!InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle)
                            || (
                                    bookType == BookType.Manga
                                    && (
                                            (
                                                !entryTitle.Contains("Vol", StringComparison.OrdinalIgnoreCase) 
                                                && !entryTitle.Contains("Box Set", StringComparison.OrdinalIgnoreCase) 
                                                && !entryTitle.Contains("Toilet-bound Hanako-kun: First Stall")
                                                && !(
                                                        entryTitle.AsParallel().Any(char.IsDigit) 
                                                        && !bookTitle.AsParallel().Any(char.IsDigit)
                                                    ) 
                                            ) 
                                            || entryTitle.Contains("Novel", StringComparison.OrdinalIgnoreCase)
                                            || (
                                                bookTitle.Equals("Naruto", StringComparison.OrdinalIgnoreCase) 
                                                && (
                                                        entryTitle.Contains("Boruto") || entryTitle.Contains("Itachi's Story")
                                                    )
                                            ) 
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Gluttony")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Attack On Titan", entryTitle, "The Harsh Mistress of the City")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Formation of the Spade Pirates")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Vol. 2: New World")
                                            || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World")
                                    )
                                )
                            )
                        )
                        {
                            LOGGER.Info("Removed {}", entryTitle);
                            continue;
                        }

                        entryTitle = TitleParse(FixVolumeRegex().Replace(entryTitle, "Vol"), bookType, bookTitle, oneShotCheck);
                        if ((!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck) && (!hardcoverCheck || !BarnesAndNobleData.Exists(entry => entry.Entry.Equals(entryTitle))))
                        {
                            decimal price = !oneShotCheck ? decimal.Parse(priceData[x].InnerText.Trim()[1..]) : decimal.Parse(WebUtility.HtmlDecode(priceData[x].InnerText).Trim()[1..]);
                            BarnesAndNobleData.Add(
                                new EntryModel
                                (
                                    entryTitle,
                                    $"${(memberStatus ? EntryModel.ApplyDiscount(price, MEMBERSHIP_DISCOUNT) : price)}",
                                    stockStatusData[x].InnerText.Trim().Replace("\n", " ") switch
                                    {
                                        "Available Online" or "Qualifies for Free Shipping" => StockStatus.IS,
                                        "Unavailable Online" or "Temporarily Out of Stock Online" => StockStatus.OOS,
                                        string status when status.Contains("Pre-order") => StockStatus.PO,
                                        _ => StockStatus.NA,
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else { LOGGER.Info("Removed {}", entryTitle); }
                    }

                    Quit:
                    if (pageCheck != null)
                    {
                        nextPage = new Uri(doc.DocumentNode.SelectSingleNode("//a[@class='next-button']").GetAttributeValue("href", "No Url"));
                        LOGGER.Info($"Next Page {nextPage}");
                    }
                    else if (!oneShotCheck && !secondCheck && BarnesAndNobleData.Count == 0 && validUrlCount <= ValidUrls.Count)
                    {
                        originalUrl = GenerateWebsiteUrl(bookType, bookTitle, secondCheck = true);
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
                LOGGER.Error($"{bookTitle} Does Not Exist @ Barnes & Noble \n{e.StackTrace}");
            }
            BarnesAndNobleData.Sort(EntryModel.VolumeSort);
            InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, BarnesAndNobleData, LOGGER);

            return BarnesAndNobleData;
        }
    }
}