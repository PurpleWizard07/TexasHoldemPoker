using NUnit.Framework;

/// <summary>
/// Unit tests for TableReadTracker.
/// Requirements: 10.3, 10.7
/// </summary>
[TestFixture]
public class TableReadTrackerTests
{
    private TableReadTracker _tracker;

    [SetUp]
    public void SetUp()
    {
        _tracker = new TableReadTracker();
    }

    // -------------------------------------------------------------------------
    // Req 10.3 — Unknown returned before 6 observations
    // -------------------------------------------------------------------------

    [Test]
    public void GetTag_ReturnsUnknown_BeforeAnyObservations()
    {
        Assert.AreEqual(OpponentTag.Unknown, _tracker.GetTag(0));
    }

    [Test]
    public void GetTag_ReturnsUnknown_After5Observations()
    {
        // Record 5 hands (one short of the 6-observation minimum)
        for (int i = 0; i < 5; i++)
            _tracker.RecordHand(0, true);

        Assert.AreEqual(OpponentTag.Unknown, _tracker.GetTag(0));
    }

    [Test]
    public void GetTag_ReturnsNonUnknown_After6Observations_AllLoose()
    {
        // 6 voluntary entries → VPIP = 6/8 = 0.75 ≥ 0.60 → Loose
        for (int i = 0; i < 6; i++)
            _tracker.RecordHand(0, true);

        Assert.AreEqual(OpponentTag.Loose, _tracker.GetTag(0));
    }

    [Test]
    public void GetTag_ReturnsTight_After8Observations_AllFolded()
    {
        // 8 non-voluntary entries → VPIP = 0/8 = 0.0 ≤ 0.30 → Tight
        for (int i = 0; i < 8; i++)
            _tracker.RecordHand(0, false);

        Assert.AreEqual(OpponentTag.Tight, _tracker.GetTag(0));
    }

    [Test]
    public void GetTag_ReturnsLoose_After8Observations_AllVoluntary()
    {
        // 8 voluntary entries → VPIP = 8/8 = 1.0 ≥ 0.60 → Loose
        for (int i = 0; i < 8; i++)
            _tracker.RecordHand(0, true);

        Assert.AreEqual(OpponentTag.Loose, _tracker.GetTag(0));
    }

    [Test]
    public void GetTag_ReturnsUnknown_WhenVpipBetweenThresholds()
    {
        // 4 voluntary out of 8 → VPIP = 0.50 → between 0.30 and 0.60 → Unknown
        for (int i = 0; i < 8; i++)
            _tracker.RecordHand(0, i < 4);

        Assert.AreEqual(OpponentTag.Unknown, _tracker.GetTag(0));
    }

    // -------------------------------------------------------------------------
    // Req 10.7 — ResetSeat clears history and returns Unknown
    // -------------------------------------------------------------------------

    [Test]
    public void ResetSeat_ClearsHistory_ReturnsUnknown()
    {
        // Build up enough observations to get a tag
        for (int i = 0; i < 8; i++)
            _tracker.RecordHand(1, true);

        Assert.AreEqual(OpponentTag.Loose, _tracker.GetTag(1), "Pre-condition: should be Loose before reset");

        _tracker.ResetSeat(1);

        Assert.AreEqual(OpponentTag.Unknown, _tracker.GetTag(1), "After reset, should return Unknown");
    }

    [Test]
    public void ResetSeat_OnUnknownSeat_DoesNotThrow()
    {
        // Resetting a seat that was never recorded should not throw
        Assert.DoesNotThrow(() => _tracker.ResetSeat(99));
        Assert.AreEqual(OpponentTag.Unknown, _tracker.GetTag(99));
    }

    [Test]
    public void ResetSeat_AllowsReaccumulation()
    {
        // Record 8 hands, reset, then record 5 more — should still be Unknown (< 6 obs)
        for (int i = 0; i < 8; i++)
            _tracker.RecordHand(2, true);

        _tracker.ResetSeat(2);

        for (int i = 0; i < 5; i++)
            _tracker.RecordHand(2, true);

        Assert.AreEqual(OpponentTag.Unknown, _tracker.GetTag(2));
    }
}
