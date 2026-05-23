namespace Tests.Internal;

/// <summary>
/// Unit tests for <see cref="InternalHelpers.SortByVolume"/>.
///
/// The sort precomputes per-entry keys then sorts on cached arrays — these tests pin its
/// observable behaviour: vol numbers are compared numerically (not lexically), name grouping
/// trumps cross-series alphabetical order, box sets and mixed types fall through to string
/// compare on filtered text, and the sort mutates in place.
/// </summary>
[TestFixture, Description("Unit tests for InternalHelpers.SortByVolume")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public sealed class SortByVolumeTests
{
    private static EntryModel Make(string entry, string price = "$5.00") =>
        new(entry, price, StockStatus.IS, "TestSite");

    // ──────────────────────────────────────────────────────────────────────
    // Trivial inputs
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void Empty_NoOp()
    {
        List<EntryModel> data = [];
        data.SortByVolume();
        Assert.That(data, Is.Empty);
    }

    [Test]
    public void Single_NoOp()
    {
        List<EntryModel> data = [Make("Foo Vol 1")];
        data.SortByVolume();
        Assert.That(data, Has.Count.EqualTo(1));
        Assert.That(data[0].Entry, Is.EqualTo("Foo Vol 1"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Same-series Vol entries sort by parsed vol number
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void VolEntries_SortByVolumeNumber()
    {
        List<EntryModel> data =
        [
            Make("Foo Vol 3"),
            Make("Foo Vol 1"),
            Make("Foo Vol 2"),
        ];
        data.SortByVolume();
        Assert.That(data.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2", "Foo Vol 3" }));
    }

    [Test]
    public void VolumeOrdering_IsNumeric_NotLexical()
    {
        // The whole reason vol numbers parse to double: pure string sort would put
        // "Vol 10" before "Vol 2". Numeric compare puts them in real reading order.
        List<EntryModel> data =
        [
            Make("Foo Vol 10"),
            Make("Foo Vol 2"),
            Make("Foo Vol 1"),
        ];
        data.SortByVolume();
        Assert.That(data.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2", "Foo Vol 10" }));
    }

    [Test]
    public void DecimalVolumes_Supported()
    {
        // Volume numbers can be fractional ("Vol 1.5") — GetCurrentVolumeNum handles those
        // through the regex's double-capture group.
        List<EntryModel> data =
        [
            Make("Foo Vol 2"),
            Make("Foo Vol 1.5"),
            Make("Foo Vol 1"),
        ];
        data.SortByVolume();
        Assert.That(data.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 1.5", "Foo Vol 2" }));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Cross-series and mixed-type fall back to string compare on filtered text
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void DifferentSeries_SortAlphabetically()
    {
        // Names "Foo" vs "Bar" — Equals fails, Similar at threshold 0 also fails → fall back.
        List<EntryModel> data =
        [
            Make("Foo Vol 1"),
            Make("Bar Vol 1"),
        ];
        data.SortByVolume();
        Assert.That(data[0].Entry, Is.EqualTo("Bar Vol 1"));
        Assert.That(data[1].Entry, Is.EqualTo("Foo Vol 1"));
    }

    [Test]
    public void BoxSets_FallBackToStringSort()
    {
        // Box Set → GetCurrentVolumeNum returns -1 → vol-number path is rejected,
        // string compare on the filtered text decides the order.
        List<EntryModel> data =
        [
            Make("Foo Box Set 2"),
            Make("Bar Box Set 1"),
            Make("Foo Box Set 1"),
        ];
        data.SortByVolume();
        Assert.That(data.Select(e => e.Entry),
            Is.EqualTo(new[] { "Bar Box Set 1", "Foo Box Set 1", "Foo Box Set 2" }));
    }

    [Test]
    public void MixedVolAndBoxSet_StringCompareFallback()
    {
        // entryTypes differ (1 vs 2) → vol path rejected → ordinal compare on filtered text.
        // "Foo Box Set 1" precedes "Foo Vol 1" because 'B' < 'V'.
        List<EntryModel> data =
        [
            Make("Foo Vol 1"),
            Make("Foo Box Set 1"),
        ];
        data.SortByVolume();
        Assert.That(data[0].Entry, Is.EqualTo("Foo Box Set 1"));
        Assert.That(data[1].Entry, Is.EqualTo("Foo Vol 1"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Idempotence + in-place contract
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void AlreadySorted_RemainsSorted()
    {
        List<EntryModel> data =
        [
            Make("Foo Vol 1"),
            Make("Foo Vol 2"),
            Make("Foo Vol 3"),
        ];
        data.SortByVolume();
        Assert.That(data.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2", "Foo Vol 3" }));
    }

    [Test]
    public void SortsInPlace_SameInstance()
    {
        // The extension mutates the receiver — callers rely on this. If we ever changed it
        // to return a new list, every site's `data.SortByVolume()` would silently discard.
        List<EntryModel> data =
        [
            Make("Foo Vol 2"),
            Make("Foo Vol 1"),
        ];
        List<EntryModel> reference = data;
        data.SortByVolume();
        Assert.That(reference, Is.SameAs(data));
        Assert.That(data[0].Entry, Is.EqualTo("Foo Vol 1"));
    }

    [Test]
    public void DoubleSort_Idempotent()
    {
        // Two sorts in a row should produce the same result as one.
        List<EntryModel> data =
        [
            Make("Foo Vol 3"),
            Make("Foo Vol 1"),
            Make("Foo Vol 2"),
        ];
        data.SortByVolume();
        data.SortByVolume();
        Assert.That(data.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2", "Foo Vol 3" }));
    }
}
