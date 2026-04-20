using UnityEngine;

/// <summary>
/// Lightweight runtime checks for UI polish/performance on different resolutions.
/// </summary>
public class UIQualityDiagnostics : MonoBehaviour
{
    [SerializeField] private bool runInDevelopmentBuildsOnly = true;
    [SerializeField] private float sampleWindowSeconds = 1f;
    [SerializeField] private float minTargetFps = 55f;
    [SerializeField] private RectTransform[] criticalUIBlocks;

    private float sampleTimer;
    private int frameCount;
    private bool enabledChecks;

    private void Awake()
    {
        enabledChecks = !runInDevelopmentBuildsOnly || Debug.isDebugBuild;
    }

    private void Update()
    {
        if (!enabledChecks)
        {
            return;
        }

        frameCount++;
        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= sampleWindowSeconds)
        {
            float fps = frameCount / sampleTimer;
            if (fps < minTargetFps)
            {
                Debug.LogWarning($"UIQualityDiagnostics: FPS dropped to {fps:0.0}. Check heavy animations/layouts.");
            }

            sampleTimer = 0f;
            frameCount = 0;
        }
    }

    [ContextMenu("Validate Critical UI Bounds")]
    public void ValidateCriticalUIBounds()
    {
        if (criticalUIBlocks == null)
        {
            return;
        }

        foreach (RectTransform block in criticalUIBlocks)
        {
            if (block == null || !block.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3[] corners = new Vector3[4];
            block.GetWorldCorners(corners);
            bool outOfBounds = false;
            foreach (Vector3 corner in corners)
            {
                if (corner.x < 0f || corner.x > Screen.width || corner.y < 0f || corner.y > Screen.height)
                {
                    outOfBounds = true;
                    break;
                }
            }

            if (outOfBounds)
            {
                Debug.LogWarning($"UIQualityDiagnostics: {block.name} is clipping outside visible bounds.");
            }
        }
    }
}
