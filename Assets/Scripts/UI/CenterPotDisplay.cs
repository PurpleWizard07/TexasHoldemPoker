using UnityEngine;
using TMPro;

/// <summary>
/// Displays the center pot with visual chip stacks.
/// </summary>
public class CenterPotDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI potAmountText;
    [SerializeField] private ChipStack chipStack;
    [SerializeField] private Transform chipsContainer;

    [Header("Settings")]
    [SerializeField] private bool showVisualChips = true;
    [SerializeField] private bool animateChanges = true;
    [SerializeField] private float countUpDuration = 0.5f;

    private decimal currentPot = 0;
    private decimal displayedPot = 0;
    private float countUpTimer = 0;
    private bool isCountingUp = false;

    private void Awake()
    {
        // Auto-create ChipStack if needed
        if (showVisualChips && chipStack == null)
        {
            chipStack = GetComponentInChildren<ChipStack>();

            if (chipStack == null && chipsContainer != null)
            {
                GameObject chipStackObj = new GameObject("PotChipStack");
                chipStackObj.transform.SetParent(chipsContainer);
                chipStackObj.transform.localPosition = Vector3.zero;
                chipStackObj.transform.localScale = Vector3.one;

                RectTransform rect = chipStackObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(150, 60);
                rect.anchoredPosition = Vector2.zero;

                chipStack = chipStackObj.AddComponent<ChipStack>();
            }
        }
    }

    private void Update()
    {
        if (isCountingUp && animateChanges)
        {
            countUpTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(countUpTimer / countUpDuration);

            // Smooth step for nice easing
            progress = progress * progress * (3f - 2f * progress);

            displayedPot = (decimal)Mathf.Lerp((float)displayedPot, (float)currentPot, progress);

            UpdatePotText(displayedPot);

            if (progress >= 1f)
            {
                isCountingUp = false;
                displayedPot = currentPot;
                UpdatePotText(currentPot);
            }
        }
    }

    /// <summary>
    /// Set the pot amount with optional animation.
    /// </summary>
    public void SetPot(decimal amount)
    {
        if (amount == currentPot)
            return;

        decimal previousPot = currentPot;
        currentPot = amount;

        if (animateChanges && amount > previousPot)
        {
            // Animate count up
            countUpTimer = 0;
            isCountingUp = true;
        }
        else
        {
            // Instant update
            displayedPot = amount;
            UpdatePotText(amount);
        }

        // Update visual chips
        if (showVisualChips && chipStack != null)
        {
            chipStack.SetAmount(amount);
        }
    }

    /// <summary>
    /// Add to the current pot (used when collecting bets).
    /// </summary>
    public void AddToPot(decimal amount)
    {
        SetPot(currentPot + amount);
    }

    /// <summary>
    /// Clear the pot (after winner takes it).
    /// </summary>
    public void ClearPot()
    {
        currentPot = 0;
        displayedPot = 0;
        isCountingUp = false;

        UpdatePotText(0);

        if (chipStack != null)
            chipStack.ClearChips();
    }

    /// <summary>
    /// Get the current pot amount.
    /// </summary>
    public decimal GetCurrentPot()
    {
        return currentPot;
    }

    /// <summary>
    /// Get the transform for chip animations targeting the pot.
    /// </summary>
    public Transform GetPotTransform()
    {
        return chipsContainer != null ? chipsContainer : transform;
    }

    private void UpdatePotText(decimal amount)
    {
        if (potAmountText != null)
        {
            potAmountText.text = $"POT: ${amount:F0}";
        }
    }
}
