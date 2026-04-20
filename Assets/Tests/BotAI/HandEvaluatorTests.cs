using NUnit.Framework;
using PokerEngine.Core;

/// <summary>
/// Unit tests for HandEvaluator.
/// Requirements: 3.4, 3.5, 3.6, 3.8, 5.3, 9.5, 15.3
/// </summary>
[TestFixture]
public class HandEvaluatorTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Card C(Rank rank, Suit suit) => new Card(rank, suit);

    // -------------------------------------------------------------------------
    // Pre-flop tier lookup (Req 3.4, 3.5, 3.6, 3.8)
    // -------------------------------------------------------------------------

    [Test]
    public void GetHandTier_AA_ReturnsPremium()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts));
        Assert.AreEqual(HandTier.Premium, tier);
    }

    [Test]
    public void GetHandTier_KK_ReturnsPremium()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts));
        Assert.AreEqual(HandTier.Premium, tier);
    }

    [Test]
    public void GetHandTier_QQ_ReturnsPremium()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Queen, Suit.Spades), C(Rank.Queen, Suit.Hearts));
        Assert.AreEqual(HandTier.Premium, tier);
    }

    [Test]
    public void GetHandTier_AKSuited_ReturnsPremium()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Spades));
        Assert.AreEqual(HandTier.Premium, tier);
    }

    [Test]
    public void GetHandTier_JJ_ReturnsStrong()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Jack, Suit.Spades), C(Rank.Jack, Suit.Hearts));
        Assert.AreEqual(HandTier.Strong, tier);
    }

    [Test]
    public void GetHandTier_TT_ReturnsStrong()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Ten, Suit.Spades), C(Rank.Ten, Suit.Hearts));
        Assert.AreEqual(HandTier.Strong, tier);
    }

    [Test]
    public void GetHandTier_AKOffsuit_ReturnsStrong()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Hearts));
        Assert.AreEqual(HandTier.Strong, tier);
    }

    [Test]
    public void GetHandTier_AQSuited_ReturnsStrong()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Ace, Suit.Spades), C(Rank.Queen, Suit.Spades));
        Assert.AreEqual(HandTier.Strong, tier);
    }

    [Test]
    public void GetHandTier_99_ReturnsPlayable()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Nine, Suit.Spades), C(Rank.Nine, Suit.Hearts));
        Assert.AreEqual(HandTier.Playable, tier);
    }

    [Test]
    public void GetHandTier_87Suited_ReturnsPlayable()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Eight, Suit.Spades), C(Rank.Seven, Suit.Spades));
        Assert.AreEqual(HandTier.Playable, tier);
    }

    [Test]
    public void GetHandTier_72Offsuit_ReturnsTrash()
    {
        var tier = HandEvaluator.GetHandTier(C(Rank.Seven, Suit.Spades), C(Rank.Two, Suit.Hearts));
        Assert.AreEqual(HandTier.Trash, tier);
    }

    // -------------------------------------------------------------------------
    // HandKey order independence (Req 15.3)
    // -------------------------------------------------------------------------

    [Test]
    public void HandKey_OrderIndependence_AceKing()
    {
        var c1 = C(Rank.Ace, Suit.Spades);
        var c2 = C(Rank.King, Suit.Hearts);
        Assert.AreEqual(HandEvaluator.HandKey(c1, c2), HandEvaluator.HandKey(c2, c1));
    }

    [Test]
    public void HandKey_OrderIndependence_SuitedConnectors()
    {
        var c1 = C(Rank.Eight, Suit.Clubs);
        var c2 = C(Rank.Seven, Suit.Clubs);
        Assert.AreEqual(HandEvaluator.HandKey(c1, c2), HandEvaluator.HandKey(c2, c1));
    }

    [Test]
    public void HandKey_OrderIndependence_Pair()
    {
        var c1 = C(Rank.Queen, Suit.Spades);
        var c2 = C(Rank.Queen, Suit.Diamonds);
        Assert.AreEqual(HandEvaluator.HandKey(c1, c2), HandEvaluator.HandKey(c2, c1));
    }

    // -------------------------------------------------------------------------
    // TPTK classification (Req 5.3)
    // -------------------------------------------------------------------------

    [Test]
    public void ClassifyHandBucket_TPTK_ReturnsStrong()
    {
        // Board: A♠ 7♦ 2♣ — hole: A♥ K♦ → top pair (Ace) top kicker (King)
        var hole  = new[] { C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Diamonds) };
        var board = new[] { C(Rank.Ace, Suit.Spades), C(Rank.Seven, Suit.Diamonds), C(Rank.Two, Suit.Clubs) };

        var category = HandEvaluator.EvaluateHand(hole, board);
        var bucket   = HandEvaluator.ClassifyHandBucket(category, hole, board);

        Assert.AreEqual(HandStrengthBucket.Strong, bucket);
    }

    [Test]
    public void IsTopPairTopKicker_True_WhenHoleHasTopPairAndBestKicker()
    {
        // Board: A♠ 7♦ 2♣ — hole: A♥ K♦
        var hole  = new[] { C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Diamonds) };
        var board = new[] { C(Rank.Ace, Suit.Spades), C(Rank.Seven, Suit.Diamonds), C(Rank.Two, Suit.Clubs) };

        Assert.IsTrue(HandEvaluator.IsTopPairTopKicker(hole, board));
    }

    [Test]
    public void IsTopPairTopKicker_False_WhenKickerBeatenByBoard()
    {
        // Board: A♠ K♦ 2♣ — hole: A♥ Q♦ → top pair but kicker (Q) beaten by board K
        var hole  = new[] { C(Rank.Ace, Suit.Hearts), C(Rank.Queen, Suit.Diamonds) };
        var board = new[] { C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Diamonds), C(Rank.Two, Suit.Clubs) };

        Assert.IsFalse(HandEvaluator.IsTopPairTopKicker(hole, board));
    }

    // -------------------------------------------------------------------------
    // IsDryBoard (Req 9.5)
    // -------------------------------------------------------------------------

    [Test]
    public void IsDryBoard_True_ForRainbowWideSpread()
    {
        // A♠ 7♦ 2♣ — rainbow, spread = 14-2 = 12 ≥ 6
        var board = new[] { C(Rank.Ace, Suit.Spades), C(Rank.Seven, Suit.Diamonds), C(Rank.Two, Suit.Clubs) };
        Assert.IsTrue(HandEvaluator.IsDryBoard(board));
    }

    [Test]
    public void IsDryBoard_False_ForMonotoneBoard()
    {
        // 9♠ 8♠ 7♠ — three spades → not dry
        var board = new[] { C(Rank.Nine, Suit.Spades), C(Rank.Eight, Suit.Spades), C(Rank.Seven, Suit.Spades) };
        Assert.IsFalse(HandEvaluator.IsDryBoard(board));
    }

    [Test]
    public void IsDryBoard_False_ForConnectedBoard()
    {
        // 9♠ 8♦ 7♣ — rainbow but spread = 9-7 = 2 < 6
        var board = new[] { C(Rank.Nine, Suit.Spades), C(Rank.Eight, Suit.Diamonds), C(Rank.Seven, Suit.Clubs) };
        Assert.IsFalse(HandEvaluator.IsDryBoard(board));
    }
}
