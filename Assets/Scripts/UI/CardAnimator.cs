using System.Collections;
using UnityEngine;

/// <summary>
/// Handles card dealing and movement animations with smooth, professional effects.
/// </summary>
public class CardAnimator : MonoBehaviour
{
    [Header("Dealing Settings")]
    [SerializeField] private Transform deckPosition; // Central deck position
    [SerializeField] private float dealDuration = 0.4f;
    [SerializeField] private float dealDelay = 0.15f; // Delay between each card
    [SerializeField] private float arcHeight = 50f; // Height of the arc during dealing
    [SerializeField] private AnimationCurve dealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Flip Settings")]
    [SerializeField] private float flipDuration = 0.3f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Scale Settings")]
    [SerializeField] private float scaleUpAmount = 1.05f;
    [SerializeField] private float scaleDuration = 0.2f;

    private void Awake()
    {
        // If no deck position is set, use this object's position
        if (deckPosition == null)
        {
            deckPosition = transform;
        }
    }

    /// <summary>
    /// Deal a card from the deck to a target position with arc motion.
    /// </summary>
    public IEnumerator DealCardToPosition(Transform card, Vector3 targetPosition, float delay = 0f)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        if (card == null) yield break;

        // Start from deck position
        Vector3 startPosition = deckPosition.position;
        card.position = startPosition;
        
        // Make card visible
        card.gameObject.SetActive(true);
        
        // Reset rotation and scale
        card.localRotation = Quaternion.identity;
        card.localScale = Vector3.one;

        float elapsed = 0f;

        while (elapsed < dealDuration)
        {
            elapsed += Time.deltaTime;
            float t = dealCurve.Evaluate(elapsed / dealDuration);
            
            // Linear interpolation for horizontal movement
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Add arc (parabolic curve) for vertical movement
            float arcProgress = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0
            currentPos.y += arcProgress * arcHeight;
            
            card.position = currentPos;
            
            // Optional: Add slight rotation during flight
            card.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 360, t));
            
            yield return null;
        }

        // Ensure final position and rotation
        card.position = targetPosition;
        card.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Flip a card with 3D rotation effect.
    /// </summary>
    public IEnumerator FlipCard(CardVisual cardVisual, bool faceUp, float delay = 0f)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        if (cardVisual == null) yield break;

        Transform cardTransform = cardVisual.transform;
        float halfDuration = flipDuration / 2f;
        float elapsed = 0f;

        // Flip to 90 degrees (edge view)
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / halfDuration);
            float angle = Mathf.Lerp(0, 90, t);
            cardTransform.localRotation = Quaternion.Euler(0, angle, 0);
            yield return null;
        }

        // Change card face at 90 degrees (invisible moment)
        cardVisual.SetFaceUp(faceUp);

        // Flip from 90 to 0 degrees (reveal)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / halfDuration);
            float angle = Mathf.Lerp(90, 0, t);
            cardTransform.localRotation = Quaternion.Euler(0, angle, 0);
            yield return null;
        }

        cardTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Deal multiple cards in sequence (round-robin style).
    /// </summary>
    public IEnumerator DealCardsSequentially(Transform[] cardTransforms, Vector3[] targetPositions)
    {
        if (cardTransforms == null || targetPositions == null) yield break;
        if (cardTransforms.Length != targetPositions.Length) yield break;

        for (int i = 0; i < cardTransforms.Length; i++)
        {
            if (cardTransforms[i] != null)
            {
                StartCoroutine(DealCardToPosition(cardTransforms[i], targetPositions[i], 0f));
                yield return new WaitForSeconds(dealDelay);
            }
        }
    }

    /// <summary>
    /// Scale up a card slightly (for emphasis).
    /// </summary>
    public IEnumerator ScaleCard(Transform card, float targetScale, float duration)
    {
        if (card == null) yield break;

        Vector3 startScale = card.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            card.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        card.localScale = endScale;
    }

    /// <summary>
    /// Pulse scale effect (scale up then back down).
    /// </summary>
    public IEnumerator PulseCard(Transform card)
    {
        yield return ScaleCard(card, scaleUpAmount, scaleDuration);
        yield return ScaleCard(card, 1f, scaleDuration);
    }

    /// <summary>
    /// Slide card to center and fade out (fold animation).
    /// </summary>
    public IEnumerator FoldCard(Transform card, Vector3 centerPosition, float duration = 0.5f)
    {
        if (card == null) yield break;

        Vector3 startPosition = card.position;
        Quaternion startRotation = card.rotation;
        Quaternion endRotation = Quaternion.Euler(0, 0, 15); // Slight tilt
        
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = card.gameObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            card.position = Vector3.Lerp(startPosition, centerPosition, t);
            card.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            canvasGroup.alpha = 1f - t;
            
            yield return null;
        }

        card.gameObject.SetActive(false);
        canvasGroup.alpha = 1f; // Reset for next use
    }

    // Public getters for settings
    public float GetDealDelay() => dealDelay;
    public Transform GetDeckPosition() => deckPosition;
}
