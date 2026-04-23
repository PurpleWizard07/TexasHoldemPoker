using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Raise/Bet amount selector that slides up from the bottom of the screen over the Action Bar.
/// Provides a slider, numeric input field, preset buttons (Min, ½ Pot, Pot, All-In),
/// a confirm button, and a warning label for out-of-range input.
/// Requirements: 7.6, 7.7, 7.8, 7.9
/// </summary>
public class RaiseControl : MonoBehaviour
{
    // ── Serialized fields ────────────────────────────────────────────────────

    [SerializeField] private UITheme theme;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Input Controls")]
    [SerializeField] private Slider raiseSlider;
    [SerializeField] private TMP_InputField amountInputField;

    [Header("Preset Buttons")]
    [SerializeField] private Button minButton;
    [SerializeField] private Button halfPotButton;
    [SerializeField] private Button potButton;
    [SerializeField] private Button allInButton;

    [Header("Confirm / Cancel")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Warning")]
    [SerializeField] private TextMeshProUGUI warningLabel;

    // ── Constants ────────────────────────────────────────────────────────────

    private const float ShowDuration    = 0.3f;
    private const float HideDuration    = 0.2f;
    private const float WarningDuration = 1.5f;
    private const float HiddenOffsetY   = -300f;   // pixels below resting position

    // ── Runtime state ────────────────────────────────────────────────────────

    private decimal _minRaise;
    private decimal _maxRaise;
    private decimal _potSize;
    private decimal _currentAmount;

    private Vector2 _restingPosition;
    private Coroutine _animCoroutine;
    private Coroutine _warningCoroutine;

    /// <summary>
    /// Invoked when the confirm button is pressed.
    /// Receives the clamped raise amount selected by the player.
    /// </summary>
    public event Action<decimal> OnConfirm;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (theme == null)
            Debug.LogWarning($"[RaiseControl] '{name}': theme is not assigned.", this);
        if (panelRect == null)
            Debug.LogWarning($"[RaiseControl] '{name}': panelRect is not assigned.", this);
        if (panelCanvasGroup == null)
            Debug.LogWarning($"[RaiseControl] '{name}': panelCanvasGroup is not assigned.", this);
        if (raiseSlider == null)
            Debug.LogWarning($"[RaiseControl] '{name}': raiseSlider is not assigned.", this);
        if (amountInputField == null)
            Debug.LogWarning($"[RaiseControl] '{name}': amountInputField is not assigned.", this);
        if (confirmButton == null)
            Debug.LogWarning($"[RaiseControl] '{name}': confirmButton is not assigned.", this);

        // Cache resting position before we move the panel off-screen
        if (panelRect != null)
            _restingPosition = panelRect.anchoredPosition;

        // Wire up controls
        if (raiseSlider != null)
            raiseSlider.onValueChanged.AddListener(OnSliderChanged);

        if (amountInputField != null)
        {
            amountInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            amountInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
        }

        if (minButton != null)
            minButton.onClick.AddListener(OnMinClicked);
        if (halfPotButton != null)
            halfPotButton.onClick.AddListener(OnHalfPotClicked);
        if (potButton != null)
            potButton.onClick.AddListener(OnPotClicked);
        if (allInButton != null)
            allInButton.onClick.AddListener(OnAllInClicked);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // Hide warning label initially
        if (warningLabel != null)
            warningLabel.gameObject.SetActive(false);

        // Start hidden below screen
        HideImmediate();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Slides the raise control up from the bottom over 0.3 seconds.
    /// Presets the slider and input field to <paramref name="minRaise"/>.
    /// Requirements: 7.6, 7.7
    /// </summary>
    public void Show(decimal minRaise, decimal maxRaise, decimal potSize)
    {
        _minRaise  = minRaise;
        _maxRaise  = maxRaise;
        _potSize   = potSize;

        // Initialise to minimum raise
        SetAmountInternal(minRaise, triggerWarning: false);

        // Configure slider range
        if (raiseSlider != null)
        {
            raiseSlider.minValue = (float)minRaise;
            raiseSlider.maxValue = (float)maxRaise;
            raiseSlider.value    = (float)minRaise;
        }

        StopAnim();
        _animCoroutine = StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// Slides the raise control back down over 0.2 seconds.
    /// Requirements: 7.8
    /// </summary>
    public void Hide()
    {
        StopAnim();
        _animCoroutine = StartCoroutine(HideRoutine());
    }

    /// <summary>
    /// Returns the currently selected (clamped) raise amount.
    /// </summary>
    public decimal GetCurrentAmount() => _currentAmount;

    // ── Backward-compatibility API (used by PokerGameManager) ────────────────

    /// <summary>
    /// Legacy 2-argument overload for backward compatibility with PokerGameManager.
    /// Calls <see cref="Show(decimal, decimal, decimal)"/> with potSize = 0.
    /// </summary>
    public void Show(decimal minRaise, decimal maxRaise)
    {
        Show(minRaise, maxRaise, potSize: 0m);
    }

    /// <summary>
    /// Returns whether the raise control panel is currently visible (alpha > 0).
    /// Used by PokerGameManager for its two-step bet/raise flow.
    /// </summary>
    public bool IsVisible()
    {
        if (panelCanvasGroup != null)
            return panelCanvasGroup.alpha > 0.01f;
        return false;
    }

    /// <summary>
    /// Legacy alias for <see cref="GetCurrentAmount()"/>.
    /// Used by PokerGameManager to retrieve the selected amount.
    /// </summary>
    public decimal GetRaiseAmount() => GetCurrentAmount();

    // ── Preset button handlers ───────────────────────────────────────────────

    private void OnMinClicked()
    {
        SetAmountInternal(_minRaise, triggerWarning: false);
    }

    private void OnHalfPotClicked()
    {
        decimal halfPot = _potSize / 2m;
        SetAmountInternal(halfPot, triggerWarning: true);
    }

    private void OnPotClicked()
    {
        SetAmountInternal(_potSize, triggerWarning: true);
    }

    private void OnAllInClicked()
    {
        SetAmountInternal(_maxRaise, triggerWarning: false);
    }

    // ── Confirm / Cancel handlers ────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        // Ensure the current amount is clamped before submitting
        decimal confirmed = ClampAmount(_currentAmount);
        _currentAmount = confirmed;

        OnConfirm?.Invoke(confirmed);
        Hide();
    }

    private void OnCancelClicked()
    {
        Hide();
    }

    // ── Input synchronisation ────────────────────────────────────────────────

    private void OnSliderChanged(float value)
    {
        decimal amount = (decimal)value;
        // Sync input field without re-triggering slider
        if (amountInputField != null)
            amountInputField.SetTextWithoutNotify(amount.ToString("0"));

        _currentAmount = amount;
    }

    private void OnInputFieldEndEdit(string text)
    {
        if (!decimal.TryParse(text, out decimal parsed))
        {
            // Invalid text — reset to current amount
            if (amountInputField != null)
                amountInputField.SetTextWithoutNotify(_currentAmount.ToString("0"));
            return;
        }

        bool outOfRange = parsed < _minRaise || parsed > _maxRaise;
        decimal clamped = ClampAmount(parsed);

        if (outOfRange)
        {
            ShowWarning($"Amount clamped to ${clamped:0}");
        }

        SetAmountInternal(clamped, triggerWarning: false);
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Sets the current amount, syncing both slider and input field.
    /// If <paramref name="triggerWarning"/> is true and the value was clamped, shows the warning.
    /// Requirements: 7.9
    /// </summary>
    private void SetAmountInternal(decimal amount, bool triggerWarning)
    {
        decimal clamped = ClampAmount(amount);

        if (triggerWarning && clamped != amount)
            ShowWarning($"Amount clamped to ${clamped:0}");

        _currentAmount = clamped;

        // Sync slider (suppress its callback to avoid loop)
        if (raiseSlider != null)
            raiseSlider.SetValueWithoutNotify((float)clamped);

        // Sync input field
        if (amountInputField != null)
            amountInputField.SetTextWithoutNotify(clamped.ToString("0"));
    }

    private decimal ClampAmount(decimal amount)
    {
        if (_maxRaise <= _minRaise) return _minRaise;
        return Math.Max(_minRaise, Math.Min(_maxRaise, amount));
    }

    // ── Warning label ────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the warning label with <paramref name="message"/> for 1.5 seconds.
    /// Requirements: 7.9
    /// </summary>
    private void ShowWarning(string message)
    {
        if (warningLabel == null) return;

        warningLabel.text = message;
        warningLabel.gameObject.SetActive(true);

        if (_warningCoroutine != null)
            StopCoroutine(_warningCoroutine);

        _warningCoroutine = StartCoroutine(HideWarningAfterDelay());
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return new WaitForSeconds(WarningDuration);

        if (warningLabel != null)
            warningLabel.gameObject.SetActive(false);

        _warningCoroutine = null;
    }

    // ── Animation routines ───────────────────────────────────────────────────

    /// <summary>
    /// Slides the panel up from the hidden position to the resting position over ShowDuration.
    /// Requirements: 7.6
    /// </summary>
    private IEnumerator ShowRoutine()
    {
        if (panelRect != null)
            panelRect.anchoredPosition = _restingPosition + new Vector2(0f, HiddenOffsetY);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        // Run slide and fade concurrently
        Coroutine slide = StartCoroutine(TweenHelper.Slide(panelRect, _restingPosition, ShowDuration));
        Coroutine fade  = StartCoroutine(TweenHelper.Fade(panelCanvasGroup, 1f, ShowDuration));

        yield return slide;
        yield return fade;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable   = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Slides the panel down to the hidden position over HideDuration.
    /// Requirements: 7.8
    /// </summary>
    private IEnumerator HideRoutine()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        Vector2 hiddenPos = _restingPosition + new Vector2(0f, HiddenOffsetY);

        Coroutine slide = StartCoroutine(TweenHelper.Slide(panelRect, hiddenPos, HideDuration));
        Coroutine fade  = StartCoroutine(TweenHelper.Fade(panelCanvasGroup, 0f, HideDuration));

        yield return slide;
        yield return fade;
    }

    /// <summary>
    /// Instantly moves the panel to the hidden position without animation.
    /// Called from Awake so the panel starts off-screen.
    /// </summary>
    private void HideImmediate()
    {
        if (panelRect != null)
            panelRect.anchoredPosition = _restingPosition + new Vector2(0f, HiddenOffsetY);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    private void StopAnim()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }
}
