using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DontDestroyOnLoad singleton that manages Accessibility Mode across all scenes.
/// Reads PlayerPrefs on every scene load and applies high-contrast / larger-text settings
/// immediately without requiring a scene reload.
/// Requirements: 11.1, 11.2, 11.3, 11.4, 11.5
/// </summary>
public class AccessibilityManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static AccessibilityManager Instance { get; private set; }

    // ── Constants ────────────────────────────────────────────────────────────

    private const string AccessibilityPrefKey = "AccessibilityMode";
    private const float  TextSizeMultiplier   = 1.25f;

    /// <summary>Dark opaque background color used in high-contrast mode.</summary>
    private static readonly Color HighContrastBackground = new Color(0.05f, 0.05f, 0.05f, 1f);

    // ── Runtime state ────────────────────────────────────────────────────────

    private bool _accessibilityEnabled;

    /// <summary>
    /// Maps each TMP_Text instance to its original font size so it can be restored
    /// when accessibility mode is disabled.
    /// </summary>
    private readonly Dictionary<TMP_Text, float> _originalFontSizes = new Dictionary<TMP_Text, float>();

    /// <summary>
    /// Maps each panel Image instance to its original color so it can be restored.
    /// </summary>
    private readonly Dictionary<Image, Color> _originalPanelColors = new Dictionary<Image, Color>();

    /// <summary>
    /// Tracks icon-only GameObjects that had a label added by this manager so they
    /// can be removed when accessibility mode is disabled.
    /// </summary>
    private readonly List<GameObject> _addedLabels = new List<GameObject>();

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton enforcement — destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Apply on first load (Awake fires before OnSceneLoaded for the initial scene)
        _accessibilityEnabled = PlayerPrefs.GetInt(AccessibilityPrefKey, 0) == 1;
        ApplyAccessibilitySettings(_accessibilityEnabled);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Scene load handler ───────────────────────────────────────────────────

    /// <summary>
    /// Called by Unity whenever a scene finishes loading.
    /// Re-reads the preference and re-applies settings to the freshly loaded scene.
    /// Requirements: 11.4
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Clear cached references — they belong to the previous scene
        _originalFontSizes.Clear();
        _originalPanelColors.Clear();
        _addedLabels.Clear();

        _accessibilityEnabled = PlayerPrefs.GetInt(AccessibilityPrefKey, 0) == 1;
        ApplyAccessibilitySettings(_accessibilityEnabled);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Persists the accessibility preference and applies the change immediately
    /// without requiring a scene reload.
    /// Requirements: 11.4, 11.5
    /// </summary>
    public void ToggleAccessibilityMode(bool enabled)
    {
        _accessibilityEnabled = enabled;
        PlayerPrefs.SetInt(AccessibilityPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAccessibilitySettings(enabled);
    }

    /// <summary>Returns whether accessibility mode is currently active.</summary>
    public bool IsAccessibilityEnabled => _accessibilityEnabled;

    // ── Private implementation ───────────────────────────────────────────────

    /// <summary>
    /// Applies or removes all accessibility settings to the current scene's active UI.
    /// Requirements: 11.1, 11.2, 11.3
    /// </summary>
    private void ApplyAccessibilitySettings(bool enabled)
    {
        if (enabled)
        {
            ApplyLargerText();
            ApplyHighContrast();
            ApplyIconLabels();
        }
        else
        {
            RestoreOriginalSettings();
        }
    }

    // ── Enable path ──────────────────────────────────────────────────────────

    /// <summary>
    /// Increases all TMP_Text font sizes by 25% relative to their default values.
    /// Stores originals so they can be restored later.
    /// Requirements: 11.1
    /// </summary>
    private void ApplyLargerText()
    {
        var allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var text in allTexts)
        {
            if (text == null) continue;

            // Only scale once — if already stored, the original is already saved
            if (!_originalFontSizes.ContainsKey(text))
            {
                _originalFontSizes[text] = text.fontSize;
            }

            text.fontSize = _originalFontSizes[text] * TextSizeMultiplier;
        }
    }

    /// <summary>
    /// Applies high-contrast colors: white text on all TMP_Text components;
    /// dark opaque background on panel Image components that use the theme's
    /// panelBackground color.
    /// Requirements: 11.2
    /// </summary>
    private void ApplyHighContrast()
    {
        // White text on all TMP_Text components
        var allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var text in allTexts)
        {
            if (text == null) continue;
            text.color = Color.white;
        }

        // Dark background on panel Images
        var theme = LoadTheme();
        var allImages = FindObjectsOfType<Image>(true);
        foreach (var img in allImages)
        {
            if (img == null) continue;

            // Target images whose color closely matches the theme's panelBackground
            if (theme != null && ColorsApproximatelyEqual(img.color, theme.panelBackground))
            {
                if (!_originalPanelColors.ContainsKey(img))
                {
                    _originalPanelColors[img] = img.color;
                }
                img.color = HighContrastBackground;
            }
        }
    }

    /// <summary>
    /// Finds icon-only Image elements (Images with no sibling or child TMP_Text)
    /// and adds a descriptive text label child if one does not already exist.
    /// Requirements: 11.3
    /// </summary>
    private void ApplyIconLabels()
    {
        var allImages = FindObjectsOfType<Image>(true);
        foreach (var img in allImages)
        {
            if (img == null) continue;

            // Skip images that are part of a TMP_Text component itself
            if (img.GetComponent<TMP_Text>() != null) continue;

            // Check whether this Image has any TMP_Text in its children or siblings
            bool hasText = HasAssociatedText(img);
            if (hasText) continue;

            // Check whether we already added a label to this image
            bool alreadyLabelled = img.transform.Find("A11yLabel") != null;
            if (alreadyLabelled) continue;

            // Derive a description from the GameObject name
            string description = DeriveDescription(img.gameObject.name);
            if (string.IsNullOrEmpty(description)) continue;

            // Create a child label
            var labelGO = CreateAccessibilityLabel(img.transform, description);
            if (labelGO != null)
            {
                _addedLabels.Add(labelGO);
            }
        }
    }

    // ── Disable path ─────────────────────────────────────────────────────────

    /// <summary>
    /// Restores all original font sizes and colors, and removes added icon labels.
    /// </summary>
    private void RestoreOriginalSettings()
    {
        // Restore font sizes
        foreach (var kvp in _originalFontSizes)
        {
            if (kvp.Key == null) continue;
            kvp.Key.fontSize = kvp.Value;
        }
        _originalFontSizes.Clear();

        // Restore panel colors
        foreach (var kvp in _originalPanelColors)
        {
            if (kvp.Key == null) continue;
            kvp.Key.color = kvp.Value;
        }
        _originalPanelColors.Clear();

        // Remove added labels
        foreach (var label in _addedLabels)
        {
            if (label != null)
                Destroy(label);
        }
        _addedLabels.Clear();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the Image has any TMP_Text component among its children
    /// or among its siblings (i.e., other children of the same parent).
    /// </summary>
    private static bool HasAssociatedText(Image img)
    {
        // Check children
        if (img.GetComponentInChildren<TMP_Text>(true) != null)
            return true;

        // Check siblings (other children of the same parent)
        Transform parent = img.transform.parent;
        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var sibling = parent.GetChild(i);
                if (sibling == img.transform) continue;
                if (sibling.GetComponent<TMP_Text>() != null)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a small TMP_Text label as a child of <paramref name="parent"/>
    /// with the given description text.
    /// </summary>
    private static GameObject CreateAccessibilityLabel(Transform parent, string description)
    {
        if (parent == null)
        {
            Debug.LogWarning("[AccessibilityManager] Cannot create label — parent transform is null.");
            return null;
        }

        var labelGO = new GameObject("A11yLabel");
        labelGO.transform.SetParent(parent, false);

        var rectTransform = labelGO.AddComponent<RectTransform>();
        rectTransform.anchorMin        = new Vector2(0f, 0f);
        rectTransform.anchorMax        = new Vector2(1f, 0f);
        rectTransform.pivot            = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -2f);
        rectTransform.sizeDelta        = new Vector2(0f, 20f);

        var text = labelGO.AddComponent<TextMeshProUGUI>();
        text.text      = description;
        text.fontSize  = 12f;
        text.color     = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return labelGO;
    }

    /// <summary>
    /// Derives a human-readable description from a GameObject name by inserting
    /// spaces before capital letters and stripping common Unity suffixes.
    /// </summary>
    private static string DeriveDescription(string goName)
    {
        if (string.IsNullOrEmpty(goName)) return string.Empty;

        // Strip common suffixes
        string name = goName
            .Replace("Button", "")
            .Replace("Icon",   "")
            .Replace("Image",  "")
            .Replace("Img",    "")
            .Trim();

        if (string.IsNullOrEmpty(name)) return goName;

        // Insert spaces before uppercase letters (CamelCase → "Camel Case")
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Loads the UITheme from Resources. Returns null if not found and logs a warning.
    /// </summary>
    private static UITheme LoadTheme()
    {
        var theme = Resources.Load<UITheme>("UITheme");
        if (theme == null)
        {
            Debug.LogWarning("[AccessibilityManager] UITheme asset not found at Resources/UITheme. " +
                             "Panel background high-contrast override will be skipped.");
        }
        return theme;
    }

    /// <summary>
    /// Returns true if two colors are approximately equal within a small tolerance.
    /// Used to identify panel Images that use the theme's panelBackground color.
    /// </summary>
    private static bool ColorsApproximatelyEqual(Color a, Color b, float tolerance = 0.05f)
    {
        return Mathf.Abs(a.r - b.r) <= tolerance &&
               Mathf.Abs(a.g - b.g) <= tolerance &&
               Mathf.Abs(a.b - b.b) <= tolerance &&
               Mathf.Abs(a.a - b.a) <= tolerance;
    }
}
