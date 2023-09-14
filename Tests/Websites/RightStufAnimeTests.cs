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

        [Test, Description("Tests Manga BookType, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public void RightStufAnime_OnePiece_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("one piece", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeOnePieceData.txt")));
        }

        [Test]
        public void RightStufAnime_Naruto_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Naruto", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeNarutoData.txt")));
        }

        [Test]
        public void RightStufAnime_Bleach_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Bleach", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeBleachData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public void RightStufAnime_COTE_Novel_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("classroom of the elite", 'N', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public void RightStufAnime_COTE_Manga_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("classroom of the elite", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeCOTEData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public void RightStufAnime_DimensionalSeduction_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("2.5 Dimensional Seduction", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeDimensionalSeductionData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void RightStufAnime_AkaneBanashi_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("Akane-Banashi", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnimeAkaneBanashiData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public void RightStufAnime_07Ghost_Test()
        {
            Assert.That(RightStufAnime.GetRightStufAnimeData("07-ghost", 'M', false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RightStufAnime\RightStufAnime07GhostData.txt")));
        }
    }
}