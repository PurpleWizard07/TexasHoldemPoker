using System.Collections;
using UnityEngine;
using PokerEngine.State;

/// <summary>
/// Self-contained MonoBehaviour that owns the action bar's show/hide animation,
/// button state management, and call-amount label updates.
/// Requirements: 7.1, 7.2, 7.3, 7.4, 7.5
/// </summary>
public class ActionBar : MonoBehaviour
{
    // ── Serialized fields ────────────────────────────────────────────────────

    [SerializeField] private UITheme theme;
    [SerializeField] private CanvasGroup barCanvasGroup;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private ActionButton foldButton;
    [SerializeField] private ActionButton checkButton;
    [SerializeField] private ActionButton callButton;
    [SerializeField] private ActionButton betButton;
    [SerializeField] private ActionButton raiseButton;
    [SerializeField] private ActionButton allInButton;
    [SerializeField] private RaiseControl raiseControl;

    // ── Constants ────────────────────────────────────────────────────────────

    private const float HideFadeDuration   = 0.2f;
    private const float HiddenAlpha        = 0.0f;
    private const float VisibleAlpha       = 1.0f;
    private const float DisabledBarAlpha   = 0.4f;

    /// <summary>
    /// How far below the resting position the bar starts when sliding in (pixels).
    /// </summary>
    private const float SlideOffsetY = -120f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private Coroutine _animCoroutine;
    private Vector2   _restingPosition;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (theme == null)
            Debug.LogWarning($"[ActionBar] '{name}': theme is not assigned.", this);

        if (barCanvasGroup == null)
            Debug.LogWarning($"[ActionBar] '{name}': barCanvasGroup is not assigned.", this);

        if (barRect == null)
            Debug.LogWarning($"[ActionBar] '{name}': barRect is not assigned.", this);

        if (foldButton == null)
            Debug.LogWarning($"[ActionBar] '{name}': foldButton is not assigned.", this);

        if (checkButton == null)
            Debug.LogWarning($"[ActionBar] '{name}': checkButton is not assigned.", this);

        if (callButton == null)
            Debug.LogWarning($"[ActionBar] '{name}': callButton is not assigned.", this);

        if (betButton == null)
            Debug.LogWarning($"[ActionBar] '{name}': betButton is not assigned.", this);

        if (raiseButton == null)
            Debug.LogWarning($"[ActionBar] '{name}': raiseButton is not assigned.", this);

        if (allInButton == null)
            Debug.LogWarning($"[ActionBar] '{name}': allInButton is not assigned.", this);

        if (raiseControl == null)
            Debug.LogWarning($"[ActionBar] '{name}': raiseControl is not assigned.", this);

        // Cache the resting position so Show/Hide can animate relative to it
        if (barRect != null)
            _restingPosition = barRect.anchoredPosition;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Slides the bar up from off-screen and fades it in over
    /// <c>theme.actionBarShowDuration</c> seconds.
    /// Requirements: 7.3
    /// </summary>
    public IEnumerator Show()
    {
        StopAnim();

        float duration = theme != null ? theme.actionBarShowDuration : 0.25f;

        // Start from hidden position (below resting) and zero alpha
        if (barRect != null)
            barRect.anchoredPosition = _restingPosition + new Vector2(0f, SlideOffsetY);

        if (barCanvasGroup != null)
            barCanvasGroup.alpha = HiddenAlpha;

        _animCoroutine = StartCoroutine(ShowRoutine(duration));
        yield return _animCoroutine;
    }

    /// <summary>
    /// Fades the bar out over 0.2 seconds.
    /// Requirements: 7.2
    /// </summary>
    public IEnumerator Hide()
    {
        StopAnim();
        _animCoroutine = StartCoroutine(HideRoutine());
        yield return _animCoroutine;
    }

    /// <summary>
    /// Enables or disables buttons based on the current <see cref="GameState"/>.
    /// When <paramref name="isHumanTurn"/> is <c>false</c>, all buttons are disabled
    /// and the bar alpha is set to 40%.
    /// Requirements: 7.2, 7.4, 7.5
    /// </summary>
    public void UpdateFromGameState(GameState state, bool isHumanTurn)
    {
        if (!isHumanTurn)
        {
            // Disable all buttons and dim the bar
            SetAllButtonsEnabled(false);

            if (barCanvasGroup != null)
                barCanvasGroup.alpha = DisabledBarAlpha;

            return;
        }

        // Restore full opacity
        if (barCanvasGroup != null)
            barCanvasGroup.alpha = VisibleAlpha;

        if (state == null)
        {
            SetAllButtonsEnabled(false);
            return;
        }

        bool handActive = !state.HandComplete && state.Phase != GamePhase.NotStarted;

        // Fold and All-In are always available when the hand is active
        SetButtonEnabled(foldButton,  handActive);
        SetButtonEnabled(allInButton, handActive);

        if (handActive)
        {
            var round  = state.RoundState;
            var player = state.GetPlayerBySeat(0); // Human is always seat 0

            if (round != null && player != null)
            {
                decimal contribution = round.GetContribution(player.Id);
                decimal currentBet   = round.CurrentBet;
                bool    hasBetToCall = currentBet > contribution;

                // Check: only when no bet to call
                SetButtonEnabled(checkButton, !hasBetToCall);

                // Call: only when there is a bet to call
                SetButtonEnabled(callButton, hasBetToCall);

                // Bet: only when no one has bet yet
                SetButtonEnabled(betButton, !hasBetToCall);

                // Raise: only when there is already a bet
                SetButtonEnabled(raiseButton, hasBetToCall);
            }
            else
            {
                SetButtonEnabled(checkButton, false);
                SetButtonEnabled(callButton,  false);
                SetButtonEnabled(betButton,   false);
                SetButtonEnabled(raiseButton, false);
            }
        }
        else
        {
            SetButtonEnabled(checkButton, false);
            SetButtonEnabled(callButton,  false);
            SetButtonEnabled(betButton,   false);
            SetButtonEnabled(raiseButton, false);
        }
    }

    /// <summary>
    /// Updates the call button label to "CALL $X" where X is formatted with commas.
    /// Requirements: 7.4
    /// </summary>
    public void SetCallAmount(decimal amount)
    {
        if (callButton == null) return;

        // Format with thousands separator, e.g. 1500 → "CALL $1,500"
        string formatted = amount.ToString("N0");
        callButton.SetLabel($"CALL ${formatted}");
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private IEnumerator ShowRoutine(float duration)
    {
        Vector2 hiddenPos  = _restingPosition + new Vector2(0f, SlideOffsetY);
        Vector2 restingPos = _restingPosition;

        // Run slide and fade concurrently
        Coroutine slide = StartCoroutine(TweenHelper.Slide(barRect, restingPos, duration));
        Coroutine fade  = StartCoroutine(TweenHelper.Fade(barCanvasGroup, VisibleAlpha, duration));

        yield return slide;
        yield return fade;
    }

    private IEnumerator HideRoutine()
    {
        if (barCanvasGroup != null)
            yield return TweenHelper.Fade(barCanvasGroup, HiddenAlpha, HideFadeDuration);
    }

    private void StopAnim()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }
    }

    private void SetAllButtonsEnabled(bool enabled)
    {
        SetButtonEnabled(foldButton,  enabled);
        SetButtonEnabled(checkButton, enabled);
        SetButtonEnabled(callButton,  enabled);
        SetButtonEnabled(betButton,   enabled);
        SetButtonEnabled(raiseButton, enabled);
        SetButtonEnabled(allInButton, enabled);
    }

    private static void SetButtonEnabled(ActionButton button, bool enabled)
    {
        if (button != null)
            button.SetEnabled(enabled);
    }
}
