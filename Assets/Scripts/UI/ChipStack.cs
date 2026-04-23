using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a stack of chip GameObjects representing a monetary amount.
/// Uses greedy decomposition across 11 denominations, caps at 10 chips,
/// and positions chips with a 5px vertical offset per stacked chip of the same denomination.
/// Falls back to colored circle sprites when chip sprites are missing from Resources.
/// Requirements: 6.3, 6.4, 12.5
/// </summary>
public class ChipStack : MonoBehaviour
{
    private static readonly int[] Denominations = { 10000, 5000, 1000, 500, 100, 50, 25, 20, 10, 5, 1 };
    private const int MaxChips = 10;
    private const float VerticalOffsetPerChip = 5f;

    [SerializeField] private ChipPool chipPool;

    // Tracks child GameObjects created for display (not from ChipPool)
    private readonly List<GameObject> _displayChips = new List<GameObject>();

    // Fallback colored circle texture (shared across all instances)
    private static Texture2D _fallbackTexture;

    /// <summary>
    /// Greedy decomposition of <paramref name="amount"/> into chip denominations.
    /// Returns a list of denomination values, highest first.
    /// Requirements: 6.3
    /// </summary>
    public List<int> CalculateChips(decimal amount)
    {
        var result = new List<int>();
        decimal remaining = amount;

        foreach (int denom in Denominations)
        {
            if (remaining <= 0m) break;
            int count = (int)(remaining / denom);
            for (int i = 0; i < count; i++)
            {
                result.Add(denom);
            }
            remaining -= count * denom;
        }

        return result;
    }

    /// <summary>
    /// Clears any existing chip display and renders chips for the given amount.
    /// Caps at 10 chips total. Positions chips with a 5px vertical offset per
    /// stacked chip of the same denomination.
    /// Requirements: 6.3, 6.4, 12.5
    /// </summary>
    public void SetAmount(decimal amount)
    {
        ClearDisplayChips();

        if (amount <= 0m) return;

        List<int> denomList = CalculateChips(amount);

        // Cap at MaxChips
        if (denomList.Count > MaxChips)
            denomList = denomList.GetRange(0, MaxChips);

        // Track how many chips of each denomination have been placed so far
        // to compute the vertical stacking offset within each denomination group.
        var denomCountSoFar = new Dictionary<int, int>();

        for (int i = 0; i < denomList.Count; i++)
        {
            int denom = denomList[i];

            if (!denomCountSoFar.TryGetValue(denom, out int stackIndex))
                stackIndex = 0;

            GameObject chipGO = CreateDisplayChip(denom);
            if (chipGO == null) continue;

            // Position: stack vertically within the same denomination group
            var rt = chipGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, stackIndex * VerticalOffsetPerChip);
            }

            denomCountSoFar[denom] = stackIndex + 1;
            _displayChips.Add(chipGO);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private GameObject CreateDisplayChip(int denomination)
    {
        var go = new GameObject($"Chip_{denomination}");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40f, 40f);

        var image = go.AddComponent<Image>();

        // Try to load the sprite from Resources
        Sprite sprite = Resources.Load<Sprite>($"chip{denomination}");
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        else
        {
            // Fallback: colored circle using a plain Texture2D
            Debug.LogWarning($"[ChipStack] Sprite 'chip{denomination}' not found in Resources — using fallback color.");
            image.sprite = CreateFallbackSprite();
            image.color = GetFallbackColor(denomination);
        }

        return go;
    }

    private void ClearDisplayChips()
    {
        foreach (var chip in _displayChips)
        {
            if (chip != null)
                Destroy(chip);
        }
        _displayChips.Clear();
    }

    /// <summary>
    /// Returns (or creates) a shared 1×1 white circle sprite used as a fallback.
    /// </summary>
    private static Sprite CreateFallbackSprite()
    {
        if (_fallbackTexture == null)
        {
            _fallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _fallbackTexture.SetPixel(0, 0, Color.white);
            _fallbackTexture.Apply();
        }

        return Sprite.Create(
            _fallbackTexture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Maps a denomination to a distinct fallback color so chips are visually distinguishable.
    /// </summary>
    private static Color GetFallbackColor(int denomination)
    {
        return denomination switch
        {
            10000 => new Color(0.9f,  0.45f, 0.05f, 1f), // orange  — 10000
            5000  => new Color(0.75f, 0.15f, 0.15f, 1f), // red     — 5000
            1000  => new Color(0.55f, 0.0f,  0.55f, 1f), // purple  — 1000
            500   => new Color(0.15f, 0.35f, 0.75f, 1f), // blue    — 500
            100   => new Color(0.0f,  0.6f,  0.0f,  1f), // green   — 100
            50    => new Color(0.8f,  0.8f,  0.0f,  1f), // yellow  — 50
            25    => new Color(0.6f,  0.3f,  0.0f,  1f), // brown   — 25
            20    => new Color(0.0f,  0.7f,  0.7f,  1f), // teal    — 20
            10    => new Color(0.9f,  0.9f,  0.9f,  1f), // white   — 10
            5     => new Color(0.8f,  0.4f,  0.6f,  1f), // pink    — 5
            _     => new Color(0.6f,  0.6f,  0.6f,  1f), // grey    — 1 / unknown
        };
    }
}
