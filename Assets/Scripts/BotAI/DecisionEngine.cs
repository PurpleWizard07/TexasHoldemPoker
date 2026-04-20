using UnityEngine;

/// <summary>
/// Stateless decision engine. Receives a DecisionContext and returns a BotAction.
/// All state is held externally in BotController / TableReadTracker / DecisionContext.
/// Requirements: 6.2–6.4, 7.1–7.10, 8.1–8.12, 9.1–9.5, 11.1–11.5
/// </summary>
public class DecisionEngine
{
    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Routes to pre-flop or post-flop decision logic based on the current street.
    /// </summary>
    public BotAction Decide(DecisionContext ctx)
    {
        if (ctx.currentStreet == Street.PreFlop)
            return DecidePreFlop(ctx);

        return DecidePostFlop(ctx);
    }

    // -------------------------------------------------------------------------
    // Pre-flop (Req 7.1–7.10)
    // -------------------------------------------------------------------------

    private BotAction DecidePreFlop(DecisionContext ctx)
    {
        int aggression  = ctx.personality.aggression;
        int tightness   = ctx.personality.tightness;
        int bluffiness  = ctx.personality.bluffiness;

        HandTier tier    = HandEvaluator.GetHandTier(ctx.holeCards[0], ctx.holeCards[1]);
        HandTier minTier = HandEvaluator.GetMinimumPlayableTier(tightness, ctx.position);

        // Req 7.1 / 7.2 — hand worse than minimum playable tier
        if (tier > minTier)
        {
            // Req 7.2 — high bluffiness can limp with trash when no bet to face
            if (bluffiness >= 8 && ctx.callAmount <= 0f && Random.Range(0f, 1f) < 0.08f)
                return Action(PokerAction.Call);

            // Req 7.1 — fold
            return Action(PokerAction.Fold);
        }

        // Req 7.3 — Premium → always raise
        if (tier == HandTier.Premium)
            return Action(PokerAction.Raise, PreFlopRaiseSize(aggression));

        // Req 7.4 / 7.5 — Strong
        if (tier == HandTier.Strong)
        {
            float raiseSize = ctx.callAmount > 0f
                ? PreFlopRaiseSize(aggression) * 3f   // Req 7.4 — 3-bet facing a raise
                : PreFlopRaiseSize(aggression);        // Req 7.5 — standard raise
            return Action(PokerAction.Raise, raiseSize);
        }

        // Req 7.6 / 7.7 — Playable
        if (tier == HandTier.Playable)
        {
            if (ctx.callAmount <= 0f)
            {
                // Req 7.6 — raise with probability aggression/10 (±0.05 noise)
                float threshold = (aggression / 10f) + Noise();
                if (Random.Range(0f, 1f) < threshold)
                    return Action(PokerAction.Raise, PreFlopRaiseSize(aggression));
                return Action(PokerAction.Call);
            }
            // Req 7.7 — facing a bet → call
            return Action(PokerAction.Call);
        }

        // Req 7.8 — Marginal (only reachable for loose bots in late position) → call
        return Action(PokerAction.Call);
    }

    // -------------------------------------------------------------------------
    // Post-flop (Req 8.1–8.12, 9.1–9.5)
    // -------------------------------------------------------------------------

    private BotAction DecidePostFlop(DecisionContext ctx)
    {
        int aggression = ctx.personality.aggression;

        // Req 9.1 — check c-bet opportunity BEFORE bucket logic
        if (ctx.currentStreet == Street.Flop && ctx.botIsPreFlopAggressor && ctx.callAmount <= 0f)
        {
            float cBetThreshold = (aggression / 10f) + Noise(); // Req 9.2
            if (Random.Range(0f, 1f) < cBetThreshold)
            {
                // Req 9.3 / 9.4 — dry vs wet board sizing
                float cBetSize = HandEvaluator.IsDryBoard(ctx.communityCards) ? 0.33f : 0.66f;
                return Action(PokerAction.Bet, cBetSize);
            }
        }

        // Classify post-flop bucket
        var category = HandEvaluator.EvaluateHand(ctx.holeCards, ctx.communityCards);
        HandStrengthBucket bucket = HandEvaluator.ClassifyHandBucket(category, ctx.holeCards, ctx.communityCards);

        // Apply exploit adjustments (Req 11.1–11.5)
        OpponentTag tag = GetActiveOpponentTag(ctx);

        switch (bucket)
        {
            case HandStrengthBucket.Strong:
                return DecideStrong(ctx);

            case HandStrengthBucket.Medium:
                return DeciideMedium(ctx, tag);

            case HandStrengthBucket.Weak:
                return DecideWeak(ctx, tag);

            case HandStrengthBucket.Draw:
                return DeciideDraw(ctx);

            default:
                return Action(PokerAction.Check);
        }
    }

    // -------------------------------------------------------------------------
    // Bucket handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Strong bucket — bet/raise with value bet size. (Req 8.1, 8.2)
    /// </summary>
    private BotAction DecideStrong(DecisionContext ctx)
    {
        float size = ValueBetSize(ctx.personality.aggression);
        return ctx.callAmount > 0f
            ? Action(PokerAction.Raise, size)  // Req 8.1
            : Action(PokerAction.Bet,   size); // Req 8.2
    }

    /// <summary>
    /// Medium bucket — call/fold/bet/check per aggression and pot odds. (Req 8.3–8.5, 11.2, 11.4)
    /// </summary>
    private BotAction DeciideMedium(DecisionContext ctx, OpponentTag tag)
    {
        int aggression = ctx.personality.aggression;

        if (ctx.callAmount > 0f)
        {
            // Req 11.4 — tight opponent → fold medium facing a bet
            if (tag == OpponentTag.Tight)
                return Action(PokerAction.Fold);

            // Req 8.3 — call if pot odds are favourable (callAmount / (pot + callAmount) < 0.35)
            float potOdds = ctx.callAmount / (ctx.potSize + ctx.callAmount);
            return potOdds < 0.35f
                ? Action(PokerAction.Call)
                : Action(PokerAction.Fold);
        }

        // No bet to face
        // Req 11.2 — loose opponent → value bet aggressively
        if (tag == OpponentTag.Loose)
            return Action(PokerAction.Bet, ValueBetSize(aggression));

        // Req 8.4 — aggression >= 6 → pot-control bet
        if (aggression >= 6)
        {
            float potControlSize = 0.25f + (aggression - 6) * 0.05f;
            return Action(PokerAction.Bet, potControlSize);
        }

        // Req 8.5 — aggression < 6 → check
        return Action(PokerAction.Check);
    }

    /// <summary>
    /// Weak bucket — fold to bet; bluff-check logic with noise. (Req 8.6, 8.7, 11.1, 11.3)
    /// </summary>
    private BotAction DecideWeak(DecisionContext ctx, OpponentTag tag)
    {
        int bluffiness = ctx.personality.bluffiness;

        if (ctx.callAmount > 0f)
            return Action(PokerAction.Fold); // Req 8.6

        // No bet to face — consider bluffing
        // Req 11.1 — loose opponent → suppress bluffing
        if (tag == OpponentTag.Loose)
            return Action(PokerAction.Check);

        // Req 11.3 — tight opponent → multiply bluff probability by 1.5
        float bluffMult = (tag == OpponentTag.Tight) ? 1.5f : 1.0f;
        float bluffProb = (bluffiness / 10f) * 0.4f * bluffMult + Noise(); // Req 8.7

        if (Random.Range(0f, 1f) < bluffProb)
            return Action(PokerAction.Bet, BluffBetSize(ctx.personality.aggression));

        return Action(PokerAction.Check);
    }

    /// <summary>
    /// Draw bucket — equity vs pot odds; semi-bluff raise/bet paths. (Req 8.8–8.10)
    /// </summary>
    private BotAction DeciideDraw(DecisionContext ctx)
    {
        int aggression = ctx.personality.aggression;
        int outs       = HandEvaluator.CountDrawOuts(ctx.holeCards, ctx.communityCards);
        float equity   = EstimateEquity(outs, ctx.currentStreet);

        if (ctx.callAmount > 0f)
        {
            float potOdds = ctx.callAmount / (ctx.potSize + ctx.callAmount);

            if (equity > potOdds)
            {
                // Req 8.9 — semi-bluff raise with high aggression
                if (aggression >= 8 && Random.Range(0f, 1f) < 0.30f)
                    return Action(PokerAction.Raise, ValueBetSize(aggression));

                return Action(PokerAction.Call); // Req 8.8
            }

            return Action(PokerAction.Fold); // Req 8.8 — equity doesn't justify call
        }

        // No bet to face — Req 8.10
        if (aggression >= 8 && Random.Range(0f, 1f) < 0.25f)
            return Action(PokerAction.Bet, BluffBetSize(aggression)); // semi-bluff

        return Action(PokerAction.Check);
    }

    // -------------------------------------------------------------------------
    // Sizing helpers (Req 7.9, 8.11, 8.12)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pre-flop raise size as a BB multiplier. (Req 7.9)
    /// aggression ≤ 3 → 2.5x | ≤ 7 → 3.0x | ≥ 8 → 3.5x
    /// </summary>
    public static float PreFlopRaiseSize(int aggression)
    {
        if (aggression <= 3) return 2.5f;
        if (aggression <= 7) return 3.0f;
        return 3.5f;
    }

    /// <summary>
    /// Post-flop value bet size as a pot fraction. (Req 8.11)
    /// aggression ≤ 3 → 0.40 | ≤ 7 → 0.60 | ≥ 8 → 0.75
    /// </summary>
    public static float ValueBetSize(int aggression)
    {
        if (aggression <= 3) return 0.40f;
        if (aggression <= 7) return 0.60f;
        return 0.75f;
    }

    /// <summary>
    /// Post-flop bluff bet size — same as value bet size so sizing doesn't reveal hand strength. (Req 8.12)
    /// </summary>
    public static float BluffBetSize(int aggression) => ValueBetSize(aggression);

    // -------------------------------------------------------------------------
    // Equity estimation — rule of 2 and 4 (Req 6.2–6.4)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Estimates equity using the rule of 2 and 4.
    /// Flop: outs × 4 / 100 | Turn: outs × 2 / 100 | River: 0.0
    /// </summary>
    public static float EstimateEquity(int outs, Street street)
    {
        switch (street)
        {
            case Street.Flop:   return Mathf.Min(outs * 4f / 100f, 1.0f); // Req 6.2
            case Street.Turn:   return Mathf.Min(outs * 2f / 100f, 1.0f); // Req 6.3
            default:            return 0.0f;                               // Req 6.4 (River / PreFlop)
        }
    }

    // -------------------------------------------------------------------------
    // Opponent tag helper (Req 11.5)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns Unknown for multi-way pots; returns the single opponent's tag heads-up.
    /// Null-safe. (Req 11.5)
    /// </summary>
    public static OpponentTag GetActiveOpponentTag(DecisionContext ctx)
    {
        // Req 11.5 — multi-way → always Unknown
        if (ctx.playersInHand > 2)
            return OpponentTag.Unknown;

        if (ctx.opponentTags == null || ctx.opponentTags.Length == 0)
            return OpponentTag.Unknown;

        // Heads-up: return the first non-default tag found
        foreach (var tag in ctx.opponentTags)
        {
            if (tag != OpponentTag.Unknown)
                return tag;
        }

        return OpponentTag.Unknown;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static BotAction Action(PokerAction action, float ratio = 0f)
        => new BotAction { action = action, betSizeRatio = ratio };

    /// <summary>
    /// Returns a small random noise value in the range [-0.05, +0.05]. (Req 7.10)
    /// </summary>
    private static float Noise() => Random.Range(-0.05f, 0.05f);
}
