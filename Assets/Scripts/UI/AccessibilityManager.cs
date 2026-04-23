using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Singleton that manages accessibility mode across all scenes.
/// Reads PlayerPrefs on every scene load and applies high-contrast / larger-text settings.
/// Requirements: 11.1, 11.2, 11.3, 11.4, 11.5
/// </summary>
public class AccessibilityManager : MonoBehaviour
{
    public static AccessibilityManager Instance { get; private set; }

    private const string AccessibilityPrefKey = "AccessibilityMode";
    private const float  TextSizeMultiplier   = 1.25f;

    private bool _accessibilityEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _accessibilityEnabled = PlayerPrefs.GetInt(AccessibilityPrefKey, 0) == 1;
        ApplyAccessibilitySettings(_accessibilityEnabled);
    }

    /// <summary>
    /// Applies or removes accessibility settings immediately without a scene reload.
    /// Requirements: 11.5
    /// </summary>
    public void ToggleAccessibilityMode(bool enabled)
    {
        _accessibilityEnabled = enabled;
        PlayerPrefs.SetInt(AccessibilityPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAccessibilitySettings(enabled);
    }

    /// <summary>
    /// Applies accessibility settings to all TextMeshPro components in the scene.
    /// Requirements: 11.1, 11.2, 11.3
    /// </summary>
    private void ApplyAccessibilitySettings(bool enabled)
    {
        var allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var text in allTexts)
        {
            // Increase body text sizes by 25% when enabled
            // We store the original size in a tag to allow toggling back
            if (enabled)
            {
                if (!text.gameObject.name.EndsWith("_a11y_scaled"))
                {
                    text.fontSize *= TextSizeMultiplier;
                    text.gameObject.name += "_a11y_scaled";
                }

                // High-contrast: white text on dark backgrounds
                text.color = Color.white;
            }
            else
            {
                if (text.gameObject.name.EndsWith("_a11y_scaled"))
                {
                    text.fontSize /= TextSizeMultiplier;
                    text.gameObject.name = text.gameObject.name.Replace("_a11y_scaled", "");
                }
            }
        }
    }
}
