using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main menu UI controller. Handles the intro animation, button hover states,
/// scene transitions, and the accessibility mode settings toggle.
/// Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ── Serialized fields ────────────────────────────────────────────────────

    [SerializeField] private UITheme theme;
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Settings Panel (optional)")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Toggle accessibilityToggle;

    // ── Constants ────────────────────────────────────────────────────────────

    private const string AccessibilityPrefKey  = "AccessibilityMode";
    private const float  TransitionDuration    = 0.4f;
    private const float  HoverScale            = 1.08f;
    private const float  NormalScale           = 1.0f;
    private const float  IntroDuration         = 1.2f;
    private const float  TitleSlideOffsetY     = -60f;  // starts below final position
    private const float  ButtonSlideOffsetY    = -40f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private Coroutine _playHoverCoroutine;
    private Coroutine _settingsHoverCoroutine;
    private Coroutine _quitHoverCoroutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (theme == null)
            Debug.LogWarning($"[MainMenuUI] '{name}': theme is not assigned.", this);

        if (titleTransform == null)
            Debug.LogWarning($"[MainMenuUI] '{name}': titleTransform is not assigned.", this);

        if (titleText == null)
            Debug.LogWarning($"[MainMenuUI] '{name}': titleText is not assigned.", this);

        if (playButton == null)
            Debug.LogWarning($"[MainMenuUI] '{name}': playButton is not assigned.", this);

        if (quitButton == null)
            Debug.LogWarning($"[MainMenuUI] '{name}': quitButton is not assigned.", this);

        if (menuCanvasGroup == null)
            Debug.LogWarning($"[MainMenuUI] '{name}': menuCanvasGroup is not assigned.", this);
    }

    private void Start()
    {
        // Start fully transparent so the intro animation fades in
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.blocksRaycasts = false;
        }

        // Hide settings panel by default
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Restore accessibility toggle state from PlayerPrefs
        if (accessibilityToggle != null)
        {
            bool accessibilityEnabled = PlayerPrefs.GetInt(AccessibilityPrefKey, 0) == 1;
            accessibilityToggle.isOn = accessibilityEnabled;
            accessibilityToggle.onValueChanged.AddListener(OnAccessibilityToggleChanged);
        }

        SetupButtonAnimations();
        StartCoroutine(PlayIntroAnimation());
    }

    // ── Intro animation ──────────────────────────────────────────────────────

    /// <summary>
    /// Staggered fade-and-slide entrance for the title and buttons, completing within 1.2s.
    /// Requirements: 2.3
    /// </summary>
    private IEnumerator PlayIntroAnimation()
    {
        float elementDuration = 0.35f;
        float stagger         = 0.15f;

        // Capture final positions and set starting offsets
        Vector2 titleFinalPos   = titleTransform != null ? titleTransform.anchoredPosition : Vector2.zero;
        Vector2 titleStartPos   = titleFinalPos + new Vector2(0f, TitleSlideOffsetY);

        Vector2 playFinalPos    = Vector2.zero;
        Vector2 settingsFinalPos = Vector2.zero;
        Vector2 quitFinalPos    = Vector2.zero;

        RectTransform playRT     = playButton     != null ? playButton.GetComponent<RectTransform>()     : null;
        RectTransform settingsRT = settingsButton != null ? settingsButton.GetComponent<RectTransform>() : null;
        RectTransform quitRT     = quitButton     != null ? quitButton.GetComponent<RectTransform>()     : null;

        if (playRT     != null) { playFinalPos     = playRT.anchoredPosition;     playRT.anchoredPosition     = playFinalPos     + new Vector2(0f, ButtonSlideOffsetY); }
        if (settingsRT != null) { settingsFinalPos = settingsRT.anchoredPosition; settingsRT.anchoredPosition = settingsFinalPos + new Vector2(0f, ButtonSlideOffsetY); }
        if (quitRT     != null) { quitFinalPos     = quitRT.anchoredPosition;     quitRT.anchoredPosition     = quitFinalPos     + new Vector2(0f, ButtonSlideOffsetY); }

        if (titleTransform != null)
            titleTransform.anchoredPosition = titleStartPos;

        // Fade the whole canvas in while sliding elements into place
        if (menuCanvasGroup != null)
            StartCoroutine(TweenHelper.Fade(menuCanvasGroup, 1f, IntroDuration * 0.6f));

        // Title slides in first
        if (titleTransform != null)
            StartCoroutine(TweenHelper.Slide(titleTransform, titleFinalPos, elementDuration));

        yield return new WaitForSeconds(stagger);

        // Buttons slide in with stagger
        if (playRT != null)
            StartCoroutine(TweenHelper.Slide(playRT, playFinalPos, elementDuration));

        yield return new WaitForSeconds(stagger);

        if (settingsRT != null)
            StartCoroutine(TweenHelper.Slide(settingsRT, settingsFinalPos, elementDuration));

        yield return new WaitForSeconds(stagger);

        if (quitRT != null)
            StartCoroutine(TweenHelper.Slide(quitRT, quitFinalPos, elementDuration));

        // Wait for the full intro to finish before enabling interaction
        float remaining = IntroDuration - (stagger * 3f);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.blocksRaycasts = true;
        }
    }

    // ── Button setup ─────────────────────────────────────────────────────────

    /// <summary>
    /// Wires hover (scale 1.08 over 0.15s) and click handlers to all buttons.
    /// Requirements: 2.4
    /// </summary>
    private void SetupButtonAnimations()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
            WireHoverEvents(playButton.gameObject,
                () => OnButtonHoverEnter(playButton.transform, ref _playHoverCoroutine),
                () => OnButtonHoverExit(playButton.transform, ref _playHoverCoroutine));
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            WireHoverEvents(settingsButton.gameObject,
                () => OnButtonHoverEnter(settingsButton.transform, ref _settingsHoverCoroutine),
                () => OnButtonHoverExit(settingsButton.transform, ref _settingsHoverCoroutine));
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            WireHoverEvents(quitButton.gameObject,
                () => OnButtonHoverEnter(quitButton.transform, ref _quitHoverCoroutine),
                () => OnButtonHoverExit(quitButton.transform, ref _quitHoverCoroutine));
        }
    }

    // ── Button click handlers ────────────────────────────────────────────────

    /// <summary>
    /// Transitions to the PokerGame scene with a fade animation within 0.4s.
    /// Requirements: 2.5
    /// </summary>
    private void OnPlayClicked()
    {
        if (SceneTransitionManager.Instance != null)
            StartCoroutine(SceneTransitionManager.Instance.TransitionToScene("PokerGame", TransitionDuration));
        else
            Debug.LogWarning("[MainMenuUI] SceneTransitionManager.Instance is null — cannot transition to PokerGame.");
    }

    /// <summary>
    /// Toggles the settings panel visibility.
    /// </summary>
    private void OnSettingsClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    /// <summary>
    /// Fades out then quits the application.
    /// </summary>
    private void OnQuitClicked()
    {
        StartCoroutine(QuitSequence());
    }

    private IEnumerator QuitSequence()
    {
        if (SceneTransitionManager.Instance != null)
            yield return SceneTransitionManager.Instance.FadeToBlack(TransitionDuration);
        else if (menuCanvasGroup != null)
            yield return TweenHelper.Fade(menuCanvasGroup, 0f, TransitionDuration);

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Accessibility ────────────────────────────────────────────────────────

    /// <summary>
    /// Persists the accessibility mode preference and applies it immediately.
    /// Requirements: 2.6, 11.4, 11.5
    /// </summary>
    private void OnAccessibilityToggleChanged(bool enabled)
    {
        PlayerPrefs.SetInt(AccessibilityPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.ToggleAccessibilityMode(enabled);
    }

    // ── Hover helpers ────────────────────────────────────────────────────────

    private void OnButtonHoverEnter(Transform t, ref Coroutine coroutine)
    {
        StopHoverCoroutine(ref coroutine);
        coroutine = StartCoroutine(LerpScale(t, HoverScale));
    }

    private void OnButtonHoverExit(Transform t, ref Coroutine coroutine)
    {
        StopHoverCoroutine(ref coroutine);
        coroutine = StartCoroutine(LerpScale(t, NormalScale));
    }

    private IEnumerator LerpScale(Transform t, float targetScale)
    {
        if (t == null) yield break;

        float duration = theme != null ? theme.buttonHoverDuration : 0.15f;
        Vector3 from   = t.localScale;
        Vector3 to     = new Vector3(targetScale, targetScale, targetScale);

        if (duration <= 0f)
        {
            t.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.LerpUnclamped(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        t.localScale = to;
    }

    private void WireHoverEvents(GameObject target, UnityEngine.Events.UnityAction onEnter,
        UnityEngine.Events.UnityAction onExit)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => onExit());
        trigger.triggers.Add(exitEntry);
    }

    private void StopHoverCoroutine(ref Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }
}
