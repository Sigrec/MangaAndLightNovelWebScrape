namespace Tests.Websites
{
    public class KinokuniyaUSATests
    {
        [SetUp]
        public void Setup()
        {
            KinokuniyaUSA.ClearData();
        }

        [Test, Description("Validates Manga w/ Box Sets & Omnibus Volumes")]
        public void KinokuniyaUSA_OnePiece_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("One Piece", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSAOnePieceMangaData.txt")));
        }

        [Test, Description("Valides Non Manga Volumes & Manga Volume w/ no Number")]
        public void KinokuniyaUSA_Naruto_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("Naruto", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSANarutoMangaData.txt")));
        }

        [Test]
        [Ignore("Not Working")]
        public void KinokuniyaUSA_Naruto_Novel_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("Naruto", Book.LightNovel, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSANarutoNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ 2-in-1 Omnibus")]
        public void KinokuniyaUSA_Bleach_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("Bleach", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSABleachMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public void KinokuniyaUSA_JujutsuKaisen_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("jujutsu kaisen", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSAJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Validates Member Status & Series w/ Non Letter or Digit Char in Title")]
        public void KinokuniyaUSA_Member_AkaneBanashi_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("Akane-Banashi", Book.Manga, true), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSAAkaneBanashiMangaData.txt")));
        }

        [Test, Description("Validates Manga w/ Based on Series that is a Novel")]
        public void KinokuniyaUSA_COTE_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("classroom of the elite", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSACOTEMangaData.txt")));
        }

         [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public void KinokuniyaUSA_COTE_Novel_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("classroom of the elite", Book.LightNovel, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSACOTENovelData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public void KinokuniyaUSA_DimensionalSeduction_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("2.5 Dimensional Seduction", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSADimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Special Edition, Paperback, & Omni Volumes")]
        public void KinokuniyaUSA_FMAB_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("fullmetal alchemist", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSAFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public void KinokuniyaUSA_Berserk_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("Berserk", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSABerserkMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Box Set without Box Set Indicator")]
        public void KinokuniyaUSA_Toilet_Manga_Test()
        {
            Assert.That(KinokuniyaUSA.GetKinokuniyaUSAData("Toilet-bound Hanako-kun", Book.Manga, false), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\KinokuniyaUSA\KinokuniyaUSAToiletMangaData.txt")));
        }
    }
}