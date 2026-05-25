using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class XiuxianFitnessApp : MonoBehaviour
{
    private const string SaveKey = "XiuxianFitnessStateV2";
    private const long ProfileCooldownTicks = 7L * 24L * 60L * 60L * 10000000L;

    private Color bg = Rgb(17, 27, 31);
    private Color panel = new Color(0.96f, 0.94f, 0.86f, 0.96f);
    private Color ink = Rgb(30, 39, 39);
    private Color muted = Rgb(91, 103, 99);
    private Color jade = Rgb(34, 154, 126);
    private Color jadeDark = Rgb(18, 74, 72);
    private Color gold = Rgb(219, 164, 78);
    private Color cinnabar = Rgb(185, 72, 76);
    private Color sky = Rgb(74, 148, 196);
    private Color spiritGlow = Rgb(127, 229, 196);

    private AppState state;
    private Font font;
    private Sprite whiteSprite;
    private Sprite softCircleSprite;
    private readonly Dictionary<int, Sprite> roundedSprites = new Dictionary<int, Sprite>();

    private RectTransform screenRoot;
    private readonly Dictionary<int, GameObject> screens = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, Button> navButtons = new Dictionary<int, Button>();

    private readonly List<Text> realmNameTexts = new List<Text>();
    private readonly List<Text> realmLayerTexts = new List<Text>();
    private readonly List<Text> realmProgressTexts = new List<Text>();
    private readonly List<Image> realmBars = new List<Image>();
    private readonly List<Text> baseSpiritTexts = new List<Text>();
    private readonly List<Text> stabilityTexts = new List<Text>();

    private InputField heightInput;
    private InputField weightInput;
    private InputField ageInput;
    private CycleOption formulaOption;
    private Button profileButton;
    private Text profileNote;

    private SimpleDropdown activityOption;
    private InputField durationInput;
    private InputField distanceInput;
    private Text intensityPreviewText;

    private RectTransform homeTaskList;
    private RectTransform taskList;
    private RectTransform logList;
    private Text trainingStatsText;
    private CanvasGroup workoutModalGroup;
    private Button breakthroughButton;
    private Text breakthroughHint;

    private Text physiqueValue;
    private Text enduranceValue;
    private Text recoveryValue;
    private Text mindValue;
    private Image physiqueBar;
    private Image enduranceBar;
    private Image recoveryBar;
    private Image mindBar;

    private CanvasGroup toastGroup;
    private Text toastText;
    private CanvasGroup gainGroup;
    private Text gainAmount;
    private Text gainSource;

    private readonly RealmInfo[] realms =
    {
        new RealmInfo(T("FanTi"), T("WeiRuDao"), 100),
        new RealmInfo(T("LianQi"), T("YiCeng"), 140),
        new RealmInfo(T("LianQi"), T("ErCeng"), 190),
        new RealmInfo(T("LianQi"), T("SanCeng"), 250),
        new RealmInfo(T("LianQi"), T("SiCeng"), 320),
        new RealmInfo(T("LianQi"), T("WuCeng"), 400),
        new RealmInfo(T("LianQi"), T("LiuCeng"), 490),
        new RealmInfo(T("LianQi"), T("QiCeng"), 590),
        new RealmInfo(T("LianQi"), T("BaCeng"), 700),
        new RealmInfo(T("LianQi"), T("JiuCeng"), 820)
    };

    private readonly ActivityInfo[] activities =
    {
        new ActivityInfo("walk", T("Walk"), 3.5f, 0.95f, 0, 4, 1, 0),
        new ActivityInfo("run", T("Run"), 8.3f, 1.12f, 1, 2, 4, 2),
        new ActivityInfo("strength", T("Strength"), 5.0f, 1.08f, 2, 2, 3, 1),
        new ActivityInfo("stretch", T("Stretch"), 2.3f, 0.82f, 3, 7, 1, 0),
        new ActivityInfo("meditation", T("Meditation"), 1.3f, 0.75f, 4, 8, 1, 0)
    };

    private readonly TaskTemplate[] taskTemplates =
    {
        new TaskTemplate("walk", T("TaskWalk"), 0, 20, 16),
        new TaskTemplate("run", T("TaskRun"), 1, 15, 18),
        new TaskTemplate("strength", T("TaskStrength"), 2, 18, 18),
        new TaskTemplate("stretch", T("TaskStretch"), 3, 12, 14),
        new TaskTemplate("meditation", T("TaskMeditation"), 4, 10, 14)
    };

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        state = LoadState();
        if (state.tasks == null || state.tasks.Count == 0)
        {
            GenerateTasks();
        }

        RecalculateProfile(false);
        BuildUi();
        ApplyProfileToForm();
        SwitchTab(state.activeTab, false);
        Render();
    }

    private void BuildUi()
    {
        EnsureCamera();

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight = 1f;

        Image background = Image("Background", canvasObject.transform, bg);
        Stretch(background.rectTransform, 0, 0, 0, 0);
        GradientGraphic backgroundGradient = background.gameObject.AddComponent<GradientGraphic>();
        backgroundGradient.top = Rgb(22, 38, 48);
        backgroundGradient.bottom = Rgb(238, 231, 204);
        AddAtmosphere(canvasObject.transform);

        RectTransform app = Rect("App", canvasObject.transform);
        Stretch(app, 12, 12, 12, 12);
        app.sizeDelta = Vector2.zero;

        BuildHeader(app);

        screenRoot = Rect("Screens", app);
        Stretch(screenRoot, 0, 80, 0, 78);

        BuildHome();
        BuildFoundation();
        BuildTraining();
        BuildRealm();
        BuildRecords();
        BuildBottomNav(app);
        BuildPopups(app);
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = bg;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = -10f;
        camera.farClipPlane = 10f;
    }

    private void AddAtmosphere(Transform parent)
    {
        RectTransform ribbonLayer = Rect("CloudRibbons", parent);
        Stretch(ribbonLayer, 0, 0, 0, 0);
        ribbonLayer.SetAsLastSibling();
        for (int i = 0; i < 4; i++)
        {
            Image ribbon = Image("MistRibbon", ribbonLayer, new Color(0.58f, 0.93f, 0.80f, 0.055f + i * 0.01f));
            IgnoreLayout(ribbon.gameObject);
            ribbon.sprite = RoundedSprite(6);
            ribbon.raycastTarget = false;
            RectTransform rect = ribbon.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(132f + i * 34f, 3f);
            rect.anchoredPosition = new Vector2(286f, -112f - i * 18f);
            rect.localEulerAngles = new Vector3(0f, 0f, -24f);
        }

        RectTransform sparkLayer = Rect("SpiritSparks", parent);
        Stretch(sparkLayer, 0, 0, 0, 0);
        sparkLayer.SetAsLastSibling();
        for (int i = 0; i < 12; i++)
        {
            float size = 2f + (i % 3);
            RectTransform spark = Circle("SpiritSpark", sparkLayer, new Color(0.75f, 1f, 0.88f, 0.16f + (i % 3) * 0.04f), size);
            IgnoreLayout(spark.gameObject);
            spark.anchorMin = new Vector2(0f, 1f);
            spark.anchorMax = new Vector2(0f, 1f);
            spark.pivot = new Vector2(0.5f, 0.5f);
            spark.anchoredPosition = new Vector2(22f + (i * 73) % 350, -72f - (i * 97) % 690);
            spark.gameObject.AddComponent<FloatMotion>().Configure(3.4f + (i % 5) * 0.45f, 3f + (i % 4), i * 0.31f);
        }
    }

    private void BuildHeader(RectTransform root)
    {
        Image headerBg = Image("Header", root, new Color(0f, 0f, 0f, 0f));
        RectTransform header = headerBg.rectTransform;
        AnchorTop(header, 0, 0, 76);
        HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(2, 2, 4, 4);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform titleBox = Rect("Title", header);
        LayoutElement titleElement = EnsureLayoutElement(titleBox.gameObject);
        titleElement.flexibleWidth = 1;
        titleElement.preferredHeight = 60;
        VerticalLayoutGroup titleLayout = titleBox.gameObject.AddComponent<VerticalLayoutGroup>();
        titleLayout.spacing = 0;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = true;
        titleLayout.childForceExpandHeight = false;
        TextLine(titleBox, T("DemoSubtitle"), 13, Rgb(228, 196, 143), FontStyle.Bold, 18, TextAnchor.MiddleLeft);
        Text title = TextLine(titleBox, T("AppTitle"), 25, Color.white, FontStyle.Bold, 32, TextAnchor.MiddleLeft);
        AddTextShadow(title, new Color(0f, 0.15f, 0.12f, 0.5f), new Vector2(0f, -2f));

        Button reset = Button(header, "Reset", new Color(0.94f, 0.78f, 0.44f, 0.92f), Rgb(47, 44, 34), 64, 42);
        reset.onClick.AddListener(delegate
        {
            state = new AppState();
            GenerateTasks();
            RecalculateProfile(false);
            ApplyProfileToForm();
            SaveState();
            SwitchTab(0, false);
            Render();
            Toast(T("ResetDone"));
        });
    }

    private void BuildHome()
    {
        RectTransform screen = Screen(0);
        BuildHero(screen, T("Today"), T("HomeTitle"), T("HomeCopy"), true);

        RectTransform metrics = Rect("Metrics", screen);
        EnsureLayoutElement(metrics.gameObject).preferredHeight = 126;
        HorizontalLayoutGroup row = metrics.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 10;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        RectTransform spirit = Panel(metrics);
        AddPanelAccent(spirit, spiritGlow, 0.18f);
        TextLine(spirit, T("Spirit"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        baseSpiritTexts.Add(TextLine(spirit, T("BaseSpirit") + " 0", 22, jadeDark, FontStyle.Bold, 34, TextAnchor.MiddleLeft));
        TextLine(spirit, T("BaseSpiritHint"), 13, muted, FontStyle.Normal, 42, TextAnchor.UpperLeft);

        RectTransform stability = Panel(metrics);
        AddPanelAccent(stability, sky, 0.15f);
        TextLine(stability, T("Stability"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        stabilityTexts.Add(TextLine(stability, T("Stability") + " 50", 22, jadeDark, FontStyle.Bold, 34, TextAnchor.MiddleLeft));
        TextLine(stability, T("StabilityHint"), 13, muted, FontStyle.Normal, 42, TextAnchor.UpperLeft);

        RectTransform tasks = Panel(screen);
        AddPanelAccent(tasks, gold, 0.16f);
        EnsureLayoutElement(tasks.gameObject).preferredHeight = 226;
        TextLine(tasks, T("TodayGoal"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(tasks, T("DailyPractice"), 20, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        homeTaskList = VerticalList("HomeTaskList", tasks, 76);
        EnsureLayoutElement(homeTaskList.gameObject).preferredHeight = 166;
    }

    private void BuildFoundation()
    {
        RectTransform screen = Screen(1);
        RectTransform profile = Panel(screen);
        EnsureLayoutElement(profile.gameObject).preferredHeight = 392;
        TextLine(profile, T("Foundation"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(profile, T("BodyFoundation"), 22, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);

        RectTransform form = Rect("Form", profile);
        EnsureLayoutElement(form.gameObject).preferredHeight = 200;
        VerticalLayoutGroup formLayout = form.gameObject.AddComponent<VerticalLayoutGroup>();
        formLayout.spacing = 8;
        heightInput = LabeledInput(form, T("Height"), "170");
        weightInput = LabeledInput(form, T("Weight"), "65");
        ageInput = LabeledInput(form, T("Age"), "25");
        formulaOption = LabeledOption(form, T("Formula"), new[] { T("NeutralFormula"), T("MaleFormula"), T("FemaleFormula") });

        profileNote = TextLine(profile, T("BodyProtectNote"), 13, muted, FontStyle.Normal, 42, TextAnchor.UpperLeft);

        profileButton = Button(profile, T("CreateFoundation"), jade, Color.white, 0, 44);
        profileButton.onClick.AddListener(SubmitProfile);

        RectTransform stats = Panel(screen);
        EnsureLayoutElement(stats.gameObject).preferredHeight = 268;
        TextLine(stats, T("Attribute"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(stats, T("AttributePanel"), 22, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        baseSpiritTexts.Add(Badge(stats, T("BaseSpirit") + " 0", 120, 32));

        RectTransform statList = VerticalList("Stats", stats, 76);
        EnsureLayoutElement(statList.gameObject).preferredHeight = 160;
        AddStat(statList, 0, T("Physique"));
        AddStat(statList, 1, T("Endurance"));
        AddStat(statList, 2, T("Recovery"));
        AddStat(statList, 3, T("Mind"));
    }

    private void BuildTraining()
    {
        RectTransform screen = Screen(2);

        RectTransform tasks = Panel(screen);
        EnsureLayoutElement(tasks.gameObject).preferredHeight = 306;
        TextLine(tasks, T("Training"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(tasks, T("DailyTask"), 22, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        Button refresh = Button(tasks, T("RefreshTasks"), Rgb(232, 242, 238), jadeDark, 86, 36);
        AnchorTopRight(refresh.GetComponent<RectTransform>(), 0, 0, 86, 36);
        refresh.onClick.AddListener(delegate
        {
            GenerateTasks();
            SaveState();
            Render();
            Toast(T("TasksRefreshed"));
        });
        taskList = VerticalList("TaskList", tasks, 76);
        EnsureLayoutElement(taskList.gameObject).preferredHeight = 252;

        RectTransform workout = Panel(screen);
        EnsureLayoutElement(workout.gameObject).preferredHeight = 258;
        TextLine(workout, T("CheckIn"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(workout, T("WorkoutStats"), 22, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        trainingStatsText = TextLine(workout, "", 14, muted, FontStyle.Normal, 116, TextAnchor.UpperLeft);
        Button open = Button(workout, T("OpenCheckIn"), jade, Color.white, 0, 46);
        open.onClick.AddListener(OpenWorkoutModal);
    }

    private void BuildRealm()
    {
        RectTransform screen = Screen(3);
        HeroRefs hero = BuildHero(screen, T("Realm"), T("RealmTitle"), T("BreakthroughHint"), false);
        breakthroughHint = hero.copy;
        breakthroughButton = Button(hero.card.rectTransform, T("TryBreakthrough"), jade, Color.white, 0, 44);
        breakthroughButton.onClick.AddListener(AttemptBreakthrough);
    }

    private void BuildRecords()
    {
        RectTransform screen = Screen(4);
        RectTransform panelRoot = Panel(screen);
        EnsureLayoutElement(panelRoot.gameObject).preferredHeight = 636;
        TextLine(panelRoot, T("Records"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(panelRoot, T("LogTitle"), 22, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        logList = VerticalList("Logs", panelRoot, 76);
        EnsureLayoutElement(logList.gameObject).preferredHeight = 560;
    }

    private HeroRefs BuildHero(RectTransform parent, string eyebrow, string title, string copy, bool home)
    {
        if (home)
        {
            return BuildHomeHero(parent, eyebrow, title, copy);
        }

        Image hero = Image("Hero", parent, jadeDark);
        hero.sprite = RoundedSprite(12);
        GradientGraphic heroGradient = hero.gameObject.AddComponent<GradientGraphic>();
        heroGradient.top = Rgb(16, 83, 88);
        heroGradient.bottom = Rgb(24, 50, 55);
        AddHeroDecor(hero.rectTransform);
        EnsureLayoutElement(hero.gameObject).preferredHeight = 224;
        HorizontalLayoutGroup layout = hero.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        RectTransform left = Rect("HeroText", hero.rectTransform);
        EnsureLayoutElement(left.gameObject).flexibleWidth = 1.3f;
        VerticalLayoutGroup leftLayout = left.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.childAlignment = TextAnchor.LowerLeft;
        leftLayout.spacing = 5;
        leftLayout.padding = new RectOffset(0, 0, 58, 0);
        TextLine(left, eyebrow, 13, Rgb(228, 196, 143), FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        Text titleText = TextLine(left, title, 22, Color.white, FontStyle.Bold, 56, TextAnchor.MiddleLeft);
        Text copyText = TextLine(left, copy, 14, Rgb(244, 239, 225), FontStyle.Normal, 72, TextAnchor.UpperLeft);

        Image card = Image("RealmCard", hero.rectTransform, new Color(1f, 0.98f, 0.94f, 0.16f));
        card.sprite = RoundedSprite(10);
        AddInnerStroke(card.rectTransform, new Color(1f, 0.9f, 0.58f, 0.16f), 2f);
        EnsureLayoutElement(card.gameObject).flexibleWidth = 0.9f;
        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(14, 14, 18, 14);
        cardLayout.spacing = 8;
        cardLayout.childAlignment = TextAnchor.LowerLeft;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        Text realmName = TextLine(card.rectTransform, T("FanTi"), 14, Rgb(228, 196, 143), FontStyle.Bold, 22, TextAnchor.MiddleLeft);
        Text realmLayer = TextLine(card.rectTransform, T("WeiRuDao"), 28, Color.white, FontStyle.Bold, 40, TextAnchor.MiddleLeft);
        Image progressBg = Image("ProgressBg", card.rectTransform, new Color(1f, 1f, 1f, 0.22f));
        progressBg.sprite = RoundedSprite(4);
        AllowLayout(progressBg.gameObject);
        EnsureLayoutElement(progressBg.gameObject).preferredHeight = 12;
        Image progressBar = Image("ProgressBar", progressBg.rectTransform, gold);
        progressBar.sprite = RoundedSprite(4);
        AddGlow(progressBar.rectTransform, new Color(1f, 0.82f, 0.32f, 0.36f), 3f);
        Stretch(progressBar.rectTransform, 0, 0, 0, 0);
        progressBar.type = UnityEngine.UI.Image.Type.Filled;
        progressBar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        Text progress = TextLine(card.rectTransform, "0 / 100 " + T("Cultivation"), 13, Rgb(244, 239, 225), FontStyle.Normal, 22, TextAnchor.MiddleLeft);

        realmNameTexts.Add(realmName);
        realmLayerTexts.Add(realmLayer);
        if (!home)
        {
            realmLayerTexts.Add(titleText);
        }

        realmProgressTexts.Add(progress);
        realmBars.Add(progressBar);
        return new HeroRefs { card = card, copy = copyText };
    }

    private HeroRefs BuildHomeHero(RectTransform parent, string eyebrow, string title, string copy)
    {
        Image hero = Image("Hero", parent, jadeDark);
        hero.sprite = RoundedSprite(12);
        GradientGraphic heroGradient = hero.gameObject.AddComponent<GradientGraphic>();
        heroGradient.top = Rgb(18, 92, 96);
        heroGradient.bottom = Rgb(20, 48, 58);
        AddHeroDecor(hero.rectTransform);
        EnsureLayoutElement(hero.gameObject).preferredHeight = 238;
        VerticalLayoutGroup layout = hero.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextLine(hero.rectTransform, eyebrow, 13, Rgb(228, 196, 143), FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        Text titleText = TextLine(hero.rectTransform, title, 24, Color.white, FontStyle.Bold, 62, TextAnchor.MiddleLeft);
        Text copyText = TextLine(hero.rectTransform, copy, 14, Rgb(244, 239, 225), FontStyle.Normal, 28, TextAnchor.MiddleLeft);

        Image progressPanel = Image("RealmProgressPanel", hero.rectTransform, new Color(1f, 0.98f, 0.94f, 0.14f));
        progressPanel.sprite = RoundedSprite(10);
        AddInnerStroke(progressPanel.rectTransform, new Color(1f, 0.92f, 0.62f, 0.15f), 2f);
        EnsureLayoutElement(progressPanel.gameObject).preferredHeight = 70;
        HorizontalLayoutGroup row = progressPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(12, 12, 10, 10);
        row.spacing = 12;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = false;

        RectTransform realmBox = Rect("RealmText", progressPanel.rectTransform);
        EnsureLayoutElement(realmBox.gameObject).preferredWidth = 104;
        VerticalLayoutGroup realmLayout = realmBox.gameObject.AddComponent<VerticalLayoutGroup>();
        realmLayout.spacing = 0;
        realmLayout.childControlHeight = true;
        Text realmName = TextLine(realmBox, T("FanTi"), 13, Rgb(228, 196, 143), FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        Text realmLayer = TextLine(realmBox, T("WeiRuDao"), 26, Color.white, FontStyle.Bold, 34, TextAnchor.MiddleLeft);

        RectTransform progressBox = Rect("ProgressText", progressPanel.rectTransform);
        EnsureLayoutElement(progressBox.gameObject).flexibleWidth = 1;
        VerticalLayoutGroup progressLayout = progressBox.gameObject.AddComponent<VerticalLayoutGroup>();
        progressLayout.spacing = 6;
        progressLayout.childControlHeight = true;
        progressLayout.childControlWidth = true;
        Image progressBg = Image("ProgressBg", progressBox, new Color(1f, 1f, 1f, 0.22f));
        progressBg.sprite = RoundedSprite(4);
        AllowLayout(progressBg.gameObject);
        EnsureLayoutElement(progressBg.gameObject).preferredHeight = 12;
        Image progressBar = Image("ProgressBar", progressBg.rectTransform, gold);
        progressBar.sprite = RoundedSprite(4);
        AddGlow(progressBar.rectTransform, new Color(1f, 0.82f, 0.32f, 0.36f), 3f);
        Stretch(progressBar.rectTransform, 0, 0, 0, 0);
        progressBar.type = UnityEngine.UI.Image.Type.Filled;
        progressBar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        Text progress = TextLine(progressBox, "0 / 100 " + T("Cultivation"), 13, Rgb(244, 239, 225), FontStyle.Normal, 22, TextAnchor.MiddleLeft);

        realmNameTexts.Add(realmName);
        realmLayerTexts.Add(realmLayer);
        realmProgressTexts.Add(progress);
        realmBars.Add(progressBar);
        return new HeroRefs { card = progressPanel, copy = copyText };
    }

    private RectTransform Screen(int tab)
    {
        RectTransform screen = Rect("Screen" + tab, screenRoot);
        Stretch(screen, 0, 0, 0, 0);

        ScrollRect scroll = screen.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 24f;

        RectTransform viewport = Rect("Viewport", screen);
        Stretch(viewport, 0, 0, 0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        content.sizeDelta = new Vector2(0, 640);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.verticalNormalizedPosition = 1f;
        screens[tab] = screen.gameObject;
        return content;
    }

    private void AddHeroDecor(RectTransform hero)
    {
        for (int i = 0; i < 4; i++)
        {
            Image line = Image("HeroRune", hero, new Color(1f, 0.94f, 0.65f, 0.12f));
            IgnoreLayout(line.gameObject);
            line.sprite = RoundedSprite(6);
            RectTransform rect = line.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(72f + i * 14f, 3f);
            rect.anchoredPosition = new Vector2(-18f, -72f - i * 20f);
            rect.localEulerAngles = new Vector3(0f, 0f, -22f);
            rect.SetAsFirstSibling();
        }
    }

    private void BuildBottomNav(Transform parent)
    {
        Image nav = Image("BottomNavigation", parent, new Color(0.95f, 0.92f, 0.82f, 0.98f));
        nav.sprite = RoundedSprite(14);
        AddInnerStroke(nav.rectTransform, new Color(1f, 0.9f, 0.6f, 0.26f), 2f);
        AnchorBottom(nav.rectTransform, 0, 0, 66);
        HorizontalLayoutGroup layout = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        AddNav(nav.rectTransform, 0, T("NavHome"));
        AddNav(nav.rectTransform, 1, T("NavFoundation"));
        AddNav(nav.rectTransform, 2, T("NavTraining"));
        AddNav(nav.rectTransform, 3, T("NavRealm"));
        AddNav(nav.rectTransform, 4, T("NavRecords"));
    }

    private void BuildPopups(Transform parent)
    {
        Image gain = Image("GainPopup", parent, jadeDark);
        gain.sprite = RoundedSprite(10);
        GradientGraphic gainGradient = gain.gameObject.AddComponent<GradientGraphic>();
        gainGradient.top = Rgb(28, 118, 105);
        gainGradient.bottom = Rgb(17, 55, 58);
        AddGlow(gain.rectTransform, new Color(0.43f, 1f, 0.78f, 0.24f), 10f);
        AnchorTopRight(gain.rectTransform, 12, 12, 184, 84);
        gainGroup = gain.gameObject.AddComponent<CanvasGroup>();
        gainGroup.alpha = 0f;
        VerticalLayoutGroup gainLayout = gain.gameObject.AddComponent<VerticalLayoutGroup>();
        gainLayout.padding = new RectOffset(14, 14, 12, 12);
        gainLayout.spacing = 2;
        TextLine(gain.rectTransform, T("GainTitle"), 13, Rgb(228, 196, 143), FontStyle.Bold, 18, TextAnchor.MiddleLeft);
        gainAmount = TextLine(gain.rectTransform, "+0", 28, Color.white, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        gainSource = TextLine(gain.rectTransform, T("DonePractice"), 12, Rgb(244, 239, 225), FontStyle.Normal, 18, TextAnchor.MiddleLeft);

        Image toast = Image("Toast", parent, new Color(0.10f, 0.15f, 0.14f, 0.95f));
        toast.sprite = RoundedSprite(10);
        AnchorBottom(toast.rectTransform, 28, 88, 48);
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastText = TextLine(toast.rectTransform, "", 14, Color.white, FontStyle.Normal, 44, TextAnchor.MiddleCenter);
        Stretch(toastText.rectTransform, 12, 0, 12, 0);

        BuildWorkoutModal(parent);
    }

    private void BuildWorkoutModal(Transform parent)
    {
        Image overlay = Image("WorkoutModal", parent, new Color(0f, 0f, 0f, 0.48f));
        Stretch(overlay.rectTransform, 0, 0, 0, 0);
        workoutModalGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        workoutModalGroup.alpha = 0f;
        workoutModalGroup.interactable = false;
        workoutModalGroup.blocksRaycasts = false;

        Button blocker = overlay.gameObject.AddComponent<Button>();
        blocker.transition = Selectable.Transition.None;
        blocker.onClick.AddListener(CloseWorkoutModal);

        Image sheet = Image("WorkoutSheet", overlay.rectTransform, panel);
        sheet.sprite = RoundedSprite(12);
        AddInnerStroke(sheet.rectTransform, new Color(0.86f, 0.62f, 0.25f, 0.28f), 2f);
        AnchorBottom(sheet.rectTransform, 16, 18, 430);
        VerticalLayoutGroup layout = sheet.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextLine(sheet.rectTransform, T("CheckIn"), 13, gold, FontStyle.Bold, 20, TextAnchor.MiddleLeft);
        TextLine(sheet.rectTransform, T("WorkoutRecord"), 22, ink, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        activityOption = LabeledDropdown(sheet.rectTransform, T("WorkoutType"), new[] { T("Walk"), T("Run"), T("Strength"), T("Stretch"), T("Meditation") });
        activityOption.OnChanged += RefreshWorkoutInputHint;
        durationInput = LabeledInput(sheet.rectTransform, T("Duration"), "20");
        distanceInput = LabeledInput(sheet.rectTransform, T("DistanceKm"), "2.0");
        intensityPreviewText = TextLine(sheet.rectTransform, "", 13, muted, FontStyle.Normal, 46, TextAnchor.UpperLeft);
        RefreshWorkoutInputHint();

        RectTransform row = Rect("ModalActions", sheet.rectTransform);
        EnsureLayoutElement(row.gameObject).preferredHeight = 46;
        HorizontalLayoutGroup actions = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        actions.spacing = 10;
        actions.childControlWidth = true;
        actions.childControlHeight = true;
        actions.childForceExpandWidth = true;
        Button cancel = Button(row, T("Cancel"), Rgb(232, 242, 238), jadeDark, 0, 46);
        Button submit = Button(row, T("SubmitWorkout"), jade, Color.white, 0, 46);
        EnsureLayoutElement(cancel.gameObject).flexibleWidth = 0.8f;
        EnsureLayoutElement(submit.gameObject).flexibleWidth = 1.2f;
        cancel.onClick.AddListener(CloseWorkoutModal);
        submit.onClick.AddListener(SubmitWorkout);
        Button sheetBlocker = sheet.gameObject.AddComponent<Button>();
        sheetBlocker.transition = Selectable.Transition.None;
    }

    private void SubmitProfile()
    {
        ReadProfileFromForm();
        if (!CanUpdateProfile())
        {
            Toast(T("ProfileCooldown") + GetCooldownText());
            Render();
            return;
        }

        state.profile.lastUpdatedTicks = DateTime.Now.Ticks;
        RecalculateProfile(true);
        GenerateTasks();
        Log(T("RebuildFoundation"), T("BaseSpirit") + " " + state.baseSpirit + ", " + T("EstimateUpdated"));
        SaveState();
        Render();
        Toast(T("FoundationSaved"));
    }

    private void SubmitWorkout()
    {
        int activityIndex = Mathf.Clamp(activityOption.Value, 0, activities.Length - 1);
        int duration = Mathf.Clamp(ParseInt(durationInput.text, 20), 1, 240);
        ActivityInfo activity = activities[activityIndex];
        float distanceKm = Mathf.Clamp(ParseFloat(distanceInput.text, 0f), 0f, 200f);
        WorkoutEstimate estimate = EstimateWorkout(activity, duration, distanceKm);
        int workoutGain = CalculateWorkoutCultivation(activity, duration, estimate.calories, estimate.cultivationBonus);
        int growth = Mathf.Max(1, Mathf.RoundToInt(duration / 18f));

        Grant(workoutGain, activity.label + T("CalorieCultivation"));
        state.realm.stability = Mathf.RoundToInt(Mathf.Clamp(state.realm.stability + activity.stability - estimate.stabilityCost, 0, 100));
        AddAttribute(activity.attributeIndex, growth);

        List<DailyTask> completed = ApplyTaskProgress(activityIndex, duration);
        int taskGain = 0;
        for (int i = 0; i < completed.Count; i++)
        {
            taskGain += completed[i].reward;
        }

        if (taskGain > 0)
        {
            Grant(taskGain, T("DailyTask"));
            state.realm.stability = Mathf.RoundToInt(Mathf.Clamp(state.realm.stability + completed.Count * 3, 0, 100));
        }

        state.lastWorkoutCalories = estimate.calories;
        state.lastWorkoutCultivation = workoutGain;
        Log(activity.label + " " + duration + T("Minutes"), T("IntensityDerived") + estimate.label + ", " + T("Burned") + " " + estimate.calories + " kcal, " + T("GainVerb") + " " + (workoutGain + taskGain) + " " + T("Cultivation") + ", " + AttributeName(activity.attributeIndex) + " +" + growth);
        SaveState();
        Render();
        CloseWorkoutModal();
        Toast(completed.Count > 0 ? T("WorkoutTaskDone") : T("WorkoutDone"));
    }

    private void AttemptBreakthrough()
    {
        RealmInfo current = realms[Mathf.Clamp(state.realm.index, 0, realms.Length - 1)];
        if (state.realm.index >= realms.Length - 1)
        {
            Toast(T("MaxRealm"));
            return;
        }

        if (state.realm.progress < current.required || state.realm.stability < 60)
        {
            Toast(T("NotReady"));
            return;
        }

        state.realm.progress -= current.required;
        state.realm.index++;
        state.realm.stability = Mathf.RoundToInt(Mathf.Clamp(state.realm.stability - 10, 35, 100));
        state.attributes.mind = Mathf.Clamp(state.attributes.mind + 3, 0, 100);
        RealmInfo next = realms[state.realm.index];
        Log(T("BreakthroughSuccess"), T("RealmUp") + " " + next.name + next.layer);
        SaveState();
        Render();
        Toast(T("BreakthroughToast"));
    }

    private void Render()
    {
        RealmInfo current = realms[Mathf.Clamp(state.realm.index, 0, realms.Length - 1)];
        float progress = Mathf.Clamp01(state.realm.progress / (float)current.required);
        for (int i = 0; i < realmNameTexts.Count; i++) realmNameTexts[i].text = current.name;
        for (int i = 0; i < realmLayerTexts.Count; i++)
        {
            realmLayerTexts[i].text = realmLayerTexts[i].fontSize >= 20 && i == 1 ? current.name + " · " + current.layer : current.layer;
        }
        for (int i = 0; i < realmProgressTexts.Count; i++) realmProgressTexts[i].text = state.realm.progress + " / " + current.required + " " + T("Cultivation");
        for (int i = 0; i < realmBars.Count; i++) realmBars[i].fillAmount = progress;
        for (int i = 0; i < baseSpiritTexts.Count; i++) baseSpiritTexts[i].text = T("BaseSpirit") + " " + state.baseSpirit;
        for (int i = 0; i < stabilityTexts.Count; i++) stabilityTexts[i].text = T("Stability") + " " + state.realm.stability;

        RenderAttribute(0, state.attributes.physique);
        RenderAttribute(1, state.attributes.endurance);
        RenderAttribute(2, state.attributes.recovery);
        RenderAttribute(3, state.attributes.mind);
        RenderProfileCooldown();
        RenderTasks(taskList, state.tasks.Count);
        RenderTasks(homeTaskList, Mathf.Min(2, state.tasks.Count));
        RenderLogs();
        RenderBreakthrough();
        RenderTrainingStats();
    }

    private void RenderTrainingStats()
    {
        if (trainingStatsText == null) return;
        int completed = 0;
        int potential = 0;
        for (int i = 0; i < state.tasks.Count; i++)
        {
            potential += state.tasks[i].reward;
            if (state.tasks[i].claimed) completed++;
        }

        trainingStatsText.text =
            T("CurrentCultivation") + " " + state.realm.progress + "\n" +
            T("BaseSpirit") + " " + state.baseSpirit + "\n" +
            T("LastWorkout") + " " + state.lastWorkoutCalories + " kcal / +" + state.lastWorkoutCultivation + " " + T("Cultivation") + "\n" +
            T("TodayTasksDone") + " " + completed + " / " + state.tasks.Count + "\n" +
            T("TaskPotential") + " +" + potential + " " + T("Cultivation");
    }

    private void RenderProfileCooldown()
    {
        bool locked = !CanUpdateProfile();
        profileButton.interactable = !locked;
        SetButtonText(profileButton, locked ? T("Cooling") : T("UpdateFoundation"));
        heightInput.interactable = !locked;
        weightInput.interactable = !locked;
        ageInput.interactable = !locked;
        formulaOption.SetInteractable(!locked);
        profileNote.text = locked ? T("ProfileCooldown") + GetCooldownText() : T("BodyProtectNote");
    }

    private void RenderTasks(RectTransform list, int limit)
    {
        if (list == null) return;
        Clear(list);
        for (int i = 0; i < limit; i++)
        {
            DailyTask task = state.tasks[i];
            Color itemColor = task.claimed ? new Color(0.88f, 0.96f, 0.90f, 0.96f) : new Color(1f, 0.98f, 0.91f, 0.97f);
            Image item = Image("Task", list, itemColor);
            item.sprite = RoundedSprite(10);
            AddInnerStroke(item.rectTransform, task.claimed ? new Color(0.33f, 0.78f, 0.58f, 0.28f) : new Color(0.92f, 0.68f, 0.30f, 0.20f), 2f);
            EnsureLayoutElement(item.gameObject).preferredHeight = 78;
            VerticalLayoutGroup itemLayout = item.gameObject.AddComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(12, 12, 8, 14);
            itemLayout.spacing = 2;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;
            TextLine(item.rectTransform, task.title, 16, ink, FontStyle.Bold, 24, TextAnchor.MiddleLeft);
            Badge(item.rectTransform, "+" + task.reward + " " + T("Cultivation"), 82, 28);
            string meta = activities[task.activityIndex].label + " " + task.target + T("Minutes") + "    " + task.progress + " / " + task.target;
            Text metaText = TextLine(item.rectTransform, meta, 12, muted, FontStyle.Normal, 20, TextAnchor.MiddleLeft);
            Image track = Image("Track", item.rectTransform, Rgb(219, 209, 188));
            track.sprite = RoundedSprite(4);
            AllowLayout(track.gameObject);
            track.transform.SetAsLastSibling();
            AnchorBottom(track.rectTransform, 0, 0, 8);
            Image bar = Image("Bar", track.rectTransform, task.claimed ? gold : jade);
            bar.sprite = RoundedSprite(4);
            AddGlow(bar.rectTransform, task.claimed ? new Color(1f, 0.75f, 0.26f, 0.32f) : new Color(0.38f, 1f, 0.78f, 0.28f), 2f);
            Stretch(bar.rectTransform, 0, 0, 0, 0);
            bar.type = UnityEngine.UI.Image.Type.Filled;
            bar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            bar.fillAmount = Mathf.Clamp01(task.progress / (float)task.target);
        }
    }

    private void RenderLogs()
    {
        if (logList == null) return;
        Clear(logList);
        if (state.logs.Count == 0)
        {
            AddLogItem(T("NoLog"), T("NoLogHint"));
            return;
        }

        int count = Mathf.Min(12, state.logs.Count);
        for (int i = 0; i < count; i++)
        {
            AddLogItem(state.logs[i].title, state.logs[i].body);
        }
    }

    private void RenderBreakthrough()
    {
        RealmInfo current = realms[Mathf.Clamp(state.realm.index, 0, realms.Length - 1)];
        bool ready = state.realm.progress >= current.required && state.realm.stability >= 60;
        breakthroughButton.interactable = ready;
        breakthroughHint.text = ready ? T("ReadyBreakthrough") : T("NeedPrefix") + current.required + T("NeedSuffix");
    }

    private void SwitchTab(int tab, bool save)
    {
        state.activeTab = Mathf.Clamp(tab, 0, 4);
        foreach (KeyValuePair<int, GameObject> pair in screens) pair.Value.SetActive(pair.Key == state.activeTab);
        foreach (KeyValuePair<int, Button> pair in navButtons) SetButtonColors(pair.Value, pair.Key == state.activeTab ? jade : Color.clear, pair.Key == state.activeTab ? Color.white : muted);
        if (save) SaveState();
    }

    private void RecalculateProfile(bool preserve)
    {
        float male = 10f * state.profile.weightKg + 6.25f * state.profile.heightCm - 5f * state.profile.ageYears + 5f;
        float female = 10f * state.profile.weightKg + 6.25f * state.profile.heightCm - 5f * state.profile.ageYears - 161f;
        float bmr = state.profile.formulaMode == 1 ? male : state.profile.formulaMode == 2 ? female : (male + female) * 0.5f;
        float heightM = state.profile.heightCm / 100f;
        float bmi = state.profile.weightKg / (heightM * heightM);
        float ageEase = Mathf.Clamp(100f - Mathf.Max(0, state.profile.ageYears - 25) * 0.7f, 72f, 104f);
        float balance = Mathf.Clamp(100f - Mathf.Abs(bmi - 22f) * 5f, 52f, 100f);
        float bmrScore = Mathf.Clamp((bmr - 1150f) / 12f, 35f, 80f);

        AttributeState next = new AttributeState();
        next.physique = Mathf.RoundToInt(Mathf.Clamp(38f + balance * 0.24f + state.profile.weightKg * 0.08f, 35f, 76f));
        next.endurance = Mathf.RoundToInt(Mathf.Clamp(42f + balance * 0.2f + ageEase * 0.12f, 35f, 76f));
        next.recovery = Mathf.RoundToInt(Mathf.Clamp(36f + bmrScore * 0.28f + ageEase * 0.18f, 35f, 76f));
        next.mind = preserve ? state.attributes.mind : 52;

        if (preserve)
        {
            next.physique = Mathf.Max(next.physique, state.attributes.physique);
            next.endurance = Mathf.Max(next.endurance, state.attributes.endurance);
            next.recovery = Mathf.Max(next.recovery, state.attributes.recovery);
            next.mind = Mathf.Max(next.mind, state.attributes.mind);
        }

        state.estimatedBmr = Mathf.RoundToInt(bmr);
        state.baseSpirit = Mathf.RoundToInt(Mathf.Clamp(bmr / 100f, 8f, 26f));
        state.attributes = next;
    }

    private WorkoutEstimate EstimateWorkout(ActivityInfo activity, int durationMinutes, float distanceKm)
    {
        float met = activity.baseMet;
        string label = T("AutoIntensityNormal");
        int bonus = 1;
        int stabilityCost = 0;

        bool distanceBased = activity.id == "walk" || activity.id == "run";
        if (distanceBased && distanceKm > 0f && durationMinutes > 0)
        {
            float speed = distanceKm / (durationMinutes / 60f);
            if (activity.id == "walk")
            {
                if (speed < 4.2f) { met = 2.8f; label = T("AutoIntensityEasy"); bonus = 0; }
                else if (speed < 6.2f) { met = 3.8f; label = T("AutoIntensityNormal"); bonus = 1; }
                else { met = 5.3f; label = T("AutoIntensityHard"); bonus = 3; stabilityCost = 1; }
            }
            else
            {
                if (speed < 7.2f) { met = 6.0f; label = T("AutoIntensityEasy"); bonus = 2; }
                else if (speed < 10.2f) { met = 8.8f; label = T("AutoIntensityNormal"); bonus = 4; stabilityCost = 2; }
                else { met = 11.5f; label = T("AutoIntensityHard"); bonus = 7; stabilityCost = 5; }
            }

            label = label + " " + speed.ToString("0.0") + " km/h";
        }
        else
        {
            bonus = activity.defaultBonus;
            stabilityCost = activity.defaultStabilityCost;
        }

        float calories = met * state.profile.weightKg * durationMinutes / 60f;
        return new WorkoutEstimate(Mathf.Max(1, Mathf.RoundToInt(calories)), bonus, stabilityCost, label);
    }

    private int CalculateWorkoutCultivation(ActivityInfo activity, int durationMinutes, int calories, int bonusCultivation)
    {
        float foundation = Mathf.Clamp(state.baseSpirit / 16f, 0.75f, 1.35f);
        float attribute = ActivityAttributeScore(activity.attributeIndex) / 70f;
        float durationBonus = Mathf.Clamp(durationMinutes / 45f, 0.45f, 1.4f);
        float raw = calories / 9f * foundation * activity.cultivationMultiplier * Mathf.Lerp(0.9f, 1.12f, Mathf.Clamp01(attribute)) + bonusCultivation + durationBonus;
        return Mathf.RoundToInt(Mathf.Clamp(raw, 2f, 160f));
    }

    private int ActivityAttributeScore(int index)
    {
        if (index == 0) return state.attributes.physique;
        if (index == 1) return state.attributes.endurance;
        if (index == 2) return state.attributes.recovery;
        return state.attributes.mind;
    }

    private void GenerateTasks()
    {
        List<TaskTemplate> pool = new List<TaskTemplate>(taskTemplates);
        List<TaskTemplate> selected = new List<TaskTemplate>();
        if (state.realm.stability < 65) selected.Add(taskTemplates[3]);
        while (selected.Count < 3 && pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            TaskTemplate item = pool[index];
            pool.RemoveAt(index);
            bool exists = false;
            for (int i = 0; i < selected.Count; i++) if (selected[i].id == item.id) exists = true;
            if (!exists) selected.Add(item);
        }

        state.tasks = new List<DailyTask>();
        for (int i = 0; i < selected.Count; i++)
        {
            TaskTemplate t = selected[i];
            state.tasks.Add(new DailyTask { id = t.id, title = t.title, activityIndex = t.activityIndex, target = t.target, reward = t.reward });
        }
    }

    private List<DailyTask> ApplyTaskProgress(int activityIndex, int duration)
    {
        List<DailyTask> completed = new List<DailyTask>();
        for (int i = 0; i < state.tasks.Count; i++)
        {
            DailyTask task = state.tasks[i];
            if (task.activityIndex != activityIndex || task.claimed) continue;
            task.progress = Mathf.Clamp(task.progress + duration, 0, task.target);
            if (task.progress >= task.target && !task.complete)
            {
                task.complete = true;
                task.claimed = true;
                completed.Add(task);
            }
        }
        return completed;
    }

    private void Grant(int amount, string source)
    {
        if (amount <= 0) return;
        state.realm.progress += amount;
        gainAmount.text = "+" + amount;
        gainSource.text = source;
        StopCoroutine("GainRoutine");
        StartCoroutine("GainRoutine");
    }

    private IEnumerator GainRoutine()
    {
        gainGroup.alpha = 1f;
        yield return new WaitForSeconds(1.2f);
        gainGroup.alpha = 0f;
    }

    private void Toast(string text)
    {
        toastText.text = text;
        StopCoroutine("ToastRoutine");
        StartCoroutine("ToastRoutine");
    }

    private IEnumerator ToastRoutine()
    {
        toastGroup.alpha = 1f;
        yield return new WaitForSeconds(2.2f);
        toastGroup.alpha = 0f;
    }

    private void OpenWorkoutModal()
    {
        if (workoutModalGroup == null) return;
        workoutModalGroup.alpha = 1f;
        workoutModalGroup.interactable = true;
        workoutModalGroup.blocksRaycasts = true;
    }

    private void CloseWorkoutModal()
    {
        if (workoutModalGroup == null) return;
        workoutModalGroup.alpha = 0f;
        workoutModalGroup.interactable = false;
        workoutModalGroup.blocksRaycasts = false;
    }

    private void RefreshWorkoutInputHint()
    {
        if (intensityPreviewText == null || activityOption == null) return;
        int activityIndex = Mathf.Clamp(activityOption.Value, 0, activities.Length - 1);
        ActivityInfo activity = activities[activityIndex];
        bool distanceBased = activity.id == "walk" || activity.id == "run";
        if (distanceInput != null)
        {
            distanceInput.interactable = distanceBased;
            if (!distanceBased)
            {
                distanceInput.text = "0";
            }
            else if (ParseFloat(distanceInput.text, 0f) <= 0f)
            {
                distanceInput.text = activity.id == "run" ? "3.0" : "2.0";
            }
        }

        intensityPreviewText.text = distanceBased ? T("DistanceHint") : T("NoDistanceHint");
    }

    private bool CanUpdateProfile()
    {
        if (state.profile.lastUpdatedTicks <= 0) return true;
        return DateTime.Now.Ticks - state.profile.lastUpdatedTicks >= ProfileCooldownTicks;
    }

    private string GetCooldownText()
    {
        if (state.profile.lastUpdatedTicks <= 0) return T("CanUpdateNow");
        DateTime next = new DateTime(state.profile.lastUpdatedTicks).AddTicks(ProfileCooldownTicks);
        return next.ToString("yyyy-MM-dd HH:mm");
    }

    private void ApplyProfileToForm()
    {
        heightInput.text = state.profile.heightCm.ToString("0");
        weightInput.text = state.profile.weightKg.ToString("0.#");
        ageInput.text = state.profile.ageYears.ToString();
        formulaOption.Value = Mathf.Clamp(state.profile.formulaMode, 0, 2);
    }

    private void ReadProfileFromForm()
    {
        state.profile.heightCm = Mathf.Clamp(ParseFloat(heightInput.text, 170f), 100f, 230f);
        state.profile.weightKg = Mathf.Clamp(ParseFloat(weightInput.text, 65f), 30f, 220f);
        state.profile.ageYears = Mathf.Clamp(ParseInt(ageInput.text, 25), 12, 90);
        state.profile.formulaMode = formulaOption.Value;
    }

    private AppState LoadState()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return new AppState();
        try
        {
            AppState loaded = JsonUtility.FromJson<AppState>(PlayerPrefs.GetString(SaveKey));
            return loaded == null ? new AppState() : loaded;
        }
        catch
        {
            return new AppState();
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }

    private void Log(string title, string body)
    {
        state.logs.Insert(0, new LogEntry { title = title, body = body });
        if (state.logs.Count > 30) state.logs.RemoveRange(30, state.logs.Count - 30);
    }

    private void AddLogItem(string title, string body)
    {
        Image item = Image("Log", logList, new Color(1f, 0.98f, 0.91f, 0.94f));
        item.sprite = RoundedSprite(10);
        AddInnerStroke(item.rectTransform, new Color(0.86f, 0.65f, 0.32f, 0.18f), 1.5f);
        EnsureLayoutElement(item.gameObject).preferredHeight = 72;
        VerticalLayoutGroup itemLayout = item.gameObject.AddComponent<VerticalLayoutGroup>();
        itemLayout.padding = new RectOffset(12, 12, 8, 8);
        itemLayout.spacing = 2;
        itemLayout.childControlWidth = true;
        itemLayout.childControlHeight = true;
        itemLayout.childForceExpandWidth = true;
        itemLayout.childForceExpandHeight = false;
        TextLine(item.rectTransform, title, 15, ink, FontStyle.Bold, 24, TextAnchor.MiddleLeft);
        TextLine(item.rectTransform, body, 12, muted, FontStyle.Normal, 32, TextAnchor.UpperLeft);
    }

    private void AddNav(RectTransform parent, int tab, string label)
    {
        Button button = Button(parent, label, Color.clear, muted, 0, 48);
        LayoutElement element = EnsureLayoutElement(button.gameObject);
        element.flexibleWidth = 1;
        element.preferredHeight = 50;
        int captured = tab;
        button.onClick.AddListener(delegate { SwitchTab(captured, true); });
        navButtons[tab] = button;
    }

    private void AddStat(RectTransform parent, int index, string label)
    {
        Image rowBg = Image("Stat" + index, parent, new Color(1f, 0.98f, 0.91f, 0.56f));
        rowBg.sprite = RoundedSprite(8);
        RectTransform row = rowBg.rectTransform;
        EnsureLayoutElement(row.gameObject).preferredHeight = 34;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        Text labelText = TextLine(row, label, 14, muted, FontStyle.Bold, 34, TextAnchor.MiddleLeft);
        EnsureLayoutElement(labelText.gameObject).preferredWidth = 52;
        Text value = TextLine(row, "50", 14, ink, FontStyle.Bold, 34, TextAnchor.MiddleCenter);
        EnsureLayoutElement(value.gameObject).preferredWidth = 38;
        Image track = Image("Track", row, Rgb(219, 209, 188));
        track.sprite = RoundedSprite(6);
        AllowLayout(track.gameObject);
        LayoutElement trackElement = EnsureLayoutElement(track.gameObject);
        trackElement.flexibleWidth = 1;
        trackElement.minHeight = 26;
        trackElement.preferredHeight = 26;
        Image bar = Image("Bar", track.rectTransform, AttributeColor(index));
        bar.sprite = RoundedSprite(6);
        AddGlow(bar.rectTransform, AttributeColor(index), 2f);
        Stretch(bar.rectTransform, 0, 0, 0, 0);
        bar.type = UnityEngine.UI.Image.Type.Filled;
        bar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

        if (index == 0) { physiqueValue = value; physiqueBar = bar; }
        if (index == 1) { enduranceValue = value; enduranceBar = bar; }
        if (index == 2) { recoveryValue = value; recoveryBar = bar; }
        if (index == 3) { mindValue = value; mindBar = bar; }
    }

    private void RenderAttribute(int index, int value)
    {
        Text valueText = index == 0 ? physiqueValue : index == 1 ? enduranceValue : index == 2 ? recoveryValue : mindValue;
        Image bar = index == 0 ? physiqueBar : index == 1 ? enduranceBar : index == 2 ? recoveryBar : mindBar;
        valueText.text = value.ToString();
        bar.fillAmount = Mathf.Clamp01(value / 100f);
        bar.color = AttributeColor(index);
    }

    private InputField LabeledInput(RectTransform parent, string label, string defaultValue)
    {
        RectTransform row = Rect(label, parent);
        EnsureLayoutElement(row.gameObject).preferredHeight = 44;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        Text labelText = TextLine(row, label, 13, muted, FontStyle.Bold, 44, TextAnchor.MiddleLeft);
        EnsureLayoutElement(labelText.gameObject).preferredWidth = 86;
        Image box = Image("Input", row, new Color(1f, 0.985f, 0.93f, 0.95f));
        box.sprite = RoundedSprite(8);
        AddInnerStroke(box.rectTransform, new Color(0.80f, 0.62f, 0.36f, 0.22f), 1.5f);
        LayoutElement boxElement = EnsureLayoutElement(box.gameObject);
        boxElement.flexibleWidth = 1;
        boxElement.preferredHeight = 44;
        InputField input = box.gameObject.AddComponent<InputField>();
        Text text = TextLine(box.rectTransform, defaultValue, 14, ink, FontStyle.Normal, 40, TextAnchor.MiddleLeft);
        Stretch(text.rectTransform, 10, 0, 10, 0);
        input.textComponent = text;
        input.text = defaultValue;
        return input;
    }

    private CycleOption LabeledOption(RectTransform parent, string label, string[] options)
    {
        RectTransform row = Rect(label, parent);
        EnsureLayoutElement(row.gameObject).preferredHeight = 44;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        Text labelText = TextLine(row, label, 13, muted, FontStyle.Bold, 44, TextAnchor.MiddleLeft);
        EnsureLayoutElement(labelText.gameObject).preferredWidth = 86;
        Button button = Button(row, options[0], new Color(1f, 0.985f, 0.93f, 0.95f), ink, 0, 44);
        EnsureLayoutElement(button.gameObject).flexibleWidth = 1;
        return new CycleOption(button, options);
    }

    private SimpleDropdown LabeledDropdown(RectTransform parent, string label, string[] options)
    {
        RectTransform row = Rect(label, parent);
        EnsureLayoutElement(row.gameObject).preferredHeight = 44;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        Text labelText = TextLine(row, label, 13, muted, FontStyle.Bold, 44, TextAnchor.MiddleLeft);
        EnsureLayoutElement(labelText.gameObject).preferredWidth = 86;

        RectTransform box = Rect("DropdownBox", row);
        LayoutElement boxElement = EnsureLayoutElement(box.gameObject);
        boxElement.flexibleWidth = 1;
        boxElement.preferredHeight = 44;
        return new SimpleDropdown(this, box, parent, options);
    }

    private RectTransform VerticalList(string name, RectTransform parent, float top)
    {
        RectTransform list = Rect(name, parent);
        LayoutElement element = EnsureLayoutElement(list.gameObject);
        element.minHeight = 80;
        element.preferredHeight = 160;
        element.flexibleHeight = 1;
        VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return list;
    }

    private RectTransform Panel(Transform parent)
    {
        Image image = Image("Panel", parent, panel);
        image.sprite = RoundedSprite(10);
        AddInnerStroke(image.rectTransform, new Color(0.82f, 0.62f, 0.34f, 0.22f), 1.5f);
        AddGlow(image.rectTransform, new Color(0.05f, 0.10f, 0.08f, 0.12f), 8f);
        VerticalLayoutGroup layout = image.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return image.rectTransform;
    }

    private Text Badge(Transform parent, string text, float width, float height)
    {
        Image badge = Image("Badge", parent, new Color(0.90f, 0.98f, 0.92f, 0.96f));
        badge.sprite = RoundedSprite(8);
        AddInnerStroke(badge.rectTransform, new Color(0.29f, 0.67f, 0.50f, 0.20f), 1f);
        AnchorTopRight(badge.rectTransform, 0, 0, width, height);
        Text label = TextLine(badge.rectTransform, text, 12, jadeDark, FontStyle.Bold, height, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform, 0, 0, 0, 0);
        return label;
    }

    private Button Button(Transform parent, string label, Color background, Color foreground, float width, float height)
    {
        Image image = Image("Button", parent, background);
        image.sprite = RoundedSprite(8);
        if (background.a > 0.01f)
        {
            AddInnerStroke(image.rectTransform, new Color(1f, 1f, 1f, 0.16f), 1.2f);
        }
        if (width > 0 || height > 0) image.rectTransform.sizeDelta = new Vector2(width, height);
        LayoutElement element = EnsureLayoutElement(image.gameObject);
        if (width > 0) element.preferredWidth = width;
        if (height > 0)
        {
            element.minHeight = height;
            element.preferredHeight = height;
        }
        Button button = image.gameObject.AddComponent<Button>();
        SetButtonColors(button, background, foreground);
        Text text = TextLine(image.rectTransform, label, 14, foreground, FontStyle.Bold, height > 0 ? height : 42, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 4, 0, 4, 0);
        return button;
    }

    private void SetButtonText(Button button, string value)
    {
        button.GetComponentInChildren<Text>().text = value;
    }

    private void SetButtonColors(Button button, Color background, Color foreground)
    {
        Image image = button.GetComponent<Image>();
        image.color = background;
        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = background == Color.clear ? new Color(0.80f, 0.95f, 0.87f, 0.32f) : Brighten(background, 1.08f);
        colors.pressedColor = background == Color.clear ? new Color(0.70f, 0.88f, 0.78f, 0.42f) : Brighten(background, 0.9f);
        colors.disabledColor = Rgb(152, 170, 162);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        Text text = button.GetComponentInChildren<Text>();
        if (text != null) text.color = foreground;
    }

    private Image Image(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = color;
        return image;
    }

    private static LayoutElement EnsureLayoutElement(GameObject obj)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>();
        return element != null ? element : obj.AddComponent<LayoutElement>();
    }

    private Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    private Sprite SoftCircleSprite()
    {
        if (softCircleSprite != null) return softCircleSprite;
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        softCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return softCircleSprite;
    }

    private Sprite RoundedSprite(int radius)
    {
        radius = Mathf.Clamp(radius, 2, 32);
        Sprite sprite;
        if (roundedSprites.TryGetValue(radius, out sprite)) return sprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float corner = radius / 32f * (size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x + 0.5f - size * 0.5f);
                float py = Mathf.Abs(y + 0.5f - size * 0.5f);
                float qx = Mathf.Max(px - (size * 0.5f - corner), 0f);
                float qy = Mathf.Max(py - (size * 0.5f - corner), 0f);
                float distance = Mathf.Sqrt(qx * qx + qy * qy);
                float alpha = Mathf.Clamp01(corner + 0.5f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 1, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        roundedSprites[radius] = sprite;
        return sprite;
    }

    private RectTransform Rect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private RectTransform Circle(string name, Transform parent, Color color, float size)
    {
        Image image = Image(name, parent, color);
        image.sprite = SoftCircleSprite();
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image.rectTransform;
    }

    private void AddTextShadow(Text text, Color color, Vector2 distance)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private void AddGlow(RectTransform target, Color color, float padding)
    {
        Outline outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(color.r, color.g, color.b, Mathf.Min(color.a, 0.28f));
        outline.effectDistance = new Vector2(Mathf.Clamp(padding, 1f, 3f), -Mathf.Clamp(padding, 1f, 3f));
        outline.useGraphicAlpha = true;
    }

    private void AddInnerStroke(RectTransform target, Color color, float thickness)
    {
        Outline outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        float size = Mathf.Clamp(thickness, 1f, 2f);
        outline.effectDistance = new Vector2(size, -size);
        outline.useGraphicAlpha = true;
    }

    private void AddPanelAccent(RectTransform panelRoot, Color color, float alpha)
    {
        Image strip = Image("PanelAccent", panelRoot, new Color(color.r, color.g, color.b, alpha));
        IgnoreLayout(strip.gameObject);
        strip.sprite = RoundedSprite(10);
        AnchorTop(strip.rectTransform, 16, 0, 5);
        strip.raycastTarget = false;
    }

    private Color AttributeColor(int index)
    {
        if (index == 0) return cinnabar;
        if (index == 1) return sky;
        if (index == 2) return jade;
        return gold;
    }

    private Text TextLine(Transform parent, string value, int size, Color color, FontStyle style, float height, TextAnchor anchor)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(0, height);
        LayoutElement element = EnsureLayoutElement(obj);
        element.minHeight = height;
        element.preferredHeight = height;
        return text;
    }

    private void Clear(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
    }

    private int ParseInt(string value, int fallback)
    {
        int result;
        return int.TryParse(value, out result) ? result : fallback;
    }

    private float ParseFloat(string value, float fallback)
    {
        float result;
        return float.TryParse(value, out result) ? result : fallback;
    }

    private void AddAttribute(int index, int amount)
    {
        if (index == 0) state.attributes.physique = Mathf.Clamp(state.attributes.physique + amount, 0, 100);
        if (index == 1) state.attributes.endurance = Mathf.Clamp(state.attributes.endurance + amount, 0, 100);
        if (index == 2) state.attributes.recovery = Mathf.Clamp(state.attributes.recovery + amount, 0, 100);
        if (index == 3 || index == 4) state.attributes.mind = Mathf.Clamp(state.attributes.mind + amount, 0, 100);
    }

    private string AttributeName(int index)
    {
        if (index == 0) return T("Physique");
        if (index == 1) return T("Endurance");
        if (index == 2) return T("Recovery");
        return T("Mind");
    }

    private static Color Rgb(float r, float g, float b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private static Color Brighten(Color color, float multiplier)
    {
        return new Color(Mathf.Clamp01(color.r * multiplier), Mathf.Clamp01(color.g * multiplier), Mathf.Clamp01(color.b * multiplier), color.a);
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void AnchorBottom(RectTransform rect, float side, float bottom, float height)
    {
        IgnoreLayout(rect.gameObject);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.offsetMin = new Vector2(side, bottom);
        rect.offsetMax = new Vector2(-side, bottom + height);
    }

    private static void AnchorTop(RectTransform rect, float side, float top, float height)
    {
        IgnoreLayout(rect.gameObject);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(side, -top - height);
        rect.offsetMax = new Vector2(-side, -top);
    }

    private static void AnchorBelow(RectTransform rect, float left, float top, float right, float height)
    {
        IgnoreLayout(rect.gameObject);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void AnchorTopRight(RectTransform rect, float right, float top, float width, float height)
    {
        IgnoreLayout(rect.gameObject);
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void IgnoreLayout(GameObject obj)
    {
        LayoutElement element = EnsureLayoutElement(obj);
        element.ignoreLayout = true;
    }

    private static void AllowLayout(GameObject obj)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>();
        if (element != null) element.ignoreLayout = false;
    }

    private static string T(string key)
    {
        switch (key)
        {
            case "AppTitle": return "\u4fee\u4ed9\u5065\u8eab\u5f55";
            case "DemoSubtitle": return "\u5065\u5eb7\u8bb0\u5f55\u4fee\u4ed9 Demo";
            case "Today": return "\u4eca\u65e5\u603b\u89c8";
            case "HomeTitle": return "\u628a\u4eca\u5929\u7684\u8fd0\u52a8\uff0c\u70bc\u6210\u4eca\u5929\u7684\u4fee\u4e3a";
            case "HomeCopy": return "\u5148\u770b\u5883\u754c\u72b6\u6001\uff0c\u518d\u5b8c\u6210\u6bcf\u65e5\u4fee\u70bc\u3002";
            case "Spirit": return "\u7075\u529b";
            case "BaseSpirit": return "\u57fa\u7840\u7075\u529b";
            case "BaseSpiritHint": return "\u7531\u8eab\u4f53\u6839\u57fa\u4f30\u7b97\uff0c\u6253\u5361\u65f6\u5e26\u6765\u57fa\u7840\u4fee\u4e3a\u6536\u76ca\u3002";
            case "Stability": return "\u7a33\u5b9a\u5ea6";
            case "StabilityHint": return "\u6062\u590d\u7c7b\u4fee\u70bc\u80fd\u63d0\u9ad8\u7a81\u7834\u7a33\u5b9a\u5ea6\u3002";
            case "TodayGoal": return "\u4eca\u65e5\u76ee\u6807";
            case "DailyPractice": return "\u6bcf\u65e5\u4fee\u70bc";
            case "Foundation": return "\u6839\u57fa";
            case "BodyFoundation": return "\u8eab\u4f53\u6839\u57fa";
            case "Height": return "\u8eab\u9ad8 cm";
            case "Weight": return "\u4f53\u91cd kg";
            case "Age": return "\u5e74\u9f84";
            case "Formula": return "\u4f30\u7b97\u6a21\u5f0f";
            case "NeutralFormula": return "\u4e2d\u6027\u4f30\u7b97";
            case "MaleFormula": return "\u7537\u6027\u516c\u5f0f";
            case "FemaleFormula": return "\u5973\u6027\u516c\u5f0f";
            case "CreateFoundation": return "\u751f\u6210\u4fee\u70bc\u6839\u57fa";
            case "UpdateFoundation": return "\u66f4\u65b0\u4fee\u70bc\u6839\u57fa";
            case "Cooling": return "\u6839\u57fa\u8c03\u606f\u4e2d";
            case "BodyProtectNote": return "\u8eab\u4f53\u4fe1\u606f\u53ea\u7528\u4e8e\u4f30\u7b97\u521d\u59cb\u72b6\u6001\u548c\u63a8\u8350\u65b9\u5411\uff0c\u4e0d\u51b3\u5b9a\u6700\u7ec8\u4e0a\u9650\u3002";
            case "Attribute": return "\u5c5e\u6027";
            case "AttributePanel": return "\u7075\u53f0\u5c5e\u6027";
            case "Physique": return "\u4f53\u9b44";
            case "Endurance": return "\u8010\u529b";
            case "Recovery": return "\u6062\u590d";
            case "Mind": return "\u5fc3\u5883";
            case "Training": return "\u4fee\u70bc";
            case "DailyTask": return "\u6bcf\u65e5\u4efb\u52a1";
            case "RefreshTasks": return "\u6362\u4e00\u7ec4";
            case "CheckIn": return "\u6253\u5361";
            case "WorkoutRecord": return "\u8fd0\u52a8\u8bb0\u5f55";
            case "WorkoutStats": return "\u4fee\u4e3a\u589e\u957f\u7edf\u8ba1";
            case "OpenCheckIn": return "\u65b0\u589e\u6253\u5361";
            case "WorkoutType": return "\u8fd0\u52a8\u7c7b\u578b";
            case "Walk": return "\u6b65\u884c";
            case "Run": return "\u8dd1\u6b65";
            case "Strength": return "\u529b\u91cf\u8bad\u7ec3";
            case "Stretch": return "\u62c9\u4f38\u6062\u590d";
            case "Meditation": return "\u9759\u5750\u8c03\u606f";
            case "Duration": return "\u65f6\u957f\u5206\u949f";
            case "DistanceKm": return "\u8ddd\u79bb km";
            case "DistanceHint": return "\u6b65\u884c/\u8dd1\u6b65\u4f1a\u6839\u636e\u8ddd\u79bb\u548c\u65f6\u95f4\u81ea\u52a8\u63a8\u5bfc\u901f\u5ea6\u3001\u5f3a\u5ea6\u4e0e\u5361\u8def\u91cc\u3002";
            case "NoDistanceHint": return "\u8be5\u8fd0\u52a8\u6309\u7c7b\u578b\u548c\u65f6\u957f\u4f30\u7b97\u6d88\u8017\uff0c\u8ddd\u79bb\u65e0\u9700\u586b\u5199\u3002";
            case "AutoIntensityEasy": return "\u8f7b\u677e";
            case "AutoIntensityNormal": return "\u9002\u4e2d";
            case "AutoIntensityHard": return "\u8f83\u9ad8";
            case "IntensityDerived": return "\u63a8\u5bfc\u5f3a\u5ea6 ";
            case "Intensity": return "\u5f3a\u5ea6";
            case "Low": return "\u4f4e";
            case "Mid": return "\u4e2d";
            case "High": return "\u9ad8";
            case "SubmitWorkout": return "\u5b8c\u6210\u6253\u5361";
            case "Cancel": return "\u53d6\u6d88";
            case "CurrentCultivation": return "\u5f53\u524d\u4fee\u4e3a";
            case "LastWorkout": return "\u6700\u8fd1\u6253\u5361";
            case "TodayTasksDone": return "\u4eca\u65e5\u4efb\u52a1\u5b8c\u6210";
            case "TaskPotential": return "\u4efb\u52a1\u6f5c\u5728\u6536\u76ca";
            case "CalorieCultivation": return "\u5361\u8def\u91cc\u4fee\u70bc";
            case "Burned": return "\u6d88\u8017";
            case "Realm": return "\u5883\u754c";
            case "RealmTitle": return "\u51e1\u4f53 \u00b7 \u672a\u5165\u9053";
            case "BreakthroughHint": return "\u79ef\u7d2f\u4fee\u4e3a\u4e0e\u7a33\u5b9a\u5ea6\u540e\uff0c\u53ef\u5c1d\u8bd5\u7a81\u7834\u3002";
            case "TryBreakthrough": return "\u5c1d\u8bd5\u7a81\u7834";
            case "Records": return "\u8bb0\u5f55";
            case "LogTitle": return "\u4fee\u70bc\u65e5\u5fd7";
            case "FanTi": return "\u51e1\u4f53";
            case "WeiRuDao": return "\u672a\u5165\u9053";
            case "LianQi": return "\u70bc\u6c14";
            case "YiCeng": return "\u4e00\u5c42";
            case "ErCeng": return "\u4e8c\u5c42";
            case "SanCeng": return "\u4e09\u5c42";
            case "SiCeng": return "\u56db\u5c42";
            case "WuCeng": return "\u4e94\u5c42";
            case "LiuCeng": return "\u516d\u5c42";
            case "QiCeng": return "\u4e03\u5c42";
            case "BaCeng": return "\u516b\u5c42";
            case "JiuCeng": return "\u4e5d\u5c42";
            case "Cultivation": return "\u4fee\u4e3a";
            case "NavHome": return "\u2302\n\u9996\u9875";
            case "NavFoundation": return "\u25c7\n\u6839\u57fa";
            case "NavTraining": return "\u2726\n\u4fee\u70bc";
            case "NavRealm": return "\u25b3\n\u5883\u754c";
            case "NavRecords": return "\u2630\n\u8bb0\u5f55";
            case "GainTitle": return "\u4fee\u4e3a\u589e\u957f";
            case "DonePractice": return "\u5b8c\u6210\u4fee\u70bc";
            case "TaskWalk": return "\u6668\u884c\u91c7\u6c14";
            case "TaskRun": return "\u5fa1\u98ce\u5c0f\u8dd1";
            case "TaskStrength": return "\u94c1\u9aa8\u953b\u4f53";
            case "TaskStretch": return "\u6d17\u9ad3\u8212\u7b4b";
            case "TaskMeditation": return "\u9759\u5750\u8c03\u606f";
            case "Minutes": return "\u5206\u949f";
            case "ProfileCooldown": return "\u8eab\u4f53\u6839\u57fa\u6bcf 7 \u5929\u53ef\u66f4\u65b0\u4e00\u6b21\uff0c";
            case "CanUpdateNow": return "\u73b0\u5728\u53ef\u4ee5\u66f4\u65b0";
            case "RebuildFoundation": return "\u91cd\u5851\u6839\u57fa";
            case "EstimateUpdated": return "\u8d44\u8d28\u4f30\u7b97\u5df2\u66f4\u65b0";
            case "FoundationSaved": return "\u4fee\u70bc\u6839\u57fa\u5df2\u751f\u6210\u3002";
            case "TasksRefreshed": return "\u5df2\u6362\u4e00\u7ec4\u4eca\u65e5\u4fee\u70bc\u3002";
            case "GainVerb": return "\u83b7\u5f97";
            case "WorkoutTaskDone": return "\u6253\u5361\u5b8c\u6210\uff0c\u5e76\u5b8c\u6210\u4e86\u4eca\u65e5\u4fee\u70bc\u4efb\u52a1\u3002";
            case "WorkoutDone": return "\u6253\u5361\u5b8c\u6210\uff0c\u4fee\u4e3a\u5df2\u589e\u957f\u3002";
            case "MaxRealm": return "\u4f60\u5df2\u7ecf\u62b5\u8fbe\u5f53\u524d Demo \u7684\u6700\u9ad8\u5883\u754c\u3002";
            case "NotReady": return "\u4fee\u4e3a\u6216\u7a33\u5b9a\u5ea6\u8fd8\u4e0d\u591f\u3002";
            case "BreakthroughSuccess": return "\u7a81\u7834\u6210\u529f";
            case "RealmUp": return "\u5883\u754c\u63d0\u5347\u81f3";
            case "BreakthroughToast": return "\u7a81\u7834\u6210\u529f\uff0c\u65b0\u7684\u4fee\u70bc\u9636\u6bb5\u5df2\u5f00\u542f\u3002";
            case "ReadyBreakthrough": return "\u4fee\u4e3a\u4e0e\u7a33\u5b9a\u5ea6\u5df2\u8db3\uff0c\u9002\u5408\u5c1d\u8bd5\u7a81\u7834\u3002";
            case "NeedPrefix": return "\u9700\u8981 ";
            case "NeedSuffix": return " \u4fee\u4e3a\u4e0e 60 \u7a33\u5b9a\u5ea6\u3002";
            case "NoLog": return "\u5c1a\u672a\u5f00\u59cb";
            case "NoLogHint": return "\u5148\u751f\u6210\u8eab\u4f53\u6839\u57fa\uff0c\u518d\u5b8c\u6210\u4e00\u6b21\u8fd0\u52a8\u6253\u5361\u3002";
            case "ResetDone": return "Demo \u6570\u636e\u5df2\u91cd\u7f6e\u3002";
            default: return key;
        }
    }

    [Serializable]
    private sealed class AppState
    {
        public ProfileState profile = new ProfileState();
        public AttributeState attributes = new AttributeState();
        public int estimatedBmr;
        public int baseSpirit;
        public RealmState realm = new RealmState();
        public List<DailyTask> tasks = new List<DailyTask>();
        public List<LogEntry> logs = new List<LogEntry>();
        public int activeTab;
        public int lastWorkoutCalories;
        public int lastWorkoutCultivation;
    }

    [Serializable]
    private sealed class ProfileState
    {
        public float heightCm = 170;
        public float weightKg = 65;
        public int ageYears = 25;
        public int formulaMode;
        public long lastUpdatedTicks;
    }

    [Serializable]
    private struct AttributeState
    {
        public int physique;
        public int endurance;
        public int recovery;
        public int mind;
    }

    [Serializable]
    private sealed class RealmState
    {
        public int index;
        public int progress;
        public int stability = 50;
    }

    [Serializable]
    private sealed class DailyTask
    {
        public string id;
        public string title;
        public int activityIndex;
        public int target;
        public int reward;
        public int progress;
        public bool complete;
        public bool claimed;
    }

    [Serializable]
    private sealed class LogEntry
    {
        public string title;
        public string body;
    }

    private struct RealmInfo
    {
        public string name;
        public string layer;
        public int required;
        public RealmInfo(string name, string layer, int required)
        {
            this.name = name;
            this.layer = layer;
            this.required = required;
        }
    }

    private struct ActivityInfo
    {
        public string id;
        public string label;
        public float baseMet;
        public float cultivationMultiplier;
        public int attributeIndex;
        public int stability;
        public int defaultBonus;
        public int defaultStabilityCost;
        public ActivityInfo(string id, string label, float baseMet, float cultivationMultiplier, int attributeIndex, int stability, int defaultBonus, int defaultStabilityCost)
        {
            this.id = id;
            this.label = label;
            this.baseMet = baseMet;
            this.cultivationMultiplier = cultivationMultiplier;
            this.attributeIndex = attributeIndex;
            this.stability = stability;
            this.defaultBonus = defaultBonus;
            this.defaultStabilityCost = defaultStabilityCost;
        }
    }

    private struct WorkoutEstimate
    {
        public string label;
        public int calories;
        public int cultivationBonus;
        public int stabilityCost;
        public WorkoutEstimate(int calories, int cultivationBonus, int stabilityCost, string label)
        {
            this.calories = calories;
            this.cultivationBonus = cultivationBonus;
            this.stabilityCost = stabilityCost;
            this.label = label;
        }
    }

    private struct TaskTemplate
    {
        public string id;
        public string title;
        public int activityIndex;
        public int target;
        public int reward;
        public TaskTemplate(string id, string title, int activityIndex, int target, int reward)
        {
            this.id = id;
            this.title = title;
            this.activityIndex = activityIndex;
            this.target = target;
            this.reward = reward;
        }
    }

    private struct HeroRefs
    {
        public Image card;
        public Text copy;
    }

    private sealed class FloatMotion : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 origin;
        private float speed = 4f;
        private float amplitude = 8f;
        private float phase;

        public void Configure(float speed, float amplitude, float phase)
        {
            this.speed = speed;
            this.amplitude = amplitude;
            this.phase = phase;
        }

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            origin = rect.anchoredPosition;
        }

        private void Update()
        {
            float t = Time.unscaledTime / Mathf.Max(0.01f, speed) + phase;
            rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 1.3f) * amplitude * 0.35f, Mathf.Sin(t) * amplitude);
        }
    }

    private sealed class PulseAlpha : MonoBehaviour
    {
        private Graphic graphic;
        private float minAlpha = 0.15f;
        private float maxAlpha = 0.32f;
        private float speed = 2f;

        public void Configure(float minAlpha, float maxAlpha, float speed)
        {
            this.minAlpha = minAlpha;
            this.maxAlpha = maxAlpha;
            this.speed = speed;
        }

        private void Awake()
        {
            graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            if (graphic == null) return;
            Color color = graphic.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f);
            graphic.color = color;
        }
    }

    private sealed class GradientGraphic : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color bottom = Color.gray;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            List<UIVertex> vertices = new List<UIVertex>();
            vh.GetUIVertexStream(vertices);
            if (vertices.Count == 0) return;

            float minY = vertices[0].position.y;
            float maxY = minY;
            for (int i = 1; i < vertices.Count; i++)
            {
                float y = vertices[i].position.y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            float height = Mathf.Max(0.001f, maxY - minY);
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex vertex = vertices[i];
                float t = Mathf.Clamp01((vertex.position.y - minY) / height);
                vertex.color *= Color.Lerp(bottom, top, t);
                vertices[i] = vertex;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(vertices);
        }
    }

    private sealed class CycleOption
    {
        private readonly Button button;
        private readonly string[] options;
        private int value;

        public CycleOption(Button button, string[] options)
        {
            this.button = button;
            this.options = options;
            button.onClick.AddListener(Next);
            Refresh();
        }

        public int Value
        {
            get { return value; }
            set
            {
                this.value = Mathf.Clamp(value, 0, options.Length - 1);
                Refresh();
            }
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }

        private void Next()
        {
            value = (value + 1) % options.Length;
            Refresh();
        }

        private void Refresh()
        {
            button.GetComponentInChildren<Text>().text = options[Mathf.Clamp(value, 0, options.Length - 1)];
        }
    }

    private sealed class SimpleDropdown
    {
        private readonly XiuxianFitnessApp app;
        private readonly Button button;
        private readonly RectTransform menuParent;
        private readonly RectTransform menu;
        private readonly string[] options;
        private int value;
        private bool open;

        public event Action OnChanged;

        public SimpleDropdown(XiuxianFitnessApp app, RectTransform parent, RectTransform menuParent, string[] options)
        {
            this.app = app;
            this.menuParent = menuParent;
            this.options = options;
            button = app.Button(parent, options[0], new Color(1f, 0.985f, 0.93f, 0.95f), app.ink, 0, 44);
            Stretch(button.GetComponent<RectTransform>(), 0, 0, 0, 0);
            button.onClick.AddListener(Toggle);

            menu = app.Rect("DropdownMenu", menuParent);
            AnchorBelow(menu, 0, 46, 0, options.Length * 40);
            Image menuBg = menu.gameObject.AddComponent<Image>();
            menuBg.sprite = app.RoundedSprite(16);
            menuBg.color = app.panel;
            VerticalLayoutGroup layout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 1;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < options.Length; i++)
            {
                int captured = i;
                Button item = app.Button(menu, options[i], new Color(1f, 0.985f, 0.93f, 0.98f), app.ink, 0, 38);
                item.onClick.AddListener(delegate { Select(captured); });
            }

            menu.gameObject.SetActive(false);
            Refresh();
        }

        public int Value
        {
            get { return value; }
            set
            {
                this.value = Mathf.Clamp(value, 0, options.Length - 1);
                Refresh();
            }
        }

        private void Toggle()
        {
            open = !open;
            if (open) PositionMenu();
            menu.gameObject.SetActive(open);
            if (open) menu.SetAsLastSibling();
        }

        private void PositionMenu()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(menuParent);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            buttonRect.GetWorldCorners(corners);

            Vector3 bottomLeft = menuParent.InverseTransformPoint(corners[0]);
            Vector3 topRight = menuParent.InverseTransformPoint(corners[2]);
            Rect parentRect = menuParent.rect;
            float left = Mathf.Max(0f, bottomLeft.x - parentRect.xMin);
            float right = Mathf.Max(0f, parentRect.xMax - topRight.x);
            float top = Mathf.Max(0f, parentRect.yMax - bottomLeft.y + 2f);
            AnchorBelow(menu, left, top, right, options.Length * 40);
        }

        private void Select(int index)
        {
            value = Mathf.Clamp(index, 0, options.Length - 1);
            open = false;
            menu.gameObject.SetActive(false);
            Refresh();
            if (OnChanged != null) OnChanged();
        }

        private void Refresh()
        {
            app.SetButtonText(button, options[Mathf.Clamp(value, 0, options.Length - 1)]);
        }
    }
}
