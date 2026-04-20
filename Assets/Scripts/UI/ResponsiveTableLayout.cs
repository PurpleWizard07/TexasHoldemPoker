using UnityEngine;

/// <summary>
/// Applies seat/action layout presets for desktop and mobile aspect ratios.
/// </summary>
public class ResponsiveTableLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform[] playerSeatPanels;
    [SerializeField] private RectTransform centerPotRoot;
    [SerializeField] private RectTransform communityCardsRoot;
    [SerializeField] private RectTransform actionControlsRoot;

    [Header("Screen Rules")]
    [SerializeField] private float desktopAspectThreshold = 1.2f;

    [Header("Desktop Preset")]
    [SerializeField] private Vector2[] desktopSeatOffsets =
    {
        new Vector2(0f, -330f),
        new Vector2(-540f, -140f),
        new Vector2(-540f, 130f),
        new Vector2(0f, 290f),
        new Vector2(540f, 130f),
        new Vector2(540f, -140f)
    };
    [SerializeField] private Vector2 desktopActionOffset = new Vector2(0f, 56f);
    [SerializeField] private Vector2 desktopPotOffset = new Vector2(0f, 110f);
    [SerializeField] private Vector2 desktopCommunityOffset = new Vector2(0f, 15f);

    [Header("Mobile Preset")]
    [SerializeField] private Vector2[] mobileSeatOffsets =
    {
        new Vector2(0f, -395f),
        new Vector2(-300f, -270f),
        new Vector2(-300f, -40f),
        new Vector2(0f, 210f),
        new Vector2(300f, -40f),
        new Vector2(300f, -270f)
    };
    [SerializeField] private Vector2 mobileActionOffset = new Vector2(0f, 34f);
    [SerializeField] private Vector2 mobilePotOffset = new Vector2(0f, 85f);
    [SerializeField] private Vector2 mobileCommunityOffset = new Vector2(0f, -8f);

    private bool usingDesktopPreset;
    private Vector2Int lastScreenSize;

    private void Start()
    {
        ApplyBestPreset(force: true);
    }

    private void Update()
    {
        if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            ApplyBestPreset(force: false);
        }
    }

    public void ApplyBestPreset(bool force)
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        float aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
        bool shouldUseDesktop = aspect >= desktopAspectThreshold;

        if (!force && shouldUseDesktop == usingDesktopPreset)
        {
            return;
        }

        usingDesktopPreset = shouldUseDesktop;
        ApplySeatPreset(usingDesktopPreset ? desktopSeatOffsets : mobileSeatOffsets);
        ApplyCenterPreset();
    }

    private void ApplySeatPreset(Vector2[] offsets)
    {
        if (playerSeatPanels == null || offsets == null)
        {
            return;
        }

        int count = Mathf.Min(playerSeatPanels.Length, offsets.Length);
        for (int i = 0; i < count; i++)
        {
            if (playerSeatPanels[i] == null)
            {
                continue;
            }

            playerSeatPanels[i].anchorMin = new Vector2(0.5f, 0.5f);
            playerSeatPanels[i].anchorMax = new Vector2(0.5f, 0.5f);
            playerSeatPanels[i].pivot = new Vector2(0.5f, 0.5f);
            playerSeatPanels[i].anchoredPosition = offsets[i];
        }
    }

    private void ApplyCenterPreset()
    {
        if (actionControlsRoot != null)
        {
            actionControlsRoot.anchorMin = new Vector2(0.5f, 0f);
            actionControlsRoot.anchorMax = new Vector2(0.5f, 0f);
            actionControlsRoot.pivot = new Vector2(0.5f, 0f);
            actionControlsRoot.anchoredPosition = usingDesktopPreset ? desktopActionOffset : mobileActionOffset;
        }

        if (centerPotRoot != null)
        {
            centerPotRoot.anchorMin = new Vector2(0.5f, 0.5f);
            centerPotRoot.anchorMax = new Vector2(0.5f, 0.5f);
            centerPotRoot.pivot = new Vector2(0.5f, 0.5f);
            centerPotRoot.anchoredPosition = usingDesktopPreset ? desktopPotOffset : mobilePotOffset;
        }

        if (communityCardsRoot != null)
        {
            communityCardsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            communityCardsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            communityCardsRoot.pivot = new Vector2(0.5f, 0.5f);
            communityCardsRoot.anchoredPosition = usingDesktopPreset ? desktopCommunityOffset : mobileCommunityOffset;
        }
    }
}
