using System;
using System.Collections.Generic;
using System.Linq;
using PokerEngine.Core;
using PokerEngine.Rules;

/// <summary>
/// Static class responsible for pre-flop tier lookup, post-flop hand bucket
/// classification, and draw detection / out counting.
/// Requirements: 3.1–3.8, 4.1–4.4, 5.1–5.7, 6.1, 9.5, 15.1–15.3
/// </summary>
public static class HandEvaluator
{
    // -------------------------------------------------------------------------
    // Rank character map (Req 3.3)
    // -------------------------------------------------------------------------
    private static readonly Dictionary<Rank, char> RankChar = new Dictionary<Rank, char>
    {
        { Rank.Ace,   'A' },
        { Rank.King,  'K' },
        { Rank.Queen, 'Q' },
        { Rank.Jack,  'J' },
        { Rank.Ten,   'T' },
        { Rank.Nine,  '9' },
        { Rank.Eight, '8' },
        { Rank.Seven, '7' },
        { Rank.Six,   '6' },
        { Rank.Five,  '5' },
        { Rank.Four,  '4' },
        { Rank.Three, '3' },
        { Rank.Two,   '2' },
    };

    // Numeric value for rank comparison (higher = better)
    private static readonly Dictionary<Rank, int> RankValue = new Dictionary<Rank, int>
    {
        { Rank.Two,   2  },
        { Rank.Three, 3  },
        { Rank.Four,  4  },
        { Rank.Five,  5  },
        { Rank.Six,   6  },
        { Rank.Seven, 7  },
        { Rank.Eight, 8  },
        { Rank.Nine,  9  },
        { Rank.Ten,   10 },
        { Rank.Jack,  11 },
        { Rank.Queen, 12 },
        { Rank.King,  13 },
        { Rank.Ace,   14 },
    };

    // -------------------------------------------------------------------------
    // Pre-flop tier lookup (Req 3.2 — hardcoded static dictionary)
    // -------------------------------------------------------------------------
    private static readonly Dictionary<string, HandTier> TierLookup =
        new Dictionary<string, HandTier>
    {
        // Premium (Req 3.4)
        { "AAs", HandTier.Premium },
        { "KKs", HandTier.Premium },
        { "QQs", HandTier.Premium },
        { "AKs", HandTier.Premium },

        // Strong (Req 3.5)
        { "JJs", HandTier.Strong },
        { "TTs", HandTier.Strong },
        { "AKo", HandTier.Strong },
        { "AQs", HandTier.Strong },
        { "AJs", HandTier.Strong },
        { "ATs", HandTier.Strong },
        { "KQs", HandTier.Strong },

        // Playable — pairs 99–22 (Req 3.6)
        { "99s", HandTier.Playable },
        { "88s", HandTier.Playable },
        { "77s", HandTier.Playable },
        { "66s", HandTier.Playable },
        { "55s", HandTier.Playable },
        { "44s", HandTier.Playable },
        { "33s", HandTier.Playable },
        { "22s", HandTier.Playable },
        // Playable — offsuit broadways / suited connectors (Req 3.6)
        { "AQo", HandTier.Playable },
        { "AJo", HandTier.Playable },
        { "ATo", HandTier.Playable },
        { "A9s", HandTier.Playable },
        { "A8s", HandTier.Playable },
        { "KQo", HandTier.Playable },
        { "KJs", HandTier.Playable },
        { "KTs", HandTier.Playable },
        { "QJs", HandTier.Playable },
        { "JTs", HandTier.Playable },
        { "T9s", HandTier.Playable },
        { "98s", HandTier.Playable },
        { "87s", HandTier.Playable },
        { "76s", HandTier.Playable },
        { "65s", HandTier.Playable },
        { "54s", HandTier.Playable },

        // Marginal — offsuit aces (Req 3.7)
        { "A8o", HandTier.Marginal },
        { "A7o", HandTier.Marginal },
        { "A6o", HandTier.Marginal },
        { "A5o", HandTier.Marginal },
        { "A4o", HandTier.Marginal },
        { "A3o", HandTier.Marginal },
        { "A2o", HandTier.Marginal },
        // Marginal — suited aces (Req 3.7)
        { "A7s", HandTier.Marginal },
        { "A6s", HandTier.Marginal },
        { "A5s", HandTier.Marginal },
        { "A4s", HandTier.Marginal },
        { "A3s", HandTier.Marginal },
        { "A2s", HandTier.Marginal },
        // Marginal — suited gappers (Req 3.7)
        { "K9s", HandTier.Marginal },
        { "Q9s", HandTier.Marginal },
        { "J9s", HandTier.Marginal },
        { "T8s", HandTier.Marginal },
        { "97s", HandTier.Marginal },
        { "86s", HandTier.Marginal },
        { "75s", HandTier.Marginal },
        { "64s", HandTier.Marginal },
        { "53s", HandTier.Marginal },
    };

    // Singleton wrapper instance for delegating to PokerEngine.dll
    private static readonly HandEvaluatorWrapper _wrapper = new HandEvaluatorWrapper();

    // -------------------------------------------------------------------------
    // HandKey — canonical form (Req 3.3, 15.1, 15.2, 15.3)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns the canonical hand key string, e.g. "AKs", "72o", "AAs".
    /// Higher rank always comes first. Pairs always use 's' suffix.
    /// Order of c1/c2 does not matter (Req 15.3).
    /// </summary>
    public static string HandKey(Card c1, Card c2)
    {
        int v1 = RankValue[c1.Rank];
        int v2 = RankValue[c2.Rank];

        // Ensure high card first
        Card high = v1 >= v2 ? c1 : c2;
        Card low  = v1 >= v2 ? c2 : c1;

        char highChar = RankChar[high.Rank];
        char lowChar  = RankChar[low.Rank];

        bool isPair  = high.Rank == low.Rank;
        bool isSuited = high.Suit == low.Suit;

        // Pairs always use 's' suffix (design doc)
        char suffix = (isPair || isSuited) ? 's' : 'o';

        return $"{highChar}{lowChar}{suffix}";
    }

    // -------------------------------------------------------------------------
    // GetHandTier — pre-flop tier lookup (Req 3.1, 3.2, 3.4–3.8)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns the HandTier for the given two hole cards.
    /// Uses a hardcoded static dictionary; never calculates at runtime (Req 3.2).
    /// </summary>
    public static HandTier GetHandTier(Card c1, Card c2)
    {
        string key = HandKey(c1, c2);
        return TierLookup.TryGetValue(key, out HandTier tier) ? tier : HandTier.Trash;
    }

    // -------------------------------------------------------------------------
    // GetMinimumPlayableTier — position-adjusted minimum tier (Req 4.1–4.4)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns the minimum playable HandTier for the given tightness and position.
    /// </summary>
    public static HandTier GetMinimumPlayableTier(int tightness, Position pos)
    {
        // Base tier from tightness (Req 4.1)
        int baseTier;
        if (tightness >= 8)
            baseTier = (int)HandTier.Strong;   // 2
        else if (tightness >= 4)
            baseTier = (int)HandTier.Playable; // 3
        else
            baseTier = (int)HandTier.Marginal; // 4

        // Position adjustment
        int adjusted;
        switch (pos)
        {
            case Position.UTG:
            case Position.UTG1:
                // Tighten by one step, min Premium=1 (Req 4.2)
                adjusted = Math.Max((int)HandTier.Premium, baseTier - 1);
                break;
            case Position.CO:
            case Position.BTN:
            case Position.SB:
            case Position.BB:
                // Loosen by one step, max Marginal=4 (Req 4.3)
                adjusted = Math.Min((int)HandTier.Marginal, baseTier + 1);
                break;
            default:
                // MP / MP1 — no adjustment (Req 4.4)
                adjusted = baseTier;
                break;
        }

        return (HandTier)adjusted;
    }

    // -------------------------------------------------------------------------
    // EvaluateHand — delegate to PokerEngine.dll (Req 5.1)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Evaluates the best 5-card hand from hole + board cards.
    /// Returns the HandCategory from PokerEngine.dll.
    /// </summary>
    public static HandCategory EvaluateHand(Card[] hole, Card[] board)
    {
        return _wrapper.GetHandCategory(hole, board);
    }

    // -------------------------------------------------------------------------
    // ClassifyHandBucket — post-flop bucket (Req 5.1–5.7)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Maps a HandCategory (from EvaluateHand) to a HandStrengthBucket.
    /// Draw is evaluated BEFORE Weak (Req 5.7).
    /// </summary>
    public static HandStrengthBucket ClassifyHandBucket(HandCategory rank, Card[] hole, Card[] board)
    {
        // Req 5.2 — two pair or better → Strong
        if (rank <= HandCategory.TwoPair)
            return HandStrengthBucket.Strong;

        if (rank == HandCategory.OnePair)
        {
            // Req 5.3 — TPTK → Strong
            if (IsTopPairTopKicker(hole, board))
                return HandStrengthBucket.Strong;

            // Req 5.4 — other one pair → Medium
            return HandStrengthBucket.Medium;
        }

        // rank == HandCategory.HighCard (no pair)
        // Req 5.7 — check Draw BEFORE Weak
        if (HasFlushDraw(hole, board) || HasOpenEndedStraightDraw(hole, board))
            return HandStrengthBucket.Draw; // Req 5.6

        return HandStrengthBucket.Weak; // Req 5.5
    }

    // -------------------------------------------------------------------------
    // IsTopPairTopKicker (Req 5.3)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns true when the hole cards make one pair using the board's highest
    /// card, and the remaining hole card is the best possible kicker given the board.
    /// </summary>
    public static bool IsTopPairTopKicker(Card[] hole, Card[] board)
    {
        if (hole == null || board == null || hole.Length < 2 || board.Length == 0)
            return false;

        // Find the highest-ranked card on the board
        Rank topBoardRank = board.Select(c => c.Rank).OrderByDescending(r => RankValue[r]).First();

        // One of the hole cards must match the top board rank
        Card? pairHoleCard = null;
        Card? kickerHoleCard = null;

        foreach (var hc in hole)
        {
            if (hc.Rank == topBoardRank && pairHoleCard == null)
                pairHoleCard = hc;
            else
                kickerHoleCard = hc;
        }

        if (pairHoleCard == null || kickerHoleCard == null)
            return false;

        // The kicker must be the best possible kicker:
        // No card on the board or in the remaining hole cards beats it
        // (i.e., no board card has a higher rank than the kicker, excluding the top pair rank)
        int kickerValue = RankValue[kickerHoleCard.Value.Rank];

        foreach (var bc in board)
        {
            if (bc.Rank == topBoardRank) continue; // skip the paired rank
            if (RankValue[bc.Rank] > kickerValue)
                return false; // a board card outranks our kicker
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // HasFlushDraw (Req 5.6, 15.x)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns true when exactly 4 cards of the same suit exist across hole + board.
    /// </summary>
    public static bool HasFlushDraw(Card[] hole, Card[] board)
    {
        if (hole == null || board == null) return false;

        var allCards = hole.Concat(board);
        var suitCounts = allCards.GroupBy(c => c.Suit).Select(g => g.Count());
        return suitCounts.Any(count => count == 4);
    }

    // -------------------------------------------------------------------------
    // HasOpenEndedStraightDraw (Req 5.6)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns true when there are 4 consecutive distinct ranks across hole + board
    /// with outs available on BOTH ends (excludes A-2-3-4 and J-Q-K-A one-ended draws).
    /// </summary>
    public static bool HasOpenEndedStraightDraw(Card[] hole, Card[] board)
    {
        if (hole == null || board == null) return false;

        var allCards = hole.Concat(board);
        // Get distinct rank values, sorted
        var rankValues = allCards
            .Select(c => RankValue[c.Rank])
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        // Look for any 4 consecutive distinct ranks
        for (int i = 0; i <= rankValues.Count - 4; i++)
        {
            int r0 = rankValues[i];
            int r1 = rankValues[i + 1];
            int r2 = rankValues[i + 2];
            int r3 = rankValues[i + 3];

            if (r1 == r0 + 1 && r2 == r0 + 2 && r3 == r0 + 3)
            {
                // Must have outs on BOTH ends: rank below r0 and rank above r3
                // Ranks run 2–14 (Ace). Ace can also be low (1) but we treat it as 14 only.
                bool lowEnd  = r0 > 2;  // there's a rank below (2 is the lowest)
                bool highEnd = r3 < 14; // there's a rank above (Ace=14 is the highest)

                if (lowEnd && highEnd)
                    return true;
            }
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // CountDrawOuts (Req 6.1)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns the number of outs for a drawing hand.
    /// Flush draw = 9 outs, OESD = 8 outs, both = 15 outs.
    /// </summary>
    public static int CountDrawOuts(Card[] hole, Card[] board)
    {
        bool flush = HasFlushDraw(hole, board);
        bool oesd  = HasOpenEndedStraightDraw(hole, board);

        if (flush && oesd) return 15;
        if (flush)         return 9;
        if (oesd)          return 8;
        return 0;
    }

    // -------------------------------------------------------------------------
    // IsDryBoard (Req 9.5)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Returns true when no suit appears 3+ times among board cards AND
    /// the spread between the highest and lowest rank is 6 or greater.
    /// </summary>
    public static bool IsDryBoard(Card[] board)
    {
        if (board == null || board.Length == 0) return false;

        // No suit appears 3+ times
        bool noFlushDraw = board
            .GroupBy(c => c.Suit)
            .All(g => g.Count() < 3);

        // Rank spread >= 6
        int maxRank = board.Max(c => RankValue[c.Rank]);
        int minRank = board.Min(c => RankValue[c.Rank]);
        bool wideSpread = (maxRank - minRank) >= 6;

        return noFlushDraw && wideSpread;
    }
}
