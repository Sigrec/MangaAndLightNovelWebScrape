namespace MangaAndLightNovelWebScrape
{
    internal static partial class InternalHelpers
    {
        private static readonly Logger LOGGER = LogManager.GetLogger("CDJapanLogs");
        [GeneratedRegex(@"[^\w+]")] internal static partial Regex RemoveNonWordsRegex();

        /// <summary>
        /// Determines if the book title inputted by the user is contained within the current title scraped from the website
        /// </summary>
        /// <param name="bookTitle">The title inputed by the user to initialize the scrape</param>
        /// <param name="curTitle">The current title scraped from the website</param>
        internal static bool TitleContainsBookTitle(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, "").Contains(RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TitleStartsWithCheck(string bookTitle, string curTitle)
        {
            return RemoveNonWordsRegex().Replace(curTitle, "").StartsWith(RemoveNonWordsRegex().Replace(bookTitle, ""), StringComparison.OrdinalIgnoreCase);
        }

        internal static void ReplaceTextInEntryTitle (ref StringBuilder curTitle, string bookTitle, string containsText, string replaceText)
        {
            if (curTitle.ToString().Contains(containsText, StringComparison.OrdinalIgnoreCase) && !bookTitle.Contains(containsText.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                curTitle.Replace(containsText, replaceText);
            }
        }

        internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove)
        {
            if (!bookTitle.Contains(charToRemove))
            {
                curTitle.Replace(charToRemove.ToString(), "");
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
    }
}