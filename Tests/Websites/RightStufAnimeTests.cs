namespace Tests.Websites
{
    [TestFixture, Description("Validations for RightStufAnime")]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class RightStufAnimeTests
    {
        [SetUp]
        public void Setup()
        {
            RightStufAnime.ClearData();
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public void RightStufAnime_OnePiece_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("one piece", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeOnePieceData.txt")));
        }

        [Test]
        public void RightStufAnime_Naruto_Manga_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Naruto", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeNarutoMangaData.txt")));
        }

        [Test]
        public void RightStufAnime_Naruto_Novel_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Naruto", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeNarutoNovelData.txt")));
        }

        [Test]
        public void RightStufAnime_Bleach_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Bleach", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeBleachData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public void RightStufAnime_COTE_Novel_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("classroom of the elite", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public void RightStufAnime_COTE_Manga_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("classroom of the elite", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeCOTEData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public void RightStufAnime_DimensionalSeduction_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("2.5 Dimensional Seduction", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeDimensionalSeductionData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void RightStufAnime_AkaneBanashi_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Akane-Banashi", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeAkaneBanashiData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public void RightStufAnime_07Ghost_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("07-ghost", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnime07GhostData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public void RightStufAnime_Berserk_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Berserk", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeBerserkData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes & Imperfect Volume")]
        public void RightStufAnime_FMAB_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("fullmetal alchemist", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeFMABData.txt")));
        }
    }
}