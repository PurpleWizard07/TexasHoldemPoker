using System.Collections;
using UnityEngine;

/// <summary>
/// Static coroutine-factory class providing common tween patterns for the poker UI.
/// All animation code should call TweenHelper rather than duplicating lerp loops.
/// Satisfies Requirements 5.2, 5.3, 6.1, 6.2, 12.1
/// </summary>
public static class TweenHelper
{
    /// <summary>
    /// Fades a CanvasGroup alpha from its current value to <paramref name="to"/> over
    /// <paramref name="duration"/> seconds.
    /// </summary>
    public static IEnumerator Fade(CanvasGroup cg, float to, float duration,
        AnimationCurve curve = null)
    {
        if (cg == null) yield break;

        AnimationCurve easeCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        float from = cg.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, easeCurve.Evaluate(t));
            yield return null;
        }

        cg.alpha = to;
    }

    /// <summary>
    /// Slides a RectTransform anchoredPosition from its current value to <paramref name="to"/>
    /// over <paramref name="duration"/> seconds.
    /// </summary>
    public static IEnumerator Slide(RectTransform rt, Vector2 to, float duration,
        AnimationCurve curve = null)
    {
        if (rt == null) yield break;

        AnimationCurve easeCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        Vector2 from = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, easeCurve.Evaluate(t));
            yield return null;
        }

        rt.anchoredPosition = to;
    }

    /// <summary>
    /// Scales a Transform up to <paramref name="peakScale"/> over the first half of
    /// <paramref name="duration"/>, then back to 1.0 over the second half.
    /// </summary>
    public static IEnumerator ScalePop(Transform t, float peakScale, float duration)
    {
        if (t == null) yield break;

        float halfDuration = duration / 2f;
        Vector3 originalScale = t.localScale;
        Vector3 peak = originalScale * peakScale;

        // Scale up to peak
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            t.localScale = Vector3.LerpUnclamped(originalScale, peak, progress);
            yield return null;
        }
        t.localScale = peak;

        // Scale back to original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            t.localScale = Vector3.LerpUnclamped(peak, originalScale, progress);
            yield return null;
        }
        t.localScale = originalScale;
    }

    /// <summary>
    /// Continuously oscillates a Transform scale between <paramref name="minScale"/> and
    /// <paramref name="maxScale"/> with the given <paramref name="period"/> in seconds.
    /// Runs indefinitely until the coroutine is stopped externally.
    /// </summary>
    public static IEnumerator Pulse(Transform t, float minScale, float maxScale, float period)
    {
        if (t == null) yield break;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < period)
            {
                elapsed += Time.deltaTime;
                float sine = Mathf.Sin((elapsed / period) * Mathf.PI);
                float scale = Mathf.Lerp(minScale, maxScale, sine);
                t.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Continuously oscillates a CanvasGroup alpha between <paramref name="minAlpha"/> and
    /// <paramref name="maxAlpha"/> with the given <paramref name="period"/> in seconds.
    /// Runs indefinitely until the coroutine is stopped externally.
    /// </summary>
    public static IEnumerator PulseAlpha(CanvasGroup cg, float minAlpha, float maxAlpha,
        float period)
    {
        if (cg == null) yield break;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < period)
            {
                elapsed += Time.deltaTime;
                float sine = Mathf.Sin((elapsed / period) * Mathf.PI);
                cg.alpha = Mathf.Lerp(minAlpha, maxAlpha, sine);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Moves a Transform along a parabolic arc from <paramref name="from"/> to
    /// <paramref name="to"/> over <paramref name="duration"/> seconds.
    ///
    /// Arc formula at parameter t (0 to 1):
    ///   position = Lerp(from, to, t) + Vector3.up * arcHeight * 4 * t * (1 - t)
    ///
    /// This standard parabola peaks at t=0.5 with height exactly arcHeight above the
    /// straight-line path, satisfying Requirements 5.2 and 6.1.
    /// </summary>
    public static IEnumerator ArcMove(Transform t, Vector3 from, Vector3 to,
        float arcHeight, float duration, AnimationCurve curve = null)
    {
        if (t == null) yield break;

        AnimationCurve easeCurve = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalised = Mathf.Clamp01(elapsed / duration);
            float curvedT = easeCurve.Evaluate(normalised);

            // Linear interpolation along the straight-line path
            Vector3 linearPos = Vector3.LerpUnclamped(from, to, curvedT);

            // Parabolic offset: peaks at t=0.5 with height = arcHeight
            float arcOffset = arcHeight * 4f * curvedT * (1f - curvedT);

            t.position = linearPos + Vector3.up * arcOffset;
            yield return null;
        }

        t.position = to;
    }
}
