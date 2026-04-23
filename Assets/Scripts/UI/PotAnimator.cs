using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates chip sprites flying between player panels and the pot display.
///
/// AnimateBetToPot  — flies chips from a player panel position to the pot along an arc,
///                    completing within 0.6 seconds (Req 6.1).
/// AnimatePotToWinner — flies chips from the pot to the winner panel in a burst pattern,
///                    completing within 1.0 second (Req 6.2).
///
/// Animations are queued so they never overlap visually (Req 6.6).
/// After each animation all rented chips are returned to the pool so
/// ChipPool.rentedCount equals zero (Property 16).
/// A chip-clink audio cue is played per landing chip when AudioSource and
/// AudioClip are assigned (Req 6.5).
///
/// Requirements: 6.1, 6.2, 6.5, 6.6
/// </summary>
public class PotAnimator : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Serialized fields
    // -------------------------------------------------------------------------

    [Header("Pot Position")]
    [Tooltip("World-space position of the pot display (center of table).")]
    [SerializeField] private Transform potTransform;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chipClinkClip;

    [Header("Arc Settings")]
    [SerializeField] private float arcHeight = 120f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    // Queue of pending animation coroutines (ensures no overlap — Req 6.6)
    private readonly Queue<IEnumerator> _animationQueue = new Queue<IEnumerator>();
    private bool _isPlaying;

    // Greedy denomination list, highest first (matches ChipStack.CalculateChips)
    private static readonly int[] Denominations = { 10000, 5000, 1000, 500, 100, 50, 25, 20, 10, 5, 1 };
    private const int MaxChips = 10;

    // Timing constants
    private const float BetToPotDuration   = 0.6f;  // Req 6.1
    private const float PotToWinnerDuration = 1.0f; // Req 6.2
    private const float ChipStaggerInterval = 0.05f; // stagger between individual chips

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enqueues a bet-to-pot animation.
    /// Chips fly from <paramref name="fromPanelPosition"/> to the pot along an arc,
    /// completing within 0.6 seconds. Animations are queued so they do not overlap.
    /// After completion all rented chips are returned to <paramref name="pool"/>.
    /// Requirements: 6.1, 6.6
    /// </summary>
    public void AnimateBetToPot(Vector3 fromPanelPosition, decimal amount, ChipPool pool)
    {
        if (pool == null)
        {
            Debug.LogWarning("[PotAnimator] AnimateBetToPot called with null ChipPool — skipping.");
            return;
        }

        Vector3 toPosition = GetPotPosition();
        _animationQueue.Enqueue(RunBetToPot(fromPanelPosition, toPosition, amount, pool));
        TryStartQueue();
    }

    /// <summary>
    /// Enqueues a pot-to-winner animation.
    /// Chips fly from the pot to <paramref name="toWinnerPosition"/> in a burst pattern,
    /// completing within 1.0 second. Animations are queued so they do not overlap.
    /// After completion all rented chips are returned to <paramref name="pool"/>.
    /// Requirements: 6.2, 6.6
    /// </summary>
    public void AnimatePotToWinner(Vector3 toWinnerPosition, decimal amount, ChipPool pool)
    {
        if (pool == null)
        {
            Debug.LogWarning("[PotAnimator] AnimatePotToWinner called with null ChipPool — skipping.");
            return;
        }

        Vector3 fromPosition = GetPotPosition();
        _animationQueue.Enqueue(RunPotToWinner(fromPosition, toWinnerPosition, amount, pool));
        TryStartQueue();
    }

    // -------------------------------------------------------------------------
    // Queue management
    // -------------------------------------------------------------------------

    private void TryStartQueue()
    {
        if (!_isPlaying && _animationQueue.Count > 0)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        _isPlaying = true;
        while (_animationQueue.Count > 0)
        {
            IEnumerator next = _animationQueue.Dequeue();
            yield return StartCoroutine(next);
        }
        _isPlaying = false;
    }

    // -------------------------------------------------------------------------
    // Bet-to-pot animation (Req 6.1)
    // -------------------------------------------------------------------------

    private IEnumerator RunBetToPot(Vector3 from, Vector3 to, decimal amount, ChipPool pool)
    {
        List<int> chips = CalculateChips(amount);
        int count = chips.Count;

        // Per-chip flight time budget: spread chips across the 0.6s window
        // Each chip starts staggered but all must land within 0.6s total.
        float perChipDuration = Mathf.Max(0.25f, BetToPotDuration - (count - 1) * ChipStaggerInterval);

        // Rent all chips up front
        var rentedChips = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
        {
            GameObject chip = pool.Rent(chips[i]);
            if (chip != null)
            {
                chip.transform.position = from;
                rentedChips.Add(chip);
            }
        }

        // Launch each chip with a small stagger
        var flyCoroutines = new List<Coroutine>(rentedChips.Count);
        for (int i = 0; i < rentedChips.Count; i++)
        {
            float delay = i * ChipStaggerInterval;
            flyCoroutines.Add(StartCoroutine(
                FlyChipArc(rentedChips[i], from, to, perChipDuration, delay, onLand: PlayClinkAudio)
            ));
        }

        // Wait for the full animation window
        yield return new WaitForSeconds(BetToPotDuration);

        // Stop any still-running coroutines and snap chips to destination
        for (int i = 0; i < flyCoroutines.Count; i++)
        {
            if (flyCoroutines[i] != null)
                StopCoroutine(flyCoroutines[i]);
            if (rentedChips[i] != null)
                rentedChips[i].transform.position = to;
        }

        // Return all chips — rentedCount must equal zero after this (Property 16)
        foreach (var chip in rentedChips)
            pool.Return(chip);
    }

    // -------------------------------------------------------------------------
    // Pot-to-winner animation (Req 6.2)
    // -------------------------------------------------------------------------

    private IEnumerator RunPotToWinner(Vector3 from, Vector3 to, decimal amount, ChipPool pool)
    {
        List<int> chips = CalculateChips(amount);
        int count = chips.Count;

        // Burst: chips fan out from the pot then converge on the winner panel.
        // Each chip gets a random radial offset at the start to create the burst look.
        float perChipDuration = Mathf.Max(0.5f, PotToWinnerDuration - (count - 1) * ChipStaggerInterval);

        var rentedChips = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
        {
            GameObject chip = pool.Rent(chips[i]);
            if (chip != null)
            {
                chip.transform.position = from;
                rentedChips.Add(chip);
            }
        }

        var flyCoroutines = new List<Coroutine>(rentedChips.Count);
        for (int i = 0; i < rentedChips.Count; i++)
        {
            float delay = i * ChipStaggerInterval;
            // Burst offset: spread chips radially around the pot position
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 burstFrom = from + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 30f;

            flyCoroutines.Add(StartCoroutine(
                FlyChipArc(rentedChips[i], burstFrom, to, perChipDuration, delay, onLand: PlayClinkAudio)
            ));
        }

        // Wait for the full animation window
        yield return new WaitForSeconds(PotToWinnerDuration);

        // Stop any still-running coroutines and snap chips to destination
        for (int i = 0; i < flyCoroutines.Count; i++)
        {
            if (flyCoroutines[i] != null)
                StopCoroutine(flyCoroutines[i]);
            if (rentedChips[i] != null)
                rentedChips[i].transform.position = to;
        }

        // Return all chips — rentedCount must equal zero after this (Property 16)
        foreach (var chip in rentedChips)
            pool.Return(chip);
    }

    // -------------------------------------------------------------------------
    // Single-chip arc flight
    // -------------------------------------------------------------------------

    /// <summary>
    /// Moves a single chip along a parabolic arc from <paramref name="from"/> to
    /// <paramref name="to"/> over <paramref name="duration"/> seconds, after an
    /// optional <paramref name="delay"/>. Invokes <paramref name="onLand"/> when
    /// the chip reaches its destination.
    /// </summary>
    private IEnumerator FlyChipArc(GameObject chip, Vector3 from, Vector3 to,
        float duration, float delay, System.Action onLand)
    {
        if (chip == null) yield break;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (chip == null) yield break;

        chip.transform.position = from;

        // Delegate arc movement to TweenHelper
        yield return StartCoroutine(TweenHelper.ArcMove(chip.transform, from, to, arcHeight, duration));

        onLand?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays the chip-clink audio cue if both AudioSource and AudioClip are assigned.
    /// Requirement 6.5
    /// </summary>
    private void PlayClinkAudio()
    {
        if (audioSource != null && chipClinkClip != null)
        {
            audioSource.PlayOneShot(chipClinkClip);
        }
    }

    // -------------------------------------------------------------------------
    // Chip decomposition (greedy, matches ChipStack.CalculateChips)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Greedy decomposition of <paramref name="amount"/> into chip denominations,
    /// capped at <see cref="MaxChips"/>. Returns denomination values highest-first.
    /// Requirements: 6.3
    /// </summary>
    private static List<int> CalculateChips(decimal amount)
    {
        var result = new List<int>();
        decimal remaining = amount;

        foreach (int denom in Denominations)
        {
            if (remaining <= 0m) break;
            int count = (int)(remaining / denom);
            for (int i = 0; i < count && result.Count < MaxChips; i++)
            {
                result.Add(denom);
            }
            remaining -= (int)(remaining / denom) * denom;
        }

        // Guarantee at least one chip so an animation always plays
        if (result.Count == 0)
            result.Add(1);

        return result;
    }

    // -------------------------------------------------------------------------
    // Backward-compatible overloads (used by PokerGameManager legacy calls)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Legacy overload: animate pot-to-winner using a Transform target.
    /// Finds the ChipPool in the scene automatically.
    /// </summary>
    public IEnumerator AnimatePotToWinner(Transform winnerTransform, decimal amount)
    {
        if (winnerTransform == null) yield break;
        ChipPool pool = FindChipPool();
        if (pool == null) { yield return new WaitForSeconds(PotToWinnerDuration); yield break; }
        AnimatePotToWinner(winnerTransform.position, amount, pool);
        yield return new WaitForSeconds(PotToWinnerDuration + 0.1f);
    }

    /// <summary>
    /// Legacy overload: animate multiple bets to pot from saved positions.
    /// Finds the ChipPool in the scene automatically.
    /// </summary>
    public IEnumerator AnimateBetsFromPositions(System.Collections.Generic.List<(Transform position, decimal amount)> bets)
    {
        if (bets == null || bets.Count == 0) yield break;
        ChipPool pool = FindChipPool();
        if (pool == null) { yield return new WaitForSeconds(BetToPotDuration + 0.1f); yield break; }

        foreach (var (position, amount) in bets)
        {
            if (position != null && amount > 0)
                AnimateBetToPot(position.position, amount, pool);
        }

        // Wait for the longest possible queued animation to finish
        yield return new WaitForSeconds(BetToPotDuration + bets.Count * ChipStaggerInterval + 0.1f);
    }

    private ChipPool FindChipPool()
    {
        var pool = FindFirstObjectByType<ChipPool>();
        if (pool == null)
            Debug.LogWarning("[PotAnimator] No ChipPool found in scene — chip animation skipped.");
        return pool;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Vector3 GetPotPosition()
    {
        if (potTransform != null)
            return potTransform.position;

        // Fallback: screen center
        return Camera.main != null
            ? Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f))
            : Vector3.zero;
    }
}
