using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PokerEngine.Core;

/// <summary>
/// Displays a single playing card with the correct sprite, optional glow highlight,
/// and drop shadow. Supports face-up/face-down toggling and animated gold glow.
/// Requirements: 5.1, 5.6, 9.4
/// </summary>
public class CardVisual : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Sprite cardBackSprite;
    [SerializeField] private Image glowBorder;   // gold highlight for winning cards
    [SerializeField] private Shadow dropShadow;  // Unity UI Shadow component

    private Card? currentCard;
    private bool isFaceUp;
    private Coroutine glowCoroutine;

    private void Awake()
    {
        if (cardImage == null)
            Debug.LogWarning($"[CardVisual] '{name}': cardImage is not assigned.", this);

        if (cardBackSprite == null)
            Debug.LogWarning($"[CardVisual] '{name}': cardBackSprite is not assigned.", this);

        if (glowBorder == null)
            Debug.LogWarning($"[CardVisual] '{name}': glowBorder is not assigned.", this);

        if (dropShadow == null)
            Debug.LogWarning($"[CardVisual] '{name}': dropShadow is not assigned.", this);

        // Ensure glow starts hidden
        if (glowBorder != null)
        {
            Color c = glowBorder.color;
            c.a = 0f;
            glowBorder.color = c;
        }
    }

    /// <summary>
    /// Assigns a card and displays it face-up or face-down.
    /// </summary>
    public void SetCard(Card card, bool faceUp = true)
    {
        currentCard = card;
        isFaceUp = faceUp;
        gameObject.SetActive(true);
        UpdateVisual();
    }

    /// <summary>
    /// Swaps between the card face sprite and the back sprite.
    /// </summary>
    public void SetFaceUp(bool faceUp)
    {
        isFaceUp = faceUp;
        UpdateVisual();
    }

    /// <summary>
    /// Animates the gold glow border on (alpha 0→1) or off (alpha 1→0) over 0.3 seconds.
    /// Req 9.4
    /// </summary>
    public IEnumerator SetHighlighted(bool highlighted)
    {
        if (glowBorder == null)
            yield break;

        if (glowCoroutine != null)
            StopCoroutine(glowCoroutine);

        float targetAlpha = highlighted ? 1f : 0f;
        float startAlpha = glowBorder.color.a;
        float elapsed = 0f;
        const float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color c = glowBorder.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            glowBorder.color = c;
            yield return null;
        }

        // Ensure final value is exact
        Color final = glowBorder.color;
        final.a = targetAlpha;
        glowBorder.color = final;
    }

    /// <summary>
    /// Hides the card visual and clears the current card data.
    /// </summary>
    public void Clear()
    {
        currentCard = null;
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
            glowCoroutine = null;
        }
        if (glowBorder != null)
        {
            Color c = glowBorder.color;
            c.a = 0f;
            glowBorder.color = c;
        }
        gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------

    private void UpdateVisual()
    {
        if (!currentCard.HasValue)
        {
            gameObject.SetActive(false);
            return;
        }

        if (cardImage == null)
            return;

        if (isFaceUp)
        {
            Sprite sprite = CardSpriteLoader.GetCardSprite(currentCard.Value);
            if (sprite != null)
                cardImage.sprite = sprite;
            else
                Debug.LogWarning($"[CardVisual] '{name}': No sprite found for card {currentCard.Value}.", this);
        }
        else
        {
            if (cardBackSprite != null)
                cardImage.sprite = cardBackSprite;
            else
                Debug.LogWarning($"[CardVisual] '{name}': cardBackSprite is null; cannot show card back.", this);
        }
    }
}
