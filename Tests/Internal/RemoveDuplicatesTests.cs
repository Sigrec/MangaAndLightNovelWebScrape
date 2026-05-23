using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Internal;

/// <summary>
/// Unit tests for <see cref="InternalHelpers.RemoveDuplicates"/>.
///
/// The dedup is internal and works against in-process <see cref="EntryModel"/> lists — no network,
/// no live sites. Each test builds a list, calls the extension, and asserts on the resulting
/// contents. Runs in milliseconds.
///
/// Pin a new case here whenever a real-world dupe pattern slips through in production.
/// </summary>
[TestFixture, Description("Unit tests for InternalHelpers.RemoveDuplicates")]
[Author("Sean (Alias -> Prem or Sigrec)")]
[SetUICulture("en")]
public sealed class RemoveDuplicatesTests
{
    private static readonly ILogger Logger = NullLogger.Instance;

    private static EntryModel Make(string entry, string price) =>
        new(entry, price, StockStatus.IS, "TestSite");

    // ──────────────────────────────────────────────────────────────────────
    // Trivial inputs — must not crash, must not mutate
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void Empty_NoOp()
    {
        List<EntryModel> input = [];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Is.Empty);
    }

    [Test]
    public void Single_NoOp()
    {
        List<EntryModel> input = [Make("Foo Vol 1", "$5.00")];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(1));
        Assert.That(input[0].Entry, Is.EqualTo("Foo Vol 1"));
    }

    [Test]
    public void NoDupes_Unchanged()
    {
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$5.00"),
            Make("Foo Vol 2", "$6.00"),
            Make("Foo Vol 3", "$7.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input.Select(e => e.Entry),
            Is.EqualTo(new[] { "Foo Vol 1", "Foo Vol 2", "Foo Vol 3" }));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Core contract — keep the cheaper of each colliding pair
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void AdjacentDupes_KeepsCheaper()
    {
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$10.00"),
            Make("Foo Vol 1", "$5.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(1));
        Assert.That(input[0].Price, Is.EqualTo("$5.00"));
    }

    [Test]
    public void NonAdjacentDupes_KeepsCheaper()
    {
        // This is the case the old adjacent-only algorithm leaked entirely — the dupe
        // sat across a non-matching entry, so the [x] vs [x-1] pair check never fired.
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$10.00"),
            Make("Bar Vol 2", "$8.00"),
            Make("Foo Vol 1", "$5.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(2));
        Assert.That(input.Single(e => e.Entry == "Foo Vol 1").Price, Is.EqualTo("$5.00"));
        Assert.That(input.Any(e => e.Entry == "Bar Vol 2"), Is.True);
    }

    [Test]
    public void ThreeWayCollision_KeepsCheapest()
    {
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$10.00"),
            Make("Foo Vol 1", "$3.00"),    // cheapest
            Make("Foo Vol 1", "$7.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(1));
        Assert.That(input[0].Price, Is.EqualTo("$3.00"));
    }

    [Test]
    public void MultipleSeparateDupePairs_AllResolved()
    {
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$10.00"),
            Make("Bar Vol 1", "$20.00"),
            Make("Foo Vol 1", "$5.00"),     // cheaper duplicate of "Foo Vol 1"
            Make("Bar Vol 1", "$15.00"),    // cheaper duplicate of "Bar Vol 1"
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(2));
        Assert.That(input.Single(e => e.Entry == "Foo Vol 1").Price, Is.EqualTo("$5.00"));
        Assert.That(input.Single(e => e.Entry == "Bar Vol 1").Price, Is.EqualTo("$15.00"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Tie-break — predicate is strict-less-than, so equal-priced dupes keep first seen
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void TiedPrices_KeepsFirstSeen()
    {
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$5.00"),  // first seen wins because predicate is `<`, not `<=`
            Make("Foo Vol 1", "$5.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(1));
        Assert.That(input[0].Price, Is.EqualTo("$5.00"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Matching key uses OrdinalIgnoreCase — title casing variants collide
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void CaseInsensitive_TreatedAsDupe()
    {
        List<EntryModel> input =
        [
            Make("foo vol 1", "$10.00"),
            Make("FOO VOL 1", "$5.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(1));
        Assert.That(input[0].Price, Is.EqualTo("$5.00"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Compaction preserves survivor order
    // ──────────────────────────────────────────────────────────────────────

    [Test]
    public void RelativeOrderPreserved_WhenNoDupes()
    {
        List<EntryModel> input =
        [
            Make("A", "$1.00"),
            Make("B", "$2.00"),
            Make("C", "$3.00"),
            Make("D", "$4.00"),
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input.Select(e => e.Entry), Is.EqualTo(new[] { "A", "B", "C", "D" }));
    }

    [Test]
    public void DupeAtFirstPosition_PreservesSurvivorOrder()
    {
        // The first-position entry is the loser (more expensive). The compacting pass
        // has to handle removal at index 0 without disturbing the order of remaining entries.
        List<EntryModel> input =
        [
            Make("Foo Vol 1", "$10.00"),  // doomed
            Make("Bar Vol 1", "$2.00"),
            Make("Foo Vol 1", "$3.00"),   // cheaper — kept
        ];
        input.RemoveDuplicates(Logger);
        Assert.That(input, Has.Count.EqualTo(2));
        Assert.That(input[0].Entry, Is.EqualTo("Bar Vol 1"));
        Assert.That(input[1].Entry, Is.EqualTo("Foo Vol 1"));
        Assert.That(input[1].Price, Is.EqualTo("$3.00"));
    }
}
