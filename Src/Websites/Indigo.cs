namespace MangaLightNovelWebScrape.Websites
{
    public partial class Indigo
    {
        public List<string> IndigoLinks = new();
        public List<EntryModel> IndigoData = new();
        public const string WEBSITE_TITLE = "Indigo";
        private const decimal PLUM_DISCOUNT = 0.1M;
        private static readonly Logger LOGGER = LogManager.GetLogger("IndigoLogs");
        private const Region WEBSITE_REGION = Region.Canada;

        [GeneratedRegex(@",| \(manga\)|(?<=\d{1,3}): .*|—", RegexOptions.IgnoreCase)] private static partial Regex TitleRegex();
        [GeneratedRegex(@"\((?:3-in-1|2-in-1|Omnibus) Edition\)")] private static partial Regex OmnibusRegex();

        protected internal async Task CreateIndigoTask(string bookTitle, BookType book, bool isMember, List<List<EntryModel>> MasterDataList)
        {
            await Task.Run(() => 
            {
                MasterDataList.Add(GetIndigoData(bookTitle, book, isMember));
            });
        }

        protected internal void ClearData()
        {
            if (this != null)
            {
                IndigoLinks.Clear();
                IndigoData.Clear();
            }
        }

        protected internal string GetUrl()
        {
            return IndigoLinks.Count != 0 ? IndigoLinks[0] : $"{WEBSITE_TITLE} Has no Link"; 
        }

        // https://www.indigo.ca/en-ca/search?q=one+piece+Manga&prefn1=BISACBindingTypeID&prefv1=Paperback%7CHardcover&prefn2=Language&prefv2=English&start=0&sz=1000
        private string GetUrl(string bookTitle, BookType bookType)
        {
            string url = $"https://www.indigo.ca/en-ca/search?q={bookTitle.Replace(' ', '+')}+{(bookType == BookType.Manga ? "manga" : "novel")}&prefn1=BISACBindingTypeID&prefv1=Paperback%7CHardcover&prefn2=Language&prefv2=English&start=0&sz=1000";
            LOGGER.Debug(url);
            IndigoLinks.Add(url);
            return url;
        }

        private string ParseTitle(string entryTitle, string bookTitle, BookType bookType)
        {
            if (OmnibusRegex().IsMatch(entryTitle))
            {
                entryTitle = OmnibusRegex().Replace(entryTitle, "Omnibus");
            }

            StringBuilder curTitle = new StringBuilder(entryTitle);
            MasterScrape.RemoveCharacterFromTitle(ref curTitle, bookTitle, '.');
            MasterScrape.RemoveCharacterFromTitle(ref curTitle, bookTitle, ':');
            
            if (entryTitle.Contains("The Manga"))
            {
                curTitle.Replace("The Manga", "");
            }

            if (entryTitle.Contains("Deluxe"))
            {
                curTitle.Replace("Deluxe", "Deluxe Edition");
            }

            if (!entryTitle.Contains("Vol") && !entryTitle.Contains("Box Set") && !entryTitle.Contains("Anniversary Book") && curTitle.ToString().AsParallel().Any(char.IsDigit))
            {
                curTitle.Insert(MasterScrape.FindVolNumRegex().Match(curTitle.ToString()).Index, "Vol ");
            }

            return curTitle.ToString();
        }

        // TODO - Need to create the logic for oneshot series
        protected internal List<EntryModel> GetIndigoData(string bookTitle, BookType bookType, bool isMember)
        {
            try
            {
                HtmlDocument doc = new();
                HtmlWeb web = new HtmlWeb();
                doc = web.Load(GetUrl(bookTitle, bookType));

                HtmlNodeCollection titleData = doc.DocumentNode.SelectNodes("//a[@class='link secondary']"); // data-adobe-tracking
                HtmlNodeCollection priceData = doc.DocumentNode.SelectNodes("//span[@class='price-wrapper']/span/span");
                HtmlNodeCollection stockStatusData = doc.DocumentNode.SelectNodes("//div[@class='mb-0 product-tile-promotion mouse']");

                string price = string.Empty;
                bool BookTitleRemovalCheck = MasterScrape.CheckEntryRemovalRegex().IsMatch(bookTitle);
                for(int x = 0; x < titleData.Count; x++)
                {
                    string entryTitle = System.Net.WebUtility.HtmlDecode(titleData[x].InnerText);
                    if ((!MasterScrape.EntryRemovalRegex().IsMatch(entryTitle) || BookTitleRemovalCheck)
                        && MasterScrape.TitleContainsBookTitle(bookTitle, entryTitle)
                        && MasterScrape.TitleStartsWithCheck(bookTitle, entryTitle)
                        && ((
                            bookType == BookType.Manga
                            && !entryTitle.Contains("Light Novel", StringComparison.OrdinalIgnoreCase)
                            && (
                                entryTitle.Contains("Manga", StringComparison.OrdinalIgnoreCase)
                                || 
                                !titleData[x].GetAttributeValue("data-adobe-tracking", "Book Type Error").Contains("novel", StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            || bookType == BookType.LightNovel
                        )
                        && 
                        !(
                            MasterScrape.RemoveUnintendedVolumes(bookTitle, "One Piece", entryTitle, "Ace's Story") 
                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Bleach", entryTitle, "Can't Fear Your Own World")
                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "of Gluttony")
                            || MasterScrape.RemoveUnintendedVolumes(bookTitle, "Berserk", entryTitle, "Flame Dragon Knight")
                        ))
                    {
                        price = priceData[x].InnerText.Trim();
                        IndigoData.Add(
                            new EntryModel(
                                ParseTitle(TitleRegex().Replace(MasterScrape.FixVolumeRegex().Replace(entryTitle, "Vol"), ""), bookTitle, bookType),
                                isMember ? EntryModel.ApplyDiscount(Convert.ToDecimal(price), PLUM_DISCOUNT) : price,
                                !stockStatusData[x].InnerText.Contains("Pre-Order") ? StockStatus.IS : StockStatus.PO,
                                WEBSITE_TITLE
                            )
                        );
                    }
                    else
                    {
                        LOGGER.Info("Removed {}", entryTitle);
                    }
                }

            }
            catch (Exception ex)
            {
                LOGGER.Error($"{bookTitle} Does Not Exist @ Indigo {ex}");
            }

            IndigoData.Sort(MasterScrape.VolumeSort);

            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new(@"Data\IndigoData.txt"))
                {
                    if (IndigoData.Count != 0)
                    {
                        foreach (EntryModel data in IndigoData)
                        {
                            LOGGER.Info(data);
                            outputFile.WriteLine(data);
                        }
                    }
                    else
                    {
                        LOGGER.Info($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at {WEBSITE_TITLE}");
                    }
                } 
            }

            return IndigoData;
        }
    }
}