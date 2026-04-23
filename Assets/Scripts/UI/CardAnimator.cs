using System.Collections;
using UnityEngine;

/// <summary>
/// Handles card dealing, flipping, folding, pulsing, and sweep animations.
/// Uses AnimationState to gate overlapping calls; stops the previous coroutine
/// before starting a new one on each method.
/// Requirements: 5.2, 5.3, 5.4, 5.5, 5.7
/// </summary>
public class CardAnimator : MonoBehaviour
{
    [SerializeField] private UITheme theme;

    [Header("Arc Settings")]
    [SerializeField] private float arcHeight = 80f;

    // Per-method coroutine tracking for overlap prevention
    private Coroutine _dealCoroutine;
    private Coroutine _flipCoroutine;
    private Coroutine _foldCoroutine;
    private Coroutine _pulseCoroutine;
    private Coroutine _sweepCoroutine;

    // Per-method animation state
    private AnimationState _dealState   = AnimationState.Idle;
    private AnimationState _flipState   = AnimationState.Idle;
    private AnimationState _foldState   = AnimationState.Idle;
    private AnimationState _pulseState  = AnimationState.Idle;
    private AnimationState _sweepState  = AnimationState.Idle;

    private void Awake()
    {
        if (theme == null)
        {
            theme = Resources.Load<UITheme>("UITheme");
            if (theme == null)
            {
                Debug.LogWarning("[CardAnimator] UITheme not found in Resources; using defaults.", this);
                theme = UITheme.CreateDefault();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Arc-moves a card from <paramref name="fromPosition"/> to the target CardVisual's
    /// world position over <see cref="UITheme.cardDealDuration"/>, after an optional delay.
    /// Requirement 5.2
    /// </summary>
    public void DealCard(CardVisual target, Vector3 fromPosition, float delay)
    {
        if (_dealCoroutine != null)
            StopCoroutine(_dealCoroutine);

        _dealState    = AnimationState.Playing;
        _dealCoroutine = StartCoroutine(DealCardRoutine(target, fromPosition, delay));
    }

    /// <summary>
    /// Y-axis 3D flip: rotates 0→90°, swaps sprite to face-up, then 90°→0°.
    /// Each half takes <see cref="UITheme.cardFlipHalfDuration"/>.
    /// Requirement 5.3
    /// </summary>
    public void FlipCard(CardVisual target)
    {
        if (_flipCoroutine != null)
            StopCoroutine(_flipCoroutine);

        _flipState    = AnimationState.Playing;
        _flipCoroutine = StartCoroutine(FlipCardRoutine(target));
    }

    /// <summary>
    /// Slides each card to <paramref name="discardPosition"/> and fades it out within 0.5 s,
    /// then calls <see cref="CardVisual.Clear"/> on each.
    /// Requirement 5.4
    /// </summary>
    public void FoldCards(CardVisual[] cards, Vector3 discardPosition)
    {
        if (_foldCoroutine != null)
            StopCoroutine(_foldCoroutine);

        _foldState    = AnimationState.Playing;
        _foldCoroutine = StartCoroutine(FoldCardsRoutine(cards, discardPosition));
    }

    /// <summary>
    /// Scale-pops each card 1.0→1.05→1.0 to draw attention to the human player's hole cards.
    /// Requirement 5.7
    /// </summary>
    public void PulsePlayerCards(CardVisual[] cards)
    {
        if (_pulseCoroutine != null)
            StopCoroutine(_pulseCoroutine);

        _pulseState    = AnimationState.Playing;
        _pulseCoroutine = StartCoroutine(PulsePlayerCardsRoutine(cards));
    }

    /// <summary>
    /// Slides all visible cards off-screen to the right and fades them out,
    /// then calls <see cref="CardVisual.Clear"/> on each.
    /// Requirement 5.5
    /// </summary>
    public void SweepAllCards(CardVisual[] allCards)
    {
        if (_sweepCoroutine != null)
            StopCoroutine(_sweepCoroutine);

        _sweepState    = AnimationState.Playing;
        _sweepCoroutine = StartCoroutine(SweepAllCardsRoutine(allCards));
    }

    // -------------------------------------------------------------------------
    // State accessors (read-only)
    // -------------------------------------------------------------------------

    public AnimationState DealState  => _dealState;
    public AnimationState FlipState  => _flipState;
    public AnimationState FoldState  => _foldState;
    public AnimationState PulseState => _pulseState;
    public AnimationState SweepState => _sweepState;

    // -------------------------------------------------------------------------
    // Coroutine implementations
    // -------------------------------------------------------------------------

    private IEnumerator DealCardRoutine(CardVisual target, Vector3 fromPosition, float delay)
    {
        if (target == null)
        {
            _dealState = AnimationState.Complete;
            yield break;
        }

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float duration = theme != null ? theme.cardDealDuration : 0.35f;

        // Capture the destination before repositioning the card to the deck
        Vector3 destination = target.transform.position;

        // Move card to deck position and activate it
        target.transform.position = fromPosition;
        target.gameObject.SetActive(true);

        yield return TweenHelper.ArcMove(
            target.transform,
            fromPosition,
            destination,
            arcHeight,
            duration);

        _dealState = AnimationState.Complete;
    }

    private IEnumerator FlipCardRoutine(CardVisual target)
    {
        if (target == null)
        {
            _flipState = AnimationState.Complete;
            yield break;
        }

        float halfDuration = theme != null ? theme.cardFlipHalfDuration : 0.15f;
        Transform t = target.transform;

        // First half: 0° → 90°
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            t.localEulerAngles = new Vector3(0f, Mathf.Lerp(0f, 90f, progress), 0f);
            yield return null;
        }
        t.localEulerAngles = new Vector3(0f, 90f, 0f);

        // Swap sprite at the invisible midpoint
        target.SetFaceUp(true);

        // Second half: 90° → 0°
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            t.localEulerAngles = new Vector3(0f, Mathf.Lerp(90f, 0f, progress), 0f);
            yield return null;
        }
        t.localEulerAngles = Vector3.zero;

        _flipState = AnimationState.Complete;
    }

    private IEnumerator FoldCardsRoutine(CardVisual[] cards, Vector3 discardPosition)
    {
        if (cards == null || cards.Length == 0)
        {
            _foldState = AnimationState.Complete;
            yield break;
        }

        const float foldDuration = 0.5f;

        // Launch slide + fade for every card simultaneously
        foreach (CardVisual card in cards)
        {
            if (card == null || !card.gameObject.activeSelf) continue;

            RectTransform rt = card.GetComponent<RectTransform>();
            CanvasGroup cg   = GetOrAddCanvasGroup(card);

            if (rt != null)
            {
                // Convert world discard position to local anchored position
                Vector2 discardAnchor = WorldToAnchoredPosition(rt, discardPosition);
                StartCoroutine(TweenHelper.Slide(rt, discardAnchor, foldDuration));
            }

            if (cg != null)
                StartCoroutine(TweenHelper.Fade(cg, 0f, foldDuration));
        }

        yield return new WaitForSeconds(foldDuration);

        // Clean up after animation
        foreach (CardVisual card in cards)
        {
            if (card == null) continue;
            ResetCanvasGroupAlpha(card);
            card.Clear();
        }

        _foldState = AnimationState.Complete;
    }

    private IEnumerator PulsePlayerCardsRoutine(CardVisual[] cards)
    {
        if (cards == null || cards.Length == 0)
        {
            _pulseState = AnimationState.Complete;
            yield break;
        }

        // ScalePop duration: two halves, each ~0.15s → total 0.3s
        const float popDuration = 0.3f;
        const float peakScale   = 1.05f;

        foreach (CardVisual card in cards)
        {
            if (card == null) continue;
            StartCoroutine(TweenHelper.ScalePop(card.transform, peakScale, popDuration));
        }

        yield return new WaitForSeconds(popDuration);

        _pulseState = AnimationState.Complete;
    }

    private IEnumerator SweepAllCardsRoutine(CardVisual[] allCards)
    {
        if (allCards == null || allCards.Length == 0)
        {
            _sweepState = AnimationState.Complete;
            yield break;
        }

        const float sweepDuration = 0.4f;

        foreach (CardVisual card in allCards)
        {
            if (card == null || !card.gameObject.activeSelf) continue;

            RectTransform rt = card.GetComponent<RectTransform>();
            CanvasGroup cg   = GetOrAddCanvasGroup(card);

            if (rt != null)
            {
                // Slide off-screen to the right (1200px offset)
                Vector2 offScreen = rt.anchoredPosition + new Vector2(1200f, 0f);
                StartCoroutine(TweenHelper.Slide(rt, offScreen, sweepDuration));
            }

            if (cg != null)
                StartCoroutine(TweenHelper.Fade(cg, 0f, sweepDuration));
        }

        yield return new WaitForSeconds(sweepDuration);

        foreach (CardVisual card in allCards)
        {
            if (card == null) continue;
            ResetCanvasGroupAlpha(card);
            card.Clear();
        }

        _sweepState = AnimationState.Complete;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the existing CanvasGroup on the card's GameObject, or adds one if absent.
    /// </summary>
    private static CanvasGroup GetOrAddCanvasGroup(CardVisual card)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = card.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    /// <summary>
    /// Resets a card's CanvasGroup alpha to 1 so it is ready for the next hand.
    /// </summary>
    private static void ResetCanvasGroupAlpha(CardVisual card)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = 1f;
    }

    /// <summary>
    /// Converts a world-space position to the anchoredPosition space of a RectTransform.
    /// Falls back to the current anchoredPosition if the canvas cannot be found.
    /// </summary>
    private static Vector2 WorldToAnchoredPosition(RectTransform rt, Vector3 worldPos)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null)
            return rt.anchoredPosition;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform ?? rt,
            screenPoint,
            cam,
            out Vector2 localPoint);

        return localPoint;
    }
}
