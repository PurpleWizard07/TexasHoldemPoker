using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Full-screen Game Over panel shown when the game session ends.
/// Animates in with a fade, displays the result headline, winner name, and final stack,
/// and provides "Play Again" and "Main Menu" buttons with hover states.
/// Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6
/// </summary>
public class GameOverUI : MonoBehaviour
{
    // ── Serialized fields ────────────────────────────────────────────────────

    [SerializeField] private UITheme theme;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private TextMeshProUGUI headlineText;
    [SerializeField] private TextMeshProUGUI winnerNameText;
    [SerializeField] private TextMeshProUGUI finalStackText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private CanvasGroup playAgainCG;
    [SerializeField] private CanvasGroup mainMenuCG;

    // ── Constants ────────────────────────────────────────────────────────────

    private const float ShowFadeDuration   = 0.6f;
    private const float ButtonFadeDuration = 0.5f;
    private const float HoverScale         = 1.06f;
    private const float NormalScale        = 1.0f;

    // ── Runtime state ────────────────────────────────────────────────────────

    private Coroutine _playAgainHoverCoroutine;
    private Coroutine _mainMenuHoverCoroutine;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (theme == null)
            Debug.LogWarning($"[GameOverUI] '{name}': theme is not assigned.", this);

        if (gameOverCanvasGroup == null)
            Debug.LogWarning($"[GameOverUI] '{name}': gameOverCanvasGroup is not assigned.", this);

        if (headlineText == null)
            Debug.LogWarning($"[GameOverUI] '{name}': headlineText is not assigned.", this);

        if (winnerNameText == null)
            Debug.LogWarning($"[GameOverUI] '{name}': winnerNameText is not assigned.", this);

        if (finalStackText == null)
            Debug.LogWarning($"[GameOverUI] '{name}': finalStackText is not assigned.", this);

        if (playAgainButton == null)
            Debug.LogWarning($"[GameOverUI] '{name}': playAgainButton is not assigned.", this);

        if (mainMenuButton == null)
            Debug.LogWarning($"[GameOverUI] '{name}': mainMenuButton is not assigned.", this);

        if (playAgainCG == null)
            Debug.LogWarning($"[GameOverUI] '{name}': playAgainCG is not assigned.", this);

        if (mainMenuCG == null)
            Debug.LogWarning($"[GameOverUI] '{name}': mainMenuCG is not assigned.", this);
    }

    private void Start()
    {
        // Set initial hidden state
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        // Wire button click listeners
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        // Wire hover events via EventTrigger
        if (playAgainButton != null)
            WireHoverEvents(playAgainButton.gameObject, OnPlayAgainHoverEnter, OnPlayAgainHoverExit);

        if (mainMenuButton != null)
            WireHoverEvents(mainMenuButton.gameObject, OnMainMenuHoverEnter, OnMainMenuHoverExit);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Backward-compatible overload called by PokerGameManager.
    /// Determines playerWon by comparing winnerName to "You".
    /// </summary>
    public void Show(string winnerName, decimal finalStack)
    {
        bool playerWon = winnerName == "You";
        StartCoroutine(ShowGameOver(playerWon, winnerName, finalStack));
    }

    /// <summary>
    /// Animates the game over screen in over 0.6 seconds, displaying the result.
    /// Requirements: 10.1, 10.2, 10.3
    /// </summary>
    public IEnumerator ShowGameOver(bool playerWon, string winnerName, decimal finalStack)
    {
        // Set initial state
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        // Set headline text and color
        if (headlineText != null)
        {
            headlineText.text = playerWon ? "You Win!" : "You Lose!";

            if (theme != null)
                headlineText.color = playerWon ? theme.accentGold : theme.accentRed;
        }

        // Set winner name
        if (winnerNameText != null)
            winnerNameText.text = winnerName;

        // Set final stack
        if (finalStackText != null)
            finalStackText.text = $"${finalStack:N0}";

        // Fade in over 0.6s
        if (gameOverCanvasGroup != null)
            yield return TweenHelper.Fade(gameOverCanvasGroup, 1f, ShowFadeDuration);

        // Enable interaction after fade completes
        if (gameOverCanvasGroup != null)
            gameOverCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Hides the game over screen by fading out.
    /// </summary>
    public IEnumerator Hide()
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.blocksRaycasts = false;
            yield return TweenHelper.Fade(gameOverCanvasGroup, 0f, ShowFadeDuration);
            gameOverCanvasGroup.alpha = 0f;
        }
    }

    // ── Button click handlers ────────────────────────────────────────────────

    /// <summary>
    /// Transitions to a new game session within 0.5s.
    /// Requirements: 10.5
    /// </summary>
    private void OnPlayAgainClicked()
    {
        if (SceneTransitionManager.Instance != null)
            StartCoroutine(SceneTransitionManager.Instance.TransitionToScene("PokerGame", ButtonFadeDuration));
        else
            Debug.LogWarning("[GameOverUI] SceneTransitionManager.Instance is null — cannot transition to PokerGame.");
    }

    /// <summary>
    /// Transitions to the Main Menu scene within 0.5s.
    /// Requirements: 10.6
    /// </summary>
    private void OnMainMenuClicked()
    {
        if (SceneTransitionManager.Instance != null)
            StartCoroutine(SceneTransitionManager.Instance.TransitionToScene("MainMenu", ButtonFadeDuration));
        else
            Debug.LogWarning("[GameOverUI] SceneTransitionManager.Instance is null — cannot transition to MainMenu.");
    }

    // ── Hover handlers ───────────────────────────────────────────────────────

    private void OnPlayAgainHoverEnter()
    {
        if (playAgainButton == null) return;
        StopHoverCoroutine(ref _playAgainHoverCoroutine);
        _playAgainHoverCoroutine = StartCoroutine(LerpScale(playAgainButton.transform, HoverScale));
    }

    private void OnPlayAgainHoverExit()
    {
        if (playAgainButton == null) return;
        StopHoverCoroutine(ref _playAgainHoverCoroutine);
        _playAgainHoverCoroutine = StartCoroutine(LerpScale(playAgainButton.transform, NormalScale));
    }

    private void OnMainMenuHoverEnter()
    {
        if (mainMenuButton == null) return;
        StopHoverCoroutine(ref _mainMenuHoverCoroutine);
        _mainMenuHoverCoroutine = StartCoroutine(LerpScale(mainMenuButton.transform, HoverScale));
    }

    private void OnMainMenuHoverExit()
    {
        if (mainMenuButton == null) return;
        StopHoverCoroutine(ref _mainMenuHoverCoroutine);
        _mainMenuHoverCoroutine = StartCoroutine(LerpScale(mainMenuButton.transform, NormalScale));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds PointerEnter and PointerExit EventTrigger entries to a GameObject.
    /// Requirements: 10.4
    /// </summary>
    private void WireHoverEvents(GameObject target, UnityEngine.Events.UnityAction onEnter,
        UnityEngine.Events.UnityAction onExit)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        // Pointer Enter
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(enterEntry);

        // Pointer Exit
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => onExit());
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// Lerps a Transform's local scale uniformly to targetScale over buttonHoverDuration.
    /// </summary>
    private IEnumerator LerpScale(Transform t, float targetScale)
    {
        if (t == null) yield break;

        float duration = theme != null ? theme.buttonHoverDuration : 0.15f;
        Vector3 from = t.localScale;
        Vector3 to   = new Vector3(targetScale, targetScale, targetScale);

        if (duration <= 0f)
        {
            t.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            t.localScale = Vector3.LerpUnclamped(from, to, progress);
            yield return null;
        }

        t.localScale = to;
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
