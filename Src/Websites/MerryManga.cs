
namespace MangaAndLightNovelWebScrape.Websites
{
    public partial class MerryManga
    {
        private List<string> MerryMangaLinks = new List<string>();
        private List<EntryModel> MerryMangaData = new List<EntryModel>();
        public const string WEBSITE_TITLE = "MerryManga";
        private static readonly Logger LOGGER = LogManager.GetLogger("MerryMangaLogs");
        public const Region REGION = Region.America;
        private static readonly XPathExpression TitleXPath = XPathExpression.Compile("//h2[@class='woocommerce-loop-product__title']");
        private static readonly XPathExpression PriceXPath = XPathExpression.Compile("//span[@class='price']/ins/span[@class='woocommerce-Price-amount amount']/bdi/text()[1] | //span[@class='price']/span[@class='woocommerce-Price-amount amount']/bdi/text()[1]");
        private static readonly XPathExpression StockStatusXPath = XPathExpression.Compile("//li[contains(@class, 'instock')] | //li[contains(@class, 'outofstock')] | //li[contains(@class, 'onbackorder')] | //li[contains(@class, 'preorder')]");

        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)|Omnibus( \d{1,2})(?:, |\s{1})Vol \d{1,3}-\d{1,3}", RegexOptions.IgnoreCase)] private static partial Regex FixOmnibusRegex();
        [GeneratedRegex(@"(?<=Box Set \d{1}).*", RegexOptions.IgnoreCase)] private static partial Regex FixBoxSetRegex();
        [GeneratedRegex(@" \(.*\)|,")] private static partial Regex FixTitleRegex();
        [GeneratedRegex(@"Vol\.", RegexOptions.IgnoreCase)] internal static partial Regex FixVolumeRegex();

        internal async Task CreateMerryMangaTask(string bookTitle, BookType bookType, List<List<EntryModel>> MasterDataList, WebDriver driver)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetMerryMangaData(bookTitle, bookType, driver));
            });
        }
    
        internal void ClearData()
        {
            MerryMangaLinks.Clear();
            MerryMangaData.Clear();
        }

        internal string GetUrl()
        {
            return MerryMangaLinks.Count != 0 ? MerryMangaLinks[0] : $"{WEBSITE_TITLE} Has no Link";
        }

        private string GenerateWebsiteUrl(string bookTitle, BookType bookType, bool isMangaOnly)
        {
            string url = $"https://www.merrymanga.com/?s={InternalHelpers.FilterBookTitle(bookTitle.Replace(" ", "+"))}&post_type=product&orderby=date&_categories={(bookType == BookType.Manga ? $"{(isMangaOnly ? string.Empty : "box-sets%2C")}manga" : "light-novels")}";
            LOGGER.Info(url);
            MerryMangaLinks.Add(url);
            return url;
        }

        private static string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            StringBuilder curTitle;
            if (FixOmnibusRegex().IsMatch(entryTitle))
            {
                entryTitle = FixOmnibusRegex().Replace(entryTitle, "Omnibus$1");
                if (!entryTitle.Contains("Vol"))
                {
                    entryTitle = entryTitle.Insert(entryTitle.IndexOf("Omnibus") + 7, " Vol");
                }
            }
            else if (FixBoxSetRegex().IsMatch(entryTitle))
            {
                entryTitle = FixBoxSetRegex().Replace(entryTitle, string.Empty);
            }
            
            curTitle = new StringBuilder(FixTitleRegex().Replace(entryTitle, string.Empty));

            if (bookType == BookType.LightNovel && !curTitle.ToString().Contains("Novel"))
            {
                curTitle.Insert(curTitle.ToString().Contains("Vol") ? curTitle.ToString().IndexOf("Vol") : curTitle.Length, " Novel ");
            }
            else if (bookType == BookType.Manga)
            {
                if (bookTitle.Equals("Boruto", StringComparison.OrdinalIgnoreCase))
                {
                    curTitle.Replace("Naruto Next Generations", string.Empty);
                }
            }

            InternalHelpers.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            InternalHelpers.ReplaceTextInEntryTitle(ref curTitle, bookTitle, "–", " ");
            return MasterScrape.MultipleWhiteSpaceRegex().Replace(curTitle.ToString().Trim(), " ");
        }

        private List<EntryModel> GetMerryMangaData(string bookTitle, BookType bookType, WebDriver driver)
        {
            try
            {
                bool isMangaOnly = false;
                WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
                driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.ToLower(), bookType, isMangaOnly));
                wait.Until(driver => driver.FindElement(By.CssSelector("div[class='container main-content']")));

                HtmlDocument doc = new HtmlDocument
                {
                    OptionCheckSyntax = false,
                    DisableServerSideCode = true
                };
                doc.LoadHtml(driver.PageSource);

                if (!isMangaOnly && doc.Text.Contains("No products were found matching your selection."))
                {
                    LOGGER.Warn("No Entries Found, Checking Manga Only Link");
                    isMangaOnly = true;
                    driver.Navigate().GoToUrl(GenerateWebsiteUrl(bookTitle.ToLower(), bookType, isMangaOnly));
                    wait.Until(driver => driver.FindElement(By.CssSelector("div[class='container main-content']")));
                    doc.LoadHtml(driver.PageSource);
                }
                else if (!doc.Text.Contains("facetwp-load-more facetwp-hidden"))
                {
                    LOGGER.Info("Loading More Entries");
                    string loadMoreCssSelector = "div[class='facetwp-facet facetwp-facet-load_more facetwp-type-pager']";
                    wait.Until(driver => driver.FindElement(By.CssSelector(loadMoreCssSelector))).Click();
                    while (wait.Until(driver => driver.FindElement(By.CssSelector(loadMoreCssSelector))).Size.Height != 0)
                    {
                        wait.Until(driver => driver.FindElement(By.CssSelector(loadMoreCssSelector))).Click();
                    }
                    doc.LoadHtml(driver.PageSource);
                    LOGGER.Info("Finished Loading More Entries");
                }
                else
                {
                    LOGGER.Info("Single Page Entry (No Additional Loading Required)");
                }

                bool BookTitleRemovalCheck = MasterScrape.EntryRemovalRegex().IsMatch(bookTitle);   

                // Get the page data from the HTML doc
                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes(TitleXPath);
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes(PriceXPath);
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes(StockStatusXPath);
                // LOGGER.Debug("Check #1 {} | {} | {}", titleData.Count, priceData.Count, stockStatusData.Count);

                for (int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = titleData[x].InnerText.Trim();
                    if (
                        InternalHelpers.BookTitleContainsEntryTitle(bookTitle, entryTitle) 
                        && (!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                        && !(
                            bookType == BookType.Manga
                            && (
                                    InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                                    || InternalHelpers.RemoveUnintendedVolumes(bookTitle, "Naruto", entryTitle, "Boruto")
                                )
                            )
                        )
                    {
                        MerryMangaData.Add(
                            new EntryModel
                            (
                                ParseTitle(FixVolumeRegex().Replace(entryTitle, "Vol").Trim(), bookTitle, bookType),
                                $"${priceData[x].InnerText.Trim()}",
                                stockStatusData[x].GetAttributeValue("class", "Unknown").Trim() switch
                                {
                                    string status when status.Contains("instock", StringComparison.OrdinalIgnoreCase) => StockStatus.IS,
                                    string status when status.Contains("outofstock", StringComparison.OrdinalIgnoreCase) => StockStatus.OOS,
                                    string status when status.Contains("preorder", StringComparison.OrdinalIgnoreCase) => StockStatus.PO,
                                    string status when status.Contains("onbackorder", StringComparison.OrdinalIgnoreCase) => StockStatus.BO,
                                    _ or "Unknown" => StockStatus.NA,
                                },
                                WEBSITE_TITLE
                            )
                        );
                    }
                    else LOGGER.Debug("Removed {}", entryTitle);
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("{} Does Not Exist @ {} \n{}", bookTitle, WEBSITE_TITLE, ex);
            }
            finally
            {
                driver?.Quit();
                MerryMangaData.Sort(EntryModel.VolumeSort);
                InternalHelpers.PrintWebsiteData(WEBSITE_TITLE, bookTitle, MerryMangaData, LOGGER);
            }

            return MerryMangaData;
        }
    }
}