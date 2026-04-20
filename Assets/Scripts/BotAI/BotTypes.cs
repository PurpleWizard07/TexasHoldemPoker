using PokerEngine.Core;

// Shared enums and structs for the Poker Bot AI system.
// No logic — data definitions only.

public enum HandTier        { Premium = 1, Strong = 2, Playable = 3, Marginal = 4, Trash = 5 }
public enum HandStrengthBucket { Strong, Medium, Weak, Draw }
public enum OpponentTag     { Unknown, Tight, Loose }
public enum Street          { PreFlop, Flop, Turn, River }
public enum Position        { UTG, UTG1, MP, MP1, CO, BTN, SB, BB }
public enum PokerAction     { Fold, Check, Call, Bet, Raise }

public struct BotAction
{
    public PokerAction action;
    public float betSizeRatio;  // BB multiplier (pre-flop) or pot fraction (post-flop)
}

public struct DecisionContext
{
    public Card[]        holeCards;
    public Card[]        communityCards;
    public Street        currentStreet;
    public float         potSize;
    public float         callAmount;
    public float         botStack;
    public bool          botIsPreFlopAggressor;
    public Position      position;
    public int           playersInHand;
    public OpponentTag[] opponentTags;
    public BotPersonality personality;
}
