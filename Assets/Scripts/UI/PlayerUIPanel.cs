using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerEngine.Core;

/// <summary>
/// UI component for displaying a single player's information.
/// </summary>
public class PlayerUIPanel : MonoBehaviour
{
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

    public void UpdatePlayer(Player player, bool isActive, bool showCards = false, decimal currentBet = 0, bool isDealer = false)
    {
        if (player == null) return;

        // Update name
        if (playerNameText != null)
            playerNameText.text = player.Name;

        // Update stack
        if (stackText != null)
            stackText.text = $"${player.Stack}";

        // Update status
        if (statusText != null)
        {
            if (player.IsFolded)
                statusText.text = "Folded";
            else if (player.IsAllIn)
                statusText.text = "All-In";
            else
                statusText.text = "";
        }

        // Highlight active player (old method - kept for compatibility)
        if (highlightImage != null)
        {
            highlightImage.enabled = isActive;
            if (isActive)
                highlightImage.color = new Color(1f, 0.8f, 0f, 0.3f); // Yellow highlight
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
