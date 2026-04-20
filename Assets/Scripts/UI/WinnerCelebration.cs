using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles winner celebration effects.
/// </summary>
public class WinnerCelebration : MonoBehaviour
{
    [SerializeField] private PokerVisualTheme visualTheme;
    [SerializeField] private GameObject celebrationPanel;
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.5f;

    public IEnumerator CelebrateWinner(Transform winnerPanel)
    {
        // Flash effect
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            float elapsed = 0f;
            Color baseColor = visualTheme != null ? visualTheme.WarningTextColor : new Color(1f, 0.85f, 0.36f, 1f);
            Color startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.32f);
            Color endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                flashImage.color = Color.Lerp(startColor, endColor, elapsed / flashDuration);
                yield return null;
            }

            flashImage.gameObject.SetActive(false);
        }

        // Scale pulse on winner panel
        if (winnerPanel != null)
        {
            Vector3 originalScale = winnerPanel.localScale;
            float pulseDuration = visualTheme != null ? visualTheme.WinnerPulseDuration : 0.22f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.1f, elapsed / pulseDuration);
                winnerPanel.localScale = originalScale * scale;
                yield return null;
            }

            // Scale back
            elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.1f, 1f, elapsed / pulseDuration);
                winnerPanel.localScale = originalScale * scale;
                yield return null;
            }

            winnerPanel.localScale = originalScale;
        }
    }
}
