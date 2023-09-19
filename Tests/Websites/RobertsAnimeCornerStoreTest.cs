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
        public void RobertsAnimeCornerStore_OnePiece_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("one piece", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreOnePieceMangaData.txt")));
        }

        [Test]
        public void RobertsAnimeCornerStore_Naruto_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Naruto", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreNarutoMangaData.txt")));
        }

        [Test]
        public void RobertsAnimeCornerStore_Bleach_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Bleach", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBleachMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public void RobertsAnimeCornerStore_COTE_Novel_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("classroom of the elite", Book.LightNovel), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTENovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public void RobertsAnimeCornerStore_COTE_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("classroom of the elite", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreCOTEMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Number & Non Letter Character")]
        public void RobertsAnimeCornerStore_DimensionalSeduction_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("2.5 Dimensional Seduction", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void RobertsAnimeCornerStore_AkaneBanashi_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Akane-Banashi", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Special Edition & Paperback Volumes")]
        public void RobertsAnimeCornerStore_FMAB_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("fullmetal alchemist", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public void RobertsAnimeCornerStore_Berserk_Manga_Test()
        {
            Assert.That(RobertsAnimeCornerStore.GetRobertsAnimeCornerStoreData("Berserk", Book.Manga), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\RobertsAnimeCornerStore\RobertsAnimeCornerStoreBerserkMangaData.txt")));
        }
    }
}