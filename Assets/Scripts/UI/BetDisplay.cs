using UnityEngine;
using TMPro;

/// <summary>
/// Displays a player's current bet amount for the round with visual chip stacks.
/// </summary>
public class BetDisplay : MonoBehaviour
{
    [Header("Text Display")]
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private GameObject betContainer;
    
    [Header("Visual Chips")]
    [SerializeField] private ChipStack chipStack;
    [SerializeField] private bool useVisualChips = true;

    private decimal currentBet = 0;

    private void Awake()
    {
        // Auto-create ChipStack if using visual chips but none assigned
        if (useVisualChips && chipStack == null)
        {
            // Check if we have one as a child
            chipStack = GetComponentInChildren<ChipStack>();
            
            // Create one if needed
            if (chipStack == null && betContainer != null)
            {
                GameObject chipStackObj = new GameObject("ChipStack");
                chipStackObj.transform.SetParent(betContainer.transform);
                chipStackObj.transform.localPosition = new Vector3(30, 0, 0); // Offset from text
                chipStackObj.transform.localScale = Vector3.one;
                
                RectTransform rect = chipStackObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100, 50);
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                
                chipStack = chipStackObj.AddComponent<ChipStack>();
            }
        }
    }

    public void SetBet(decimal amount)
    {
        currentBet = amount;
        
        if (amount > 0)
        {
            if (betText != null)
                betText.text = $"${amount}";
            
            if (betContainer != null)
                betContainer.SetActive(true);
            
            // Update visual chips
            if (useVisualChips && chipStack != null)
            {
                chipStack.SetAmount(amount);
            }
        }
        else
        {
            Clear();
        }
    }

    /// <summary>
    /// Get the current bet amount.
    /// </summary>
    public decimal GetCurrentBet()
    {
        return currentBet;
    }

    /// <summary>
    /// Get the world position of this bet display (for chip animations).
    /// </summary>
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void Clear()
    {
        currentBet = 0;
        
        if (betContainer != null)
            betContainer.SetActive(false);
        
        if (chipStack != null)
            chipStack.ClearChips();
    }

    // Clear bet when new round starts
    public void ClearForNewRound()
    {
        Clear();
    }
}
