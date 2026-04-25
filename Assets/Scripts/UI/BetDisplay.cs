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

        // Force TMP to update its layout data before we read preferredWidth
        if (betText != null)
            betText.ForceMeshUpdate();

        // Enforce chip icon size so it never gets squashed
        if (chipIcon != null)
        {
            var iconRect = chipIcon.GetComponent<RectTransform>();
            if (iconRect != null)
                iconRect.sizeDelta = new Vector2(32f, 32f);
        }

        // Resize pill: chip(32) + gap(6) + text + padding(16)
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            float textWidth = betText != null ? betText.preferredWidth : 50f;
            rt.sizeDelta = new Vector2(32f + 6f + textWidth + 16f, rt.sizeDelta.y);
        }

        // Rebuild layout so all children reposition correctly
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
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
        long whole = (long)amount;
        if (whole >= 1_000_000)
            return $"{whole / 1_000_000f:0.#}M";
        if (whole >= 1_000)
            return $"{whole / 1_000f:0.#}K";
        return $"{whole}";
    }
}
