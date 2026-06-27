using AmazonUSAScraper = MangaAndLightNovelWebScrape.Websites.AmazonUSA;

namespace Tests.Websites.AmazonUSA;

/// <summary>
/// Unit tests for <see cref="AmazonUSAScraper.CleanAndParseTitle"/>.
///
/// These run against the in-process title parser only — no network, no Playwright, no DOM.
/// They lock in the current behavior of the parser so regex/StringBuilder edits can be
/// verified in milliseconds, without flake from Amazon changing its inventory.
///
/// Inputs model titles AFTER <c>FormatVolumeRegex</c> has run (i.e. "Vol." / "Volume" already
/// normalised to "Vol"), since that is what the production pipeline passes to
/// <c>CleanAndParseTitle</c>. See <see cref="AmazonUSA.GetData"/> for the live call site.
///
/// Add a new TestCase row whenever you find a real Amazon listing that the parser mishandles
/// — pin the broken behavior here, fix the parser, watch it go green.
/// </summary>
[TestFixture, Description("Pure unit tests for AmazonUSAScraper.CleanAndParseTitle")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public sealed class AmazonUSATitleParserTests
{
    // ──────────────────────────────────────────────────────────────────────
    // Plain manga volumes — comma + "Vol N" → "Vol N"
    // ──────────────────────────────────────────────────────────────────────
    [TestCase("Jujutsu Kaisen, Vol 1",      "jujutsu kaisen", "Jujutsu Kaisen Vol 1")]
    [TestCase("Jujutsu Kaisen, Vol 23",     "jujutsu kaisen", "Jujutsu Kaisen Vol 23")]
    [TestCase("Bleach, Vol 10",             "Bleach",         "Bleach Vol 10")]
    [TestCase("Naruto, Vol 5",              "Naruto",         "Naruto Vol 5")]
    public void Manga_StripsCommaBeforeVol(string raw, string bookTitle, string expected)
    {
        string actual = AmazonUSAScraper.CleanAndParseTitle(raw, BookType.Manga, bookTitle);
        Assert.That(actual, Is.EqualTo(expected));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Omnibus — "(3-in-1 Edition)" / "(Omnibus Edition)" both collapse to "Omnibus"
    // ──────────────────────────────────────────────────────────────────────
    [TestCase("Naruto (3-in-1 Edition), Vol 1", "Naruto", "Naruto Omnibus Vol 1")]
    [TestCase("Bleach (3-in-1 Edition), Vol 5", "Bleach", "Bleach Omnibus Vol 5")]
    [TestCase("One Piece (Omnibus Edition), Vol 2", "One Piece", "One Piece Omnibus Vol 2")]
    public void Manga_OmnibusEdition_CollapsesToOmnibus(string raw, string bookTitle, string expected)
    {
        string actual = AmazonUSAScraper.CleanAndParseTitle(raw, BookType.Manga, bookTitle);
        Assert.That(actual, Is.EqualTo(expected));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Box Set — keep "Box Set N" intact
    // ──────────────────────────────────────────────────────────────────────
    [TestCase("Naruto Box Set 1",      "Naruto", "Naruto Box Set 1")]
    [TestCase("Bleach Box Set 2",      "Bleach", "Bleach Box Set 2")]
    public void Manga_BoxSet_PreservesNumber(string raw, string bookTitle, string expected)
    {
        string actual = AmazonUSAScraper.CleanAndParseTitle(raw, BookType.Manga, bookTitle);
        Assert.That(actual, Is.EqualTo(expected));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Light novel — "(Light Novel)" suffix collapses to "Novel" before the Vol number
    // ──────────────────────────────────────────────────────────────────────
    [TestCase("Overlord, Vol 1 (Light Novel)",                    "Overlord", "Overlord Novel Vol 1")]
    [TestCase("Classroom of the Elite, Vol 1 (light novel)",      "classroom of the elite", "Classroom of the Elite Novel Vol 1")]
    public void LightNovel_LightNovelSuffix_CollapsesToNovelBeforeVol(string raw, string bookTitle, string expected)
    {
        string actual = AmazonUSAScraper.CleanAndParseTitle(raw, BookType.LightNovel, bookTitle);
        Assert.That(actual, Is.EqualTo(expected));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Boruto disambiguation — strip the " Naruto Next Generations" tail
    // ──────────────────────────────────────────────────────────────────────
    [TestCase("Boruto: Naruto Next Generations, Vol 1",  "Boruto", "Boruto Vol 1")]
    [TestCase("Boruto: Naruto Next Generations, Vol 12", "Boruto", "Boruto Vol 12")]
    public void Manga_Boruto_StripsNarutoNextGenerationsTail(string raw, string bookTitle, string expected)
    {
        string actual = AmazonUSAScraper.CleanAndParseTitle(raw, BookType.Manga, bookTitle);
        Assert.That(actual, Is.EqualTo(expected));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Dashed bookTitle — "-" is preserved when present in the user-supplied title
    // (InternalHelpers.ReplaceTextInEntryTitle only replaces "-" when bookTitle has none)
    // ──────────────────────────────────────────────────────────────────────
    [Test]
    public void Manga_DashInBookTitle_DashPreserved()
    {
        string actual = AmazonUSAScraper.CleanAndParseTitle("Akane-Banashi, Vol 1", BookType.Manga, "Akane-Banashi");
        Assert.That(actual, Is.EqualTo("Akane-Banashi Vol 1"));
    }
}
