using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PokerEngine.Core;
using PokerEngine.State;

/// <summary>
/// Manages the five community card visuals (flop ×3, turn, river).
/// Handles reveal animations with staggered flips and winning card highlights.
/// Requirements: 5.3, 9.4
/// </summary>
public class CommunityCardsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UITheme theme;
    [SerializeField] private CardAnimator cardAnimator;

    [Header("Community Cards")]
    [SerializeField] private CardVisual flopCard1;
    [SerializeField] private CardVisual flopCard2;
    [SerializeField] private CardVisual flopCard3;
    [SerializeField] private CardVisual turnCard;
    [SerializeField] private CardVisual riverCard;

    private CardVisual[] _allCards;

    private void Awake()
    {
        if (theme == null)
        {
            theme = Resources.Load<UITheme>("UITheme");
            if (theme == null)
                Debug.LogWarning("[CommunityCardsDisplay] UITheme not found in Resources.", this);
        }

        if (cardAnimator == null)
            Debug.LogWarning("[CommunityCardsDisplay] cardAnimator is not assigned.", this);

        if (flopCard1 == null)
            Debug.LogWarning("[CommunityCardsDisplay] flopCard1 is not assigned.", this);

        if (flopCard2 == null)
            Debug.LogWarning("[CommunityCardsDisplay] flopCard2 is not assigned.", this);

        if (flopCard3 == null)
            Debug.LogWarning("[CommunityCardsDisplay] flopCard3 is not assigned.", this);

        if (turnCard == null)
            Debug.LogWarning("[CommunityCardsDisplay] turnCard is not assigned.", this);

        if (riverCard == null)
            Debug.LogWarning("[CommunityCardsDisplay] riverCard is not assigned.", this);

        _allCards = new CardVisual[] { flopCard1, flopCard2, flopCard3, turnCard, riverCard };
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the three flop cards and flips them face-up with a 0.1s stagger between each.
    /// Requirement 5.3
    /// </summary>
    public IEnumerator RevealFlop(Card[] cards)
    {
        if (cards == null || cards.Length < 3)
        {
            Debug.LogWarning("[CommunityCardsDisplay] RevealFlop: requires at least 3 cards.", this);
            yield break;
        }

        CardVisual[] flopCards = { flopCard1, flopCard2, flopCard3 };

        for (int i = 0; i < flopCards.Length; i++)
        {
            CardVisual visual = flopCards[i];
            if (visual == null)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            visual.SetCard(cards[i], faceUp: false);

            if (cardAnimator != null)
                cardAnimator.FlipCard(visual);

            if (i < flopCards.Length - 1)
                yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Sets the turn card and flips it face-up.
    /// Requirement 5.3
    /// </summary>
    public IEnumerator RevealTurn(Card card)
    {
        if (turnCard == null)
            yield break;

        turnCard.SetCard(card, faceUp: false);

        if (cardAnimator != null)
            cardAnimator.FlipCard(turnCard);

        yield return null;
    }

    /// <summary>
    /// Sets the river card and flips it face-up.
    /// Requirement 5.3
    /// </summary>
    public IEnumerator RevealRiver(Card card)
    {
        if (riverCard == null)
            yield break;

        riverCard.SetCard(card, faceUp: false);

        if (cardAnimator != null)
            cardAnimator.FlipCard(riverCard);

        yield return null;
    }

    /// <summary>
    /// Hides all five community card visuals.
    /// </summary>
    public void ClearAll()
    {
        foreach (CardVisual visual in _allCards)
        {
            if (visual != null)
                visual.Clear();
        }
    }

    /// <summary>
    /// Shows all five card slots face-down (card backs). Called at the start of a hand.
    /// Used by UIManager.
    /// </summary>
    public void ShowAllCardBacks()
    {
        foreach (CardVisual visual in _allCards)
        {
            if (visual == null) continue;
            visual.SetFaceUp(false);
            visual.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Updates community card visuals based on the current game phase.
    /// Cards are shown face-up only for phases where they have been revealed.
    /// Used by UIManager.
    /// </summary>
    public void UpdateCards(IReadOnlyList<Card> communityCards, GamePhase currentPhase)
    {
        if (_allCards == null) return;

        for (int i = 0; i < _allCards.Length; i++)
        {
            CardVisual visual = _allCards[i];
            if (visual == null) continue;

            if (communityCards != null && i < communityCards.Count)
            {
                bool faceUp = ShouldCardBeVisible(i, currentPhase);
                visual.SetCard(communityCards[i], faceUp);
                visual.gameObject.SetActive(true);
            }
            else
            {
                visual.SetFaceUp(false);
                visual.gameObject.SetActive(true);
            }
        }
    }

    private static bool ShouldCardBeVisible(int cardIndex, GamePhase phase)
    {
        return phase switch
        {
            GamePhase.NotStarted => false,
            GamePhase.PreFlop    => false,
            GamePhase.Flop       => cardIndex < 3,
            GamePhase.Turn       => cardIndex < 4,
            GamePhase.River      => true,
            GamePhase.Showdown   => true,
            GamePhase.Complete   => true,
            _                    => false
        };
    }

    /// <summary>
    /// Highlights the winning cards by index (0=flopCard1, 1=flopCard2, 2=flopCard3, 3=turnCard, 4=riverCard).
    /// Requirement 9.4
    /// </summary>
    public void HighlightWinningCards(int[] cardIndices)
    {
        if (cardIndices == null)
            return;

        foreach (int index in cardIndices)
        {
            if (index < 0 || index >= _allCards.Length)
            {
                Debug.LogWarning($"[CommunityCardsDisplay] HighlightWinningCards: index {index} is out of range.", this);
                continue;
            }

            CardVisual visual = _allCards[index];
            if (visual != null)
                StartCoroutine(visual.SetHighlighted(true));
        }
    }
}
