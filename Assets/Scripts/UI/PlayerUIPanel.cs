using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.Core;

/// <summary>
/// UI component for displaying a single player's information.
/// </summary>
public class PlayerUIPanel : MonoBehaviour
{
    [SerializeField] private PokerVisualTheme visualTheme;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image highlightImage;
    [SerializeField] private CardVisual[] cardVisuals = new CardVisual[2];
    [SerializeField] private BetDisplay betDisplay;
    [SerializeField] private DealerButtonDisplay dealerButton;
    [SerializeField] private PlayerActionDisplay actionDisplay;
    
    [Header("Glow Effect")]
    [SerializeField] private Image glowOutline;
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float minGlowAlpha = 0.3f;
    [SerializeField] private float maxGlowAlpha = 1f;
    [SerializeField] private Color glowColor = new Color(0f, 1f, 0.5f, 1f); // Green glow
    
    private bool isGlowing = false;
    private float glowTimer = 0f;

    private void Awake()
    {
        ApplyTheme();
    }

    public void UpdatePlayer(Player player, bool isActive, bool showCards = false, decimal currentBet = 0, bool isDealer = false)
    {
        if (player == null) return;

        // Update name
        if (playerNameText != null)
            playerNameText.text = player.Name;

        // Update stack
        if (stackText != null)
        {
            stackText.text = $"${player.Stack}";
            stackText.color = player.IsAllIn ? new Color(1f, 0.45f, 0.45f, 1f) : stackText.color;
        }

        // Update status
        if (statusText != null)
        {
            if (player.IsFolded)
            {
                statusText.text = "Folded";
                statusText.color = visualTheme != null ? visualTheme.DangerTextColor : new Color(0.95f, 0.34f, 0.34f, 1f);
            }
            else if (player.IsAllIn)
            {
                statusText.text = "All-In";
                statusText.color = visualTheme != null ? visualTheme.WarningTextColor : new Color(1f, 0.74f, 0.35f, 1f);
            }
            else
            {
                statusText.text = "";
            }
        }

        // Highlight active player (old method - kept for compatibility)
        if (highlightImage != null)
        {
            highlightImage.enabled = isActive;
            if (isActive)
                highlightImage.color = visualTheme != null ? new Color(visualTheme.ActiveSeatGlowColor.r, visualTheme.ActiveSeatGlowColor.g, visualTheme.ActiveSeatGlowColor.b, 0.26f) : new Color(1f, 0.8f, 0f, 0.3f);
        }
        
        // Enable/disable glowing outline
        SetGlowActive(isActive);

        // Update bet display
        if (betDisplay != null)
        {
            betDisplay.SetBet(currentBet);
        }

        // Update dealer button
        if (dealerButton != null)
        {
            dealerButton.SetActive(isDealer);
        }

        // Update cards
        UpdateCardDisplay(player, showCards);
    }
    
    private void Update()
    {
        if (isGlowing && glowOutline != null)
        {
            // Animate the glow with a pulsing effect
            glowTimer += Time.deltaTime * glowSpeed;
            float alpha = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, (Mathf.Sin(glowTimer) + 1f) / 2f);
            
            Color color = glowColor;
            color.a = alpha;
            glowOutline.color = color;
        }
    }
    
    private void SetGlowActive(bool active)
    {
        isGlowing = active;
        
        if (glowOutline != null)
        {
            glowOutline.enabled = active;
            if (active)
            {
                glowTimer = 0f;
                glowOutline.color = glowColor;
            }
        }
    }

    private void ApplyTheme()
    {
        if (visualTheme == null)
        {
            return;
        }

        glowColor = visualTheme.ActiveSeatGlowColor;
        minGlowAlpha = 0.25f;
        maxGlowAlpha = 0.9f;

        if (playerNameText != null)
        {
            playerNameText.color = visualTheme.PrimaryTextColor;
            playerNameText.fontSize = visualTheme.CaptionFontSize;
        }

        if (stackText != null)
        {
            stackText.color = visualTheme.PositiveTextColor;
            stackText.fontSize = visualTheme.BodyFontSize;
        }

        if (statusText != null)
        {
            statusText.color = visualTheme.SecondaryTextColor;
            statusText.fontSize = visualTheme.CaptionFontSize;
        }
    }

    private void UpdateCardDisplay(Player player, bool showCards)
    {
        if (cardVisuals == null || cardVisuals.Length < 2) return;

        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] == null) continue;

            if (i < player.HoleCards.Count && !player.IsFolded)
            {
                cardVisuals[i].SetCard(player.HoleCards[i], showCards);
            }
            else
            {
                cardVisuals[i].Clear();
            }
        }
    }

    public void ClearCards()
    {
        if (cardVisuals == null) return;
        
        foreach (var cardVisual in cardVisuals)
        {
            if (cardVisual != null)
                cardVisual.Clear();
        }
    }

    /// <summary>
    /// Show the last action taken by this player.
    /// </summary>
    public void ShowAction(string action, decimal amount = 0)
    {
        if (actionDisplay != null)
            actionDisplay.ShowAction(action, amount);
    }

    /// <summary>
    /// Clear the action display (called at start of new betting round).
    /// </summary>
    public void ClearAction()
    {
        if (actionDisplay != null)
            actionDisplay.Clear();
    }
}
