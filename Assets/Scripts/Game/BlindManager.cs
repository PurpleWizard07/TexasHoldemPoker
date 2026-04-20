using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages blind level increases over time (tournament style).
/// </summary>
public class BlindManager : MonoBehaviour
{
    [System.Serializable]
    public class BlindLevel
    {
        public int level;
        public float smallBlind;
        public float bigBlind;
        public int handsUntilIncrease; // Number of hands before next level

        public BlindLevel(int lvl, float sb, float bb, int hands)
        {
            level = lvl;
            smallBlind = sb;
            bigBlind = bb;
            handsUntilIncrease = hands;
        }
    }

    [Header("Blind Settings")]
    [SerializeField] private bool enableBlindIncreases = true;
    [SerializeField] private List<BlindLevel> blindLevels = new List<BlindLevel>();

    [Header("Display")]
    [SerializeField] private TMPro.TextMeshProUGUI blindLevelText;
    [SerializeField] private TMPro.TextMeshProUGUI nextIncreaseText;

    private int currentLevelIndex = 0;
    private int handsPlayedAtCurrentLevel = 0;

    private void Start()
    {
        // Initialize default blind structure if empty
        if (blindLevels.Count == 0)
        {
            InitializeDefaultBlinds();
        }

        UpdateDisplay();
    }

    private void InitializeDefaultBlinds()
    {
        blindLevels.Add(new BlindLevel(1, 5f, 10f, 10));      // Level 1: 5/10 for 10 hands
        blindLevels.Add(new BlindLevel(2, 10f, 20f, 10));     // Level 2: 10/20 for 10 hands
        blindLevels.Add(new BlindLevel(3, 15f, 30f, 10));     // Level 3: 15/30 for 10 hands
        blindLevels.Add(new BlindLevel(4, 25f, 50f, 10));     // Level 4: 25/50 for 10 hands
        blindLevels.Add(new BlindLevel(5, 50f, 100f, 10));    // Level 5: 50/100 for 10 hands
        blindLevels.Add(new BlindLevel(6, 75f, 150f, 10));    // Level 6: 75/150 for 10 hands
        blindLevels.Add(new BlindLevel(7, 100f, 200f, 10));   // Level 7: 100/200 for 10 hands
        blindLevels.Add(new BlindLevel(8, 150f, 300f, 10));   // Level 8: 150/300 for 10 hands
        blindLevels.Add(new BlindLevel(9, 200f, 400f, 10));   // Level 9: 200/400 for 10 hands
        blindLevels.Add(new BlindLevel(10, 300f, 600f, 999)); // Level 10: 300/600 (stays here)
    }

    /// <summary>
    /// Called after each hand completes.
    /// </summary>
    public void OnHandComplete()
    {
        if (!enableBlindIncreases) return;

        handsPlayedAtCurrentLevel++;

        // Check if we should increase blinds
        var currentLevel = GetCurrentLevel();
        if (currentLevel != null && handsPlayedAtCurrentLevel >= currentLevel.handsUntilIncrease)
        {
            IncreaseBlindLevel();
        }

        UpdateDisplay();
    }

    private void IncreaseBlindLevel()
    {
        if (currentLevelIndex < blindLevels.Count - 1)
        {
            currentLevelIndex++;
            handsPlayedAtCurrentLevel = 0;

            var newLevel = GetCurrentLevel();
            Debug.Log($"Blinds increased! Level {newLevel.level}: ${newLevel.smallBlind}/${newLevel.bigBlind}");

            // Notify about blind increase (you can add UI popup here)
            ShowBlindIncreaseNotification(newLevel);
        }
    }

    private void ShowBlindIncreaseNotification(BlindLevel level)
    {
        // TODO: Show a popup or notification
        Debug.Log($"=== BLINDS INCREASED ===");
        Debug.Log($"New blinds: ${level.smallBlind}/${level.bigBlind}");
    }

    /// <summary>
    /// Get current blind level.
    /// </summary>
    public BlindLevel GetCurrentLevel()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < blindLevels.Count)
            return blindLevels[currentLevelIndex];
        return null;
    }

    /// <summary>
    /// Get current small blind amount.
    /// </summary>
    public float GetSmallBlind()
    {
        var level = GetCurrentLevel();
        return level != null ? level.smallBlind : 5f;
    }

    /// <summary>
    /// Get current big blind amount.
    /// </summary>
    public float GetBigBlind()
    {
        var level = GetCurrentLevel();
        return level != null ? level.bigBlind : 10f;
    }

    /// <summary>
    /// Get hands remaining until next blind increase.
    /// </summary>
    public int GetHandsUntilIncrease()
    {
        var level = GetCurrentLevel();
        if (level == null) return 0;
        return level.handsUntilIncrease - handsPlayedAtCurrentLevel;
    }

    private void UpdateDisplay()
    {
        var level = GetCurrentLevel();
        if (level == null) return;

        // Update blind level text
        if (blindLevelText != null)
        {
            blindLevelText.text = $"Blinds: ${level.smallBlind}/${level.bigBlind}";
        }

        // Update next increase text
        if (nextIncreaseText != null)
        {
            int handsLeft = GetHandsUntilIncrease();
            if (handsLeft > 0 && currentLevelIndex < blindLevels.Count - 1)
            {
                nextIncreaseText.text = $"Increase in {handsLeft} hands";
            }
            else
            {
                nextIncreaseText.text = "Max level";
            }
        }
    }

    /// <summary>
    /// Reset blind manager to level 1.
    /// </summary>
    public void Reset()
    {
        currentLevelIndex = 0;
        handsPlayedAtCurrentLevel = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// Enable or disable blind increases.
    /// </summary>
    public void SetBlindIncreasesEnabled(bool enabled)
    {
        enableBlindIncreases = enabled;
    }
}
