using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple raise amount control with text input field.
/// Click Raise/Bet to show, enter amount, click Raise/Bet again to confirm.
/// </summary>
public class RaiseControl : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject raisePanel;
    [SerializeField] private TMP_InputField amountInputField;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TextMeshProUGUI rangeText;
    [SerializeField] private PokerVisualTheme visualTheme;

    private decimal minRaise;
    private decimal maxRaise;
    private decimal currentRaise;
    private bool isVisible = false;

    private void Awake()
    {
        // Setup input field listener
        if (amountInputField != null)
        {
            amountInputField.onValueChanged.AddListener(OnInputChanged);
            amountInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        }
        if (amountSlider != null)
        {
            amountSlider.onValueChanged.AddListener(OnSliderChanged);
            amountSlider.wholeNumbers = true;
        }

        // Hide panel initially
        if (raisePanel != null)
            raisePanel.SetActive(false);

        ApplyTheme();
    }

    /// <summary>
    /// Check if the raise control is currently visible.
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }

    /// <summary>
    /// Show the raise control with min/max bounds.
    /// </summary>
    public void Show(decimal minRaiseAmount, decimal maxRaiseAmount)
    {
        minRaise = minRaiseAmount;
        maxRaise = maxRaiseAmount;
        
        // Set initial amount to minimum
        currentRaise = minRaise;
        
        // Set input field to minimum value
        if (amountInputField != null)
        {
            amountInputField.text = minRaise.ToString("0");
            amountInputField.Select();
            amountInputField.ActivateInputField();
        }
        if (amountSlider != null)
        {
            amountSlider.minValue = (float)minRaise;
            amountSlider.maxValue = (float)maxRaise;
            amountSlider.value = (float)currentRaise;
        }
        if (rangeText != null)
        {
            rangeText.text = $"MIN ${minRaise:0}  -  MAX ${maxRaise:0}";
        }

        // Show panel
        if (raisePanel != null)
            raisePanel.SetActive(true);
        
        isVisible = true;
        Debug.Log($"Raise control shown: Min=${minRaise}, Max=${maxRaise}");
    }

    /// <summary>
    /// Hide the raise control.
    /// </summary>
    public void Hide()
    {
        if (raisePanel != null)
            raisePanel.SetActive(false);
        
        isVisible = false;
    }

    /// <summary>
    /// Get the current raise amount selected.
    /// </summary>
    public decimal GetRaiseAmount()
    {
        // Parse current input field value
        if (amountInputField != null && decimal.TryParse(amountInputField.text, out decimal amount))
        {
            currentRaise = System.Math.Clamp(amount, minRaise, maxRaise);
        }
        
        return currentRaise;
    }

    private void OnInputChanged(string value)
    {
        // Try to parse the input
        if (decimal.TryParse(value, out decimal amount))
        {
            // Clamp to valid range
            currentRaise = System.Math.Clamp(amount, minRaise, maxRaise);
        }
        else
        {
            // Invalid input, reset to minimum
            currentRaise = minRaise;
        }

        if (amountSlider != null)
        {
            amountSlider.SetValueWithoutNotify((float)currentRaise);
        }
    }

    /// <summary>
    /// Set the raise amount programmatically.
    /// </summary>
    public void SetRaiseAmount(decimal amount)
    {
        amount = System.Math.Clamp(amount, minRaise, maxRaise);
        currentRaise = amount;
        
        if (amountInputField != null)
            amountInputField.text = amount.ToString("0");
        if (amountSlider != null)
            amountSlider.SetValueWithoutNotify((float)amount);
    }

    /// <summary>
    /// Quick set to minimum raise.
    /// </summary>
    public void SetToMinimum()
    {
        SetRaiseAmount(minRaise);
    }

    /// <summary>
    /// Quick set to pot-sized raise.
    /// </summary>
    public void SetToPotSize(decimal potSize)
    {
        SetRaiseAmount(potSize);
    }

    /// <summary>
    /// Quick set to half pot.
    /// </summary>
    public void SetToHalfPot(decimal potSize)
    {
        SetRaiseAmount(potSize / 2);
    }

    private void OnSliderChanged(float value)
    {
        SetRaiseAmount((decimal)value);
    }

    private void ApplyTheme()
    {
        if (visualTheme == null)
        {
            return;
        }

        if (amountInputField != null && amountInputField.textComponent != null)
        {
            amountInputField.textComponent.color = visualTheme.PrimaryTextColor;
            amountInputField.textComponent.fontSize = visualTheme.BodyFontSize;
        }
        if (rangeText != null)
        {
            rangeText.color = visualTheme.SecondaryTextColor;
            rangeText.fontSize = visualTheme.CaptionFontSize;
        }
    }
}
