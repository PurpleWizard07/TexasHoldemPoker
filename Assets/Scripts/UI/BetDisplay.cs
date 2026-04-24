using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the player's current round bet as a chip icon + formatted amount
/// in a small dark pill, intended to sit just above the PlayerUIPanel.
/// Hidden when bet is zero.
/// </summary>
public class BetDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image chipIcon;
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private Image pillBackground;

    [Header("Chip Sprite")]
    [SerializeField] private Sprite chipSprite;

    [Header("Animation Origin")]
    [SerializeField] private Transform chipAnimationOrigin;

    private decimal currentBet = 0;

    private void Awake()
    {
        // Load chip sprite from Resources if not assigned
        if (chipIcon != null && chipSprite == null)
        {
            // Try to load any chip sprite from Resources
            var loaded = Resources.Load<Sprite>("chip25");
            if (loaded != null)
            {
                chipSprite = loaded;
                chipIcon.sprite = chipSprite;
            }
        }

        Hide();
    }

    public void SetBet(decimal amount)
    {
        currentBet = amount;

        if (amount > 0)
        {
            if (betText != null)
                betText.text = FormatAmount(amount);

            if (chipIcon != null && chipSprite != null)
                chipIcon.sprite = chipSprite;

            Show();
        }
        else
        {
            Hide();
        }
    }

    public decimal GetCurrentBet() => currentBet;

    public Transform GetAnimationOrigin() =>
        chipAnimationOrigin != null ? chipAnimationOrigin : transform;

    public void Clear()
    {
        currentBet = 0;
        Hide();
    }

    public void ClearForNewRound() => Clear();

    private void Show()
    {
        if (pillBackground != null) pillBackground.enabled = true;
        if (chipIcon != null)       chipIcon.enabled = true;
        if (betText != null)        betText.enabled = true;
    }

    private void Hide()
    {
        if (pillBackground != null) pillBackground.enabled = false;
        if (chipIcon != null)       chipIcon.enabled = false;
        if (betText != null)        betText.enabled = false;
    }

    /// <summary>Format 1100 → 1.1K, 500 → 500</summary>
    private string FormatAmount(decimal amount)
    {
        if (amount >= 1_000_000m)
            return $"{amount / 1_000_000m:0.#}M";
        if (amount >= 1_000m)
            return $"{amount / 1_000m:0.#}K";
        return $"{amount:0}";
    }
}
