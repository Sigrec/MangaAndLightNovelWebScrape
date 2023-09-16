using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests.Websites
{
    public class BarnesAndNobleTests
    {
        [SetUp]
        public void Setup()
        {
            BarnesAndNoble.ClearData();
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public void BarnesAndNoble_OnePiece_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("one piece", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleOnePieceData.txt")));
        }

        [Test]
        public void BarnesAndNoble_Naruto_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Naruto", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleNarutoData.txt")));
        }

        [Test]
        public void BarnesAndNoble_Naruto_Novel_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Naruto", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleNarutoNovelData.txt")));
        }

        [Test]
        public void BarnesAndNoble_Bleach_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Bleach", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleBleachData.txt")));
        }

        [Test, Description("Validates Manga w/ Based on Series that is a Novel")]
        public void BarnesAndNoble_COTE_Manga_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("classroom of the elite", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleCOTEMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries & Volume Numbers with Decimals")]
        public void BarnesAndNoble_COTE_Novel_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("classroom of the elite", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleCOTENovelData.txt")));
        }

        // Not done, is having issues
        [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        public void BarnesAndNoble_Overlord_Novel_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Overlord", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleOverlordNovelData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        public void BarnesAndNoble_Overlord_Manga_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("overlord", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleOverlordData.txt")));
        }

        [Test, Description("Validates Series w/ Number")]
        public void BarnesAndNoble_DimensionalSeduction_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("2.5 Dimensional Seduction", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleDimensionalSeductionData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void BarnesAndNoble_AkaneBanashi_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Akane-Banashi", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleAkaneBanashiData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        public void BarnesAndNoble_07Ghost_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("07-ghost", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNoble07GhostData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public void BarnesAndNoble_JujutsuKaisen_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("jujutsu kaisen", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleJujutsuKaisenData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes")]
        public void BarnesAndNoble_FMAB_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("fullmetal alchemist", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleFMABData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public void BarnesAndNoble_Berserk_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Berserk", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleBerserkData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        public void BarnesAndNoble_Toilet_Test()
        {
            Assert.That(BarnesAndNoble.GetBarnesAndNobleData("Toilet-bound Hanako-kun", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BarnesAndNoble\BarnesAndNobleToiletData.txt")));
        }
    }
}