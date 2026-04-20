using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays the last action taken by a player (Fold, Check, Call, Raise, etc.)
/// </summary>
public class PlayerActionDisplay : MonoBehaviour
{
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private TextMeshProUGUI actionText;
    
    [Header("Colors")]
    [SerializeField] private Color foldColor = new Color(0.8f, 0.2f, 0.2f); // Red
    [SerializeField] private Color checkColor = new Color(0.5f, 0.5f, 0.5f); // Gray
    [SerializeField] private Color callColor = new Color(0.3f, 0.7f, 0.3f); // Green
    [SerializeField] private Color raiseColor = new Color(1f, 0.6f, 0f); // Orange
    [SerializeField] private Color allInColor = new Color(1f, 0f, 0f); // Bright Red

    private void Start()
    {
        Clear();
    }

    /// <summary>
    /// Show an action with appropriate color.
    /// </summary>
    public void ShowAction(string action, decimal amount = 0)
    {
        if (actionPanel != null)
            actionPanel.SetActive(true);

        if (actionText != null)
        {
            // Format the action text
            string displayText = FormatAction(action, amount);
            actionText.text = displayText;
            
            // Set color based on action type
            actionText.color = GetActionColor(action);
        }
    }

    /// <summary>
    /// Clear the action display.
    /// </summary>
    public void Clear()
    {
        if (actionPanel != null)
            actionPanel.SetActive(false);
        
        if (actionText != null)
            actionText.text = "";
    }

    private string FormatAction(string action, decimal amount)
    {
        action = action.ToUpper();
        
        // Return only action names, no amounts
        switch (action)
        {
            case "FOLD":
                return "FOLD";
            
            case "CHECK":
                return "CHECK";
            
            case "CALL":
                return "CALL";
            
            case "BET":
                return "BET";
            
            case "RAISE":
                return "RAISE";
            
            case "ALLIN":
            case "ALL-IN":
                return "ALL-IN";
            
            default:
                return action;
        }
    }

    private Color GetActionColor(string action)
    {
        action = action.ToUpper();
        
        switch (action)
        {
            case "FOLD":
                return foldColor;
            
            case "CHECK":
                return checkColor;
            
            case "CALL":
                return callColor;
            
            case "BET":
            case "RAISE":
                return raiseColor;
            
            case "ALLIN":
            case "ALL-IN":
                return allInColor;
            
            default:
                return Color.white;
        }
    }
}
