using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.State;

/// <summary>
/// Orchestration layer for all in-game UI.
/// Receives calls from PokerGameManager and delegates to the appropriate child components.
/// All existing public method signatures are preserved unchanged.
/// Requirements: 1.3, 1.4, 1.5, 12.2
/// </summary>
public class UIManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Legacy serialized fields (kept for backward compatibility with existing
    // scene wiring — UIManager still owns these but delegates to new components)
    // -------------------------------------------------------------------------

    [Header("Legacy UI References (kept for backward compatibility)")]
    [SerializeField] private TextMeshProUGUI potText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Button startHandButton;
    [SerializeField] private Button foldButton;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button callButton;
    [SerializeField] private Button betButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Button allInButton;

    [Header("Legacy Player UI")]
    [SerializeField] private PlayerUIPanel[] playerPanels;
    [SerializeField] private CommunityCardsDisplay communityCardsDisplay;
    [SerializeField] private RaiseControl raiseControl;

    // -------------------------------------------------------------------------
    // New component references (wired via Inspector or PokerUISetup)
    // -------------------------------------------------------------------------

    [Header("New Components")]
    [SerializeField] private CenterPotDisplay centerPotDisplay;
    [SerializeField] private ActionBar actionBar;
    [SerializeField] private ShowdownUI showdownUI;
    [SerializeField] private WinnerCelebration winnerCelebration;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private CardDealerManager cardDealerManager;
    [SerializeField] private PotAnimator potAnimator;

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

        // Distribute theme to child components that accept it
        ApplyThemeToChildren();

        gameManager = FindFirstObjectByType<PokerGameManager>();

        // Wire legacy button listeners (preserved for backward compatibility)
        if (startHandButton != null)
            startHandButton.onClick.AddListener(() => gameManager?.StartNewHand());
        if (foldButton != null)
            foldButton.onClick.AddListener(() => gameManager?.OnFoldClicked());
        if (checkButton != null)
            checkButton.onClick.AddListener(() => gameManager?.OnCheckClicked());
        if (callButton != null)
            callButton.onClick.AddListener(() => gameManager?.OnCallClicked());
        if (betButton != null)
            betButton.onClick.AddListener(() => gameManager?.OnBetClicked());
        if (raiseButton != null)
            raiseButton.onClick.AddListener(() => gameManager?.OnRaiseClicked());
        if (allInButton != null)
            allInButton.onClick.AddListener(() => gameManager?.OnAllInClicked());
    }

    // -------------------------------------------------------------------------
    // Theme distribution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies the loaded UITheme to all child components that expose a public
    /// <c>theme</c> field. Uses reflection-free direct assignment where possible.
    /// Requirements: 1.3, 1.4
    /// </summary>
    private void ApplyThemeToChildren()
    {
        if (theme == null) return;

        // PlayerUIPanels
        if (playerPanels != null)
        {
            foreach (var panel in playerPanels)
            {
                if (panel != null)
                    SetThemeOnComponent(panel);
            }
        }

        // New components
        SetThemeOnComponent(centerPotDisplay);
        SetThemeOnComponent(actionBar);
        SetThemeOnComponent(showdownUI);
        SetThemeOnComponent(winnerCelebration);
        SetThemeOnComponent(gameOverUI);
        SetThemeOnComponent(cardDealerManager);
        SetThemeOnComponent(potAnimator);
    }

    /// <summary>
    /// Sets the <c>theme</c> serialized field on a MonoBehaviour via reflection.
    /// Logs a warning if the component is null; silently skips if it has no theme field.
    /// </summary>
    private void SetThemeOnComponent(MonoBehaviour component)
    {
        if (component == null) return;

        var field = component.GetType().GetField(
            "theme",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        if (field != null && field.FieldType == typeof(UITheme))
        {
            // Only overwrite if the field is currently null (don't stomp Inspector assignments)
            if (field.GetValue(component) == null)
                field.SetValue(component, theme);
        }
    }

    // -------------------------------------------------------------------------
    // Existing public methods — signatures MUST NOT change
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables the human player's action buttons.
    /// Delegates to ActionBar when available; falls back to legacy buttons.
    /// </summary>
    public void EnablePlayerActions(bool enable)
    {
        // Delegate to ActionBar
        if (actionBar != null)
        {
            if (!enable)
                StartCoroutine(actionBar.Hide());
            // Show is triggered by UpdateGameState when it's the human's turn
        }

        // Legacy fallback
        if (foldButton != null)   foldButton.interactable   = enable;
        if (checkButton != null)  checkButton.interactable  = enable;
        if (callButton != null)   callButton.interactable   = enable;
        if (betButton != null)    betButton.interactable    = enable;
        if (raiseButton != null)  raiseButton.interactable  = enable;
        if (allInButton != null)  allInButton.interactable  = enable;
    }

    /// <summary>
    /// Updates all UI elements to reflect the current game state (normal play mode).
    /// Only the human player's hole cards are shown face-up.
    /// </summary>
    public void UpdateGameState(GameState state)
    {
        if (state == null) return;

        // Clear player actions at the start of a new betting round
        if (state.Phase != lastPhase)
        {
            ClearAllPlayerActions();
            lastPhase = state.Phase;
        }

        // --- Pot display ---
        UpdatePotDisplay(state);

        // --- Phase display ---
        UpdatePhaseDisplay(state);

        // --- Community cards ---
        UpdateCommunityCards(state, false);

        // --- Player panels (normal mode — only show human cards) ---
        UpdatePlayerPanels(state, false);

        // --- Button / action bar states ---
        UpdateButtonStates(state);
    }

    /// <summary>
    /// Updates all UI elements for showdown mode — all active (non-folded) players'
    /// hole cards are shown face-up.
    /// </summary>
    public void UpdateGameStateShowdown(GameState state)
    {
        if (state == null) return;

        // --- Pot display ---
        UpdatePotDisplay(state);

        // --- Phase display ---
        UpdatePhaseDisplay(state);

        // --- Community cards ---
        UpdateCommunityCards(state, true);

        // --- Player panels (showdown mode — show all active players' cards) ---
        UpdatePlayerPanels(state, true);

        // --- Button / action bar states ---
        UpdateButtonStates(state);
    }

    /// <summary>
    /// Shows an action label on the specified player's panel.
    /// </summary>
    public void ShowPlayerAction(int seatIndex, string action, decimal amount = 0)
    {
        if (playerPanels == null || seatIndex < 0 || seatIndex >= playerPanels.Length)
        {
            Debug.LogWarning($"[UIManager] ShowPlayerAction: seatIndex {seatIndex} is out of range.");
            return;
        }

        if (playerPanels[seatIndex] == null)
        {
            Debug.LogWarning($"[UIManager] ShowPlayerAction: playerPanels[{seatIndex}] is null.");
            return;
        }

        playerPanels[seatIndex].ShowAction(action, amount);
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
        decimal totalPot = state.TotalContributions.Values.Sum();

        // Delegate to CenterPotDisplay
        if (centerPotDisplay != null)
        {
            centerPotDisplay.SetPot(totalPot);
        }
        else if (potText != null)
        {
            // Legacy fallback
            potText.text = $"POT: ${totalPot}";
        }
    }

    private void UpdatePhaseDisplay(GameState state)
    {
        // Delegate to CenterPotDisplay
        if (centerPotDisplay != null)
        {
            centerPotDisplay.SetPhase(state.Phase);
            bool handInProgress = !state.HandComplete && state.Phase != GamePhase.NotStarted;
            centerPotDisplay.SetHandInProgress(handInProgress);
        }
        else if (phaseText != null)
        {
            // Legacy fallback
            phaseText.text = $"{state.Phase}";
        }

        // Legacy start hand button
        if (startHandButton != null)
        {
            bool handActive = !state.HandComplete && state.Phase != GamePhase.NotStarted;
            startHandButton.interactable = !handActive;
        }
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

            bool showCards;
            if (isShowdown)
                showCards = !player.IsFolded;
            else
                showCards = (i == 0); // Only show human player's cards in normal play

            decimal currentRoundBet = 0;
            if (state.RoundState != null && !state.HandComplete)
                currentRoundBet = state.RoundState.GetContribution(player.Id);

            playerPanels[i].UpdatePlayer(player, isActive, showCards, currentRoundBet, isDealer);
        }
    }

    private void UpdateButtonStates(GameState state)
    {
        bool handActive = !state.HandComplete && state.Phase != GamePhase.NotStarted;
        bool isHumanTurn = gameManager != null && gameManager.IsHumanTurn();

        // Delegate to ActionBar
        if (actionBar != null)
        {
            actionBar.UpdateFromGameState(state, isHumanTurn);

            // Update call amount label
            if (isHumanTurn && handActive && state.RoundState != null)
            {
                var player = state.GetPlayerBySeat(0);
                if (player != null)
                {
                    decimal callAmount = state.RoundState.CurrentBet - state.RoundState.GetContribution(player.Id);
                    if (callAmount > 0)
                        actionBar.SetCallAmount(callAmount);
                }
            }
        }

        // Legacy button fallback
        if (foldButton != null)   foldButton.interactable   = handActive && isHumanTurn;
        if (allInButton != null)  allInButton.interactable  = handActive && isHumanTurn;
        if (startHandButton != null) startHandButton.interactable = !handActive;

        if (handActive && isHumanTurn)
        {
            var round  = state.RoundState;
            var player = state.GetPlayerBySeat(0);
            if (round != null && player != null)
            {
                decimal contribution = round.GetContribution(player.Id);
                decimal currentBet   = round.CurrentBet;
                bool hasBetToCall    = currentBet > contribution;

                if (checkButton != null)  checkButton.interactable  = !hasBetToCall;
                if (callButton != null)   callButton.interactable   = hasBetToCall;
                if (betButton != null)    betButton.interactable    = !hasBetToCall;
                if (raiseButton != null)  raiseButton.interactable  = hasBetToCall;
            }
        }
        else
        {
            if (checkButton != null)  checkButton.interactable  = false;
            if (callButton != null)   callButton.interactable   = false;
            if (betButton != null)    betButton.interactable    = false;
            if (raiseButton != null)  raiseButton.interactable  = false;
        }

        // Hide raise control when it's not the human's turn
        if (raiseControl != null && !isHumanTurn)
            raiseControl.Hide();
    }

    /// <summary>
    /// Clears all player action labels — called at the start of each new betting round.
    /// </summary>
    private void ClearAllPlayerActions()
    {
        if (playerPanels == null) return;

        foreach (var panel in playerPanels)
        {
            if (panel != null)
                panel.ClearAction();
        }
    }
}
