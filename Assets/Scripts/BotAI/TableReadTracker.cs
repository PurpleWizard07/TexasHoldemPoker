using System.Collections.Generic;

/// <summary>
/// Tracks per-seat VPIP (Voluntarily Put money In Pot) using an 8-slot circular buffer.
/// Plain C# class — not a MonoBehaviour.
/// </summary>
public class TableReadTracker
{
    private const int BufferSize = 6; // minimum observations before tagging
    private const int HistorySize = 8; // circular buffer length

    // seat → 8-slot circular buffer of voluntary-entry booleans
    private readonly Dictionary<int, bool[]> _history = new Dictionary<int, bool[]>();

    // seat → next write position in the circular buffer
    private readonly Dictionary<int, int> _writeIndex = new Dictionary<int, int>();

    // seat → total number of hands recorded (capped at HistorySize for VPIP, but tracked separately for observation count)
    private readonly Dictionary<int, int> _observationCount = new Dictionary<int, int>();

    /// <summary>
    /// Records whether a seat voluntarily entered the pot this hand.
    /// Writes to the circular buffer and advances the write index.
    /// </summary>
    public void RecordHand(int seat, bool enteredVoluntarily)
    {
        if (!_history.ContainsKey(seat))
        {
            _history[seat] = new bool[HistorySize];
            _writeIndex[seat] = 0;
            _observationCount[seat] = 0;
        }

        int idx = _writeIndex[seat];
        _history[seat][idx] = enteredVoluntarily;
        _writeIndex[seat] = (idx + 1) % HistorySize;
        _observationCount[seat]++;
    }

    /// <summary>
    /// Returns the opponent tag for a seat based on VPIP over the last 8 hands.
    /// Returns Unknown if fewer than 6 observations have been recorded.
    /// Tight: VPIP &lt;= 0.30 | Loose: VPIP &gt;= 0.60 | Unknown: otherwise.
    /// </summary>
    public OpponentTag GetTag(int seat)
    {
        if (!_observationCount.ContainsKey(seat) || _observationCount[seat] < BufferSize)
            return OpponentTag.Unknown;

        bool[] buffer = _history[seat];
        int trueCount = 0;
        for (int i = 0; i < HistorySize; i++)
        {
            if (buffer[i]) trueCount++;
        }

        float vpip = (float)trueCount / HistorySize;

        if (vpip <= 0.30f) return OpponentTag.Tight;
        if (vpip >= 0.60f) return OpponentTag.Loose;
        return OpponentTag.Unknown;
    }

    /// <summary>
    /// Clears the circular buffer and observation count for the given seat.
    /// </summary>
    public void ResetSeat(int seat)
    {
        _history.Remove(seat);
        _writeIndex.Remove(seat);
        _observationCount.Remove(seat);
    }
}
