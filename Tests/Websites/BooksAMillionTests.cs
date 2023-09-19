using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests.Websites
{
    public class BooksAMillionTests
    {
        [SetUp]
        public void Setup()
        {
            BooksAMillion.ClearData();
        }

        [Test, Description("Tests Manga book, Box Sets, Omnibus, & Manga w/ No Vol Number")]
        public void BooksAMillion_OnePiece_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("one piece", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionOnePieceMangaData.txt")));
        }

        [Test]
        public void BooksAMillion_Naruto_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("Naruto", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionNarutoMangaData.txt")));
        }

        // [Test]
        // public void BooksAMillion_Naruto_Novel_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("Naruto", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionNarutoNovelData.txt")));
        // }

        [Test, Description("Manga w/ 2-in-1 Omni & Omni w/ Missing Info")]
        public void BooksAMillion_Bleach_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("Bleach", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionBleachMangaData.txt")));
        }

        [Test, Description("Validates Manga w/ Novel Entries")]
        public void BooksAMillion_COTE_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("classroom of the elite", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionCOTEMangaData.txt")));
        }

        [Test, Description("Validates Novel w/ Manga Entries")]
        public void BooksAMillion_COTE_Novel_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("classroom of the elite", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionCOTENovelData.txt")));
        }

        // // Not done, is having issues
        // [Test, Description("Validates Novel w/ Novel after Volume and lowercase")]
        // public void BooksAMillion_Overlord_Novel_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("Overlord", Book.LightNovel, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionOverlordNovelData.txt")));
        // }

        // [Test, Description("Validates Manga w/ Novel Entries & Has Paperback & Hardcover Initially")]
        // public void BooksAMillion_Overlord_Manga_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("overlord", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionOverlordMangaData.txt")));
        // }

        [Test, Description("Validates Series w/ Number")]
        public void BooksAMillion_DimensionalSeduction_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("2.5 Dimensional Seduction", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionDimensionalSeductionMangaData.txt")));
        }

        [Test, Description("Validates Series w/ Non Letter or Digit Char in Title")]
        public void BooksAMillion_AkaneBanashi_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("Akane-Banashi", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionAkaneBanashiMangaData.txt")));
        }

        // [Test, Description("Validates Series w/ Non Letter or Digit Char & Numbers in Title")]
        // public void BooksAMillion_07Ghost_Manga_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("07-ghost", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillion07GhostMangaData.txt")));
        // }

        [Test, Description("Validates Manga Series w/ Volumes that does not contain Vol type string but is valid")]
        public void BooksAMillion_JujutsuKaisen_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("jujutsu kaisen", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionJujutsuKaisenMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Hardcover & Paperback Volumes")]
        public void BooksAMillion_FMAB_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("fullmetal alchemist", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionFMABMangaData.txt")));
        }

        [Test, Description("Validates Manga Series w/ Deluxe Hardcover & Paperback Volumes")]
        public void BooksAMillion_Berserk_Manga_Test()
        {
            Assert.That(BooksAMillion.GetBooksAMillionData("Berserk", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionBerserkMangaData.txt")));
        }

        // [Test, Description("Validates Manga Series w/ Illustration Entries & Vol 0")]
        // public void BooksAMillion_Toilet_Manga_Test()
        // {
        //     Assert.That(BooksAMillion.GetBooksAMillionData("Toilet-bound Hanako-kun", Book.Manga, false, 1), Is.EqualTo(ImportDataToList(@"C:\MangaLightNovelWebScrape\Tests\Websites\Data\BooksAMillion\BooksAMillionToiletMangaData.txt")));
        // }
    }
}