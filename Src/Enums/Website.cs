namespace MangaAndLightNovelWebScrape.Enums
{
    /// <summary>
    /// Identifier for every retailer the library knows how to scrape. Each value maps to
    /// a single <see cref="MangaAndLightNovelWebScrape.Websites.IWebsite"/> implementation
    /// dispatched from <see cref="MasterScrape.InitializeScrapeAsync"/>. The XML doc on
    /// each member names the <see cref="Region"/>(s) the site serves — pass only sites
    /// whose region matches the scrape's <see cref="MasterScrape.Region"/>.
    /// </summary>
    public enum Website
    {
        /// <summary>allstarcomics.com.au — Australia.</summary>
        AllStarComics,
        /// <summary>amazon.co.jp — Japan.</summary>
        AmazonJapan,
        /// <summary>amazon.com — America.</summary>
        AmazonUSA,
        /// <summary>booksamillion.com — America.</summary>
        BooksAMillion,
        /// <summary>cdjapan.co.jp — Japan. Currently paused.</summary>
        CDJapan,
        /// <summary>store.crunchyroll.com — America.</summary>
        Crunchyroll,
        /// <summary>forbiddenplanet.com — Britain.</summary>
        ForbiddenPlanet,
        /// <summary>instocktrades.com — America.</summary>
        InStockTrades,
        /// <summary>united-states.kinokuniya.com — America.</summary>
        KinokuniyaUSA,
        /// <summary>mangamart.com — America.</summary>
        MangaMart,
        /// <summary>mangamate.shop — Australia.</summary>
        MangaMate,
        /// <summary>merrymanga.com — America.</summary>
        MerryManga,
        /// <summary>okcomics.co.uk — Britain.</summary>
        OKComics,
        /// <summary>animecornerstore.com — America.</summary>
        RobertsAnimeCornerStore,
        /// <summary>scifier.com — America, Australia, Britain, Canada, Europe (multi-region).</summary>
        SciFier,
        /// <summary>travellingman.com — Britain.</summary>
        TravellingMan
    }
}
