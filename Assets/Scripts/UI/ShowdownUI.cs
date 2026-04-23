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

    // Off-screen Y positions for slide animations
    private const float OffScreenTopY    =  1200f;
    private const float CenterY          =     0f;
    private const float OffScreenBottomY = -1200f;

    private Coroutine _showWinnerCoroutine;
    private Coroutine _hideWinnerCoroutine;
    private Coroutine _showCountdownCoroutine;
    private Coroutine _fadeOutAllCoroutine;

    private void Awake()
    {
        ValidateReferences();

        if (theme == null)
        {
            theme = Resources.Load<UITheme>("UITheme");
            if (theme == null)
                Debug.LogError("[ShowdownUI] UITheme asset not found at Resources/UITheme. Using null — some animations may be skipped.");
        }
    }

    private void Start()
    {
        if (winnerPanel != null)
            winnerPanel.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (screenFlashOverlay != null)
            screenFlashOverlay.alpha = 0f;
    }

    private void ValidateReferences()
    {
        if (winnerPanel == null)        Debug.LogWarning("[ShowdownUI] winnerPanel is not assigned.", this);
        if (winnerPanelRect == null)    Debug.LogWarning("[ShowdownUI] winnerPanelRect is not assigned.", this);
        if (winnerNameText == null)     Debug.LogWarning("[ShowdownUI] winnerNameText is not assigned.", this);
        if (winnerAmountText == null)   Debug.LogWarning("[ShowdownUI] winnerAmountText is not assigned.", this);
        if (winnerHandText == null)     Debug.LogWarning("[ShowdownUI] winnerHandText is not assigned.", this);
        if (winnerPanelCG == null)      Debug.LogWarning("[ShowdownUI] winnerPanelCG is not assigned.", this);
        if (countdownPanel == null)     Debug.LogWarning("[ShowdownUI] countdownPanel is not assigned.", this);
        if (countdownText == null)      Debug.LogWarning("[ShowdownUI] countdownText is not assigned.", this);
        if (flashParticles == null)     Debug.LogWarning("[ShowdownUI] flashParticles is not assigned.", this);
        if (screenFlashOverlay == null) Debug.LogWarning("[ShowdownUI] screenFlashOverlay is not assigned.", this);
    }

    /// <summary>
    /// Slides the winner panel in from the top over 0.5s, populates all three text fields,
    /// triggers a white screen flash (30% opacity fading over 0.3s), and plays confetti particles.
    /// When handName is "All Others Folded", displays it as-is (no hole card reveal).
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

        // Populate text fields before activating the panel
        if (winnerNameText != null)   winnerNameText.text   = playerName;
        if (winnerAmountText != null) winnerAmountText.text = $"${amount:N0}";
        if (winnerHandText != null)   winnerHandText.text   = handName;

        // Reset alpha and park panel off-screen above
        if (winnerPanelCG != null)
            winnerPanelCG.alpha = 1f;

        winnerPanelRect.anchoredPosition = new Vector2(winnerPanelRect.anchoredPosition.x, OffScreenTopY);
        winnerPanel.SetActive(true);

        // Slide down to center — Req 9.1
        float slideDuration = theme != null ? theme.winnerSlideDuration : 0.5f;
        yield return StartCoroutine(
            TweenHelper.Slide(winnerPanelRect,
                new Vector2(winnerPanelRect.anchoredPosition.x, CenterY),
                slideDuration));

        // Screen flash + confetti in parallel — Req 9.2
        StartCoroutine(PlayScreenFlash());

        if (flashParticles != null)
            flashParticles.Play();
    }

    /// <summary>
    /// Convenience wrapper: shows the winner panel with "All Others Folded" as the hand name.
    /// Card reveal is intentionally omitted per Req 9.5.
    /// </summary>
    public IEnumerator ShowFoldWinner(string playerName, decimal amount)
    {
        yield return StartCoroutine(ShowWinner(playerName, amount, "All Others Folded"));
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

        float slideDuration = theme != null ? theme.fadeTransitionDuration : 0.4f;
        yield return StartCoroutine(
            TweenHelper.Slide(winnerPanelRect,
                new Vector2(winnerPanelRect.anchoredPosition.x, OffScreenBottomY),
                slideDuration));

        winnerPanel.SetActive(false);
    }

    /// <summary>
    /// Displays a countdown from 3 to 1 with a scale-pop animation per number.
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
            // Scale-pop: peak 1.4×, 0.4s; hold remainder of the second
            yield return StartCoroutine(TweenHelper.ScalePop(countdownText.transform, 1.4f, 0.4f));
            yield return new WaitForSeconds(0.6f);
        }

        countdownPanel.SetActive(false);
    }

    /// <summary>
    /// Fades out all showdown elements over 0.4s, then hides panels.
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

        if (winnerPanel != null)    winnerPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
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
