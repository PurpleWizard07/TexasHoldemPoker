using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DontDestroyOnLoad singleton that owns the full-screen black fade overlay Canvas.
/// Exposes coroutines for fading to/from black and transitioning between scenes.
/// Satisfies Requirements 2.5, 10.5, 10.6, 12.2
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeOverlay;

    private void Awake()
    {
        // Singleton enforcement — destroy duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeOverlay == null)
        {
            Debug.LogWarning("[SceneTransitionManager] fadeOverlay is not assigned. " +
                             "Fade transitions will not work.");
        }
    }

    /// <summary>
    /// Lerps the overlay alpha from its current value to 1.0 over <paramref name="duration"/> seconds.
    /// Enables raycast blocking while fading in.
    /// </summary>
    public IEnumerator FadeToBlack(float duration)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.blocksRaycasts = true;
        yield return TweenHelper.Fade(fadeOverlay, 1f, duration);
        fadeOverlay.alpha = 1f; // guarantee exact value
    }

    /// <summary>
    /// Lerps the overlay alpha from its current value to 0.0 over <paramref name="duration"/> seconds.
    /// Disables raycast blocking once fully transparent.
    /// </summary>
    public IEnumerator FadeFromBlack(float duration)
    {
        if (fadeOverlay == null) yield break;

        yield return TweenHelper.Fade(fadeOverlay, 0f, duration);
        fadeOverlay.alpha = 0f; // guarantee exact value
        fadeOverlay.blocksRaycasts = false;
    }

    /// <summary>
    /// Fades to black, loads <paramref name="sceneName"/>, then fades from black.
    /// The overlay alpha is set to exactly 1.0 before <see cref="SceneManager.LoadScene"/>
    /// is called — preventing any transparent flash mid-load.
    /// On an invalid scene name, logs a <see cref="Debug.LogError"/> and fades back from black.
    /// </summary>
    public IEnumerator TransitionToScene(string sceneName, float fadeDuration = 0.4f)
    {
        // Fade to black
        yield return FadeToBlack(fadeDuration);

        // Guarantee alpha is exactly 1.0 before loading — no flash
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
        }

        // Attempt scene load
        bool loadSucceeded = false;
        try
        {
            SceneManager.LoadScene(sceneName);
            loadSucceeded = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SceneTransitionManager] Failed to load scene '{sceneName}': {ex.Message}");
        }

        if (!loadSucceeded)
        {
            // Recover by fading back from black so the player isn't stuck on a black screen
            yield return FadeFromBlack(fadeDuration);
            yield break;
        }

        // Fade from black after successful load
        yield return FadeFromBlack(fadeDuration);
    }
}
