using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utility window that programmatically builds the MainMenu and PokerGame scene hierarchies.
/// Menu: Tools → Poker UI → Setup Scenes
/// Satisfies Requirements 3.1, 3.2, 3.3, 3.5
/// </summary>
public class PokerUISetup : EditorWindow
{
    // -------------------------------------------------------------------------
    // Window
    // -------------------------------------------------------------------------

    [MenuItem("Tools/Poker UI/Setup Scenes")]
    public static void ShowWindow()
    {
        GetWindow<PokerUISetup>("Poker UI Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Poker UI Scene Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click 'Run Setup' to build the MainMenu and PokerGame scene hierarchies.\n" +
            "Both scenes will be saved automatically after setup.",
            MessageType.Info);
        EditorGUILayout.Space();

        if (GUILayout.Button("Run Setup", GUILayout.Height(40)))
        {
            RunSetup();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Validate Canvas Scalers"))
        {
            ValidateAllCanvasScalers();
        }
    }

    // -------------------------------------------------------------------------
    // Main entry point
    // -------------------------------------------------------------------------

    private static void RunSetup()
    {
        SetupMainMenuScene();
        SetupPokerGameScene();
        Debug.Log("[PokerUISetup] Setup complete. Both scenes have been saved.");
    }

    // =========================================================================
    // MAIN MENU SCENE
    // =========================================================================

    private static void SetupMainMenuScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",
            OpenSceneMode.Single);

        // --- Root Canvas ---
        var canvasGO = CreateCanvasRoot("MainMenuCanvas");

        // --- Background Image ---
        var bgGO = CreateFullScreenImage(canvasGO, "Background");

        // --- Title TextMeshPro ---
        var titleGO = new GameObject("Title");
        Undo.RegisterCreatedObjectUndo(titleGO, "Create Title");
        GameObjectUtility.SetParentAndAlign(titleGO, canvasGO);
        var titleRect = titleGO.GetComponent<RectTransform>();
        if (titleRect == null) titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.65f);
        titleRect.anchorMax = new Vector2(0.9f, 0.90f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleTMP = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
        titleTMP.text = "POKER";
        titleTMP.fontSize = 72;
        titleTMP.alignment = TMPro.TextAlignmentOptions.Center;

        // --- Button Layout Group ---
        var layoutGO = new GameObject("ButtonLayout");
        Undo.RegisterCreatedObjectUndo(layoutGO, "Create ButtonLayout");
        GameObjectUtility.SetParentAndAlign(layoutGO, canvasGO);
        var layoutRect = layoutGO.GetComponent<RectTransform>();
        if (layoutRect == null) layoutRect = layoutGO.AddComponent<RectTransform>();
        layoutRect.anchorMin = new Vector2(0.3f, 0.25f);
        layoutRect.anchorMax = new Vector2(0.7f, 0.60f);
        layoutRect.offsetMin = Vector2.zero;
        layoutRect.offsetMax = Vector2.zero;
        var vlg = layoutGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Buttons: Play, Settings, Quit
        CreateMenuButton(layoutGO, "PlayButton", "PLAY");
        CreateMenuButton(layoutGO, "SettingsButton", "SETTINGS");
        CreateMenuButton(layoutGO, "QuitButton", "QUIT");

        // --- MainMenuUI component on root Canvas ---
        // MainMenuUI does not exist yet — will be created in a later task.
        // TODO: Add canvasGO.AddComponent<MainMenuUI>() once MainMenuUI.cs is created (Task 13.1).
        Debug.Log("[PokerUISetup] MainMenuUI component skipped — script not yet created (Task 13.1).");

        // --- SceneTransitionManager with fade overlay ---
        SetupSceneTransitionManager();

        // --- Validate ---
        ValidateCanvasScalersInScene("MainMenu");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PokerUISetup] MainMenu scene saved.");
    }

    // =========================================================================
    // POKER GAME SCENE
    // =========================================================================

    private static void SetupPokerGameScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/PokerGame.unity",
            OpenSceneMode.Single);

        // --- Root Canvas ---
        var canvasGO = CreateCanvasRoot("PokerGameCanvas");

        // --- TableBackground ---
        CreateFullScreenImage(canvasGO, "TableBackground");

        // --- CommunityCardsDisplay ---
        var ccdGO = CreateChildGO(canvasGO, "CommunityCardsDisplay");
        SetAnchors(ccdGO, new Vector2(0.25f, 0.38f), new Vector2(0.75f, 0.62f));
        // TODO: Add ccdGO.AddComponent<CommunityCardsDisplay>() once script is created (Task 18.1).
        Debug.Log("[PokerUISetup] CommunityCardsDisplay component skipped — script not yet created (Task 18.1).");

        // --- CenterPotDisplay ---
        var cpdGO = CreateChildGO(canvasGO, "CenterPotDisplay");
        SetAnchors(cpdGO, new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.58f));
        cpdGO.AddComponent<CenterPotDisplay>();

        // --- ActionBar ---
        var actionBarGO = CreateChildGO(canvasGO, "ActionBar");
        SetAnchors(actionBarGO, new Vector2(0.1f, 0.0f), new Vector2(0.9f, 0.18f));
        // TODO: Add actionBarGO.AddComponent<ActionBar>() once script is created (Task 9.4).
        Debug.Log("[PokerUISetup] ActionBar component skipped — script not yet created (Task 9.4).");

        // ActionButton children: Fold, Check, Call, Bet, Raise, AllIn
        string[] actionButtonNames = { "Fold", "Check", "Call", "Bet", "Raise", "AllIn" };
        foreach (var btnName in actionButtonNames)
        {
            var abGO = CreateChildGO(actionBarGO, btnName + "Button");
            var btn = abGO.AddComponent<UnityEngine.UI.Button>();
            var btnImage = abGO.AddComponent<UnityEngine.UI.Image>();
            btn.targetGraphic = btnImage;
            var labelGO = new GameObject("Label");
            Undo.RegisterCreatedObjectUndo(labelGO, "Create ActionButton Label");
            GameObjectUtility.SetParentAndAlign(labelGO, abGO);
            var labelTMP = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            labelTMP.text = btnName.ToUpper();
            labelTMP.fontSize = 18;
            labelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            SetStretchToFill(labelGO);
            // TODO: Add abGO.AddComponent<ActionButton>() once script is created (Task 9.1).
        }
        Debug.Log("[PokerUISetup] ActionButton components skipped — script not yet created (Task 9.1).");

        // --- RaiseControl ---
        var raiseGO = CreateChildGO(canvasGO, "RaiseControl");
        SetAnchors(raiseGO, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.32f));
        // TODO: Add raiseGO.AddComponent<RaiseControl>() once script is created (Task 9.6).
        Debug.Log("[PokerUISetup] RaiseControl component skipped — script not yet created (Task 9.6).");

        // --- ShowdownUI ---
        var showdownGO = CreateChildGO(canvasGO, "ShowdownUI");
        SetAnchors(showdownGO, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f));
        // TODO: Add showdownGO.AddComponent<ShowdownUI>() once script is created (Task 11.1).
        Debug.Log("[PokerUISetup] ShowdownUI component skipped — script not yet created (Task 11.1).");

        // --- WinnerCelebration ---
        var winnerGO = CreateChildGO(canvasGO, "WinnerCelebration");
        SetStretchToFill(winnerGO);
        // TODO: Add winnerGO.AddComponent<WinnerCelebration>() once script is created (Task 11.4).
        Debug.Log("[PokerUISetup] WinnerCelebration component skipped — script not yet created (Task 11.4).");

        // --- GameOverUI ---
        var gameOverGO = CreateChildGO(canvasGO, "GameOverUI");
        SetStretchToFill(gameOverGO);
        // TODO: Add gameOverGO.AddComponent<GameOverUI>() once script is created (Task 12.1).
        Debug.Log("[PokerUISetup] GameOverUI component skipped — script not yet created (Task 12.1).");

        // --- CardDealerManager ---
        var cdmGO = new GameObject("CardDealerManager");
        Undo.RegisterCreatedObjectUndo(cdmGO, "Create CardDealerManager");
        // TODO: Add cdmGO.AddComponent<CardDealerManager>() once script is created (Task 6.2).
        Debug.Log("[PokerUISetup] CardDealerManager component skipped — script not yet created (Task 6.2).");

        // --- ChipPool ---
        var chipPoolGO = new GameObject("ChipPool");
        Undo.RegisterCreatedObjectUndo(chipPoolGO, "Create ChipPool");
        chipPoolGO.AddComponent<ChipPool>();

        // --- PotAnimator ---
        var potAnimGO = new GameObject("PotAnimator");
        Undo.RegisterCreatedObjectUndo(potAnimGO, "Create PotAnimator");
        potAnimGO.AddComponent<PotAnimator>();

        // --- Six PlayerUIPanel instances around the table ---
        SetupPlayerPanels(canvasGO);

        // --- Validate ---
        ValidateCanvasScalersInScene("PokerGame");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PokerUISetup] PokerGame scene saved.");
    }

    // =========================================================================
    // PLAYER PANELS
    // =========================================================================

    // Oval anchor positions for 6 player seats
    private static readonly Vector2[] SeatAnchorMins = new Vector2[]
    {
        new Vector2(0.35f, 0.02f), // Seat 0 — bottom center (human)
        new Vector2(0.65f, 0.05f), // Seat 1 — bottom right
        new Vector2(0.70f, 0.65f), // Seat 2 — top right
        new Vector2(0.35f, 0.72f), // Seat 3 — top center
        new Vector2(0.05f, 0.65f), // Seat 4 — top left
        new Vector2(0.10f, 0.05f), // Seat 5 — bottom left
    };

    private static readonly Vector2[] SeatAnchorMaxs = new Vector2[]
    {
        new Vector2(0.65f, 0.22f), // Seat 0
        new Vector2(0.90f, 0.25f), // Seat 1
        new Vector2(0.95f, 0.85f), // Seat 2
        new Vector2(0.65f, 0.92f), // Seat 3
        new Vector2(0.30f, 0.85f), // Seat 4
        new Vector2(0.35f, 0.25f), // Seat 5
    };

    private static void SetupPlayerPanels(GameObject canvasGO)
    {
        for (int i = 0; i < 6; i++)
        {
            var panelGO = CreateChildGO(canvasGO, $"PlayerUIPanel_{i}");
            SetAnchors(panelGO, SeatAnchorMins[i], SeatAnchorMaxs[i]);

            // Panel background image
            panelGO.AddComponent<UnityEngine.UI.Image>();

            // Player name label
            var nameGO = new GameObject("PlayerName");
            Undo.RegisterCreatedObjectUndo(nameGO, "Create PlayerName");
            GameObjectUtility.SetParentAndAlign(nameGO, panelGO);
            var nameTMP = nameGO.AddComponent<TMPro.TextMeshProUGUI>();
            nameTMP.text = $"Player {i}";
            nameTMP.fontSize = 18;
            nameTMP.alignment = TMPro.TextAlignmentOptions.Center;
            SetAnchors(nameGO, new Vector2(0f, 0.6f), new Vector2(1f, 1f));

            // Stack label
            var stackGO = new GameObject("StackText");
            Undo.RegisterCreatedObjectUndo(stackGO, "Create StackText");
            GameObjectUtility.SetParentAndAlign(stackGO, panelGO);
            var stackTMP = stackGO.AddComponent<TMPro.TextMeshProUGUI>();
            stackTMP.text = "$1000";
            stackTMP.fontSize = 16;
            stackTMP.alignment = TMPro.TextAlignmentOptions.Center;
            SetAnchors(stackGO, new Vector2(0f, 0.3f), new Vector2(1f, 0.6f));

            // Glow outline image
            var glowGO = new GameObject("GlowOutline");
            Undo.RegisterCreatedObjectUndo(glowGO, "Create GlowOutline");
            GameObjectUtility.SetParentAndAlign(glowGO, panelGO);
            glowGO.AddComponent<UnityEngine.UI.Image>();
            SetStretchToFill(glowGO);
            glowGO.SetActive(false);

            // Folded label
            var foldedGO = new GameObject("FoldedLabel");
            Undo.RegisterCreatedObjectUndo(foldedGO, "Create FoldedLabel");
            GameObjectUtility.SetParentAndAlign(foldedGO, panelGO);
            var foldedTMP = foldedGO.AddComponent<TMPro.TextMeshProUGUI>();
            foldedTMP.text = "FOLDED";
            foldedTMP.fontSize = 14;
            foldedTMP.alignment = TMPro.TextAlignmentOptions.Center;
            SetStretchToFill(foldedGO);
            foldedGO.SetActive(false);

            // AllIn badge
            var allInGO = new GameObject("AllInBadge");
            Undo.RegisterCreatedObjectUndo(allInGO, "Create AllInBadge");
            GameObjectUtility.SetParentAndAlign(allInGO, panelGO);
            allInGO.AddComponent<UnityEngine.UI.Image>();
            SetAnchors(allInGO, new Vector2(0.6f, 0.0f), new Vector2(1.0f, 0.3f));
            allInGO.SetActive(false);

            // CanvasGroup for alpha control
            panelGO.AddComponent<CanvasGroup>();

            // TODO: Add panelGO.AddComponent<PlayerUIPanel>() once script is created (Task 8.1).
        }
        Debug.Log("[PokerUISetup] PlayerUIPanel components skipped — script not yet created (Task 8.1).");
    }

    // =========================================================================
    // SCENE TRANSITION MANAGER
    // =========================================================================

    private static void SetupSceneTransitionManager()
    {
        // Persistent SceneTransitionManager GameObject
        var stmGO = new GameObject("SceneTransitionManager");
        Undo.RegisterCreatedObjectUndo(stmGO, "Create SceneTransitionManager");
        stmGO.AddComponent<SceneTransitionManager>();

        // Full-screen black overlay Canvas (DontDestroyOnLoad is handled by the component itself)
        var overlayCanvasGO = new GameObject("FadeOverlayCanvas");
        Undo.RegisterCreatedObjectUndo(overlayCanvasGO, "Create FadeOverlayCanvas");
        GameObjectUtility.SetParentAndAlign(overlayCanvasGO, stmGO);

        var overlayCanvas = overlayCanvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999; // always on top

        overlayCanvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Full-screen black image
        var overlayImageGO = new GameObject("FadeImage");
        Undo.RegisterCreatedObjectUndo(overlayImageGO, "Create FadeImage");
        GameObjectUtility.SetParentAndAlign(overlayImageGO, overlayCanvasGO);
        var overlayImage = overlayImageGO.AddComponent<UnityEngine.UI.Image>();
        overlayImage.color = Color.black;
        SetStretchToFill(overlayImageGO);

        // CanvasGroup for alpha control (used by SceneTransitionManager.fadeOverlay)
        var overlayCG = overlayImageGO.AddComponent<CanvasGroup>();
        overlayCG.alpha = 0f;
        overlayCG.blocksRaycasts = false;

        // Wire the fadeOverlay reference via SerializedObject
        var so = new SerializedObject(stmGO.GetComponent<SceneTransitionManager>());
        var fadeOverlayProp = so.FindProperty("fadeOverlay");
        if (fadeOverlayProp != null)
        {
            fadeOverlayProp.objectReferenceValue = overlayCG;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("[PokerUISetup] Could not find 'fadeOverlay' serialized property on SceneTransitionManager. " +
                             "Please assign it manually in the Inspector.");
        }
    }

    // =========================================================================
    // VALIDATION
    // =========================================================================

    /// <summary>
    /// Validates all Canvas components in the currently open scene use ScaleWithScreenSize at 1920×1080.
    /// </summary>
    private static void ValidateCanvasScalersInScene(string sceneName)
    {
        var scalers = Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsSortMode.None);
        foreach (var scaler in scalers)
        {
            bool modeOk = scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            bool resOk = scaler.referenceResolution == new Vector2(1920, 1080);
            bool matchOk = Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f);

            if (!modeOk || !resOk || !matchOk)
            {
                Debug.LogWarning(
                    $"[PokerUISetup] Canvas '{scaler.gameObject.name}' in scene '{sceneName}' " +
                    $"has incorrect CanvasScaler settings. " +
                    $"Expected: ScaleWithScreenSize, 1920×1080, match=0.5. " +
                    $"Got: mode={scaler.uiScaleMode}, res={scaler.referenceResolution}, match={scaler.matchWidthOrHeight}",
                    scaler.gameObject);
            }
        }
    }

    /// <summary>
    /// Public validation entry point — validates whichever scenes are currently open.
    /// </summary>
    private static void ValidateAllCanvasScalers()
    {
        ValidateCanvasScalersInScene(EditorSceneManager.GetActiveScene().name);
        Debug.Log("[PokerUISetup] Canvas scaler validation complete. Check Console for any warnings.");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    /// <summary>Creates a Canvas root with CanvasScaler (ScaleWithScreenSize 1920×1080 match 0.5) and GraphicRaycaster.</summary>
    private static GameObject CreateCanvasRoot(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        return go;
    }

    /// <summary>Creates a full-screen Image child of <paramref name="parent"/>.</summary>
    private static GameObject CreateFullScreenImage(GameObject parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        GameObjectUtility.SetParentAndAlign(go, parent);
        go.AddComponent<UnityEngine.UI.Image>();
        SetStretchToFill(go);
        return go;
    }

    /// <summary>Creates a plain child GameObject with a RectTransform parented to <paramref name="parent"/>.</summary>
    private static GameObject CreateChildGO(GameObject parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        GameObjectUtility.SetParentAndAlign(go, parent);
        // Ensure RectTransform exists
        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        return go;
    }

    /// <summary>Creates a Button child with a TextMeshProUGUI label.</summary>
    private static GameObject CreateMenuButton(GameObject parent, string name, string label)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        GameObjectUtility.SetParentAndAlign(go, parent);

        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 60f);

        var img = go.AddComponent<UnityEngine.UI.Image>();
        var btn = go.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;

        var labelGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(labelGO, $"Create {name} Label");
        GameObjectUtility.SetParentAndAlign(labelGO, go);
        var tmp = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        SetStretchToFill(labelGO);

        return go;
    }

    /// <summary>Sets RectTransform anchors to stretch-fill the parent.</summary>
    private static void SetStretchToFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Sets RectTransform anchor min/max and zeroes offsets.</summary>
    private static void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
