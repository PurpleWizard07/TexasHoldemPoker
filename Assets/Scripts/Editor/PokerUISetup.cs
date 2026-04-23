using System.Collections.Generic;
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
        LogManualAssignmentChecklist();
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

        // Add CanvasGroup to root canvas for MainMenuUI.menuCanvasGroup
        var canvasGroupOnRoot = canvasGO.AddComponent<CanvasGroup>();

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
        var playBtnGO     = CreateMenuButton(layoutGO, "PlayButton",     "PLAY");
        var settingsBtnGO = CreateMenuButton(layoutGO, "SettingsButton", "SETTINGS");
        var quitBtnGO     = CreateMenuButton(layoutGO, "QuitButton",     "QUIT");

        // --- MainMenuUI component on root Canvas ---
        var mainMenuUI = canvasGO.AddComponent<MainMenuUI>();
        AutoAssignMainMenuUI(mainMenuUI, titleGO, titleTMP, playBtnGO, settingsBtnGO, quitBtnGO, canvasGroupOnRoot);

        // --- SceneTransitionManager with fade overlay ---
        SetupSceneTransitionManager();

        // --- Validate ---
        ValidateCanvasScalersInScene("MainMenu");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PokerUISetup] MainMenu scene saved.");
    }

    /// <summary>
    /// Auto-assigns all serialized references on MainMenuUI using SerializedObject.
    /// Requirements: 3.1, 3.2
    /// </summary>
    private static void AutoAssignMainMenuUI(
        MainMenuUI mainMenuUI,
        GameObject titleGO,
        TMPro.TextMeshProUGUI titleTMP,
        GameObject playBtnGO,
        GameObject settingsBtnGO,
        GameObject quitBtnGO,
        CanvasGroup menuCanvasGroup)
    {
        var so = new SerializedObject(mainMenuUI);

        // UITheme
        var uiTheme = Resources.Load<UITheme>("UITheme");
        SetSerializedRef(so, "theme", uiTheme);

        // Title transform and text
        SetSerializedRef(so, "titleTransform", titleGO.GetComponent<RectTransform>());
        SetSerializedRef(so, "titleText", titleTMP);

        // Buttons
        SetSerializedRef(so, "playButton",     playBtnGO.GetComponent<UnityEngine.UI.Button>());
        SetSerializedRef(so, "settingsButton", settingsBtnGO.GetComponent<UnityEngine.UI.Button>());
        SetSerializedRef(so, "quitButton",     quitBtnGO.GetComponent<UnityEngine.UI.Button>());

        // Canvas group
        SetSerializedRef(so, "menuCanvasGroup", menuCanvasGroup);

        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] MainMenuUI references auto-assigned.");
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
        var communityCardsDisplay = ccdGO.AddComponent<CommunityCardsDisplay>();

        // --- CenterPotDisplay ---
        var cpdGO = CreateChildGO(canvasGO, "CenterPotDisplay");
        SetAnchors(cpdGO, new Vector2(0.35f, 0.42f), new Vector2(0.65f, 0.58f));
        var centerPotDisplay = cpdGO.AddComponent<CenterPotDisplay>();

        // --- ActionBar ---
        var actionBarGO = CreateChildGO(canvasGO, "ActionBar");
        SetAnchors(actionBarGO, new Vector2(0.1f, 0.0f), new Vector2(0.9f, 0.18f));
        var actionBarCG = actionBarGO.AddComponent<CanvasGroup>();
        var actionBar = actionBarGO.AddComponent<ActionBar>();

        // ActionButton children: Fold, Check, Call, Bet, Raise, AllIn
        string[] actionButtonNames = { "Fold", "Check", "Call", "Bet", "Raise", "AllIn" };
        var actionButtonGOs = new GameObject[actionButtonNames.Length];
        var actionButtons   = new ActionButton[actionButtonNames.Length];
        for (int i = 0; i < actionButtonNames.Length; i++)
        {
            var abGO = CreateChildGO(actionBarGO, actionButtonNames[i] + "Button");
            var btn = abGO.AddComponent<UnityEngine.UI.Button>();
            var btnImage = abGO.AddComponent<UnityEngine.UI.Image>();
            btn.targetGraphic = btnImage;
            abGO.AddComponent<CanvasGroup>();
            var labelGO = new GameObject("Label");
            Undo.RegisterCreatedObjectUndo(labelGO, "Create ActionButton Label");
            GameObjectUtility.SetParentAndAlign(labelGO, abGO);
            var labelTMP = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            labelTMP.text = actionButtonNames[i].ToUpper();
            labelTMP.fontSize = 18;
            labelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            SetStretchToFill(labelGO);
            var actionButton = abGO.AddComponent<ActionButton>();
            actionButtonGOs[i] = abGO;
            actionButtons[i]   = actionButton;
        }

        // --- RaiseControl ---
        var raiseGO = CreateChildGO(canvasGO, "RaiseControl");
        SetAnchors(raiseGO, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.32f));
        raiseGO.AddComponent<CanvasGroup>();
        var raiseControl = raiseGO.AddComponent<RaiseControl>();

        // --- ShowdownUI ---
        var showdownGO = CreateChildGO(canvasGO, "ShowdownUI");
        SetAnchors(showdownGO, new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f));
        var showdownUI = showdownGO.AddComponent<ShowdownUI>();
        var showdownWinnerPanel = SetupShowdownWinnerPanel(showdownGO);

        // --- WinnerCelebration ---
        var winnerGO = CreateChildGO(canvasGO, "WinnerCelebration");
        SetStretchToFill(winnerGO);
        var winnerCelebCG = winnerGO.AddComponent<CanvasGroup>();
        var winnerCelebration = winnerGO.AddComponent<WinnerCelebration>();

        // --- GameOverUI ---
        var gameOverGO = CreateChildGO(canvasGO, "GameOverUI");
        SetStretchToFill(gameOverGO);
        var gameOverCG = gameOverGO.AddComponent<CanvasGroup>();
        var gameOverUI = gameOverGO.AddComponent<GameOverUI>();
        var (playAgainBtnGO, mainMenuBtnGO) = SetupGameOverButtons(gameOverGO);

        // --- CardDealerManager ---
        var cdmGO = new GameObject("CardDealerManager");
        Undo.RegisterCreatedObjectUndo(cdmGO, "Create CardDealerManager");
        var cardAnimatorGO = new GameObject("CardAnimator");
        Undo.RegisterCreatedObjectUndo(cardAnimatorGO, "Create CardAnimator");
        var cardAnimator = cardAnimatorGO.AddComponent<CardAnimator>();
        var cardDealerManager = cdmGO.AddComponent<CardDealerManager>();

        // --- ChipPool ---
        var chipPoolGO = new GameObject("ChipPool");
        Undo.RegisterCreatedObjectUndo(chipPoolGO, "Create ChipPool");
        var chipPool = chipPoolGO.AddComponent<ChipPool>();

        // --- PotAnimator ---
        var potAnimGO = new GameObject("PotAnimator");
        Undo.RegisterCreatedObjectUndo(potAnimGO, "Create PotAnimator");
        var potAnimator = potAnimGO.AddComponent<PotAnimator>();

        // --- Six PlayerUIPanel instances around the table ---
        var playerPanels = SetupPlayerPanels(canvasGO);

        // --- UIManager ---
        var uiManagerGO = new GameObject("UIManager");
        Undo.RegisterCreatedObjectUndo(uiManagerGO, "Create UIManager");
        var uiManager = uiManagerGO.AddComponent<UIManager>();

        // --- Auto-assign all references ---
        AutoAssignActionBar(actionBar, actionButtons, raiseControl, actionBarCG);
        AutoAssignCardDealerManager(cardDealerManager, cardAnimator);
        AutoAssignShowdownUI(showdownUI, showdownWinnerPanel);
        AutoAssignWinnerCelebration(winnerCelebration, winnerCelebCG);
        AutoAssignGameOverUI(gameOverUI, gameOverCG, playAgainBtnGO, mainMenuBtnGO);
        AutoAssignUIManager(uiManager, playerPanels, communityCardsDisplay, centerPotDisplay,
            actionBar, raiseControl, showdownUI, winnerCelebration, gameOverUI,
            cardDealerManager, potAnimator, chipPool);

        // --- Validate ---
        ValidateCanvasScalersInScene("PokerGame");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PokerUISetup] PokerGame scene saved.");
    }

    /// <summary>Creates the winner panel sub-hierarchy inside ShowdownUI and returns the panel GO.</summary>
    private static GameObject SetupShowdownWinnerPanel(GameObject showdownGO)
    {
        var winnerPanel = CreateChildGO(showdownGO, "WinnerPanel");
        SetStretchToFill(winnerPanel);
        var winnerPanelCG = winnerPanel.AddComponent<CanvasGroup>();

        var winnerNameGO = new GameObject("WinnerNameText");
        Undo.RegisterCreatedObjectUndo(winnerNameGO, "Create WinnerNameText");
        GameObjectUtility.SetParentAndAlign(winnerNameGO, winnerPanel);
        var winnerNameTMP = winnerNameGO.AddComponent<TMPro.TextMeshProUGUI>();
        winnerNameTMP.text = "Player Name";
        winnerNameTMP.fontSize = 36;
        winnerNameTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(winnerNameGO, new Vector2(0f, 0.6f), new Vector2(1f, 1f));

        var winnerAmountGO = new GameObject("WinnerAmountText");
        Undo.RegisterCreatedObjectUndo(winnerAmountGO, "Create WinnerAmountText");
        GameObjectUtility.SetParentAndAlign(winnerAmountGO, winnerPanel);
        var winnerAmountTMP = winnerAmountGO.AddComponent<TMPro.TextMeshProUGUI>();
        winnerAmountTMP.text = "$0";
        winnerAmountTMP.fontSize = 28;
        winnerAmountTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(winnerAmountGO, new Vector2(0f, 0.3f), new Vector2(1f, 0.6f));

        var winnerHandGO = new GameObject("WinnerHandText");
        Undo.RegisterCreatedObjectUndo(winnerHandGO, "Create WinnerHandText");
        GameObjectUtility.SetParentAndAlign(winnerHandGO, winnerPanel);
        var winnerHandTMP = winnerHandGO.AddComponent<TMPro.TextMeshProUGUI>();
        winnerHandTMP.text = "Royal Flush";
        winnerHandTMP.fontSize = 22;
        winnerHandTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(winnerHandGO, new Vector2(0f, 0f), new Vector2(1f, 0.3f));

        var countdownPanel = CreateChildGO(showdownGO, "CountdownPanel");
        SetStretchToFill(countdownPanel);
        countdownPanel.SetActive(false);
        var countdownTextGO = new GameObject("CountdownText");
        Undo.RegisterCreatedObjectUndo(countdownTextGO, "Create CountdownText");
        GameObjectUtility.SetParentAndAlign(countdownTextGO, countdownPanel);
        var countdownTMP = countdownTextGO.AddComponent<TMPro.TextMeshProUGUI>();
        countdownTMP.text = "3";
        countdownTMP.fontSize = 72;
        countdownTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetStretchToFill(countdownTextGO);

        // Screen flash overlay
        var flashOverlayGO = CreateChildGO(showdownGO, "ScreenFlashOverlay");
        SetStretchToFill(flashOverlayGO);
        flashOverlayGO.AddComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0f);
        var flashCG = flashOverlayGO.AddComponent<CanvasGroup>();
        flashCG.alpha = 0f;
        flashCG.blocksRaycasts = false;

        // Wire ShowdownUI references
        var so = new SerializedObject(showdownGO.GetComponent<ShowdownUI>());
        SetSerializedRef(so, "winnerPanel",        winnerPanel);
        SetSerializedRef(so, "winnerPanelRect",    winnerPanel.GetComponent<RectTransform>());
        SetSerializedRef(so, "winnerNameText",     winnerNameTMP);
        SetSerializedRef(so, "winnerAmountText",   winnerAmountTMP);
        SetSerializedRef(so, "winnerHandText",     winnerHandTMP);
        SetSerializedRef(so, "winnerPanelCG",      winnerPanelCG);
        SetSerializedRef(so, "countdownPanel",     countdownPanel);
        SetSerializedRef(so, "countdownText",      countdownTMP);
        SetSerializedRef(so, "screenFlashOverlay", flashCG);
        so.ApplyModifiedProperties();

        return winnerPanel;
    }

    /// <summary>Creates Play Again and Main Menu buttons inside GameOverUI and returns them.</summary>
    private static (GameObject playAgain, GameObject mainMenu) SetupGameOverButtons(GameObject gameOverGO)
    {
        var headlineGO = new GameObject("HeadlineText");
        Undo.RegisterCreatedObjectUndo(headlineGO, "Create HeadlineText");
        GameObjectUtility.SetParentAndAlign(headlineGO, gameOverGO);
        var headlineTMP = headlineGO.AddComponent<TMPro.TextMeshProUGUI>();
        headlineTMP.text = "You Win!";
        headlineTMP.fontSize = 72;
        headlineTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(headlineGO, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.9f));

        var winnerNameGO = new GameObject("WinnerNameText");
        Undo.RegisterCreatedObjectUndo(winnerNameGO, "Create WinnerNameText");
        GameObjectUtility.SetParentAndAlign(winnerNameGO, gameOverGO);
        var winnerNameTMP = winnerNameGO.AddComponent<TMPro.TextMeshProUGUI>();
        winnerNameTMP.text = "Player";
        winnerNameTMP.fontSize = 28;
        winnerNameTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(winnerNameGO, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.6f));

        var finalStackGO = new GameObject("FinalStackText");
        Undo.RegisterCreatedObjectUndo(finalStackGO, "Create FinalStackText");
        GameObjectUtility.SetParentAndAlign(finalStackGO, gameOverGO);
        var finalStackTMP = finalStackGO.AddComponent<TMPro.TextMeshProUGUI>();
        finalStackTMP.text = "$0";
        finalStackTMP.fontSize = 24;
        finalStackTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(finalStackGO, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.45f));

        var playAgainGO = CreateMenuButton(gameOverGO, "PlayAgainButton", "PLAY AGAIN");
        SetAnchors(playAgainGO, new Vector2(0.2f, 0.15f), new Vector2(0.48f, 0.3f));
        var playAgainCG = playAgainGO.AddComponent<CanvasGroup>();

        var mainMenuGO = CreateMenuButton(gameOverGO, "MainMenuButton", "MAIN MENU");
        SetAnchors(mainMenuGO, new Vector2(0.52f, 0.15f), new Vector2(0.8f, 0.3f));
        var mainMenuCG = mainMenuGO.AddComponent<CanvasGroup>();

        // Wire GameOverUI references
        var so = new SerializedObject(gameOverGO.GetComponent<GameOverUI>());
        SetSerializedRef(so, "headlineText",    headlineTMP);
        SetSerializedRef(so, "winnerNameText",  winnerNameTMP);
        SetSerializedRef(so, "finalStackText",  finalStackTMP);
        SetSerializedRef(so, "playAgainButton", playAgainGO.GetComponent<UnityEngine.UI.Button>());
        SetSerializedRef(so, "mainMenuButton",  mainMenuGO.GetComponent<UnityEngine.UI.Button>());
        SetSerializedRef(so, "playAgainCG",     playAgainCG);
        SetSerializedRef(so, "mainMenuCG",      mainMenuCG);
        so.ApplyModifiedProperties();

        return (playAgainGO, mainMenuGO);
    }

    // =========================================================================
    // AUTO-ASSIGNMENT METHODS
    // =========================================================================

    /// <summary>
    /// Auto-assigns ActionBar serialized references.
    /// Requirements: 3.1, 3.2
    /// </summary>
    private static void AutoAssignActionBar(ActionBar actionBar, ActionButton[] buttons, RaiseControl raiseControl, CanvasGroup barCG)
    {
        var so = new SerializedObject(actionBar);
        var uiTheme = Resources.Load<UITheme>("UITheme");
        SetSerializedRef(so, "theme",          uiTheme);
        SetSerializedRef(so, "barCanvasGroup", barCG);
        SetSerializedRef(so, "barRect",        actionBar.GetComponent<RectTransform>());
        if (buttons.Length >= 6)
        {
            SetSerializedRef(so, "foldButton",   buttons[0]);
            SetSerializedRef(so, "checkButton",  buttons[1]);
            SetSerializedRef(so, "callButton",   buttons[2]);
            SetSerializedRef(so, "betButton",    buttons[3]);
            SetSerializedRef(so, "raiseButton",  buttons[4]);
            SetSerializedRef(so, "allInButton",  buttons[5]);
        }
        SetSerializedRef(so, "raiseControl", raiseControl);
        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] ActionBar references auto-assigned.");
    }

    /// <summary>
    /// Auto-assigns CardDealerManager serialized references.
    /// Requirements: 3.1, 3.2
    /// </summary>
    private static void AutoAssignCardDealerManager(CardDealerManager cdm, CardAnimator cardAnimator)
    {
        var so = new SerializedObject(cdm);
        var uiTheme = Resources.Load<UITheme>("UITheme");
        SetSerializedRef(so, "cardAnimator", cardAnimator);
        SetSerializedRef(so, "theme",        uiTheme);
        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] CardDealerManager references auto-assigned.");
    }

    /// <summary>
    /// Auto-assigns ShowdownUI serialized references (sub-hierarchy already wired in SetupShowdownWinnerPanel).
    /// </summary>
    private static void AutoAssignShowdownUI(ShowdownUI showdownUI, GameObject winnerPanel)
    {
        var so = new SerializedObject(showdownUI);
        var uiTheme = Resources.Load<UITheme>("UITheme");
        SetSerializedRef(so, "theme", uiTheme);
        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] ShowdownUI theme reference auto-assigned.");
    }

    /// <summary>
    /// Auto-assigns WinnerCelebration serialized references.
    /// </summary>
    private static void AutoAssignWinnerCelebration(WinnerCelebration wc, CanvasGroup cg)
    {
        var so = new SerializedObject(wc);
        var uiTheme = Resources.Load<UITheme>("UITheme");
        SetSerializedRef(so, "theme",          uiTheme);
        SetSerializedRef(so, "celebrationCG",  cg);

        // Create text children
        var wcGO = wc.gameObject;
        var youWinGO = new GameObject("YouWinText");
        Undo.RegisterCreatedObjectUndo(youWinGO, "Create YouWinText");
        GameObjectUtility.SetParentAndAlign(youWinGO, wcGO);
        var youWinTMP = youWinGO.AddComponent<TMPro.TextMeshProUGUI>();
        youWinTMP.text = "YOU WIN!";
        youWinTMP.fontSize = 72;
        youWinTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(youWinGO, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.9f));

        var amountGO = new GameObject("AmountText");
        Undo.RegisterCreatedObjectUndo(amountGO, "Create AmountText");
        GameObjectUtility.SetParentAndAlign(amountGO, wcGO);
        var amountTMP = amountGO.AddComponent<TMPro.TextMeshProUGUI>();
        amountTMP.text = "+$0";
        amountTMP.fontSize = 36;
        amountTMP.alignment = TMPro.TextAlignmentOptions.Center;
        SetAnchors(amountGO, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.5f));

        SetSerializedRef(so, "youWinText",  youWinTMP);
        SetSerializedRef(so, "amountText",  amountTMP);
        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] WinnerCelebration references auto-assigned.");
    }

    /// <summary>
    /// Auto-assigns GameOverUI serialized references (buttons already wired in SetupGameOverButtons).
    /// </summary>
    private static void AutoAssignGameOverUI(GameOverUI gameOverUI, CanvasGroup cg,
        GameObject playAgainBtnGO, GameObject mainMenuBtnGO)
    {
        var so = new SerializedObject(gameOverUI);
        var uiTheme = Resources.Load<UITheme>("UITheme");
        SetSerializedRef(so, "theme",               uiTheme);
        SetSerializedRef(so, "gameOverCanvasGroup", cg);
        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] GameOverUI references auto-assigned.");
    }

    /// <summary>
    /// Auto-assigns UIManager serialized references.
    /// Requirements: 3.1, 3.2
    /// </summary>
    private static void AutoAssignUIManager(
        UIManager uiManager,
        PlayerUIPanel[] playerPanels,
        CommunityCardsDisplay communityCardsDisplay,
        CenterPotDisplay centerPotDisplay,
        ActionBar actionBar,
        RaiseControl raiseControl,
        ShowdownUI showdownUI,
        WinnerCelebration winnerCelebration,
        GameOverUI gameOverUI,
        CardDealerManager cardDealerManager,
        PotAnimator potAnimator,
        ChipPool chipPool)
    {
        var so = new SerializedObject(uiManager);

        // Player panels array
        var playerPanelsProp = so.FindProperty("playerPanels");
        if (playerPanelsProp != null)
        {
            playerPanelsProp.arraySize = playerPanels.Length;
            for (int i = 0; i < playerPanels.Length; i++)
                playerPanelsProp.GetArrayElementAtIndex(i).objectReferenceValue = playerPanels[i];
        }

        SetSerializedRef(so, "communityCardsDisplay", communityCardsDisplay);
        SetSerializedRef(so, "centerPotDisplay",      centerPotDisplay);
        SetSerializedRef(so, "actionBar",             actionBar);
        SetSerializedRef(so, "raiseControl",          raiseControl);
        SetSerializedRef(so, "showdownUI",            showdownUI);
        SetSerializedRef(so, "winnerCelebration",     winnerCelebration);
        SetSerializedRef(so, "gameOverUI",            gameOverUI);
        SetSerializedRef(so, "cardDealerManager",     cardDealerManager);
        SetSerializedRef(so, "potAnimator",           potAnimator);
        SetSerializedRef(so, "chipPool",              chipPool);

        so.ApplyModifiedProperties();
        Debug.Log("[PokerUISetup] UIManager references auto-assigned.");
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

    private static PlayerUIPanel[] SetupPlayerPanels(GameObject canvasGO)
    {
        var panels = new PlayerUIPanel[6];
        var uiTheme = Resources.Load<UITheme>("UITheme");

        for (int i = 0; i < 6; i++)
        {
            var panelGO = CreateChildGO(canvasGO, $"PlayerUIPanel_{i}");
            SetAnchors(panelGO, SeatAnchorMins[i], SeatAnchorMaxs[i]);

            // Panel background image
            var panelBgImage = panelGO.AddComponent<UnityEngine.UI.Image>();

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
            var glowImage = glowGO.AddComponent<UnityEngine.UI.Image>();
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
            var panelCG = panelGO.AddComponent<CanvasGroup>();

            // PlayerUIPanel component
            var panel = panelGO.AddComponent<PlayerUIPanel>();
            panels[i] = panel;

            // Auto-assign PlayerUIPanel references
            var so = new SerializedObject(panel);
            SetSerializedRef(so, "theme",             uiTheme);
            SetSerializedRef(so, "playerNameText",    nameTMP);
            SetSerializedRef(so, "stackText",         stackTMP);
            SetSerializedRef(so, "panelBackground",   panelBgImage);
            SetSerializedRef(so, "glowOutline",       glowImage);
            SetSerializedRef(so, "foldedLabel",       foldedTMP);
            SetSerializedRef(so, "allInBadge",        allInGO);
            SetSerializedRef(so, "panelCanvasGroup",  panelCG);
            so.ApplyModifiedProperties();
        }

        Debug.Log("[PokerUISetup] PlayerUIPanel components created and references auto-assigned.");
        return panels;
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
    // SHARED SERIALIZED-OBJECT HELPERS
    // =========================================================================

    /// <summary>
    /// Sets a single serialized object reference property by field name.
    /// Logs a warning if the property is not found.
    /// </summary>
    private static void SetSerializedRef(SerializedObject so, string propertyName, Object value)
    {
        var prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = value;
        else
            Debug.LogWarning($"[PokerUISetup] SerializedProperty '{propertyName}' not found on {so.targetObject.GetType().Name}.");
    }

    // =========================================================================
    // MANUAL ASSIGNMENT CHECKLIST
    // =========================================================================

    /// <summary>
    /// Logs a checklist of references that still require manual assignment in the Inspector.
    /// Requirements: 3.1, 3.2
    /// </summary>
    private static void LogManualAssignmentChecklist()
    {
        var checklist = new List<string>
        {
            "=== MANUAL ASSIGNMENT CHECKLIST ===",
            "The following references could NOT be auto-assigned and require manual Inspector work:",
            "",
            "[ MainMenu scene ]",
            "  • SceneTransitionManager → Fade Overlay: already wired by PokerUISetup",
            "  • MainMenuUI → Theme: auto-assigned (verify UITheme asset exists at Assets/Resources/UITheme)",
            "  • MainMenuUI → Settings Panel (optional): drag a settings panel GO if you create one",
            "  • MainMenuUI → Accessibility Toggle (optional): drag a Toggle component if you add one",
            "",
            "[ PokerGame scene ]",
            "  • CardDealerManager → Deck Transform: drag a Transform to mark the deck position on the table",
            "  • CardDealerManager → Player Card Slots [0-5]: drag CardVisual pairs for each seat's hole cards",
            "  • CardDealerManager → Community Card Visuals [0-4]: drag the 5 CardVisual GOs from CommunityCardsDisplay",
            "  • CommunityCardsDisplay → Card Visuals [0-4]: drag the 5 CardVisual child GOs",
            "  • PlayerUIPanel [0-5] → Avatar Image: drag an Image component for each player's avatar",
            "  • PlayerUIPanel [0-5] → Dealer Badge: drag an Image component for the dealer chip badge",
            "  • PlayerUIPanel [0-5] → Card Visuals [0-1]: drag two CardVisual components per panel",
            "  • PlayerUIPanel [0-5] → Bet Display: drag a BetDisplay component if present",
            "  • PlayerUIPanel [0-5] → Action Display: drag a PlayerActionDisplay component if present",
            "  • PotAnimator → Pot Transform: drag the CenterPotDisplay Transform as the pot position",
            "  • PotAnimator → Audio Source / Chip Clink Clip (optional): assign for chip-clink audio",
            "  • ShowdownUI → Flash Particles (optional): assign a ParticleSystem for confetti",
            "  • WinnerCelebration → Confetti System (optional): assign a ParticleSystem",
            "  • WinnerCelebration → Sparkle System (optional): assign a ParticleSystem",
            "  • RaiseControl → Panel Rect / Canvas Group / Slider / Input Field / Buttons: wire child controls",
            "  • ActionButton [each] → Button / Image / Label / Canvas Group: wire child components",
            "",
            "All other references have been auto-assigned. Run 'Tools → Poker UI → Setup Scenes' to re-run setup.",
            "=== END CHECKLIST ==="
        };

        Debug.Log(string.Join("\n", checklist));
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
