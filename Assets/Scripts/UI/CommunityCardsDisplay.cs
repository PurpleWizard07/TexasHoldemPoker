using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;
using PokerEngine.State;

/// <summary>
/// Displays the 5 community cards (flop, turn, river) progressively based on game phase.
/// Once a card is revealed it stays face-up for the rest of the hand.
/// </summary>
public class CommunityCardsDisplay : MonoBehaviour
{
    [SerializeField] private CardVisual[] cardVisuals = new CardVisual[5];

    // Tracks which card slots have been revealed — never flipped back
    private bool[] _revealed = new bool[5];

    public void UpdateCards(IReadOnlyList<Card> communityCards, GamePhase currentPhase)
    {
        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] == null) continue;

            if (i < communityCards.Count)
            {
                // If already revealed, keep it face-up — never reset it
                if (_revealed[i])
                {
                    cardVisuals[i].SetCard(communityCards[i], true);
                }
                else
                {
                    // Not yet revealed — show face-down (animation will reveal it)
                    cardVisuals[i].SetCard(communityCards[i], false);
                }
            }
            else
            {
                // Empty slot — show card back
                cardVisuals[i].SetFaceUp(false);
                cardVisuals[i].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Called by CardDealerManager after flip animation to permanently reveal a card.
    /// </summary>
    public void RevealCard(int index)
    {
        if (index < 0 || index >= cardVisuals.Length || cardVisuals[index] == null) return;
        _revealed[index] = true;
        cardVisuals[index].SetFaceUp(true);
    }

    /// <summary>
    /// Load card data face-down without revealing. Used before deal animation.
    /// </summary>
    public void LoadCardFaceDown(int index, Card card)
    {
        if (index < 0 || index >= cardVisuals.Length || cardVisuals[index] == null) return;
        cardVisuals[index].SetCard(card, false);
    }

    public void ShowAllCardBacks()
    {
        for (int i = 0; i < cardVisuals.Length; i++)
        {
            if (cardVisuals[i] != null)
            {
                cardVisuals[i].SetFaceUp(false);
                cardVisuals[i].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Reset revealed state — call at the start of each new hand.
    /// </summary>
    public void ResetForNewHand()
    {
        for (int i = 0; i < _revealed.Length; i++)
            _revealed[i] = false;
    }

    public void Clear()
    {
        ResetForNewHand();
        foreach (var cardVisual in cardVisuals)
        {
            if (cardVisual != null)
                cardVisual.Clear();
        }
    }

    // Legacy fallback
    public void UpdateCards(IReadOnlyList<Card> communityCards)
    {
        UpdateCards(communityCards, GamePhase.River);
    }
}
