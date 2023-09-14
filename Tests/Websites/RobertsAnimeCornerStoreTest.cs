namespace Tests.Websites
{
    [TestFixture]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class RobertsAnimeCornerStoreTest
    {
        [SetUp]
        public void Setup()
        {
            RobertsAnimeCornerStore.ClearData();
        }

        [Test]
        public void RobertsAnimeCornerStore_OnePiece_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("one piece", 'M'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOnePieceData.txt")));
        }

        [Test]
        public void RobertsAnimeCornerStore_Naruto_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Naruto", 'M'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoData.txt")));
        }

        [Test]
        public void RobertsAnimeCornerStore_Bleach_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Bleach", 'M'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBleachData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public void RobertsAnimeCornerStore_COTE_Novel_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("classroom of the elite", 'N'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public void RobertsAnimeCornerStore_COTE_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("classroom of the elite", 'M'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTEData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public void RobertsAnimeCornerStore_DimensionalSeduction_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("2.5 Dimensional Seduction", 'M'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreDimensionalSeductionData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void RobertsAnimeCornerStore_AkaneBanashi_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Akane-Banashi", 'M'), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAkaneBanashiData.txt")));
        }
    }
}