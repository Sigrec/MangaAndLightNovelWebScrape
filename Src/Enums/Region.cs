namespace MangaAndLightNovelWebScrape.Enums
{
    /// <summary>
    /// Geographic region a scrape runs against. Each region maps to the set of supported
    /// retailers in that market (see <see cref="Helpers.GetRegionWebsiteList(Region)"/>).
    /// <para>
    /// Although declared <see cref="FlagsAttribute"/>, <see cref="MasterScrape"/> only
    /// accepts a single region at a time — combining flags throws.
    /// </para>
    /// </summary>
    [Flags]
    public enum Region
    {
        /// <summary>United States retailers.</summary>
        America = 1 << 0,
        /// <summary>Australian retailers.</summary>
        Australia = 1 << 1,
        /// <summary>UK retailers.</summary>
        Britain = 1 << 2,
        /// <summary>Canadian retailers.</summary>
        Canada = 1 << 3,
        /// <summary>European (non-UK) retailers.</summary>
        Europe = 1 << 4,
        /// <summary>Japanese retailers.</summary>
        Japan = 1 << 5,
    }

    /// <summary>
    /// Extension helpers for <see cref="Region"/>.
    /// </summary>
    public static class RegionMethods
    {
        /// <summary>
        /// Returns <c>true</c> if <paramref name="region"/> has more than one
        /// <see cref="Region"/> flag set. <see cref="MasterScrape.InitializeScrapeAsync"/>
        /// rejects multi-region inputs.
        /// </summary>
        public static bool IsMultiRegion(this Region region)
        {
            ushort count = 0;
            foreach (Region r in Enum.GetValues<Region>())
            {
                if (count > 1)
                {
                    return true;
                }
                else if(region.HasFlag(r))
                {
                    count++;
                }
            }
            return false;
        }
    }
}
