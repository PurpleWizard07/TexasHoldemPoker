using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.Core;

/// <summary>
/// Displays a single player's seat: name, stack, avatar, cards, bet, dealer badge,
/// all-in badge, folded label, and last-action label. Handles glow/dim/badge transitions.
/// Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8
/// </summary>
public class PlayerUIPanel : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Serialized fields
    // -------------------------------------------------------------------------

    [SerializeField] private UITheme theme;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI stackText;

    [Header("Images")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image glowOutline;      // gold pulsing border (Req 4.2)
    [SerializeField] private Image dealerBadge;

    [Header("Badges / Labels")]
    [SerializeField] private GameObject allInBadge;  // Req 4.4
    [SerializeField] private TextMeshProUGUI foldedLabel; // Req 4.3

    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Cards")]
    [SerializeField] private CardVisual[] cardVisuals = new CardVisual[2];

    [Header("Displays")]
    [SerializeField] private BetDisplay betDisplay;
    [SerializeField] private PlayerActionDisplay actionDisplay;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private Coroutine glowPulseCoroutine;
    private Coroutine actionFadeCoroutine;
    private CanvasGroup glowCanvasGroup;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Ensure glowOutline has a CanvasGroup for PulseAlpha
        if (glowOutline != null)
        {
            glowCanvasGroup = glowOutline.GetComponent<CanvasGroup>();
            if (glowCanvasGroup == null)
                glowCanvasGroup = glowOutline.gameObject.AddComponent<CanvasGroup>();
        }

        // Apply theme colors where possible
        if (theme != null)
        {
            if (panelBackground != null)
                panelBackground.color = theme.panelBackground;

            if (glowOutline != null)
                glowOutline.color = theme.glowGold;
        }

        // Start with glow hidden
        if (glowOutline != null)
            glowOutline.gameObject.SetActive(false);

        // Start with badges/labels hidden
        if (allInBadge != null)
            allInBadge.SetActive(false);

        if (foldedLabel != null)
            foldedLabel.gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates all panel fields and triggers glow/dim/badge transitions.
    /// When player is null the panel hides itself entirely (Req 4.8).
    /// </summary>
    public void UpdatePlayer(Player player, bool isActive, bool showCards,
        decimal currentBet, bool isDealer)
    {
        // Req 4.8 — hide panel when player data is null
        if (player == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // --- Name & stack ---
        if (playerNameText != null)
            playerNameText.text = player.Name;

        if (stackText != null)
            stackText.text = $"${player.Stack:N0}";

        // --- Dealer badge ---
        if (dealerBadge != null)
            dealerBadge.gameObject.SetActive(isDealer);

        // --- Bet display ---
        if (betDisplay != null)
            betDisplay.SetBet(currentBet);

        // --- Cards ---
        UpdateCardDisplay(player, showCards);

        // --- Folded state (Req 4.3) ---
        bool isFolded = player.IsFolded;
        if (foldedLabel != null)
            foldedLabel.gameObject.SetActive(isFolded);

        if (panelCanvasGroup != null)
        {
            if (isFolded)
                panelCanvasGroup.alpha = 0.45f;   // ≤ 0.45 per spec
            else
                panelCanvasGroup.alpha = 1f;
        }

        // Apply muted text color when folded
        if (theme != null)
        {
            Color textColor = isFolded ? theme.textMuted : theme.textPrimary;
            if (playerNameText != null) playerNameText.color = textColor;
            if (stackText != null) stackText.color = textColor;
        }

        // --- All-in badge (Req 4.4) ---
        if (allInBadge != null)
            allInBadge.SetActive(player.IsAllIn);

        // --- Active glow (Req 4.2) ---
        if (isActive)
            EnableGlow();
        else
            DisableGlow();
    }

    /// <summary>
    /// Fades in the action label over 0.2 s, then auto-fades out after 2.0 s (Req 4.7).
    /// </summary>
    public void ShowAction(string action, decimal amount = 0)
    {
        if (actionDisplay == null) return;

        // Show the action text immediately via PlayerActionDisplay
        actionDisplay.ShowAction(action, amount);

        // Manage the auto-fade coroutine
        if (actionFadeCoroutine != null)
            StopCoroutine(actionFadeCoroutine);

        actionFadeCoroutine = StartCoroutine(ActionFadeRoutine(action, amount));
    }

    /// <summary>Clears the action label immediately.</summary>
    public void ClearAction()
    {
        if (actionFadeCoroutine != null)
        {
            StopCoroutine(actionFadeCoroutine);
            actionFadeCoroutine = null;
        }

        if (actionDisplay != null)
            actionDisplay.Clear();
    }

    /// <summary>Clears all card visuals.</summary>
    public void ClearCards()
    {
        if (cardVisuals == null) return;
        foreach (var cv in cardVisuals)
        {
            if (cv != null) cv.Clear();
        }
    }

    /// <summary>
    /// Coroutine: animate the panel to its active (glowing) state over 0.2 s.
    /// </summary>
    public IEnumerator AnimateActivate()
    {
        if (panelCanvasGroup != null)
            yield return TweenHelper.Fade(panelCanvasGroup, 1f, 0.2f);

        EnableGlow();
    }

    /// <summary>
    /// Coroutine: animate the panel to its inactive (dim) state over 0.2 s.
    /// </summary>
    public IEnumerator AnimateDeactivate()
    {
        DisableGlow();

        if (panelCanvasGroup != null)
        {
            float targetAlpha = (panelCanvasGroup.alpha <= 0.45f) ? panelCanvasGroup.alpha : 1f;
            yield return TweenHelper.Fade(panelCanvasGroup, targetAlpha, 0.2f);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables the glow outline and starts a PulseAlpha coroutine with a 1.0–1.5 s period
    /// using the theme's gold glow color (Req 4.2).
    /// </summary>
    private void EnableGlow()
    {
        if (glowOutline == null) return;

        glowOutline.gameObject.SetActive(true);

        // Apply theme glow color
        if (theme != null)
            glowOutline.color = theme.glowGold;

        // Stop any existing pulse
        if (glowPulseCoroutine != null)
            StopCoroutine(glowPulseCoroutine);

        // Period between 1.0 and 1.5 s — use 1.25 s as the midpoint
        const float pulsePeriod = 1.25f;

        if (glowCanvasGroup != null)
            glowPulseCoroutine = StartCoroutine(TweenHelper.PulseAlpha(glowCanvasGroup, 0.4f, 1f, pulsePeriod));
    }

    /// <summary>Disables the glow outline and stops the pulse coroutine (Req 4.2).</summary>
    private void DisableGlow()
    {
        if (glowPulseCoroutine != null)
        {
            StopCoroutine(glowPulseCoroutine);
            glowPulseCoroutine = null;
        }

        if (glowOutline != null)
            glowOutline.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates card visuals based on the player's hole cards and fold state.
    /// </summary>
    private void UpdateCardDisplay(Player player, bool showCards)
    {
        if (cardVisuals == null || cardVisuals.Length < 2) return;

        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] == null) continue;

            if (!player.IsFolded && i < player.HoleCards.Count)
                cardVisuals[i].SetCard(player.HoleCards[i], showCards);
            else
                cardVisuals[i].Clear();
        }
    }

    /// <summary>
    /// Fades in the action display over 0.2 s, holds for 2.0 s, then fades out (Req 4.7).
    /// </summary>
    private IEnumerator ActionFadeRoutine(string action, decimal amount)
    {
        // PlayerActionDisplay already shows the text; we animate its CanvasGroup if present
        CanvasGroup actionCG = actionDisplay != null
            ? actionDisplay.GetComponent<CanvasGroup>()
            : null;

        if (actionCG != null)
        {
            actionCG.alpha = 0f;
            yield return TweenHelper.Fade(actionCG, 1f, 0.2f);
        }
        else
        {
            // No CanvasGroup — just wait the fade-in duration
            yield return new WaitForSeconds(0.2f);
        }

        // Hold for 2.0 s
        yield return new WaitForSeconds(2.0f);

        // Fade out
        if (actionCG != null)
            yield return TweenHelper.Fade(actionCG, 0f, 0.3f);

        if (actionDisplay != null)
            actionDisplay.Clear();

        actionFadeCoroutine = null;
    }
}
