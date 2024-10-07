namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class BullMoose
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("BullMooseLogs");
        private List<string> BullMooseLinks = new List<string>();
        private List<EntryModel> BullMooseData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "Bull Moose";
        // private const decimal MEMBERSHIP_DISCOUNT = 0.1M;
        public const Region REGION = Region.America;

        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//div[@class='producttitlelink product-grid-variant']/a");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@itemprop='price']");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//div[@class='variant-availability pv-avail-status-class']/div[2]/span/text()");
        private static readonly XPathExpression FormatXPath = XPathExpression.Compile("//span[@class='see-more-format']");
        private static readonly XPathExpression PageCheckXPath = XPathExpression.Compile("//div[@id='pagingdiv']/center/div/span[last() - 1]/a");

        [GeneratedRegex(@"Vol\.|Volume", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();
        [GeneratedRegex(@"\d{1,3}$", RegexOptions.IgnoreCase)] internal static partial Regex FindVolNumRegex();
        [GeneratedRegex(@"(?<=\d{1,3})@.*|(?<=Vol \d{1,3}):.*", RegexOptions.IgnoreCase)] internal static partial Regex TitleParseRegex();

        internal void ClearData()
        {
            BullMooseLinks.Clear();
            BullMooseData.Clear();
        }

        internal async Task CreateBullMooseTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() =>
            {
                MasterDataList.Add(GetBullMooseData(bookTitle, bookType, driver));
            });
        }

        internal string GetUrl()
        {
            return BullMooseLinks.Count != 0 ? BullMooseLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, int pageNum, bool checkBoxSet)
        {
            string url;
            if (checkBoxSet){
                url = $"https://www.bullmoose.com/search?q={InternalHelpers.FilterBookTitle(bookTitle)}%20Box%20Set&so=0&page={pageNum}&af=-2042|-2|-5005";
                LOGGER.Info("Box Set Url: {}", url);
            }
            else
            {
                url = $"https://www.bullmoose.com/search?q={InternalHelpers.FilterBookTitle(bookTitle)}&so=1&page={pageNum}&af=-2042|-2|-5005";
                LOGGER.Info("Base Url: {}", url);
            }
            BullMooseLinks.Add(url);
            return url;
        }

        private string ParseTitle(string entryTitle, string bookTitle)
        {
            StringBuilder curTitle = new StringBuilder(entryTitle);
            LOGGER.Debug("{}", entryTitle);
            if ((entryTitle.Count(c => c == ',') >= 2) || (entryTitle.Contains("Volume") && entryTitle.Contains("Vol.")))
            {
                entryTitle = curTitle.ToString();
                int index = entryTitle.LastIndexOf(',');
                curTitle.Remove(index, entryTitle.Length - index);
                LOGGER.Debug("{} | {}", entryTitle, curTitle.ToString());
            }
            curTitle.Replace(",", string.Empty);
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            curTitle.TrimEnd();

            if (entryTitle.Contains("Box Set"))
            {
                if (entryTitle.Contains("Vol"))
                {
                    int volIdx = curTitle.ToString().IndexOf("Vol");
                    curTitle.Remove(volIdx, curTitle.Length - volIdx);
                }
            }
            else
            {
                if (!entryTitle.Contains("Vol") && char.IsDigit(curTitle.ToString()[curTitle.Length - 1]))
                {
                    curTitle.Insert(FindVolNumRegex().Match(curTitle.ToString()).Index, "Vol ");
                }
            }

            if (bookTitle.Contains(':') && entryTitle.Contains('@'))
            {
                curTitle.Replace('@', ':');
            }
            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, '@');
            
            return curTitle.ToString().Trim();
        }

        private List<EntryModel> GetBullMooseData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(60));
                HtmlDocument doc = new()
                {
                    OptionCheckSyntax = false,
                };
                int curPage = 1;
                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);
                bool checkBoxSet = false;

                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, curPage, checkBoxSet));
                wait.Until(driver => driver.FindElement(By.ClassName("product-variant-grid")));
                doc.LoadHtml(driver.PageSource);
                HtmlNode page = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                int maxPage = page != null ? int.Parse(page.InnerText.Trim()) : 1;
                LOGGER.Debug("Max Page Num = {}", maxPage);

                while (true)
                {
                    HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                    HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                    HtmlNodeCollection formatData = doc.DocumentNode.SelectNodes(FormatXPath);
                    HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                    LOGGER.Debug("{} | {} | {} | {}", titleData != null ? titleData.Count : 0, priceData != null ? priceData.Count : 0, formatData != null ? formatData.Count : 0, stockStatusData != null ? stockStatusData.Count : 0);

                    for (int x = 0; x < titleData.Count; x++)
                    {
                        string entryTitle = FixVolumeRegex().Replace(titleData[x].GetAttributeValue("title", "Title Error"), "Vol");
                        entryTitle = entryTitle[(entryTitle.IndexOf('/') + 1)..].Trim();
                        // LOGGER.Debug("{} | {} | {} | {} | {}", InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle), !MasterScrape.EntryRemovalRegex().IsMatch(entryTitle), BookTitleRemovalCheck, entryTitle.Contains("Vol"), entryTitle.Any(char.IsDigit));
                        if ((!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                            && formatData[x].InnerText.Contains("Book", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            BullMooseData.Add(
                                new EntryModel
                                (
                                    ParseTitle(TitleParseRegex().Replace(entryTitle, string.Empty), bookTitle),
                                    $"${priceData[x].InnerText.Trim()}",
                                    stockStatusData[x].InnerText.Trim() switch
                                    {
                                        string status when status.Contains("out of stock", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                        string status when status.Contains("special order", StringComparison.OrdinalIgnoreCase) => StockStatus.BO,
                                        string status when status.Contains("pre-order", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                        string status when status.Contains("in stock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                        _ => StockStatus.NA,
                                    },
                                    WEBSITE_TITLE
                                )
                            );
                        }
                        else
                        {
                            LOGGER.Debug("Removed {}", entryTitle);
                        }
                    }

                    if (curPage == maxPage)
                    {
                        if (checkBoxSet)
                        {
                            break;
                        }
                        else
                        {
                            curPage = 1;
                            checkBoxSet = true;
                            driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, curPage, checkBoxSet));
                            wait.Until(driver => driver.FindElement(By.ClassName("product-variant-grid")));
                            doc.LoadHtml(driver.PageSource);

                            page = doc.DocumentNode.SelectSingleNode(PageCheckXPath);
                            maxPage = page != null ? int.Parse(page.InnerText.Trim()) : 1;
                            LOGGER.Debug("Box Set Max Page Num = {}", maxPage);
                        }
                    }
                    else
                    {
                        curPage++;
                        driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle, bookType, curPage, checkBoxSet));
                        wait.Until(driver => driver.FindElement(By.ClassName("product-variant-grid")));
                        doc.LoadHtml(driver.PageSource);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }
            finally
            {
                driver?.Quit();
                BullMooseData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, BullMooseData, LOGGER);
            }
            return BullMooseData;
        }
    }
}