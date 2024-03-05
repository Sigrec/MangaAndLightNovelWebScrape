namespace MangaAndLightNovelWebScrape
{
    internal static partial class InternalHelpers
    {
        // private static readonly Logger LOGGER = LogManager.GetLogger("MerryMangaLogs");
        internal static char[] trimedChars = [' ', '\'', '!', '-', ',', ':'];
        [GeneratedRegex(@"[^\w+]")] internal static partial Regex RemoveNonWordsRegex();

        internal static List<EntryModel> RemoveDuplicateEntries(List<EntryModel> entries)
        {
            List<EntryModel> output = new List<EntryModel>();
            foreach (EntryModel entry in entries)
            {
                if (!output.Contains(entry))
                {
                    output.Add(entry);
                }
            }
            return output;
        }

        /// <summary>
        /// Determines if the book title inputted by the user is contained within the current title scraped from the website
        /// </summary>
        /// <param name="bookTitle">The title inputed by the user to initialize the scrape</param>
        /// <param name="curTitle">The current title scraped from the website</param>
        internal static bool BookTitleContainsEntryTitle(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, string.Empty).Contains(RemoveNonWordsRegex().Replace(bookTitle, string.Empty), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether this entry should be removed if it contains a certain text that is also not contained in the book title
        /// </summary>
        /// <param name="bookTitle"></param>
        /// <param name="entryTitle"></param>
        /// <param name="textToCheck"></param>
        /// <returns></returns>
        internal static bool RemoveEntryTitleCheck(string bookTitle, string entryTitle, string textToCheck)
        {
            return !entryTitle.Contains(textToCheck, StringComparison.OrdinalIgnoreCase) && !bookTitle.Contains(textToCheck, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TitleStartsWithCheck(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, string.Empty).StartsWith(RemoveNonWordsRegex().Replace(bookTitle, string.Empty), StringComparison.OrdinalIgnoreCase);
        }

        internal static void ReplaceTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, string containsText, string replaceText)
        {
            if (!bookTitle.Contains(containsText, StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(containsText, replaceText);
            }
        }

        internal static void ReplaceTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, char containsText, char replaceText)
        {
            if (!bookTitle.Contains(containsText))
            {
                curTitle.Replace(containsText, replaceText);
            }
        }

        internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove)
        {
            if (!bookTitle.Contains(charToRemove))
            {
                curTitle.Replace(charToRemove.ToString(), string.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookTitle"></param>
        /// <param name="searchTitle"></param>
        /// <param name="curTitle"></param>
        /// <param name="removeText"></param>
        /// <returns>True if the curTitle should be removed</returns>
        internal static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, string removeText)
        {
            return bookTitle.Contains(searchTitle, StringComparison.OrdinalIgnoreCase) && curTitle.Contains(removeText, StringComparison.OrdinalIgnoreCase);
        }

        internal static string FilterBookTitle(string bookTitle)
        {
            foreach (char var in trimedChars){
                bookTitle = bookTitle.Replace(var.ToString(), $"%{Convert.ToByte(var):x2}");
            }
            return bookTitle;
        }

        /// <summary>
        /// Trims the end of the StingBuilder Content. On Default only the white space char is truncated.
        /// </summary>
        /// <param name="pTrimChars">Array of additional chars to be truncated. A little bit more efficient than using char[]</param>
        /// <returns></returns>
        internal static StringBuilder TrimEnd(this StringBuilder pStringBuilder, HashSet<char> pTrimChars = null)
        {
            if (pStringBuilder == null || pStringBuilder.Length == 0)
                return pStringBuilder;

            int i = pStringBuilder.Length - 1;

            for (; i >= 0; i--)
            {
                var lChar = pStringBuilder[i];

                if (pTrimChars == null)
                {
                    if (char.IsWhiteSpace(lChar) == false)
                        break;
                }
                else if ((char.IsWhiteSpace(lChar) == false) && (pTrimChars.Contains(lChar) == false))
                    break;
            }

            if (i < pStringBuilder.Length - 1)
                pStringBuilder.Length = i + 1;

            return pStringBuilder;
        }

        internal static void PrintWebsiteData(string website, string bookTitle, List<EntryModel> dataList, Logger WebsiteLogger)
        {
            if (MasterScrape.IsDebugEnabled)
            {
                using (StreamWriter outputFile = new($@"Data\{website.Replace(" ", string.Empty)}Data.txt"))
                {
                    if (dataList.Count != 0)
                    {
                        foreach (EntryModel data in dataList)
                        {
                            WebsiteLogger.Info(data);
                            outputFile.WriteLine(data);
                        }
                    }
                    else
                    {
                        WebsiteLogger.Error($"{bookTitle} Does Not Exist at {website}");
                        outputFile.WriteLine($"{bookTitle} Does Not Exist at {website}");
                    }
                } 
            }
        }

        internal static bool ContainsAny(this string input, List<string> values)
        {
            foreach (string val in values)
            {
                if (input.Contains(val, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}