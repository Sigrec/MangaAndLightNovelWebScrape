using System.Text.RegularExpressions;
[assembly: Description("Validation for Various Data Parses")]
namespace Tests
{
    [TestFixture]
    [Author("Sean (Alias -> Sigrec or Prem)")]
    [SetUICulture("en")]
    public class ParsingTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void BarnesAndNoble_TitleParse_Test()
        {
            Assert.Multiple(() => {
               Assert.That(BarnesAndNoble.TitleParse("Classroom of the Elite: Horikita (Manga) Vol. 1", 'M'), Is.EqualTo("Classroom of the Elite Horikita Vol 1"));
               Assert.That(BarnesAndNoble.TitleParse("One Piece, Vol. 97", 'M'), Is.EqualTo("One Piece Vol 97")); 
               Assert.That(BarnesAndNoble.TitleParse("One Piece (Omnibus Edition), Vol. 1: East Blue Vols. 1-2-3", 'M'), Is.EqualTo("One Piece Omnibus Vol 1")); 
               Assert.That(BarnesAndNoble.TitleParse("One Piece Box Set 4: Dressrosa to Reverie: Volumes 71-90 with Premium", 'M'), Is.EqualTo("One Piece Box Set 4"));
               Assert.That(BarnesAndNoble.TitleParse("Classroom of the Elite: Year 2 (Light Novel) Vol. 4.5", 'M'), Is.EqualTo("Classroom of the Elite Year 2 Vol 4.5"));
               Assert.That(BarnesAndNoble.TitleParse("Overlord, Vol. 6 (light novel): The Men of the Kingdom Part II", 'N'), Is.EqualTo("Overlord Vol 6")); 
            });
        }

        [Test]
        public void KinokuniyaUSA_TitleParse_Test()
        {
            Assert.Multiple(() => {
                Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World (Manga) Vol. 7 (Skeleton Knight in Another World (Manga)", 'M', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Vol 7"));
                Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World 1 (Skeleton Knight in Another World)MANGA (TRA", 'M', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Vol 1"));
                Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World (Light Novel) Vol. 10 (Skeleton Knight in Another World (Light Novel))", 'N', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Novel Vol 10"));
                Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World 1(Skeleton Knight in Another World)NOVEL <1>", 'N', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Novel Vol 1"));
                Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World LIGHT NOVEL 3(Skeleton Knight in Another World) <3>", 'N', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Novel Vol 3"));
                Assert.That(KinokuniyaUSA.TitleParse("The Undead King (Overlord)NOVEL 1", 'N', "overlord"), Is.EqualTo("Overlord Novel Vol 1"));
                Assert.That(KinokuniyaUSA.TitleParse("The Dark Warrior (Overlord)NOVEL 2", 'N', "overlord"), Is.EqualTo("Overlord Novel Vol 2"));
                Assert.That(KinokuniyaUSA.TitleParse("Overlord 12(Overlord)NOVEL", 'N', "overlord"), Is.EqualTo("Overlord Novel Vol 12"));
                Assert.That(KinokuniyaUSA.TitleParse("Re:ZERO -Starting Life in Another World- Ex, Vol. 5 (light novel)", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re:ZERO -Starting Life in Another World- Ex Novel Vol 5"));
                Assert.That(KinokuniyaUSA.TitleParse("Re:zero Starting Life in Another World 8(Re: Zero Starting Life in Another World)NOVEL <8>", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re:zero Starting Life in Another World Novel Vol 8").IgnoreCase);
                Assert.That(KinokuniyaUSA.TitleParse("Re Zero Starting Life in Another World- Ex 3: The Love Ballad of the Sword Devil (Re: Zero Starting Life in Another World)NOVEL (TRA)", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re Zero Starting Life in Another World- Ex Novel Vol 3"));
                Assert.That(KinokuniyaUSA.TitleParse("The Love Song of the Sword Devil (Re: Zero Starting Life in Another World EX 2)novel", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re Zero Starting Life in Another World- Ex Novel Vol 3"));
            });
        }
    }
}