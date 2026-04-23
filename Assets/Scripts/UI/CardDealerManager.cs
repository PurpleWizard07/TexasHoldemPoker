using System.Collections;
using UnityEngine;
using PokerEngine.Core;

/// <summary>
/// Manages the card dealing sequence for poker hands.
/// Orchestrates hole card dealing (round-robin), community card reveals, and hand resets.
/// Requirements: 5.2, 5.3, 5.5
/// </summary>
public class CardDealerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardAnimator cardAnimator;
    [SerializeField] private UITheme theme;

    [Header("Deck Position")]
    [Tooltip("World-space position of the central deck from which cards are dealt")]
    [SerializeField] private Transform deckTransform;

    [Header("Player Hole Card Visuals")]
    [Tooltip("Two CardVisual slots per seat, indexed by seat index. Outer array = seats (0-5), inner = [card0, card1].")]
    [SerializeField] private CardVisualPair[] playerCardSlots = new CardVisualPair[6];

    [Header("Community Card Visuals")]
    [Tooltip("Five CardVisual slots: indices 0-2 = flop, 3 = turn, 4 = river")]
    [SerializeField] private CardVisual[] communityCardVisuals = new CardVisual[5];

    // Animation state gate — prevents overlapping deal sequences
    private AnimationState _dealState = AnimationState.Idle;
    private Coroutine _dealCoroutine;

    // -------------------------------------------------------------------------
    // Serializable helper so the Inspector can expose per-seat card pairs
    // -------------------------------------------------------------------------

    [System.Serializable]
    public class CardVisualPair
    {
        public CardVisual card0;
        public CardVisual card1;

        public CardVisual Get(int index) => index == 0 ? card0 : card1;
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (cardAnimator == null)
            Debug.LogWarning("[CardDealerManager] cardAnimator is not assigned.", this);

        if (deckTransform == null)
            Debug.LogWarning("[CardDealerManager] deckTransform is not assigned — cards will deal from world origin.", this);

        if (theme == null)
        {
            theme = Resources.Load<UITheme>("UITheme");
            if (theme == null)
            {
                Debug.LogWarning("[CardDealerManager] UITheme not found in Resources; using defaults.", this);
                theme = UITheme.CreateDefault();
            }
        }

        for (int i = 0; i < playerCardSlots.Length; i++)
        {
            if (playerCardSlots[i] == null)
                Debug.LogWarning($"[CardDealerManager] playerCardSlots[{i}] is null.", this);
        }

        for (int i = 0; i < communityCardVisuals.Length; i++)
        {
            if (communityCardVisuals[i] == null)
                Debug.LogWarning($"[CardDealerManager] communityCardVisuals[{i}] is not assigned.", this);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Deals hole cards to each active seat in round-robin order:
    /// pass 1 → card 0 to each seat in activeSeatIndices order,
    /// pass 2 → card 1 to each seat in activeSeatIndices order.
    /// Total deal completes within 2.5 s for 6 players.
    /// Requirements: 5.2
    /// </summary>
    public void DealHoleCards(Player[] players, int[] activeSeatIndices)
    {
        if (cardAnimator == null)
        {
            Debug.LogWarning("[CardDealerManager] DealHoleCards: cardAnimator is missing.", this);
            return;
        }

        if (activeSeatIndices == null || activeSeatIndices.Length == 0)
        {
            Debug.LogWarning("[CardDealerManager] DealHoleCards: activeSeatIndices is empty.", this);
            return;
        }

        if (_dealCoroutine != null)
            StopCoroutine(_dealCoroutine);

        _dealState = AnimationState.Playing;
        _dealCoroutine = StartCoroutine(DealHoleCardsRoutine(players, activeSeatIndices));
    }

    /// <summary>
    /// Flips community cards face-up with a 0.1 s stagger between each card.
    /// Requirements: 5.3
    /// </summary>
    public void RevealCommunityCards(Card[] cards, int count)
    {
        if (cardAnimator == null)
        {
            Debug.LogWarning("[CardDealerManager] RevealCommunityCards: cardAnimator is missing.", this);
            return;
        }

        if (cards == null || count <= 0)
        {
            Debug.LogWarning("[CardDealerManager] RevealCommunityCards: cards array is null or count is zero.", this);
            return;
        }

        StartCoroutine(RevealCommunityCardsRoutine(cards, count));
    }

    /// <summary>
    /// Sweeps all visible cards off-screen before a new hand begins.
    /// Requirements: 5.5
    /// </summary>
    public void ClearAllCards()
    {
        if (cardAnimator == null)
        {
            Debug.LogWarning("[CardDealerManager] ClearAllCards: cardAnimator is missing.", this);
            return;
        }

        cardAnimator.SweepAllCards(GatherAllCardVisuals());
        _dealState = AnimationState.Idle;
    }

    /// <summary>
    /// Returns the current deal animation state.
    /// </summary>
    public AnimationState DealState => _dealState;

    // -------------------------------------------------------------------------
    // Coroutine implementations
    // -------------------------------------------------------------------------

    private IEnumerator DealHoleCardsRoutine(Player[] players, int[] activeSeatIndices)
    {
        // Timing budget: 2.5 s for 6 players = 12 cards total.
        // Each card deal arc takes theme.cardDealDuration (0.35 s).
        // Stagger delay between consecutive card deals so animations overlap slightly.
        // With 12 cards and 2.5 s budget: stagger = (2.5 - cardDealDuration) / 11 ≈ 0.195 s
        // We use a fixed stagger that keeps total time ≤ 2.5 s for up to 6 players.
        float dealDuration = theme != null ? theme.cardDealDuration : 0.35f;
        int totalCards = activeSeatIndices.Length * 2; // two passes
        float maxTotalTime = 2.5f;
        float stagger = totalCards > 1
            ? Mathf.Min((maxTotalTime - dealDuration) / (totalCards - 1), 0.2f)
            : 0f;

        Vector3 deckPosition = deckTransform != null ? deckTransform.position : Vector3.zero;

        int cardIndex = 0;

        // Pass 1: deal card slot 0 to each active seat in order
        for (int i = 0; i < activeSeatIndices.Length; i++)
        {
            int seatIndex = activeSeatIndices[i];
            CardVisual target = GetPlayerCardVisual(seatIndex, 0);

            if (target != null)
            {
                float delay = cardIndex * stagger;
                cardAnimator.DealCard(target, deckPosition, delay);
            }

            cardIndex++;
        }

        // Pass 2: deal card slot 1 to each active seat in order
        for (int i = 0; i < activeSeatIndices.Length; i++)
        {
            int seatIndex = activeSeatIndices[i];
            CardVisual target = GetPlayerCardVisual(seatIndex, 1);

            if (target != null)
            {
                float delay = cardIndex * stagger;
                cardAnimator.DealCard(target, deckPosition, delay);
            }

            cardIndex++;
        }

        // Wait for all deal animations to finish
        float totalWait = (totalCards - 1) * stagger + dealDuration;
        yield return new WaitForSeconds(totalWait);

        _dealState = AnimationState.Complete;
    }

    private IEnumerator RevealCommunityCardsRoutine(Card[] cards, int count)
    {
        const float stagger = 0.1f;

        int limit = Mathf.Min(count, communityCardVisuals.Length, cards.Length);

        for (int i = 0; i < limit; i++)
        {
            CardVisual visual = communityCardVisuals[i];
            if (visual == null)
            {
                yield return new WaitForSeconds(stagger);
                continue;
            }

            // Ensure the card visual has the correct card data before flipping
            visual.SetCard(cards[i], faceUp: false);
            cardAnimator.FlipCard(visual);

            if (i < limit - 1)
                yield return new WaitForSeconds(stagger);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the CardVisual for a given seat and card slot (0 or 1).
    /// Logs a warning and returns null if the slot is unassigned.
    /// </summary>
    private CardVisual GetPlayerCardVisual(int seatIndex, int cardSlot)
    {
        if (seatIndex < 0 || seatIndex >= playerCardSlots.Length)
        {
            Debug.LogWarning($"[CardDealerManager] Seat index {seatIndex} is out of range.", this);
            return null;
        }

        CardVisualPair pair = playerCardSlots[seatIndex];
        if (pair == null)
        {
            Debug.LogWarning($"[CardDealerManager] playerCardSlots[{seatIndex}] is null.", this);
            return null;
        }

        CardVisual visual = pair.Get(cardSlot);
        if (visual == null)
            Debug.LogWarning($"[CardDealerManager] playerCardSlots[{seatIndex}].card{cardSlot} is not assigned.", this);

        return visual;
    }

    /// <summary>
    /// Collects all CardVisual references (player hole cards + community cards) into a flat array
    /// for use with <see cref="CardAnimator.SweepAllCards"/>.
    /// </summary>
    private CardVisual[] GatherAllCardVisuals()
    {
        int playerCardCount = playerCardSlots.Length * 2;
        int total = playerCardCount + communityCardVisuals.Length;
        CardVisual[] all = new CardVisual[total];

        int idx = 0;
        foreach (CardVisualPair pair in playerCardSlots)
        {
            all[idx++] = pair?.card0;
            all[idx++] = pair?.card1;
        }

        foreach (CardVisual cv in communityCardVisuals)
            all[idx++] = cv;

        return all;
    }
}
