using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles winner celebration effects for both human and bot wins.
/// Requirements: 9.1, 9.2
/// </summary>
public class WinnerCelebration : MonoBehaviour
{
    [SerializeField] private UITheme theme;
    [SerializeField] private CanvasGroup celebrationCG;
    [SerializeField] private TextMeshProUGUI youWinText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private ParticleSystem confettiSystem;   // full-screen gold burst
    [SerializeField] private ParticleSystem sparkleSystem;

    private Coroutine _activeCoroutine;

    private void Awake()
    {
        if (theme == null)
        {
            theme = Resources.Load<UITheme>("UITheme");
            if (theme == null)
            {
                Debug.LogWarning("[WinnerCelebration] UITheme not found in Resources; using defaults.");
                theme = UITheme.CreateDefault();
            }
        }

        if (celebrationCG == null)
            Debug.LogWarning("[WinnerCelebration] celebrationCG is not assigned.");
        if (youWinText == null)
            Debug.LogWarning("[WinnerCelebration] youWinText is not assigned.");
        if (amountText == null)
            Debug.LogWarning("[WinnerCelebration] amountText is not assigned.");
        if (confettiSystem == null)
            Debug.LogWarning("[WinnerCelebration] confettiSystem is not assigned — confetti will be skipped.");
        if (sparkleSystem == null)
            Debug.LogWarning("[WinnerCelebration] sparkleSystem is not assigned — sparkle will be skipped.");

        // Ensure hidden on start
        if (celebrationCG != null)
        {
            celebrationCG.alpha = 0f;
            celebrationCG.interactable = false;
            celebrationCG.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Human win: full-screen confetti + sparkle particles, "YOU WIN!" scale-pop,
    /// amount text, then auto-dismiss after 3.5s with 0.4s fade-out.
    /// Satisfies Requirements 9.1, 9.2
    /// </summary>
    public IEnumerator CelebrateHumanWin(decimal amount)
    {
        StopActiveCoroutine();
        _activeCoroutine = StartCoroutine(RunHumanWin(amount));
        yield return _activeCoroutine;
    }

    /// <summary>
    /// Bot win: smaller banner, no full-screen particles, auto-dismiss.
    /// Satisfies Requirement 9.2
    /// </summary>
    public IEnumerator CelebrateBotWin(string botName, decimal amount)
    {
        StopActiveCoroutine();
        _activeCoroutine = StartCoroutine(RunBotWin(botName, amount));
        yield return _activeCoroutine;
    }

    private IEnumerator RunHumanWin(decimal amount)
    {
        // Set text content
        if (youWinText != null)
            youWinText.text = "YOU WIN!";
        if (amountText != null)
            amountText.text = $"+${amount:N0}";

        // Fade in celebration panel
        if (celebrationCG != null)
        {
            celebrationCG.interactable = true;
            celebrationCG.blocksRaycasts = true;
            yield return TweenHelper.Fade(celebrationCG, 1f, 0.2f);
        }

        // Play full-screen particles (Req 9.1)
        if (confettiSystem != null)
            confettiSystem.Play();
        if (sparkleSystem != null)
            sparkleSystem.Play();

        // Scale-pop "YOU WIN!" text: 1.0 → 1.3 → 1.0 (Req 9.2)
        if (youWinText != null)
            yield return TweenHelper.ScalePop(youWinText.transform, 1.3f, 0.5f);

        // Hold then auto-dismiss
        yield return AutoDismiss();
    }

    private IEnumerator RunBotWin(string botName, decimal amount)
    {
        // Set text content — smaller banner style
        if (youWinText != null)
            youWinText.text = $"{botName} Wins!";
        if (amountText != null)
            amountText.text = $"+${amount:N0}";

        // Fade in (no particles for bot win)
        if (celebrationCG != null)
        {
            celebrationCG.interactable = true;
            celebrationCG.blocksRaycasts = true;
            yield return TweenHelper.Fade(celebrationCG, 1f, 0.2f);
        }

        // Auto-dismiss
        yield return AutoDismiss();
    }

    /// <summary>
    /// Waits 3.5s then fades out over 0.4s and stops any particles.
    /// </summary>
    private IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(3.5f);

        // Stop particles before fade-out
        if (confettiSystem != null && confettiSystem.isPlaying)
            confettiSystem.Stop();
        if (sparkleSystem != null && sparkleSystem.isPlaying)
            sparkleSystem.Stop();

        // Fade out over 0.4s (uses theme.fadeTransitionDuration = 0.4f)
        float fadeDuration = theme != null ? theme.fadeTransitionDuration : 0.4f;
        if (celebrationCG != null)
        {
            yield return TweenHelper.Fade(celebrationCG, 0f, fadeDuration);
            celebrationCG.interactable = false;
            celebrationCG.blocksRaycasts = false;
        }
    }

    private void StopActiveCoroutine()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        // Reset state in case a previous celebration was interrupted
        if (confettiSystem != null && confettiSystem.isPlaying)
            confettiSystem.Stop();
        if (sparkleSystem != null && sparkleSystem.isPlaying)
            sparkleSystem.Stop();

        if (celebrationCG != null)
        {
            celebrationCG.alpha = 0f;
            celebrationCG.interactable = false;
            celebrationCG.blocksRaycasts = false;
        }
    }
}
