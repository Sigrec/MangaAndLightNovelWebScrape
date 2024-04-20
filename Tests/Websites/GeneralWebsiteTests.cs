// Hellsing, One Piece
using MangaAndLightNovelWebScrape.Websites;

namespace Tests.Websites
{
    [TestFixture]
    [Author("Sean (Alias -> Prem or Sigrec)")]
    [SetUICulture("en")]
    public class GeneralWebsiteTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Region_Test()
        {
            Assert.Multiple(() => {
                Assert.That(!AmazonJapan.REGION.HasFlag(Region.America) && !AmazonJapan.REGION.HasFlag(Region.Australia) && !AmazonJapan.REGION.HasFlag(Region.Britain) && !AmazonJapan.REGION.HasFlag(Region.Canada) && !AmazonJapan.REGION.HasFlag(Region.Europe) && AmazonJapan.REGION.HasFlag(Region.Japan));

                Assert.That(AmazonUSA.REGION.HasFlag(Region.America) && !AmazonUSA.REGION.HasFlag(Region.Australia) && !AmazonUSA.REGION.HasFlag(Region.Britain) && !AmazonUSA.REGION.HasFlag(Region.Canada) && !AmazonUSA.REGION.HasFlag(Region.Europe) && !AmazonUSA.REGION.HasFlag(Region.Japan));

                Assert.That(BarnesAndNoble.REGION.HasFlag(Region.America) && !BarnesAndNoble.REGION.HasFlag(Region.Australia) && !BarnesAndNoble.REGION.HasFlag(Region.Britain) && !BarnesAndNoble.REGION.HasFlag(Region.Canada) && !BarnesAndNoble.REGION.HasFlag(Region.Europe) && !BarnesAndNoble.REGION.HasFlag(Region.Japan));

                Assert.That(BooksAMillion.REGION.HasFlag(Region.America) && !BooksAMillion.REGION.HasFlag(Region.Australia) && !BooksAMillion.REGION.HasFlag(Region.Britain) && !BooksAMillion.REGION.HasFlag(Region.Canada) && !BooksAMillion.REGION.HasFlag(Region.Europe) && !BooksAMillion.REGION.HasFlag(Region.Japan));

                Assert.That(!CDJapan.REGION.HasFlag(Region.America) && !CDJapan.REGION.HasFlag(Region.Australia) && !CDJapan.REGION.HasFlag(Region.Britain) && !CDJapan.REGION.HasFlag(Region.Canada) && !CDJapan.REGION.HasFlag(Region.Europe) && CDJapan.REGION.HasFlag(Region.Japan));

                Assert.That(Crunchyroll.REGION.HasFlag(Region.America) && !Crunchyroll.REGION.HasFlag(Region.Australia) && !Crunchyroll.REGION.HasFlag(Region.Britain) && !Crunchyroll.REGION.HasFlag(Region.Canada) && !Crunchyroll.REGION.HasFlag(Region.Europe) && !Crunchyroll.REGION.HasFlag(Region.Japan));

                Assert.That(!ForbiddenPlanet.REGION.HasFlag(Region.America) && !ForbiddenPlanet.REGION.HasFlag(Region.Australia) && ForbiddenPlanet.REGION.HasFlag(Region.Britain) && !ForbiddenPlanet.REGION.HasFlag(Region.Canada) && !ForbiddenPlanet.REGION.HasFlag(Region.Europe) && !ForbiddenPlanet.REGION.HasFlag(Region.Japan));

                Assert.That(!Indigo.REGION.HasFlag(Region.America) && !Indigo.REGION.HasFlag(Region.Australia) && !Indigo.REGION.HasFlag(Region.Britain) && Indigo.REGION.HasFlag(Region.Canada) && !Indigo.REGION.HasFlag(Region.Europe) && !Indigo.REGION.HasFlag(Region.Japan));

                Assert.That(InStockTrades.REGION.HasFlag(Region.America) && !InStockTrades.REGION.HasFlag(Region.Australia) && !InStockTrades.REGION.HasFlag(Region.Britain) && !InStockTrades.REGION.HasFlag(Region.Canada) && !InStockTrades.REGION.HasFlag(Region.Europe) && !InStockTrades.REGION.HasFlag(Region.Japan));

                Assert.That(KinokuniyaUSA.REGION.HasFlag(Region.America) && !KinokuniyaUSA.REGION.HasFlag(Region.Australia) && !KinokuniyaUSA.REGION.HasFlag(Region.Britain) && !KinokuniyaUSA.REGION.HasFlag(Region.Canada) && !KinokuniyaUSA.REGION.HasFlag(Region.Europe) && !KinokuniyaUSA.REGION.HasFlag(Region.Japan));

                Assert.That(!MangaMate.REGION.HasFlag(Region.America) && MangaMate.REGION.HasFlag(Region.Australia) && !MangaMate.REGION.HasFlag(Region.Britain) && !MangaMate.REGION.HasFlag(Region.Canada) && !MangaMate.REGION.HasFlag(Region.Europe) && !MangaMate.REGION.HasFlag(Region.Japan));

                Assert.That(MerryManga.REGION.HasFlag(Region.America) && !MerryManga.REGION.HasFlag(Region.Australia) && !MerryManga.REGION.HasFlag(Region.Britain) && !MerryManga.REGION.HasFlag(Region.Canada) && !MerryManga.REGION.HasFlag(Region.Europe) && !MerryManga.REGION.HasFlag(Region.Japan));

                Assert.That(RobertsAnimeCornerStore.REGION.HasFlag(Region.America) && !RobertsAnimeCornerStore.REGION.HasFlag(Region.Australia) && !RobertsAnimeCornerStore.REGION.HasFlag(Region.Britain) && !RobertsAnimeCornerStore.REGION.HasFlag(Region.Canada) && !RobertsAnimeCornerStore.REGION.HasFlag(Region.Europe) && !RobertsAnimeCornerStore.REGION.HasFlag(Region.Japan));

                Assert.That(!TravellingMan.REGION.HasFlag(Region.America) && !TravellingMan.REGION.HasFlag(Region.Australia) && TravellingMan.REGION.HasFlag(Region.Britain) && !TravellingMan.REGION.HasFlag(Region.Canada) && !TravellingMan.REGION.HasFlag(Region.Europe) && !TravellingMan.REGION.HasFlag(Region.Japan));

                Assert.That(SciFier.REGION.HasFlag(Region.America) && SciFier.REGION.HasFlag(Region.Australia) && SciFier.REGION.HasFlag(Region.Britain) && SciFier.REGION.HasFlag(Region.Canada) && SciFier.REGION.HasFlag(Region.Europe) && !SciFier.REGION.HasFlag(Region.Japan));

                Assert.That(!SpeedyHen.REGION.HasFlag(Region.America) && !SpeedyHen.REGION.HasFlag(Region.Australia) && SpeedyHen.REGION.HasFlag(Region.Britain) && !SpeedyHen.REGION.HasFlag(Region.Canada) && !SpeedyHen.REGION.HasFlag(Region.Europe) && !SpeedyHen.REGION.HasFlag(Region.Japan));

                Assert.That(!Waterstones.REGION.HasFlag(Region.America) && !Waterstones.REGION.HasFlag(Region.Australia) && Waterstones.REGION.HasFlag(Region.Britain) && !Waterstones.REGION.HasFlag(Region.Canada) && !Waterstones.REGION.HasFlag(Region.Europe) && !Waterstones.REGION.HasFlag(Region.Japan));

                // Assert.That(Wordery.REGION.HasFlag(Region.America) && Wordery.REGION.HasFlag(Region.Australia) && Wordery.REGION.HasFlag(Region.Britain) && Wordery.REGION.HasFlag(Region.Canada) && Wordery.REGION.HasFlag(Region.Europe) && !Wordery.REGION.HasFlag(Region.Japan));
            });
        }
    }
}