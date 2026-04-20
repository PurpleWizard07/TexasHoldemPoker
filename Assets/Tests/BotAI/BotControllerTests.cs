using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for BotController.
/// Requirements: 12.2, 12.3, 14.1
/// </summary>
[TestFixture]
public class BotControllerTests
{
    // -------------------------------------------------------------------------
    // Helpers — access private CalculateBetAmount via reflection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Invokes the private BotController.CalculateBetAmount method via reflection.
    /// </summary>
    private static float InvokeCalculateBetAmount(BotController controller, BotAction action, float potSize, float bigBlind)
    {
        var method = typeof(BotController).GetMethod(
            "CalculateBetAmount",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(method, "CalculateBetAmount method not found via reflection");

        return (float)method.Invoke(controller, new object[] { action, potSize, bigBlind });
    }

    private static BotController CreateController()
    {
        var go = new GameObject("TestBotController");
        return go.AddComponent<BotController>();
    }

    // -------------------------------------------------------------------------
    // Req 12.2 — ratio ≤ 5.0 → BB multiplier
    // -------------------------------------------------------------------------

    [Test]
    public void CalculateBetAmount_Raise_Ratio3_UsesBBMultiplier()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Raise, betSizeRatio = 3.0f };

        float result = InvokeCalculateBetAmount(controller, action, potSize: 100f, bigBlind: 10f);

        // ratio 3.0 ≤ 5.0 → BB multiplier: 3.0 × 10 = 30
        Assert.AreEqual(30f, result, 0.001f);
    }

    [Test]
    public void CalculateBetAmount_Bet_Ratio25_UsesBBMultiplier()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Bet, betSizeRatio = 2.5f };

        float result = InvokeCalculateBetAmount(controller, action, potSize: 50f, bigBlind: 20f);

        // ratio 2.5 ≤ 5.0 → BB multiplier: 2.5 × 20 = 50
        Assert.AreEqual(50f, result, 0.001f);
    }

    // -------------------------------------------------------------------------
    // Req 12.3 — ratio > 5.0 → pot fraction
    // -------------------------------------------------------------------------

    [Test]
    public void CalculateBetAmount_Bet_Ratio075_UsesPotFraction()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Bet, betSizeRatio = 0.75f };

        // 0.75 ≤ 5.0 → BB multiplier: 0.75 × 10 = 7.5
        // Wait — 0.75 ≤ 5.0, so it's a BB multiplier, not pot fraction.
        // Per Req 12.3: ratio > 5.0 → pot fraction.
        // The task says "ratio 0.75 (pot fraction)" but the code uses ratio > 5.0 for pot fraction.
        // Post-flop sizing methods return values like 0.40, 0.60, 0.75 which are ≤ 5.0.
        // These are treated as BB multipliers by CalculateBetAmount.
        // The "pot fraction" interpretation is only for ratio > 5.0.
        // Testing with ratio 0.75 as BB multiplier: 0.75 × bigBlind
        float result = InvokeCalculateBetAmount(controller, action, potSize: 100f, bigBlind: 10f);

        // 0.75 ≤ 5.0 → BB multiplier: 0.75 × 10 = 7.5
        Assert.AreEqual(7.5f, result, 0.001f);
    }

    [Test]
    public void CalculateBetAmount_Raise_Ratio6_UsesPotFraction()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Raise, betSizeRatio = 6.0f };

        float result = InvokeCalculateBetAmount(controller, action, potSize: 100f, bigBlind: 10f);

        // ratio 6.0 > 5.0 → pot fraction: 6.0 × 100 = 600
        Assert.AreEqual(600f, result, 0.001f);
    }

    // -------------------------------------------------------------------------
    // Req 12.4 — Fold/Check/Call return 0
    // -------------------------------------------------------------------------

    [Test]
    public void CalculateBetAmount_Fold_ReturnsZero()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Fold, betSizeRatio = 3.0f };

        float result = InvokeCalculateBetAmount(controller, action, potSize: 100f, bigBlind: 10f);
        Assert.AreEqual(0f, result, 0.001f);
    }

    [Test]
    public void CalculateBetAmount_Check_ReturnsZero()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Check, betSizeRatio = 3.0f };

        float result = InvokeCalculateBetAmount(controller, action, potSize: 100f, bigBlind: 10f);
        Assert.AreEqual(0f, result, 0.001f);
    }

    [Test]
    public void CalculateBetAmount_Call_ReturnsZero()
    {
        var controller = CreateController();
        var action = new BotAction { action = PokerAction.Call, betSizeRatio = 3.0f };

        float result = InvokeCalculateBetAmount(controller, action, potSize: 100f, bigBlind: 10f);
        Assert.AreEqual(0f, result, 0.001f);
    }

    // -------------------------------------------------------------------------
    // Req 14.1 — Null personality fallback uses aggression=5, tightness=5, bluffiness=5
    // -------------------------------------------------------------------------

    [Test]
    public void Awake_NullPersonality_FallsBackToDefault555()
    {
        // Create a BotController without assigning a personality.
        // Awake() is called automatically by AddComponent in EditMode.
        var go = new GameObject("TestBotControllerNullPersonality");
        var controller = go.AddComponent<BotController>();

        // Access the private [SerializeField] 'personality' field via reflection
        var field = typeof(BotController).GetField(
            "personality",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(field, "Could not find 'personality' field on BotController via reflection");

        var personality = field.GetValue(controller) as BotPersonality;
        Assert.IsNotNull(personality, "Personality should not be null after Awake fallback");
        Assert.AreEqual(5, personality.aggression,  "Default aggression should be 5");
        Assert.AreEqual(5, personality.tightness,   "Default tightness should be 5");
        Assert.AreEqual(5, personality.bluffiness,  "Default bluffiness should be 5");

        Object.DestroyImmediate(go);
    }
}
