namespace MangaAndLightNovelWebScrape.Enums
{
    [Flags]
    public enum Region
    {
        America = 1 << 0,
        Australia = 1 << 1,
        Britain = 1 << 2,
        Canada = 1 << 3,
        Europe = 1 << 4,
        Japan = 1 << 5,
    }

    public static class RegionMethods
    {
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