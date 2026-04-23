using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Self-contained action button for the poker Action Bar.
/// Handles hover/press scale animations, enabled/disabled opacity transitions,
/// and color-coding by action type.
/// Requirements: 7.1, 7.2, 7.3, 2.4
/// </summary>
public class ActionButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    // ── Serialized fields ────────────────────────────────────────────────────

    [SerializeField] private UITheme theme;
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ActionButtonType buttonType;

    // ── Constants ────────────────────────────────────────────────────────────

    private const float AlphaDisabled  = 0.35f;
    private const float AlphaEnabled   = 1.0f;
    private const float FadeDuration   = 0.2f;
    private const float ScaleHover     = 1.06f;
    private const float ScalePress     = 0.94f;
    private const float ScaleNormal    = 1.0f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private Coroutine _fadeCoroutine;
    private Coroutine _scaleCoroutine;

    // ── Enum ─────────────────────────────────────────────────────────────────

    public enum ActionButtonType { Fold, Check, Call, Bet, Raise, AllIn }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Null-check all serialized references and warn on missing ones
        if (theme == null)
            Debug.LogWarning($"[ActionButton] '{name}': theme is not assigned.", this);

        if (button == null)
            Debug.LogWarning($"[ActionButton] '{name}': button is not assigned.", this);

        if (buttonImage == null)
            Debug.LogWarning($"[ActionButton] '{name}': buttonImage is not assigned.", this);

        if (label == null)
            Debug.LogWarning($"[ActionButton] '{name}': label is not assigned.", this);

        if (canvasGroup == null)
            Debug.LogWarning($"[ActionButton] '{name}': canvasGroup is not assigned.", this);

        ApplyButtonColor();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the button label text directly.
    /// </summary>
    public void SetLabel(string text)
    {
        if (label != null)
            label.text = text;
    }

    /// <summary>
    /// Sets the button's interactable state and animates opacity to 0.35 (disabled)
    /// or 1.0 (enabled) over 0.2 seconds.
    /// Requirements: 7.2, 7.3
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (button != null)
            button.interactable = enabled;

        float targetAlpha = enabled ? AlphaEnabled : AlphaDisabled;

        if (canvasGroup != null)
        {
            StopFade();
            _fadeCoroutine = StartCoroutine(TweenHelper.Fade(canvasGroup, targetAlpha, FadeDuration));
        }
    }

    // ── Pointer event handlers ───────────────────────────────────────────────

    /// <summary>
    /// Hover enter: scale to 1.06 over theme.buttonHoverDuration.
    /// Requirements: 2.4
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        float duration = theme != null ? theme.buttonHoverDuration : 0.15f;
        StartScaleTo(ScaleHover, duration);
    }

    /// <summary>
    /// Hover exit: scale back to 1.0 over theme.buttonHoverDuration.
    /// Requirements: 2.4
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        float duration = theme != null ? theme.buttonHoverDuration : 0.15f;
        StartScaleTo(ScaleNormal, duration);
    }

    /// <summary>
    /// Press: scale to 0.94 immediately (short duration).
    /// Requirements: 7.1
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        StartScaleTo(ScalePress, 0.05f);
    }

    /// <summary>
    /// Release: scale back to 1.0 over theme.buttonHoverDuration.
    /// Requirements: 7.1
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        float duration = theme != null ? theme.buttonHoverDuration : 0.15f;
        StartScaleTo(ScaleNormal, duration);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Applies the background color to buttonImage based on buttonType.
    /// Requirements: 7.1
    /// </summary>
    private void ApplyButtonColor()
    {
        if (buttonImage == null || theme == null) return;

        Color color;
        switch (buttonType)
        {
            case ActionButtonType.Fold:
                color = theme.accentRed;
                break;
            case ActionButtonType.Check:
            case ActionButtonType.Call:
                color = theme.accentBlue;
                break;
            case ActionButtonType.Bet:
            case ActionButtonType.Raise:
                color = theme.accentGold;
                break;
            case ActionButtonType.AllIn:
                color = theme.accentOrange;
                break;
            default:
                color = theme.accentBlue;
                break;
        }

        buttonImage.color = color;
    }

    /// <summary>
    /// Stops any running scale coroutine and starts a new one lerping to targetScale.
    /// </summary>
    private void StartScaleTo(float targetScale, float duration)
    {
        StopScale();
        _scaleCoroutine = StartCoroutine(LerpScale(targetScale, duration));
    }

    /// <summary>
    /// Lerps transform.localScale uniformly to targetScale over duration seconds.
    /// </summary>
    private IEnumerator LerpScale(float targetScale, float duration)
    {
        Vector3 from = transform.localScale;
        Vector3 to   = new Vector3(targetScale, targetScale, targetScale);

        if (duration <= 0f)
        {
            transform.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        transform.localScale = to;
    }

    private void StopFade()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    private void StopScale()
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }
    }
}
