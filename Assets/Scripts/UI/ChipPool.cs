using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Object pool for flying chip GameObjects used during Chip_Animation sequences.
/// Avoids per-frame allocations by pre-instantiating chips and recycling them.
/// Satisfies Requirements 6.3 and 12.3.
/// </summary>
public class ChipPool : MonoBehaviour
{
    [SerializeField] private int initialPoolSize = 24;

    private readonly List<GameObject> _pool = new List<GameObject>();
    private readonly List<GameObject> _rented = new List<GameObject>();

    /// <summary>Number of chips currently rented (used by Property 16 tests).</summary>
    public int rentedCount => _rented.Count;

    private void Awake()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            _pool.Add(CreateChip());
        }
    }

    /// <summary>
    /// Rent a chip GameObject configured with the sprite for the given denomination.
    /// If the pool is exhausted a new chip is instantiated and a warning is logged.
    /// </summary>
    /// <param name="denomination">Chip denomination value (e.g. 1, 5, 100, 500).</param>
    /// <returns>An active chip GameObject ready for animation.</returns>
    public GameObject Rent(int denomination)
    {
        // Find an inactive chip in the pool
        GameObject chip = null;
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && !_pool[i].activeSelf)
            {
                chip = _pool[i];
                break;
            }
        }

        // Pool exhausted — expand
        if (chip == null)
        {
            Debug.LogWarning($"[ChipPool] Pool exhausted (size={_pool.Count}). Expanding by 1.");
            chip = CreateChip();
            _pool.Add(chip);
        }

        // Load and assign denomination sprite
        string spriteName = $"chip{denomination}";
        Sprite sprite = Resources.Load<Sprite>(spriteName);
        if (sprite == null)
        {
            Debug.LogWarning($"[ChipPool] Sprite not found: Resources/{spriteName}.png — using null sprite.");
        }

        var image = chip.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
        }

        chip.name = $"FlyingChip_{denomination}";
        chip.SetActive(true);
        _rented.Add(chip);
        return chip;
    }

    /// <summary>
    /// Return a chip to the pool. Resets position, scale, and alpha, then deactivates it.
    /// </summary>
    /// <param name="chip">The chip GameObject previously obtained from <see cref="Rent"/>.</param>
    public void Return(GameObject chip)
    {
        if (chip == null) return;

        chip.transform.localPosition = Vector3.zero;
        chip.transform.localScale = Vector3.one;

        // Reset alpha via Image color
        var image = chip.GetComponent<Image>();
        if (image != null)
        {
            Color c = image.color;
            c.a = 1f;
            image.color = c;
        }

        chip.SetActive(false);
        _rented.Remove(chip);
    }

    /// <summary>
    /// Return all currently rented chips to the pool.
    /// </summary>
    public void ReturnAll()
    {
        // Iterate a copy because Return modifies _rented
        var toReturn = new List<GameObject>(_rented);
        foreach (var chip in toReturn)
        {
            Return(chip);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private GameObject CreateChip()
    {
        var go = new GameObject("FlyingChip");
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>();
        go.transform.SetParent(transform, false);
        go.SetActive(false);
        return go;
    }
}
