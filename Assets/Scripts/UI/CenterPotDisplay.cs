using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PokerEngine.State;

/// <summary>
/// Displays the center pot amount, current game phase, and the start-hand button.
/// Requirements: 8.1, 8.2, 8.3, 8.4
/// </summary>
public class CenterPotDisplay : MonoBehaviour
{
    [SerializeField] private UITheme theme;
    [SerializeField] private TextMeshProUGUI potAmountText;   // large gold text
    [SerializeField] private TextMeshProUGUI phaseText;       // smaller white text
    [SerializeField] private ChipStack potChipStack;
    [SerializeField] private Button startHandButton;

    // CanvasGroup used for phase text fade transition
    private CanvasGroup _phaseCanvasGroup;

    // Tracks the last known values to detect changes
    private decimal _currentPot = -1m;
    private GamePhase _currentPhase = (GamePhase)(-1);

    // Coroutine handles to allow stopping before starting a new one
    private Coroutine _potPopCoroutine;
    private Coroutine _phaseFadeCoroutine;

    // Animation state guards
    private AnimationState _potAnimState = AnimationState.Idle;
    private AnimationState _phaseAnimState = AnimationState.Idle;

    private void Awake()
    {
        if (theme == null)
            Debug.LogWarning("[CenterPotDisplay] 'theme' is not assigned.", this);

        if (potAmountText == null)
            Debug.LogWarning("[CenterPotDisplay] 'potAmountText' is not assigned.", this);

        if (phaseText == null)
            Debug.LogWarning("[CenterPotDisplay] 'phaseText' is not assigned.", this);

        if (potChipStack == null)
            Debug.LogWarning("[CenterPotDisplay] 'potChipStack' is not assigned.", this);

        if (startHandButton == null)
            Debug.LogWarning("[CenterPotDisplay] 'startHandButton' is not assigned.", this);

        // Ensure the phaseText GameObject has a CanvasGroup for fade animations
        if (phaseText != null)
        {
            _phaseCanvasGroup = phaseText.GetComponent<CanvasGroup>();
            if (_phaseCanvasGroup == null)
                _phaseCanvasGroup = phaseText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Updates the pot amount text formatted as "$X,XXX".
    /// Triggers a scale-pop (1.0 → 1.2 → 1.0) over 0.3s if the amount changed.
    /// Requirements: 8.1, 8.2
    /// </summary>
    public void SetPot(decimal amount)
    {
        bool changed = amount != _currentPot;
        _currentPot = amount;

        if (potAmountText != null)
            potAmountText.text = FormatPot(amount);

        if (potChipStack != null)
            potChipStack.SetAmount(amount);

        if (changed && potAmountText != null)
        {
            if (_potPopCoroutine != null)
                StopCoroutine(_potPopCoroutine);

            _potPopCoroutine = StartCoroutine(PotScalePopRoutine());
        }
    }

    /// <summary>
    /// Updates the phase text with the given phase string.
    /// Triggers a fade-out / fade-in over 0.25s if the phase changed.
    /// Requirements: 8.3, 8.4
    /// </summary>
    public void SetPhase(GamePhase phase)
    {
        bool changed = phase != _currentPhase;
        _currentPhase = phase;

        if (!changed)
            return;

        if (_phaseFadeCoroutine != null)
            StopCoroutine(_phaseFadeCoroutine);

        _phaseFadeCoroutine = StartCoroutine(PhaseFadeRoutine(PhaseToString(phase)));
    }

    /// <summary>
    /// Shows or hides the start hand button.
    /// Requirements: 8.1 (hand-in-progress state)
    /// </summary>
    public void SetHandInProgress(bool inProgress)
    {
        if (startHandButton != null)
            startHandButton.gameObject.SetActive(!inProgress);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private IEnumerator PotScalePopRoutine()
    {
        _potAnimState = AnimationState.Playing;
        yield return TweenHelper.ScalePop(potAmountText.transform, 1.2f, 0.3f);
        _potAnimState = AnimationState.Complete;
    }

    private IEnumerator PhaseFadeRoutine(string newPhaseText)
    {
        _phaseAnimState = AnimationState.Playing;

        const float halfDuration = 0.125f; // 0.25s total → 0.125s each half

        // Fade out
        if (_phaseCanvasGroup != null)
            yield return TweenHelper.Fade(_phaseCanvasGroup, 0f, halfDuration);

        // Update text while invisible
        if (phaseText != null)
            phaseText.text = newPhaseText;

        // Fade back in
        if (_phaseCanvasGroup != null)
            yield return TweenHelper.Fade(_phaseCanvasGroup, 1f, halfDuration);

        _phaseAnimState = AnimationState.Complete;
    }

    /// <summary>
    /// Formats a decimal amount as "$X,XXX" (no decimal places, with commas).
    /// </summary>
    private static string FormatPot(decimal amount)
    {
        // Use "C0" which gives locale-aware currency with no decimals, e.g. "$1,234"
        return amount.ToString("C0");
    }

    /// <summary>
    /// Converts a <see cref="GamePhase"/> to a human-readable display string.
    /// </summary>
    private static string PhaseToString(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.NotStarted => "Not Started",
            GamePhase.PreFlop    => "Pre-Flop",
            GamePhase.Flop       => "Flop",
            GamePhase.Turn       => "Turn",
            GamePhase.River      => "River",
            GamePhase.Showdown   => "Showdown",
            GamePhase.Complete   => "Complete",
            _                    => phase.ToString()
        };
    }
}
