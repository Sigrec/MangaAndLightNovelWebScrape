using MangaLightNovelWebScrape;

namespace Helpers
{
    public static class Helpers
    {
        /// <summary>
        /// Trims the end of the StingBuilder Content. On Default only the white space char is truncated.
        /// </summary>
        /// <param name="pTrimChars">Array of additional chars to be truncated. A little bit more efficient than using char[]</param>
        /// <returns></returns>
        public static StringBuilder TrimEnd(this StringBuilder pStringBuilder, HashSet<char> pTrimChars = null)
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