using UnityEngine;

/// <summary>
/// Centralized visual tokens for poker UI style, spacing, and timings.
/// Create one or more theme assets and assign in UI components.
/// </summary>
[CreateAssetMenu(fileName = "PokerVisualTheme", menuName = "Poker/UI Visual Theme")]
public class PokerVisualTheme : ScriptableObject
{
    [Header("Table")]
    [SerializeField] private Color tableFeltColor = new Color(0.05f, 0.33f, 0.21f, 1f);
    [SerializeField] private Color panelBackgroundColor = new Color(0.07f, 0.09f, 0.12f, 0.85f);
    [SerializeField] private Color panelBorderColor = new Color(0.37f, 0.45f, 0.57f, 1f);

    [Header("Text")]
    [SerializeField] private Color primaryTextColor = new Color(0.95f, 0.97f, 1f, 1f);
    [SerializeField] private Color secondaryTextColor = new Color(0.71f, 0.78f, 0.88f, 1f);
    [SerializeField] private Color positiveTextColor = new Color(0.29f, 0.92f, 0.57f, 1f);
    [SerializeField] private Color warningTextColor = new Color(1f, 0.73f, 0.31f, 1f);
    [SerializeField] private Color dangerTextColor = new Color(1f, 0.39f, 0.39f, 1f);

    [Header("State Colors")]
    [SerializeField] private Color activeSeatGlowColor = new Color(0.17f, 0.87f, 1f, 1f);
    [SerializeField] private Color dealerBadgeColor = new Color(1f, 0.82f, 0.29f, 1f);
    [SerializeField] private Color actionFoldColor = new Color(0.93f, 0.34f, 0.34f, 1f);
    [SerializeField] private Color actionCheckColor = new Color(0.76f, 0.81f, 0.88f, 1f);
    [SerializeField] private Color actionCallColor = new Color(0.31f, 0.88f, 0.6f, 1f);
    [SerializeField] private Color actionRaiseColor = new Color(1f, 0.67f, 0.31f, 1f);
    [SerializeField] private Color actionAllInColor = new Color(1f, 0.31f, 0.31f, 1f);

    [Header("Sizing")]
    [SerializeField] private float titleFontSize = 30f;
    [SerializeField] private float bodyFontSize = 22f;
    [SerializeField] private float captionFontSize = 16f;
    [SerializeField] private float actionButtonFontSize = 24f;
    [SerializeField] private float panelCornerRadius = 14f;
    [SerializeField] private float buttonCornerRadius = 10f;

    [Header("Animation Timings")]
    [SerializeField] private float dealDuration = 0.28f;
    [SerializeField] private float dealDelay = 0.1f;
    [SerializeField] private float cardFlipDuration = 0.2f;
    [SerializeField] private float chipFlightDuration = 0.44f;
    [SerializeField] private float winnerPulseDuration = 0.22f;

    public Color TableFeltColor => tableFeltColor;
    public Color PanelBackgroundColor => panelBackgroundColor;
    public Color PanelBorderColor => panelBorderColor;
    public Color PrimaryTextColor => primaryTextColor;
    public Color SecondaryTextColor => secondaryTextColor;
    public Color PositiveTextColor => positiveTextColor;
    public Color WarningTextColor => warningTextColor;
    public Color DangerTextColor => dangerTextColor;
    public Color ActiveSeatGlowColor => activeSeatGlowColor;
    public Color DealerBadgeColor => dealerBadgeColor;
    public Color ActionFoldColor => actionFoldColor;
    public Color ActionCheckColor => actionCheckColor;
    public Color ActionCallColor => actionCallColor;
    public Color ActionRaiseColor => actionRaiseColor;
    public Color ActionAllInColor => actionAllInColor;
    public float TitleFontSize => titleFontSize;
    public float BodyFontSize => bodyFontSize;
    public float CaptionFontSize => captionFontSize;
    public float ActionButtonFontSize => actionButtonFontSize;
    public float PanelCornerRadius => panelCornerRadius;
    public float ButtonCornerRadius => buttonCornerRadius;
    public float DealDuration => dealDuration;
    public float DealDelay => dealDelay;
    public float CardFlipDuration => cardFlipDuration;
    public float ChipFlightDuration => chipFlightDuration;
    public float WinnerPulseDuration => winnerPulseDuration;
}
