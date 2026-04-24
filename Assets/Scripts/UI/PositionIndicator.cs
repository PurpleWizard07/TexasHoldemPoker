using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays position indicators (D, SB, BB) in a circle beside player panels.
/// </summary>
public class PositionIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image circleBackground;
    [SerializeField] private TextMeshProUGUI positionText;
    
    [Header("Visual Settings")]
    [SerializeField] private Color dealerColor = new Color(1f, 0.85f, 0f, 1f); // Gold
    [SerializeField] private Color smallBlindColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue
    [SerializeField] private Color bigBlindColor = new Color(1f, 0.3f, 0.3f, 1f); // Red
    
    public enum PositionType
    {
        None,
        Dealer,
        SmallBlind,
        BigBlind
    }
    
    /// <summary>
    /// Set the position indicator type and visibility.
    /// </summary>
    public void SetPosition(PositionType type)
    {
        if (type == PositionType.None)
        {
            Hide();
            return;
        }
        
        Show();
        
        switch (type)
        {
            case PositionType.Dealer:
                if (positionText != null) positionText.text = "D";
                if (circleBackground != null) circleBackground.color = dealerColor;
                break;
                
            case PositionType.SmallBlind:
                if (positionText != null) positionText.text = "SB";
                if (circleBackground != null) circleBackground.color = smallBlindColor;
                break;
                
            case PositionType.BigBlind:
                if (positionText != null) positionText.text = "BB";
                if (circleBackground != null) circleBackground.color = bigBlindColor;
                break;
        }
    }
    
    private void Show()
    {
        gameObject.SetActive(true);
    }
    
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
