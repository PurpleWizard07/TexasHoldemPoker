using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.State;

/// <summary>
/// Orchestration layer for all in-game UI.
/// Receives calls from PokerGameManager and delegates to the new overhauled components.
/// All existing public method signatures are preserved unchanged.
/// Requirements: 1.3, 1.4, 1.5, 12.2
/// </summary>
public class UIManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Serialized fields — new component references
    // -------------------------------------------------------------------------

    [Header("Player Panels")]
    [SerializeField] private PlayerUIPanel[] playerPanels = new PlayerUIPanel[6];

    [Header("Community Cards")]
    [SerializeField] private CommunityCardsDisplay communityCardsDisplay;

    [Header("Center Pot")]
    [SerializeField] private CenterPotDisplay centerPotDisplay;

    [Header("Action Controls")]
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private RaiseControl raiseControl;

    [Header("Showdown / Celebration")]
    [SerializeField] private ShowdownUI showdownUI;
    [SerializeField] private WinnerCelebration winnerCelebration;
    [SerializeField] private GameOverUI gameOverUI;

    [Header("Card & Chip Managers")]
    [SerializeField] private CardDealerManager cardDealerManager;
    [SerializeField] private PotAnimator potAnimator;
    [SerializeField] private ChipPool chipPool;

    // -------------------------------------------------------------------------
    // Theme
    // -------------------------------------------------------------------------

    private UITheme theme;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private PokerGameManager gameManager;
    private GamePhase lastPhase = GamePhase.NotStarted;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Load UITheme from Resources (Req 1.4, 1.5)
        theme = Resources.Load<UITheme>("UITheme");
        if (theme == null)
        {
            Debug.LogError(
                "[UIManager] UITheme asset not found at Resources/UITheme. " +
                "Falling back to hardcoded defaults. " +
                "Create the asset via Create → Poker → UITheme and place it in Assets/Resources/.");
            theme = UITheme.CreateDefault();
        }

        ValidateReferences();
        ApplyThemeToChildren();

        gameManager = FindFirstObjectByType<PokerGameManager>();
    }

    // -------------------------------------------------------------------------
    // Reference validation
    // -------------------------------------------------------------------------

    private void ValidateReferences()
    {
        if (playerPanels == null || playerPanels.Length == 0)
            Debug.LogWarning("[UIManager] playerPanels array is empty.", this);

        if (communityCardsDisplay == null)
            Debug.LogWarning("[UIManager] communityCardsDisplay is not assigned.", this);

        if (centerPotDisplay == null)
            Debug.LogWarning("[UIManager] centerPotDisplay is not assigned.", this);

        if (actionBar == null)
            Debug.LogWarning("[UIManager] actionBar is not assigned.", this);

        if (raiseControl == null)
            Debug.LogWarning("[UIManager] raiseControl is not assigned.", this);

        if (showdownUI == null)
            Debug.LogWarning("[UIManager] showdownUI is not assigned.", this);

        if (winnerCelebration == null)
            Debug.LogWarning("[UIManager] winnerCelebration is not assigned.", this);

        if (gameOverUI == null)
            Debug.LogWarning("[UIManager] gameOverUI is not assigned.", this);

        if (cardDealerManager == null)
            Debug.LogWarning("[UIManager] cardDealerManager is not assigned.", this);

        if (potAnimator == null)
            Debug.LogWarning("[UIManager] potAnimator is not assigned.", this);

        if (chipPool == null)
            Debug.LogWarning("[UIManager] chipPool is not assigned.", this);
    }

    // -------------------------------------------------------------------------
    // Theme distribution
    // -------------------------------------------------------------------------

    private void ApplyThemeToChildren()
    {
        if (theme == null) return;

        SetTheme(centerPotDisplay);
        SetTheme(actionBar);
        SetTheme(showdownUI);
        SetTheme(winnerCelebration);
        SetTheme(gameOverUI);
        SetTheme(cardDealerManager);
        SetTheme(potAnimator);
        SetTheme(raiseControl);

        if (playerPanels != null)
            foreach (var panel in playerPanels)
                SetTheme(panel);
    }

    private void SetTheme(MonoBehaviour component)
    {
        if (component == null) return;

        var field = component.GetType().GetField(
            "theme",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        if (field != null && field.FieldType == typeof(UITheme) && field.GetValue(component) == null)
            field.SetValue(component, theme);
    }

    // -------------------------------------------------------------------------
    // Existing public methods — signatures MUST NOT change
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables the human player's action controls.
    /// </summary>
    public void EnablePlayerActions(bool enable)
    {
        if (actionBar == null) return;

        if (enable)
            StartCoroutine(actionBar.Show());
        else
            StartCoroutine(actionBar.Hide());
    }

    /// <summary>
    /// Updates all UI elements to reflect the current game state (normal play mode).
    /// Only the human player's hole cards are shown face-up.
    /// </summary>
    public void UpdateGameState(GameState state)
    {
        if (state == null) return;

        if (state.Phase != lastPhase)
        {
            ClearAllPlayerActions();
            lastPhase = state.Phase;
        }

        UpdatePotDisplay(state);
        UpdatePhaseDisplay(state);
        UpdateCommunityCards(state, false);
        UpdatePlayerPanels(state, false);
        UpdateButtonStates(state);
    }

    /// <summary>
    /// Updates all UI elements for showdown mode — all active players' hole cards shown face-up.
    /// </summary>
    public void UpdateGameStateShowdown(GameState state)
    {
        if (state == null) return;

        UpdatePotDisplay(state);
        UpdatePhaseDisplay(state);
        UpdateCommunityCards(state, true);
        UpdatePlayerPanels(state, true);
        UpdateButtonStates(state);
    }

    /// <summary>
    /// Shows an action label on the specified player's panel.
    /// </summary>
    public void ShowPlayerAction(int seatIndex, string action, decimal amount = 0)
    {
        if (playerPanels == null || seatIndex < 0 || seatIndex >= playerPanels.Length)
        {
            Debug.LogWarning($"[UIManager] ShowPlayerAction: seatIndex {seatIndex} out of range.");
            return;
        }

        playerPanels[seatIndex]?.ShowAction(action, amount);
    }

    /// <summary>
    /// Returns the Transform of the player panel at the given seat index.
    /// Used by PokerGameManager for chip animation targeting.
    /// </summary>
    public Transform GetPlayerPanelTransform(int seatIndex)
    {
        if (playerPanels == null || seatIndex < 0 || seatIndex >= playerPanels.Length)
            return null;

        return playerPanels[seatIndex]?.transform;
    }

    /// <summary>
    /// Returns all BetDisplay components from the player panels.
    /// Used by PokerGameManager to collect bet positions for chip animations.
    /// </summary>
    public BetDisplay[] GetAllBetDisplays()
    {
        if (playerPanels == null) return null;

        var displays = new List<BetDisplay>();
        foreach (var panel in playerPanels)
        {
            if (panel == null) continue;
            var bet = panel.GetComponentInChildren<BetDisplay>();
            if (bet != null)
                displays.Add(bet);
        }
        return displays.ToArray();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void UpdatePotDisplay(GameState state)
    {
        if (centerPotDisplay == null) return;

        decimal totalPot = state.TotalContributions.Values.Sum();
        centerPotDisplay.SetPot(totalPot);
    }

    private void UpdatePhaseDisplay(GameState state)
    {
        if (centerPotDisplay == null) return;

        centerPotDisplay.SetPhase(state.Phase);
        bool handInProgress = !state.HandComplete && state.Phase != GamePhase.NotStarted;
        centerPotDisplay.SetHandInProgress(handInProgress);
    }

    private void UpdateCommunityCards(GameState state, bool isShowdown)
    {
        if (communityCardsDisplay == null) return;

        if (state.Phase == GamePhase.NotStarted)
            communityCardsDisplay.ShowAllCardBacks();
        else
            communityCardsDisplay.UpdateCards(state.CommunityCards, state.Phase);
    }

    private void UpdatePlayerPanels(GameState state, bool isShowdown)
    {
        if (playerPanels == null) return;

        for (int i = 0; i < playerPanels.Length && i < state.Players.Count; i++)
        {
            if (playerPanels[i] == null)
            {
                Debug.LogWarning($"[UIManager] playerPanels[{i}] is null — skipping update.");
                continue;
            }

            var player = state.Players[i];
            bool isActive = state.CurrentSeatToAct == i;
            bool isDealer = state.DealerSeat == i;
            bool showCards = isShowdown ? !player.IsFolded : (i == 0);

            decimal currentRoundBet = 0;
            if (state.RoundState != null && !state.HandComplete)
                currentRoundBet = state.RoundState.GetContribution(player.Id);

            playerPanels[i].UpdatePlayer(player, isActive, showCards, currentRoundBet, isDealer);
        }
    }

    private void UpdateButtonStates(GameState state)
    {
        if (actionBar == null) return;

        bool isHumanTurn = gameManager != null && gameManager.IsHumanTurn();
        actionBar.UpdateFromGameState(state, isHumanTurn);

        // Update call amount label
        if (isHumanTurn && !state.HandComplete && state.RoundState != null)
        {
            var player = state.GetPlayerBySeat(0);
            if (player != null)
            {
                decimal callAmount = state.RoundState.CurrentBet - state.RoundState.GetContribution(player.Id);
                if (callAmount > 0)
                    actionBar.SetCallAmount(callAmount);
            }
        }

        // Hide raise control when not human's turn
        if (raiseControl != null && !isHumanTurn)
            raiseControl.Hide();
    }

    private void ClearAllPlayerActions()
    {
        if (playerPanels == null) return;
        foreach (var panel in playerPanels)
            panel?.ClearAction();
    }
}
