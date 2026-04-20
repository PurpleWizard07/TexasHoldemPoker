using NUnit.Framework;
using PokerEngine.Core;
using UnityEngine;

/// <summary>
/// Unit tests for DecisionEngine.
/// Requirements: 7.3, 8.6, 9.1
/// </summary>
[TestFixture]
public class DecisionEngineTests
{
    private DecisionEngine _engine;

    [SetUp]
    public void SetUp()
    {
        _engine = new DecisionEngine();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Card C(Rank rank, Suit suit) => new Card(rank, suit);

    private static BotPersonality MakePersonality(int aggression, int tightness, int bluffiness)
    {
        var p = ScriptableObject.CreateInstance<BotPersonality>();
        p.aggression  = aggression;
        p.tightness   = tightness;
        p.bluffiness  = bluffiness;
        return p;
    }

    // -------------------------------------------------------------------------
    // Req 7.3 — Premium hand always raises pre-flop
    // -------------------------------------------------------------------------

    [Test]
    public void Decide_PremiumHand_PreFlop_AlwaysRaises()
    {
        // AA is Premium regardless of personality
        var ctx = new DecisionContext
        {
            holeCards      = new[] { C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts) },
            communityCards = new Card[0],
            currentStreet  = Street.PreFlop,
            potSize        = 10f,
            callAmount     = 0f,
            botStack       = 1000f,
            position       = Position.BTN,
            playersInHand  = 2,
            botIsPreFlopAggressor = false,
            opponentTags   = new[] { OpponentTag.Unknown },
            personality    = MakePersonality(5, 5, 5),
        };

        var result = _engine.Decide(ctx);
        Assert.AreEqual(PokerAction.Raise, result.action);
    }

    [Test]
    public void Decide_PremiumHand_PreFlop_RaisesWithAggression1()
    {
        // Even with lowest aggression, Premium always raises (Req 7.3)
        var ctx = new DecisionContext
        {
            holeCards      = new[] { C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts) },
            communityCards = new Card[0],
            currentStreet  = Street.PreFlop,
            potSize        = 10f,
            callAmount     = 0f,
            botStack       = 1000f,
            position       = Position.BTN,
            playersInHand  = 2,
            botIsPreFlopAggressor = false,
            opponentTags   = new[] { OpponentTag.Unknown },
            personality    = MakePersonality(1, 1, 1),
        };

        var result = _engine.Decide(ctx);
        Assert.AreEqual(PokerAction.Raise, result.action);
    }

    // -------------------------------------------------------------------------
    // Req 8.6 — Weak bucket with callAmount > 0 returns Fold
    // -------------------------------------------------------------------------

    [Test]
    public void Decide_WeakBucket_FacingBet_ReturnsFold()
    {
        // 7♠ 2♥ vs board A♠ K♦ Q♣ — high card, no draw → Weak bucket
        // With a callAmount > 0, must fold (Req 8.6)
        var ctx = new DecisionContext
        {
            holeCards      = new[] { C(Rank.Seven, Suit.Spades), C(Rank.Two, Suit.Hearts) },
            communityCards = new[] { C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs) },
            currentStreet  = Street.Flop,
            potSize        = 20f,
            callAmount     = 10f,   // facing a bet
            botStack       = 1000f,
            position       = Position.BTN,
            playersInHand  = 2,
            botIsPreFlopAggressor = false,
            opponentTags   = new[] { OpponentTag.Unknown },
            personality    = MakePersonality(5, 5, 5),
        };

        var result = _engine.Decide(ctx);
        Assert.AreEqual(PokerAction.Fold, result.action);
    }

    // -------------------------------------------------------------------------
    // Req 9.1 — C-bet fires on flop when botIsPreFlopAggressor=true and no bet
    // -------------------------------------------------------------------------

    [Test]
    public void Decide_CBet_FiresOnFlop_WhenPreFlopAggressor_NoBetToFace()
    {
        // With aggression=10, cBetThreshold = 1.0 + noise (min 0.95).
        // Random.Range(0,1) < 0.95 is true ~95% of the time.
        // Run 50 iterations — probability all fail = 0.05^50 ≈ 0, effectively guaranteed.
        var ctx = new DecisionContext
        {
            holeCards      = new[] { C(Rank.Seven, Suit.Spades), C(Rank.Two, Suit.Hearts) },
            communityCards = new[] { C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs) },
            currentStreet  = Street.Flop,
            potSize        = 20f,
            callAmount     = 0f,   // no bet to face
            botStack       = 1000f,
            position       = Position.BTN,
            playersInHand  = 2,
            botIsPreFlopAggressor = true,
            opponentTags   = new[] { OpponentTag.Unknown },
            personality    = MakePersonality(10, 5, 5),
        };

        bool cBetFired = false;
        for (int i = 0; i < 50; i++)
        {
            var result = _engine.Decide(ctx);
            if (result.action == PokerAction.Bet)
            {
                cBetFired = true;
                break;
            }
        }

        Assert.IsTrue(cBetFired, "C-bet should fire at least once in 50 attempts with aggression=10");
    }

    [Test]
    public void Decide_NoCBet_WhenNotPreFlopAggressor()
    {
        // When botIsPreFlopAggressor=false, c-bet logic is skipped entirely
        // With 7♠ 2♥ vs A♠ K♦ Q♣ and no bet, result should be Check (Weak bucket, no bluff with bluffiness=1)
        var ctx = new DecisionContext
        {
            holeCards      = new[] { C(Rank.Seven, Suit.Spades), C(Rank.Two, Suit.Hearts) },
            communityCards = new[] { C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs) },
            currentStreet  = Street.Flop,
            potSize        = 20f,
            callAmount     = 0f,
            botStack       = 1000f,
            position       = Position.BTN,
            playersInHand  = 2,
            botIsPreFlopAggressor = false,
            opponentTags   = new[] { OpponentTag.Unknown },
            personality    = MakePersonality(5, 5, 1), // bluffiness=1 → bluff prob very low
        };

        // With bluffiness=1: bluffProb = (1/10)*0.4 + noise = 0.04 + noise (max ~0.09)
        // Run 20 times — all should be Check (probability of any Bet ≈ 0.09^20 ≈ negligible)
        bool gotBet = false;
        for (int i = 0; i < 20; i++)
        {
            var result = _engine.Decide(ctx);
            if (result.action == PokerAction.Bet) gotBet = true;
        }
        // We can't assert never-bet due to randomness, but we can assert c-bet path is not taken
        // (c-bet would be Bet with cBetSize 0.33 or 0.66; bluff would also be Bet)
        // The key assertion is that without being pre-flop aggressor, the c-bet block is skipped.
        // We verify this by checking the action is NOT forced to Bet every time.
        Assert.IsFalse(gotBet, "Without being pre-flop aggressor and with bluffiness=1, should not bet in 20 attempts");
    }

    // -------------------------------------------------------------------------
    // Static sizing helpers (sanity checks)
    // -------------------------------------------------------------------------

    [Test]
    public void PreFlopRaiseSize_Aggression4_Returns3()
    {
        Assert.AreEqual(3.0f, DecisionEngine.PreFlopRaiseSize(4));
    }

    [Test]
    public void ValueBetSize_Aggression8_Returns075()
    {
        Assert.AreEqual(0.75f, DecisionEngine.ValueBetSize(8));
    }
}
