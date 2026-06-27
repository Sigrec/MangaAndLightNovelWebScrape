namespace MangaAndLightNovelWebScrape.Enums
{
    /// <summary>
    /// Format of book being searched for. Each site filters its catalog differently based
    /// on this value — Manga vs. LightNovel changes query keywords, category filters, and
    /// the title-cleanup rules applied to matched entries.
    /// </summary>
    public enum BookType
    {
        /// <summary>Manga / graphic novel volumes.</summary>
        Manga,
        /// <summary>Light-novel volumes (prose, not illustrated).</summary>
        LightNovel
    }
}
