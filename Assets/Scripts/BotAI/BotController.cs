using System.Linq;
using UnityEngine;
using PokerEngine.Core;
using PokerEngine.Rules;
using PokerEngine.State;

/// <summary>
/// MonoBehaviour that wires together all bot AI sub-components and replaces
/// the random-action logic in PokerGameManager.
/// Requirements: 2.1, 12.1–12.4, 13.1–13.8, 14.1–14.4
/// </summary>
public class BotController : MonoBehaviour
{
    [SerializeField] private BotPersonality personality;

    private DecisionEngine _decisionEngine;
    private TableReadTracker _tableReadTracker;

    // Seat index of the last pre-flop aggressor (set externally or tracked internally)
    private int _preFlopAggressorSeat = -1;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _decisionEngine   = new DecisionEngine();
        _tableReadTracker = new TableReadTracker();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by PokerGameManager when it is this bot's turn to act.
    /// Returns a PlayerAction ready for the game engine. (Req 14.2)
    /// </summary>
    public PlayerAction RequestAction(GameState state, Player player)
    {
        // Guard null inputs — return Check as a safe no-op (design: error handling)
        if (state == null || player == null)
        {
            Debug.LogWarning("[BotController] RequestAction called with null state or player. Returning Check.");
            return player != null
                ? PlayerAction.Check(player.Id)
                : PlayerAction.Check(System.Guid.Empty);
        }

        // Personality can be assigned later by PokerGameManager after Awake().
        if (personality == null)
        {
            Debug.LogWarning($"[BotController] No BotPersonality assigned on '{name}'. Using default (5/5/5).");
            personality = ScriptableObject.CreateInstance<BotPersonality>();
            personality.aggression = 5;
            personality.tightness = 5;
            personality.bluffiness = 5;
        }

        DecisionContext ctx = PopulateDecisionContext(state, player);
        BotAction botAction = _decisionEngine.Decide(ctx);

        float potSize  = (float)state.TotalContributions.Values.Sum();
        float bigBlind = (float)state.BigBlind;
        float betAmount = CalculateBetAmount(botAction, potSize, bigBlind);

        // Clamp to player stack (design: stack underflow prevention)
        betAmount = Mathf.Min(betAmount, (float)player.Stack);

        return ToPlayerAction(botAction.action, player.Id, betAmount);
    }

    /// <summary>
    /// Called by PokerGameManager after each hand resolves.
    /// Records voluntary-entry data for each active seat. (Req 14.3)
    /// </summary>
    public void OnHandComplete(GameState state, int[] voluntaryEntrySeats)
    {
        if (state == null) return;

        foreach (var p in state.Players)
        {
            bool enteredVoluntarily = voluntaryEntrySeats != null &&
                                      System.Array.IndexOf(voluntaryEntrySeats, p.SeatIndex) >= 0;
            _tableReadTracker.RecordHand(p.SeatIndex, enteredVoluntarily);
        }
    }

    /// <summary>
    /// Allows PokerGameManager to assign a personality at runtime (e.g. from the botPersonalities array).
    /// </summary>
    public void SetPersonality(BotPersonality p)
    {
        if (p != null) personality = p;
    }

    /// <summary>
    /// Allows PokerGameManager to inform the controller who raised pre-flop.
    /// </summary>
    public void SetPreFlopAggressor(int seatIndex)
    {
        _preFlopAggressorSeat = seatIndex;
    }

    // -------------------------------------------------------------------------
    // DecisionContext population (Req 13.1–13.8)
    // -------------------------------------------------------------------------

    private DecisionContext PopulateDecisionContext(GameState state, Player player)
    {
        var round = state.RoundState;

        // Req 13.3 — current street
        Street street = PhaseToStreet(state.Phase);

        // Req 13.1 — hole cards
        Card[] holeCards = player.HoleCards.ToArray();

        // Req 13.2 — community cards (0 pre-flop, 3 flop, 4 turn, 5 river)
        Card[] communityCards = state.CommunityCards.ToArray();

        // Req 13.4 — pot size, call amount, stack
        float potSize    = (float)state.TotalContributions.Values.Sum();
        float playerContrib = (float)round.GetContribution(player.Id);
        float callAmount = Mathf.Max(0f, (float)round.CurrentBet - playerContrib);
        float botStack   = (float)player.Stack;

        // Req 13.5 — position
        Position position = SeatToPosition(player.SeatIndex, state.DealerSeat, state.Players.Count);

        // Req 13.6 — players still active in the hand
        int playersInHand = state.Players.Count(p => !p.IsFolded);

        // Req 13.7 — pre-flop aggressor flag
        bool isAggressor = _preFlopAggressorSeat == player.SeatIndex;

        // Req 13.8 — opponent tags
        OpponentTag[] opponentTags = state.Players
            .Select(p => _tableReadTracker.GetTag(p.SeatIndex))
            .ToArray();

        return new DecisionContext
        {
            holeCards             = holeCards,
            communityCards        = communityCards,
            currentStreet         = street,
            potSize               = potSize,
            callAmount            = callAmount,
            botStack              = botStack,
            position              = position,
            playersInHand         = playersInHand,
            botIsPreFlopAggressor = isAggressor,
            opponentTags          = opponentTags,
            personality           = personality,
        };
    }

    // -------------------------------------------------------------------------
    // Bet amount conversion (Req 12.1–12.4)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a BotAction betSizeRatio to a chip amount.
    /// ratio &lt;= 1.0 → pot fraction (post-flop sizing, e.g. 0.33 / 0.60 / 0.75)
    /// ratio &gt; 1.0  → BB multiplier (pre-flop sizing, e.g. 2.5x / 3.0x / 3.5x)
    /// Check / Call / Fold → 0. (Req 12.4)
    /// </summary>
    private float CalculateBetAmount(BotAction action, float potSize, float bigBlind)
    {
        switch (action.action)
        {
            case PokerAction.Fold:
            case PokerAction.Check:
            case PokerAction.Call:
                return 0f; // Req 12.4

            case PokerAction.Bet:
            case PokerAction.Raise:
                float amount = action.betSizeRatio <= 1.0f
                    ? action.betSizeRatio * potSize    // post-flop pot-fraction sizing
                    : action.betSizeRatio * bigBlind;  // pre-flop BB-multiplier sizing

                // Keep engine validation happy: any bet/raise must be at least one big blind.
                return Mathf.Max(bigBlind, amount);

            default:
                return 0f;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static PlayerAction ToPlayerAction(PokerAction action, System.Guid playerId, float amount)
    {
        // Round to whole chips — no fractional amounts at the table
        decimal chips = (decimal)Mathf.Round(amount);
        switch (action)
        {
            case PokerAction.Fold:  return PlayerAction.Fold(playerId);
            case PokerAction.Check: return PlayerAction.Check(playerId);
            case PokerAction.Call:  return PlayerAction.Call(playerId);
            case PokerAction.Bet:   return PlayerAction.Bet(playerId, chips);
            case PokerAction.Raise: return PlayerAction.Raise(playerId, chips);
            default:                return PlayerAction.Check(playerId);
        }
    }

    private static Street PhaseToStreet(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Flop:  return Street.Flop;
            case GamePhase.Turn:  return Street.Turn;
            case GamePhase.River: return Street.River;
            default:              return Street.PreFlop;
        }
    }

    /// <summary>
    /// Maps a seat index to a Position enum based on dealer seat and player count.
    /// Positions cycle: BTN (dealer) → SB → BB → UTG → UTG1 → MP → MP1 → CO → BTN
    /// </summary>
    private static Position SeatToPosition(int seatIndex, int dealerSeat, int playerCount)
    {
        // Offset from dealer (0 = dealer/BTN)
        int offset = (seatIndex - dealerSeat + playerCount) % playerCount;

        switch (playerCount)
        {
            case 2:
                // Heads-up: dealer = SB/BTN, other = BB
                return offset == 0 ? Position.BTN : Position.BB;

            case 3:
                if (offset == 0) return Position.BTN;
                if (offset == 1) return Position.SB;
                return Position.BB;

            case 4:
                if (offset == 0) return Position.BTN;
                if (offset == 1) return Position.SB;
                if (offset == 2) return Position.BB;
                return Position.UTG;

            case 5:
                if (offset == 0) return Position.BTN;
                if (offset == 1) return Position.SB;
                if (offset == 2) return Position.BB;
                if (offset == 3) return Position.UTG;
                return Position.MP;

            default: // 6+
                if (offset == 0) return Position.BTN;
                if (offset == 1) return Position.SB;
                if (offset == 2) return Position.BB;
                if (offset == 3) return Position.UTG;
                if (offset == 4) return Position.UTG1;
                if (offset == 5) return Position.MP;
                if (offset == 6) return Position.MP1;
                return Position.CO;
        }
    }
}
