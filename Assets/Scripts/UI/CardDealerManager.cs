using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;

/// <summary>
/// Manages the card dealing sequence for poker hands.
/// Orchestrates dealing to players and community cards with proper timing.
/// </summary>
public class CardDealerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardAnimator cardAnimator;
    [SerializeField] private PlayerUIPanel[] playerPanels;
    [SerializeField] private CommunityCardsDisplay communityCardsDisplay;
    
    [Header("Timing")]
    [SerializeField] private float delayBetweenCards = 0.15f;
    [SerializeField] private float delayBeforeCommunityCards = 0.5f;

    /// <summary>
    /// Deal hole cards to all players (2 cards each, round-robin style).
    /// </summary>
    public IEnumerator DealHoleCards(IReadOnlyList<Player> players)
    {
        if (cardAnimator == null || playerPanels == null)
        {
            Debug.LogWarning("CardDealerManager: Missing references!");
            yield break;
        }

        // Deal first card to each player
        for (int i = 0; i < players.Count; i++)
        {
            if (i < playerPanels.Length && playerPanels[i] != null)
            {
                var cardVisuals = playerPanels[i].GetComponentsInChildren<CardVisual>(true);
                if (cardVisuals.Length > 0)
                {
                    Transform cardTransform = cardVisuals[0].transform;
                    Vector3 targetPos = cardTransform.position;
                    
                    yield return cardAnimator.DealCardToPosition(cardTransform, targetPos, 0f);
                    yield return new WaitForSeconds(delayBetweenCards);
                }
            }
        }

        // Deal second card to each player
        for (int i = 0; i < players.Count; i++)
        {
            if (i < playerPanels.Length && playerPanels[i] != null)
            {
                var cardVisuals = playerPanels[i].GetComponentsInChildren<CardVisual>(true);
                if (cardVisuals.Length > 1)
                {
                    Transform cardTransform = cardVisuals[1].transform;
                    Vector3 targetPos = cardTransform.position;
                    
                    yield return cardAnimator.DealCardToPosition(cardTransform, targetPos, 0f);
                    yield return new WaitForSeconds(delayBetweenCards);
                }
            }
        }

        Debug.Log("Hole cards dealt to all players");
    }

    /// <summary>
    /// Deal and reveal the flop (3 community cards).
    /// </summary>
    public IEnumerator DealFlop()
    {
        if (cardAnimator == null || communityCardsDisplay == null)
        {
            Debug.LogWarning("CardDealerManager: Missing references!");
            yield break;
        }

        yield return new WaitForSeconds(delayBeforeCommunityCards);

        var cardVisuals = communityCardsDisplay.GetComponentsInChildren<CardVisual>(true);
        
        // Deal 3 cards for the flop
        for (int i = 0; i < 3 && i < cardVisuals.Length; i++)
        {
            Transform cardTransform = cardVisuals[i].transform;
            Vector3 targetPos = cardTransform.position;
            
            // Deal card face down first
            yield return cardAnimator.DealCardToPosition(cardTransform, targetPos, 0f);
            yield return new WaitForSeconds(0.1f);
            
            // Flip to reveal
            yield return cardAnimator.FlipCard(cardVisuals[i], true, 0f);
            yield return new WaitForSeconds(delayBetweenCards);
        }

        Debug.Log("Flop dealt and revealed");
    }

    /// <summary>
    /// Deal and reveal the turn (4th community card).
    /// </summary>
    public IEnumerator DealTurn()
    {
        if (cardAnimator == null || communityCardsDisplay == null)
        {
            Debug.LogWarning("CardDealerManager: Missing references!");
            yield break;
        }

        yield return new WaitForSeconds(delayBeforeCommunityCards);

        var cardVisuals = communityCardsDisplay.GetComponentsInChildren<CardVisual>(true);
        
        if (cardVisuals.Length > 3)
        {
            Transform cardTransform = cardVisuals[3].transform;
            Vector3 targetPos = cardTransform.position;
            
            // Deal card face down
            yield return cardAnimator.DealCardToPosition(cardTransform, targetPos, 0f);
            yield return new WaitForSeconds(0.1f);
            
            // Flip to reveal
            yield return cardAnimator.FlipCard(cardVisuals[3], true, 0f);
        }

        Debug.Log("Turn dealt and revealed");
    }

    /// <summary>
    /// Deal and reveal the river (5th community card).
    /// </summary>
    public IEnumerator DealRiver()
    {
        if (cardAnimator == null || communityCardsDisplay == null)
        {
            Debug.LogWarning("CardDealerManager: Missing references!");
            yield break;
        }

        yield return new WaitForSeconds(delayBeforeCommunityCards);

        var cardVisuals = communityCardsDisplay.GetComponentsInChildren<CardVisual>(true);
        
        if (cardVisuals.Length > 4)
        {
            Transform cardTransform = cardVisuals[4].transform;
            Vector3 targetPos = cardTransform.position;
            
            // Deal card face down
            yield return cardAnimator.DealCardToPosition(cardTransform, targetPos, 0f);
            yield return new WaitForSeconds(0.1f);
            
            // Flip to reveal
            yield return cardAnimator.FlipCard(cardVisuals[4], true, 0f);
        }

        Debug.Log("River dealt and revealed");
    }

    /// <summary>
    /// Flip player's hole cards face up (for showdown).
    /// </summary>
    public IEnumerator RevealPlayerCards(int seatIndex, float delay = 0f)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (seatIndex < 0 || seatIndex >= playerPanels.Length || playerPanels[seatIndex] == null)
            yield break;

        var cardVisuals = playerPanels[seatIndex].GetComponentsInChildren<CardVisual>(true);
        
        foreach (var cardVisual in cardVisuals)
        {
            if (cardVisual != null && cardVisual.gameObject.activeSelf)
            {
                yield return cardAnimator.FlipCard(cardVisual, true, 0f);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    /// <summary>
    /// Fold animation - cards slide to center and fade.
    /// </summary>
    public IEnumerator FoldPlayerCards(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= playerPanels.Length || playerPanels[seatIndex] == null)
            yield break;

        var cardVisuals = playerPanels[seatIndex].GetComponentsInChildren<CardVisual>(true);
        Vector3 centerPosition = cardAnimator.GetDeckPosition().position;

        foreach (var cardVisual in cardVisuals)
        {
            if (cardVisual != null && cardVisual.gameObject.activeSelf)
            {
                StartCoroutine(cardAnimator.FoldCard(cardVisual.transform, centerPosition, 0.5f));
            }
        }

        yield return new WaitForSeconds(0.5f);
    }
}
