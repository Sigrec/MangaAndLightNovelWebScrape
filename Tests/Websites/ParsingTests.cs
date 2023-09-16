// Hellsing, One Piece
namespace Tests.Websites
{
    [TestFixture]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class ParsingTests
    {
        [SetUp]
        public void Setup()
        {

        }

        // [Test]
        // public void AmazonUSA_TitleParse_Test()
        // {
        //     Assert.Multiple(() => {
        //         Assert.That(AmazonUSA.TitleParse("Jujutsu Kaisen, Vol. 17 (17)", 'M', "jujutsu kaisen"), Is.EqualTo("Jujutsu Kaisen Vol 17"));
        //         Assert.That(AmazonUSA.TitleParse("Jujutsu Kaisen 0", 'M', "jujutsu kaisen"), Is.EqualTo("Jujutsu Kaisen Vol 0"));
        //         Assert.That(AmazonUSA.TitleParse("One Piece Box Set: East Blue and Baroque Works, Volumes 1-23 (One Piece Box Sets)", 'M', "one piece"), Is.EqualTo("One Piece Box Set 1"));
        //         Assert.That(AmazonUSA.TitleParse("One Piece Box Set 3: Thriller Bark to New World: Volumes 47-70 with Premium (3) (One Piece Box Sets)", 'M', "one piece"), Is.EqualTo("One Piece Box Set 3"));
        //         Assert.That(AmazonUSA.TitleParse("One Piece, Vol. 2: Buggy the Clown (One Piece Graphic Novel)", 'M', "one piece"), Is.EqualTo("One Piece Vol 2"));
        //         Assert.That(AmazonUSA.TitleParse("One Piece: Skypeia 25-26-27", 'M', "one piece"), Is.EqualTo("One Piece Omnibus Vol 9"));
        //         Assert.That(AmazonUSA.TitleParse("One Piece: East Blue 1-2-3", 'M', "one piece"), Is.EqualTo("One Piece Omnibus Vol 1"));
        //         Assert.That(AmazonUSA.TitleParse("The Seven Deadly Sins Manga Box Set 3", 'M', "the seven deadly sins"), Is.EqualTo("The Seven Deadly Sins Box Set 3"));
        //         Assert.That(AmazonUSA.TitleParse("The Seven Deadly Sins Omnibus 1 (Vol. 1-3)", 'M', "the seven deadly sins"), Is.EqualTo("The Seven Deadly Sins Omnibus Vol 1"));
        //         Assert.That(AmazonUSA.TitleParse("The Seven Deadly Sins 3 (Seven Deadly Sins, The)", 'M', "the seven deadly sins"), Is.EqualTo("The Seven Deadly Sins Vol 3"));
        //         Assert.That(AmazonUSA.TitleParse("One Piece (Omnibus Edition), Vol. 33: Includes vols. 97, 98 & 99 (33)", 'M', "one piece"), Is.EqualTo("One Piece Omnibus Vol 33"));
        //         Assert.That(AmazonUSA.TitleParse("07-GHOST, Vol. 17", 'M', "07-ghost"), Is.EqualTo("07-Ghost Vol 17").IgnoreCase);
        //     });
        // }

        // [Test]
        // public void BooksAMillion_TitleParse_Test()
        // {
        //     Assert.Multiple(() => {
        //         Assert.That(BooksAMillion.TitleParse("World Trigger, Vol. 10, 10", 'M', "world trigger"), Is.EqualTo("World Trigger Vol 10"));
        //         Assert.That(BooksAMillion.TitleParse("07-Ghost, Volume 12", 'M', "07-ghost"), Is.EqualTo("07-Ghost Vol 12"));
        //         Assert.That(BooksAMillion.TitleParse("One Piece Box Set 4 : Dressrosa to Reverie: Volumes 71-90 with Premium", 'M', "one piece"), Is.EqualTo("One Piece Box Set 4"));
        //         Assert.That(BooksAMillion.TitleParse("One Piece (Omnibus Edition), Vol. 33 : Includes Vols. 97, 98 & 99", 'M', "one piece"), Is.EqualTo("One Piece Omnibus Vol 33"));
        //         Assert.That(BooksAMillion.TitleParse("One Piece, Vol. 11 : Volume 11", 'M', "one piece"), Is.EqualTo("One Piece Vol 11"));
        //         Assert.That(BooksAMillion.TitleParse("Jujutsu Kaisen 0", 'M', "jujutsu kaisen"), Is.EqualTo("Jujutsu Kaisen Vol 0"));
        //     });
        // }

        // [Test]
        // public void KinokuniyaUSA_TitleParse_Test()
        // {
        //     Assert.Multiple(() => {
        //         Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World (Manga) Vol. 7 (Skeleton Knight in Another World (Manga)", 'M', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Vol 7"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World 1 (Skeleton Knight in Another World)MANGA (TRA", 'M', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Vol 1"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World (Light Novel) Vol. 10 (Skeleton Knight in Another World (Light Novel))", 'N', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Novel Vol 10"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World 1(Skeleton Knight in Another World)NOVEL <1>", 'N', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Novel Vol 1"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Skeleton Knight in Another World LIGHT NOVEL 3(Skeleton Knight in Another World) <3>", 'N', "Skeleton Knight in Another World"), Is.EqualTo("Skeleton Knight in Another World Novel Vol 3"));
        //         Assert.That(KinokuniyaUSA.TitleParse("The Undead King (Overlord)NOVEL 1", 'N', "overlord"), Is.EqualTo("Overlord Novel Vol 1"));
        //         Assert.That(KinokuniyaUSA.TitleParse("The Dark Warrior (Overlord)NOVEL 2", 'N', "overlord"), Is.EqualTo("Overlord Novel Vol 2"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Overlord 12(Overlord)NOVEL", 'N', "overlord"), Is.EqualTo("Overlord Novel Vol 12"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Re:ZERO -Starting Life in Another World- Ex, Vol. 5 (light novel)", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re:ZERO -Starting Life in Another World- Ex Novel Vol 5"));
        //         Assert.That(KinokuniyaUSA.TitleParse("Re:zero Starting Life in Another World 8(Re: Zero Starting Life in Another World)NOVEL <8>", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re:zero Starting Life in Another World Novel Vol 8").IgnoreCase);
        //         Assert.That(KinokuniyaUSA.TitleParse("Re Zero Starting Life in Another World- Ex 3: The Love Ballad of the Sword Devil (Re: Zero Starting Life in Another World)NOVEL (TRA)", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re Zero Starting Life in Another World- Ex Novel Vol 3"));
        //         Assert.That(KinokuniyaUSA.TitleParse("The Love Song of the Sword Devil (Re: Zero Starting Life in Another World EX 2)novel", 'N', "Re:ZERO -Starting Life in Another World-"), Is.EqualTo("Re: Zero Starting Life in Another World Ex Novel Vol 2").IgnoreCase);
        //         Assert.That(KinokuniyaUSA.TitleParse("One Piece (Omnibus Edition), Vol. 32 : Includes vols. 94, 95 & 96 (One Piece (Omnibus Edition))", 'M', "one piece"), Is.EqualTo("One Piece Omnibus Vol 32").IgnoreCase);
        //         Assert.That(KinokuniyaUSA.TitleParse("One Piece Box Set 4: Dressrosa to Reverie : Volumes 71-90 with Premium (One Piece Box Sets)", 'M', "one piece"), Is.EqualTo("One Piece Box Set 4").IgnoreCase);
        //         Assert.That(KinokuniyaUSA.TitleParse("07-GHOST, Vol. 1 (07-ghost)", 'M', "07-ghost"), Is.EqualTo("07-GHOST Vol 1").IgnoreCase);
        //         Assert.That(KinokuniyaUSA.TitleParse("Hellsing Deluxe Volume 1", 'M', "hellsing"), Is.EqualTo("Hellsing Deluxe Vol 1").IgnoreCase);
        //     });
        // }
    }
}