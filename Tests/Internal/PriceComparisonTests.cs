using MasterScrapeTarget = MangaAndLightNovelWebScrape.MasterScrape;

namespace Tests.Internal;

/// <summary>
/// Unit tests for <see cref="MasterScrapeTarget.PriceComparison"/> — the cross-site merge that
/// keeps the cheaper price when both sites carry the same volume.
///
/// Each test feeds two synthetic per-site result lists and asserts on the merged output.
/// No network, no scraping — the merge is pure given its inputs.
///
/// Inputs follow the convention <c>"&lt;Series&gt; Vol N"</c> for volumes and <c>"&lt;Series&gt; Box Set N"</c>
/// for box sets, mirroring what <see cref="EntryModel.GetCurrentVolumeNum"/> expects.
/// </summary>
[TestFixture, Description("Unit tests for MasterScrape.PriceComparison")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public sealed class PriceComparisonTests
{
    private static EntryModel Make(string entry, string price, string website = "SiteA") =>
        new(entry, price, StockStatus.IS, website);

    // ──────────────────────────────────────────────────────────────────────
    // Trivial inputs
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void EmptySmaller_PassesBiggerThrough()
    {
        List<EntryModel> smaller = [];
        List<EntryModel> bigger =
        [
            Make("Foo Vol 1", "$5.00", "SiteB"),
            Make("Foo Vol 2", "$6.00", "SiteB"),
        ];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(2));
        Assert.That(merged.Select(e => e.Entry), Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2" }));
    }

    [Test]
    public void EmptyBigger_EmitsSmaller()
    {
        List<EntryModel> smaller = [Make("Foo Vol 1", "$5.00")];
        List<EntryModel> bigger = [];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(1));
        Assert.That(merged[0].Entry, Is.EqualTo("Foo Vol 1"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Cheaper-side wins per shared volume
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void SameVolume_KeepsCheaperPrice()
    {
        List<EntryModel> smaller = [Make("Foo Vol 1", "$5.00", "SiteA")];
        List<EntryModel> bigger = [Make("Foo Vol 1", "$10.00", "SiteB")];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(1));
        Assert.That(merged[0].Price, Is.EqualTo("$5.00"));
        Assert.That(merged[0].Website, Is.EqualTo("SiteA"));
    }

    [Test]
    public void SameVolume_BiggerCheaper_KeepsBigger()
    {
        List<EntryModel> smaller = [Make("Foo Vol 1", "$10.00", "SiteA")];
        List<EntryModel> bigger = [Make("Foo Vol 1", "$3.00", "SiteB")];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(1));
        Assert.That(merged[0].Price, Is.EqualTo("$3.00"));
        Assert.That(merged[0].Website, Is.EqualTo("SiteB"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Leftovers from both sides flow through
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void DisjointVolumes_AllPreserved()
    {
        // Smaller has Vol 1 and 3, bigger has Vol 2 and 4. No volume overlap.
        List<EntryModel> smaller =
        [
            Make("Foo Vol 1", "$5.00"),
            Make("Foo Vol 3", "$7.00"),
        ];
        List<EntryModel> bigger =
        [
            Make("Foo Vol 2", "$6.00"),
            Make("Foo Vol 4", "$8.00"),
        ];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(4));
        // VolumeSort orders by vol number when names match
        Assert.That(merged.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2", "Foo Vol 3", "Foo Vol 4" }));
    }

    [Test]
    public void Mixed_OverlapAndLeftovers()
    {
        // Vols 1+2 shared (cheaper picked), 3 from smaller only, 4 from bigger only.
        List<EntryModel> smaller =
        [
            Make("Foo Vol 1", "$5.00", "SiteA"),     // cheaper than bigger's Vol 1
            Make("Foo Vol 2", "$15.00", "SiteA"),    // more expensive than bigger's Vol 2
            Make("Foo Vol 3", "$7.00", "SiteA"),     // smaller-only
        ];
        List<EntryModel> bigger =
        [
            Make("Foo Vol 1", "$8.00", "SiteB"),
            Make("Foo Vol 2", "$10.00", "SiteB"),    // cheaper than smaller's Vol 2
            Make("Foo Vol 4", "$9.00", "SiteB"),     // bigger-only
        ];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(4));
        Assert.That(merged.Single(e => e.Entry == "Foo Vol 1").Website, Is.EqualTo("SiteA"));
        Assert.That(merged.Single(e => e.Entry == "Foo Vol 2").Website, Is.EqualTo("SiteB"));
        Assert.That(merged.Single(e => e.Entry == "Foo Vol 3").Website, Is.EqualTo("SiteA"));
        Assert.That(merged.Single(e => e.Entry == "Foo Vol 4").Website, Is.EqualTo("SiteB"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // "Imperfect" sentinel filters smaller-side candidates out of matching
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void ImperfectEntryInSmaller_DoesNotMatch_StillPassesThrough()
    {
        // Smaller has an Imperfect entry. It can't match bigger's Vol 1 — but it still
        // shows up in the final list as an unmatched leftover.
        List<EntryModel> smaller = [Make("Foo Vol 1 Imperfect", "$1.00")];
        List<EntryModel> bigger = [Make("Foo Vol 1", "$10.00")];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        // Both rows survive — they're treated as distinct volumes because Imperfect
        // disqualifies the smaller from matching.
        Assert.That(merged, Has.Count.EqualTo(2));
        Assert.That(merged.Any(e => e.Entry == "Foo Vol 1"), Is.True);
        Assert.That(merged.Any(e => e.Entry == "Foo Vol 1 Imperfect"), Is.True);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Box Sets — multiple entries share vol = -1; per-bucket linear scan still works
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void BoxSets_MatchByNameSimilarity()
    {
        // Box Sets all hash to vol = -1, so the dict bucket holds both. The name-similarity
        // check is what distinguishes them within the bucket.
        List<EntryModel> smaller =
        [
            Make("Naruto Box Set 1", "$50.00"),
            Make("Naruto Box Set 2", "$60.00"),
        ];
        List<EntryModel> bigger =
        [
            Make("Naruto Box Set 1", "$45.00"),
            Make("Naruto Box Set 2", "$70.00"),
        ];

        List<EntryModel> merged = MasterScrapeTarget.PriceComparison(smaller, bigger);

        Assert.That(merged, Has.Count.EqualTo(2));
        Assert.That(merged.Single(e => e.Entry == "Naruto Box Set 1").Price, Is.EqualTo("$45.00"));
        Assert.That(merged.Single(e => e.Entry == "Naruto Box Set 2").Price, Is.EqualTo("$60.00"));
    }
}
