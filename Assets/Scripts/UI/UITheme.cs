using UnityEngine;
using TMPro;

/// <summary>
/// Centralized theme ScriptableObject for the Poker UI overhaul.
/// Stores all palette colors, font assets, animation durations, and layout constants.
/// Requirements: 1.4, 1.5, 12.5
/// </summary>
[CreateAssetMenu(menuName = "Poker/UITheme")]
public class UITheme : ScriptableObject
{
    // Minimum font size constants used in OnValidate
    private const float MinTitleFontSize = 36f;
    private const float MinLabelFontSize = 18f;
    private const float MinValueFontSize = 14f;

    [Header("Colors")]
    [Tooltip("Deep green felt background (~#1A3A2A)")]
    public Color primaryBackground = new Color(0.102f, 0.227f, 0.165f, 1f);

    [Tooltip("Dark burgundy panel background (alpha ~0.82)")]
    public Color panelBackground = new Color(0.18f, 0.06f, 0.08f, 0.82f);

    [Tooltip("Gold accent color (#FFD700) — used for pot text, glow, highlights")]
    public Color accentGold = new Color(1f, 0.843f, 0f, 1f);

    [Tooltip("Red accent — used for Fold button")]
    public Color accentRed = new Color(0.75f, 0.15f, 0.15f, 1f);

    [Tooltip("Blue accent — used for Check/Call button")]
    public Color accentBlue = new Color(0.15f, 0.35f, 0.75f, 1f);

    [Tooltip("Orange accent — used for All-In button")]
    public Color accentOrange = new Color(0.9f, 0.45f, 0.05f, 1f);

    [Tooltip("Primary text color (white)")]
    public Color textPrimary = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Muted text color (grey) — used for folded player state")]
    public Color textMuted = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Tooltip("Gold glow color — used for active player border pulse")]
    public Color glowGold = new Color(1f, 0.843f, 0f, 0.85f);

    [Header("Fonts")]
    [Tooltip("Display/title font — must be used at >= 36pt")]
    public TMP_FontAsset titleFont;

    [Tooltip("Label font — must be used at >= 18pt")]
    public TMP_FontAsset labelFont;

    [Tooltip("Value/body font — must be used at >= 14pt")]
    public TMP_FontAsset valueFont;

    [Header("Font Size Minimums (reference only — enforced by OnValidate)")]
    [Tooltip("Minimum font size for titleFont usage (36pt)")]
    public float titleFontMinSize = MinTitleFontSize;

    [Tooltip("Minimum font size for labelFont usage (18pt)")]
    public float labelFontMinSize = MinLabelFontSize;

    [Tooltip("Minimum font size for valueFont usage (14pt)")]
    public float valueFontMinSize = MinValueFontSize;

    [Header("Animation Durations")]
    [Tooltip("Duration of full-screen fade transitions (seconds)")]
    public float fadeTransitionDuration = 0.4f;

    [Tooltip("Duration of button hover scale animation (seconds)")]
    public float buttonHoverDuration = 0.15f;

    [Tooltip("Duration of action bar slide-in animation (seconds)")]
    public float actionBarShowDuration = 0.25f;

    [Tooltip("Duration of a single card deal arc animation (seconds)")]
    public float cardDealDuration = 0.35f;

    [Tooltip("Duration of each half of a card flip (seconds)")]
    public float cardFlipHalfDuration = 0.15f;

    [Tooltip("Duration of a chip flying to/from pot (seconds)")]
    public float chipFlyDuration = 0.6f;

    [Tooltip("Duration of pot-to-winner chip collection animation (seconds)")]
    public float potCollectDuration = 0.8f;

    [Tooltip("Duration of winner panel slide-in animation (seconds)")]
    public float winnerSlideDuration = 0.5f;

    [Tooltip("Speed of the gold glow pulse (cycles per second)")]
    public float glowPulseSpeed = 2.0f;

    [Header("Layout")]
    [Tooltip("Corner radius for panel backgrounds (pixels)")]
    public float panelCornerRadius = 8f;

    [Tooltip("Minimum alpha for panel backgrounds")]
    public float panelAlphaMin = 0.75f;

    [Tooltip("Maximum alpha for panel backgrounds")]
    public float panelAlphaMax = 0.90f;

    /// <summary>
    /// Called by the Unity Editor when the asset is modified.
    /// Logs warnings if font size minimums fall below the required hierarchy values.
    /// </summary>
    private void OnValidate()
    {
        if (titleFontMinSize < MinTitleFontSize)
        {
            Debug.LogWarning(
                $"[UITheme] titleFontMinSize ({titleFontMinSize}pt) is below the required minimum of {MinTitleFontSize}pt. " +
                "Title font must be used at 36pt or larger.",
                this);
        }

        if (labelFontMinSize < MinLabelFontSize)
        {
            Debug.LogWarning(
                $"[UITheme] labelFontMinSize ({labelFontMinSize}pt) is below the required minimum of {MinLabelFontSize}pt. " +
                "Label font must be used at 18pt or larger.",
                this);
        }

        if (valueFontMinSize < MinValueFontSize)
        {
            Debug.LogWarning(
                $"[UITheme] valueFontMinSize ({valueFontMinSize}pt) is below the required minimum of {MinValueFontSize}pt. " +
                "Value font must be used at 14pt or larger.",
                this);
        }

        if (panelAlphaMin < 0f || panelAlphaMin > 1f)
        {
            Debug.LogWarning($"[UITheme] panelAlphaMin ({panelAlphaMin}) is out of range [0, 1].", this);
        }

        if (panelAlphaMax < 0f || panelAlphaMax > 1f)
        {
            Debug.LogWarning($"[UITheme] panelAlphaMax ({panelAlphaMax}) is out of range [0, 1].", this);
        }

        if (panelAlphaMin > panelAlphaMax)
        {
            Debug.LogWarning(
                $"[UITheme] panelAlphaMin ({panelAlphaMin}) is greater than panelAlphaMax ({panelAlphaMax}).",
                this);
        }
    }

    /// <summary>
    /// Creates a new UITheme instance with hardcoded default values.
    /// Used as a runtime fallback when Resources.Load&lt;UITheme&gt;("UITheme") returns null.
    /// Requirements: 1.5, 12.5
    /// </summary>
    public static UITheme CreateDefault()
    {
        var theme = CreateInstance<UITheme>();
        theme.name = "UITheme_Default";

        // Colors
        theme.primaryBackground = new Color(0.102f, 0.227f, 0.165f, 1f);   // ~#1A3A2A deep green
        theme.panelBackground   = new Color(0.18f,  0.06f,  0.08f,  0.82f); // dark burgundy
        theme.accentGold        = new Color(1f,     0.843f, 0f,     1f);    // #FFD700
        theme.accentRed         = new Color(0.75f,  0.15f,  0.15f,  1f);
        theme.accentBlue        = new Color(0.15f,  0.35f,  0.75f,  1f);
        theme.accentOrange      = new Color(0.9f,   0.45f,  0.05f,  1f);
        theme.textPrimary       = new Color(1f,     1f,     1f,     1f);
        theme.textMuted         = new Color(0.55f,  0.55f,  0.55f,  1f);
        theme.glowGold          = new Color(1f,     0.843f, 0f,     0.85f);

        // Fonts — null in fallback; components must handle null font gracefully
        theme.titleFont = null;
        theme.labelFont = null;
        theme.valueFont = null;

        // Font size minimums
        theme.titleFontMinSize = MinTitleFontSize;
        theme.labelFontMinSize = MinLabelFontSize;
        theme.valueFontMinSize = MinValueFontSize;

        // Animation durations
        theme.fadeTransitionDuration = 0.4f;
        theme.buttonHoverDuration    = 0.15f;
        theme.actionBarShowDuration  = 0.25f;
        theme.cardDealDuration       = 0.35f;
        theme.cardFlipHalfDuration   = 0.15f;
        theme.chipFlyDuration        = 0.6f;
        theme.potCollectDuration     = 0.8f;
        theme.winnerSlideDuration    = 0.5f;
        theme.glowPulseSpeed         = 2.0f;

        // Layout
        theme.panelCornerRadius = 8f;
        theme.panelAlphaMin     = 0.75f;
        theme.panelAlphaMax     = 0.90f;

        return theme;
    }
}
