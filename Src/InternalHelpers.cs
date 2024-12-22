using System.Diagnostics;

namespace MangaAndLightNovelWebScrape
{
    internal static partial class InternalHelpers
    {
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
            // Check if charToRemove exists in bookTitle
            if (bookTitle.Contains(charToRemove))
            {
                // Use a single pass to remove all occurrences of charToRemove in curTitle
                int index = 0;
                while ((index = curTitle.ToString().IndexOf(charToRemove, index)) != -1)
                {
                    curTitle.Remove(index, 1);
                }
            }
        }

        internal static void RemoveCharacterFromTitle(ref StringBuilder curTitle, string bookTitle, char charToRemove, string textToCheck)
        {
            string title = curTitle.ToString();
            if (!bookTitle.Contains(charToRemove) && !title.Contains(textToCheck))
            {
                int index = 0;
                while (index < curTitle.Length)
                {
                    if (curTitle[index] == charToRemove)
                    {
                        curTitle.Remove(index, 1); // Remove character at index
                    }
                    else
                    {
                        index++; // Only increment if no removal occurred
                    }
                }
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
            return bookTitle.IndexOf(searchTitle, StringComparison.OrdinalIgnoreCase) >= 0 &&
                curTitle.IndexOf(removeText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool RemoveUnintendedVolumes(string bookTitle, string searchTitle, string curTitle, params string[] removeText)
        {
            if (!bookTitle.Contains(searchTitle, StringComparison.OrdinalIgnoreCase)) return false;

            foreach (var text in removeText)
            {
                if (curTitle.Contains(text, StringComparison.OrdinalIgnoreCase)) 
                {
                    return true;
                }
            }

            return false;
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
                // Clean up website string once before using it for file path.
                string filePath = $@"Data\{website.Replace(" ", string.Empty)}Data.txt";

                using (StreamWriter outputFile = new(filePath))
                {
                    if (dataList.Count > 0)
                    {
                        // If we have data, write it to both the logger and the output file.
                        foreach (EntryModel data in dataList)
                        {
                            WebsiteLogger.Info(data);  // Log the data entry
                            outputFile.WriteLine(data);  // Write to the file
                        }
                    }
                    else
                    {
                        string message = $"{bookTitle} Does Not Exist at {website}";
                        WebsiteLogger.Error(message);  // Log the error message
                        outputFile.WriteLine(message);  // Write the error to the file
                    }
                }
            }
        }

        internal static bool ContainsAny(this string input, List<string> values)
        {
            return values.Any(val => input.Contains(val, StringComparison.OrdinalIgnoreCase));
        }

        internal static void RemoveDuplicates(this List<EntryModel> input, Logger LOGGER)
        {
            for (int x = input.Count - 1; x > 0; x--)
            {
                if (input[x].Entry.Equals(input[x - 1].Entry, StringComparison.OrdinalIgnoreCase))
                {
                    if (input[x].ParsePrice() >= input[x - 1].ParsePrice())
                    {
                        LOGGER.Info($"Removed Duplicate {input[x]}");
                        input.RemoveAt(x);  // Remove the current entry
                    }
                    else
                    {
                        LOGGER.Info($"Removed Duplicate {input[x - 1]}");
                        input.RemoveAt(x - 1);  // Remove the previous entry
                    }
                }
            }
        }

        /// <summary>
        /// Applies a coupon to the price by substracting the coupon amount
        /// </summary>
        /// <param name="initialPrice"></param>
        /// <param name="couponAmount"></param>
        /// <returns></returns>
        internal static string ApplyCoupon(decimal initialPrice, decimal couponAmount)
        {
            return decimal.Subtract(initialPrice, couponAmount).ToString("0.00");
        }
    }
}