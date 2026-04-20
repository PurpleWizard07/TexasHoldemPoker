using UnityEngine;

/// <summary>
/// Fits a root RectTransform to the device safe area.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private RectTransform targetRoot;

    private Rect lastSafeArea;
    private Vector2Int lastResolution;

    private void Awake()
    {
        if (targetRoot == null)
        {
            targetRoot = GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        ApplySafeArea(force: true);
    }

    private void Update()
    {
        if (Screen.width != lastResolution.x || Screen.height != lastResolution.y || Screen.safeArea != lastSafeArea)
        {
            ApplySafeArea(force: false);
        }
    }

    public void ApplySafeArea(bool force)
    {
        if (targetRoot == null)
        {
            return;
        }

        Rect safe = Screen.safeArea;
        if (!force && safe == lastSafeArea && lastResolution.x == Screen.width && lastResolution.y == Screen.height)
        {
            return;
        }

        lastSafeArea = safe;
        lastResolution = new Vector2Int(Screen.width, Screen.height);

        Vector2 minAnchor = safe.position;
        Vector2 maxAnchor = safe.position + safe.size;
        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        targetRoot.anchorMin = minAnchor;
        targetRoot.anchorMax = maxAnchor;
        targetRoot.offsetMin = Vector2.zero;
        targetRoot.offsetMax = Vector2.zero;
    }
}
