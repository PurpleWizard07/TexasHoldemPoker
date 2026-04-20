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

        // Hide panel initially
        if (raisePanel != null)
            raisePanel.SetActive(false);
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
}
