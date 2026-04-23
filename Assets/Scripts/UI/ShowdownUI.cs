using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles showdown UI: winner panel slide-in/out, screen flash, confetti particles,
/// countdown timer, and fade-out of all showdown elements.
/// Requirements: 9.1, 9.2, 9.3, 9.5, 9.6, 9.7
/// </summary>
public class ShowdownUI : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private UITheme theme;

    [Header("Winner Panel")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private RectTransform winnerPanelRect;
    [SerializeField] private TextMeshProUGUI winnerNameText;
    [SerializeField] private TextMeshProUGUI winnerAmountText;
    [SerializeField] private TextMeshProUGUI winnerHandText;
    [SerializeField] private CanvasGroup winnerPanelCG;

    [Header("Countdown Panel")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Effects")]
    [SerializeField] private ParticleSystem flashParticles;
    [SerializeField] private CanvasGroup screenFlashOverlay;

    // Off-screen Y position used to park the winner panel above the visible area
    private const float OffScreenTopY = 1200f;
    // Center Y position for the winner panel when visible
    private const float CenterY = 0f;
    // Off-screen Y position used to slide the winner panel downward when dismissed
    private const float OffScreenBottomY = -1200f;

    private Coroutine _showWinnerCoroutine;
    private Coroutine _hideWinnerCoroutine;
    private Coroutine _showCountdownCoroutine;
    private Coroutine _fadeOutAllCoroutine;

    private void Awake()
    {
        ValidateReferences();

        // Load theme fallback
        if (theme == null)
        {
            theme = Resources.Load<UITheme>("UITheme");
            if (theme == null)
                theme = UITheme.CreateDefault();
        }
    }

    private void Start()
    {
        // Ensure panels start hidden
        if (winnerPanel != null)
            winnerPanel.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (screenFlashOverlay != null)
            screenFlashOverlay.alpha = 0f;
    }

    private void ValidateReferences()
    {
        if (winnerPanel == null)
            Debug.LogWarning("[ShowdownUI] winnerPanel is not assigned.", this);
        if (winnerPanelRect == null)
            Debug.LogWarning("[ShowdownUI] winnerPanelRect is not assigned.", this);
        if (winnerNameText == null)
            Debug.LogWarning("[ShowdownUI] winnerNameText is not assigned.", this);
        if (winnerAmountText == null)
            Debug.LogWarning("[ShowdownUI] winnerAmountText is not assigned.", this);
        if (winnerHandText == null)
            Debug.LogWarning("[ShowdownUI] winnerHandText is not assigned.", this);
        if (winnerPanelCG == null)
            Debug.LogWarning("[ShowdownUI] winnerPanelCG is not assigned.", this);
        if (countdownPanel == null)
            Debug.LogWarning("[ShowdownUI] countdownPanel is not assigned.", this);
        if (countdownText == null)
            Debug.LogWarning("[ShowdownUI] countdownText is not assigned.", this);
        if (flashParticles == null)
            Debug.LogWarning("[ShowdownUI] flashParticles is not assigned.", this);
        if (screenFlashOverlay == null)
            Debug.LogWarning("[ShowdownUI] screenFlashOverlay is not assigned.", this);
    }

    /// <summary>
    /// Slides the winner panel in from the top over 0.5s, populates all three text fields,
    /// triggers a white screen flash (30% opacity fading over 0.3s), and plays confetti particles.
    /// When handName is "All Others Folded", displays it as-is without revealing hole cards.
    /// Requirements: 9.1, 9.2, 9.5
    /// </summary>
    public IEnumerator ShowWinner(string playerName, decimal amount, string handName)
    {
        if (_showWinnerCoroutine != null)
            StopCoroutine(_showWinnerCoroutine);

        _showWinnerCoroutine = StartCoroutine(ShowWinnerRoutine(playerName, amount, handName));
        yield return _showWinnerCoroutine;
    }

    private IEnumerator ShowWinnerRoutine(string playerName, decimal amount, string handName)
    {
        if (winnerPanel == null || winnerPanelRect == null)
            yield break;

        // Populate text fields before making the panel visible
        if (winnerNameText != null)
            winnerNameText.text = playerName;

        if (winnerAmountText != null)
            winnerAmountText.text = $"${amount:N0}";

        if (winnerHandText != null)
            winnerHandText.text = handName;

        // Reset panel alpha and position it off-screen above
        if (winnerPanelCG != null)
            winnerPanelCG.alpha = 1f;

        winnerPanelRect.anchoredPosition = new Vector2(winnerPanelRect.anchoredPosition.x, OffScreenTopY);
        winnerPanel.SetActive(true);

        // Slide down to center
        float slideDuration = theme != null ? theme.winnerSlideDuration : 0.5f;
        yield return StartCoroutine(
            TweenHelper.Slide(winnerPanelRect,
                new Vector2(winnerPanelRect.anchoredPosition.x, CenterY),
                slideDuration));

        // Trigger screen flash and confetti in parallel
        StartCoroutine(PlayScreenFlash());

        if (flashParticles != null)
            flashParticles.Play();
    }

    /// <summary>
    /// Slides the winner panel out downward over 0.4s, then deactivates it.
    /// Requirement: 9.6
    /// </summary>
    public void HideWinnerPanel()
    {
        if (_hideWinnerCoroutine != null)
            StopCoroutine(_hideWinnerCoroutine);

        _hideWinnerCoroutine = StartCoroutine(HideWinnerRoutine());
    }

    private IEnumerator HideWinnerRoutine()
    {
        if (winnerPanel == null || winnerPanelRect == null || !winnerPanel.activeSelf)
            yield break;

        float fadeDuration = theme != null ? theme.fadeTransitionDuration : 0.4f;
        yield return StartCoroutine(
            TweenHelper.Slide(winnerPanelRect,
                new Vector2(winnerPanelRect.anchoredPosition.x, OffScreenBottomY),
                fadeDuration));

        winnerPanel.SetActive(false);
    }

    /// <summary>
    /// Displays a countdown from 3 to 1, with a scale-pop animation per number.
    /// Requirement: 9.7
    /// </summary>
    public IEnumerator ShowCountdown()
    {
        if (_showCountdownCoroutine != null)
            StopCoroutine(_showCountdownCoroutine);

        _showCountdownCoroutine = StartCoroutine(ShowCountdownRoutine());
        yield return _showCountdownCoroutine;
    }

    private IEnumerator ShowCountdownRoutine()
    {
        if (countdownPanel == null || countdownText == null)
        {
            Debug.LogWarning("[ShowdownUI] Countdown panel or text not assigned — skipping countdown.");
            yield return new WaitForSeconds(3f);
            yield break;
        }

        countdownPanel.SetActive(true);

        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();

            // Scale-pop: peak at 1.4, duration 0.4s
            yield return StartCoroutine(TweenHelper.ScalePop(countdownText.transform, 1.4f, 0.4f));

            // Hold for the remainder of the second
            yield return new WaitForSeconds(0.6f);
        }

        countdownPanel.SetActive(false);
    }

    /// <summary>
    /// Fades out all showdown elements (winner panel canvas group) over 0.4s, then hides panels.
    /// Requirement: 9.6
    /// </summary>
    public IEnumerator FadeOutAll()
    {
        if (_fadeOutAllCoroutine != null)
            StopCoroutine(_fadeOutAllCoroutine);

        _fadeOutAllCoroutine = StartCoroutine(FadeOutAllRoutine());
        yield return _fadeOutAllCoroutine;
    }

    private IEnumerator FadeOutAllRoutine()
    {
        float fadeDuration = theme != null ? theme.fadeTransitionDuration : 0.4f;

        if (winnerPanelCG != null && winnerPanel != null && winnerPanel.activeSelf)
            yield return StartCoroutine(TweenHelper.Fade(winnerPanelCG, 0f, fadeDuration));

        if (winnerPanel != null)
            winnerPanel.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    /// <summary>
    /// Plays a white screen flash at 30% opacity that fades to 0 over 0.3s.
    /// Requirement: 9.2
    /// </summary>
    private IEnumerator PlayScreenFlash()
    {
        if (screenFlashOverlay == null)
            yield break;

        screenFlashOverlay.alpha = 0.3f;
        yield return StartCoroutine(TweenHelper.Fade(screenFlashOverlay, 0f, 0.3f));
    }
}
