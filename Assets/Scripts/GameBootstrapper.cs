using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class GameBootstrapper : MonoBehaviour
{
    private enum Team
    {
        Player,
        Enemy
    }

    private enum UnitKind
    {
        Rifleman,
        Tank,
        Harvester
    }

    private enum BuildingKind
    {
        Command,
        Power,
        Refinery,
        Barracks,
        Factory,
        Turret
    }

    private enum GameMode
    {
        Menu,
        Playing,
        Ended
    }

    [Serializable]
    private sealed class UnitStats
    {
        public string Label;
        public int MaxHp;
        public float Speed;
        public int Damage;
        public float Range;
        public float FireSeconds;
        public int Cost;
        public float Radius;

        public UnitStats(string label, int maxHp, float speed, int damage, float range, float fireSeconds, int cost, float radius)
        {
            Label = label;
            MaxHp = maxHp;
            Speed = speed;
            Damage = damage;
            Range = range;
            FireSeconds = fireSeconds;
            Cost = cost;
            Radius = radius;
        }
    }

    [Serializable]
    private sealed class BuildingStats
    {
        public string Label;
        public int MaxHp;
        public int Cost;
        public Vector2 Footprint;
        public int Damage;
        public float Range;
        public float FireSeconds;

        public BuildingStats(string label, int maxHp, int cost, Vector2 footprint, int damage = 0, float range = 0f, float fireSeconds = 0f)
        {
            Label = label;
            MaxHp = maxHp;
            Cost = cost;
            Footprint = footprint;
            Damage = damage;
            Range = range;
            FireSeconds = fireSeconds;
        }
    }

    private sealed class UnitEntity
    {
        public int Id;
        public UnitKind Kind;
        public Team Team;
        public GameObject Root;
        public SpriteRenderer Renderer;
        public SpriteRenderer Selection;
        public Transform HpFill;
        public int Hp;
        public int MaxHp;
        public bool Selected;
        public Vector2? MoveTarget;
        public UnitEntity AttackUnit;
        public BuildingEntity AttackBuilding;
        public OreNode HarvestTarget;
        public int Cargo;
        public float Cooldown;
        public float GatherCooldown;
        public bool LastOrderWasAttack;
    }

    private sealed class BuildingEntity
    {
        public int Id;
        public BuildingKind Kind;
        public Team Team;
        public GameObject Root;
        public SpriteRenderer Renderer;
        public SpriteRenderer Selection;
        public Transform HpFill;
        public int Hp;
        public int MaxHp;
        public bool Selected;
        public float Cooldown;
        public Vector2? RallyPoint;
    }

    private sealed class OreNode
    {
        public int Id;
        public GameObject Root;
        public SpriteRenderer Renderer;
        public int Amount;
    }

    private sealed class Wave
    {
        public float At;
        public UnitKind[] Units;
        public string Message;

        public Wave(float at, string message, params UnitKind[] units)
        {
            At = at;
            Message = message;
            Units = units;
        }
    }

    private sealed class LevelData
    {
        public string Title;
        public string Briefing;
        public string Objective;
        public Vector2 MapSize;
        public int StartingResources;
        public List<Tuple<BuildingKind, Vector2>> PlayerBuildings = new List<Tuple<BuildingKind, Vector2>>();
        public List<Tuple<UnitKind, Vector2>> PlayerUnits = new List<Tuple<UnitKind, Vector2>>();
        public List<Tuple<BuildingKind, Vector2>> EnemyBuildings = new List<Tuple<BuildingKind, Vector2>>();
        public List<Tuple<UnitKind, Vector2>> EnemyUnits = new List<Tuple<UnitKind, Vector2>>();
        public List<Tuple<Vector2, int>> Ore = new List<Tuple<Vector2, int>>();
        public List<Wave> Waves = new List<Wave>();
    }

    private const int OreCargo = 120;
    private const float PixelsPerUnit = 48f;

    private readonly Color playerColor = new Color(0.48f, 0.75f, 0.46f);
    private readonly Color enemyColor = new Color(0.78f, 0.28f, 0.26f);
    private readonly Color oreColor = new Color(0.78f, 0.68f, 0.22f);
    private readonly Color groundColor = new Color(0.08f, 0.12f, 0.09f);
    private readonly Color panelColor = new Color(0.07f, 0.1f, 0.08f, 0.94f);

    private readonly Dictionary<UnitKind, UnitStats> unitStats = new Dictionary<UnitKind, UnitStats>();
    private readonly Dictionary<BuildingKind, BuildingStats> buildingStats = new Dictionary<BuildingKind, BuildingStats>();
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly List<UnitEntity> units = new List<UnitEntity>();
    private readonly List<BuildingEntity> buildings = new List<BuildingEntity>();
    private readonly List<OreNode> oreNodes = new List<OreNode>();
    private readonly HashSet<UnitEntity> selectedUnits = new HashSet<UnitEntity>();
    private readonly List<GameObject> worldObjects = new List<GameObject>();

    private Camera mainCamera;
    private Canvas canvas;
    private Font uiFont;
    private EventSystem eventSystem;
    private GameMode mode = GameMode.Menu;
    private LevelData currentLevel;
    private BuildingEntity selectedBuilding;
    private BuildingKind? pendingBuilding;
    private GameObject placementGhost;
    private Image selectionBox;
    private Vector2 dragStartScreen;
    private Vector2 dragCurrentScreen;
    private bool dragging;
    private int nextEntityId = 1;
    private int nextOreId = 1;
    private int resources;
    private float missionSeconds;
    private int waveIndex;
    private Text resourceText;
    private Text objectiveText;
    private Text timerText;
    private Text messageText;
    private Text hintText;
    private RawImage minimap;
    private Texture2D minimapTexture;
    private float minimapRefresh;
    private readonly List<Button> commandButtons = new List<Button>();

    private void Awake()
    {
        Application.targetFrameRate = 60;
        SetupCamera();
        SetupSprites();
        SetupStats();
        SetupEventSystem();
        ShowMenu();
    }

    private void Update()
    {
        if (mode != GameMode.Playing)
        {
            return;
        }

        missionSeconds += Time.deltaTime;
        HandleCamera();
        HandleInput();
        UpdatePlacementGhost();
        UpdateUnits();
        UpdateBuildings();
        CleanupDestroyed();
        UpdateWaves();
        UpdateUi();
        CheckVictory();
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 8.5f;
        mainCamera.backgroundColor = groundColor;
        mainCamera.transform.position = new Vector3(10f, 8f, -10f);
    }

    private void SetupEventSystem()
    {
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            return;
        }

        GameObject eventObject = new GameObject("EventSystem");
        eventSystem = eventObject.AddComponent<EventSystem>();
        eventObject.AddComponent<StandaloneInputModule>();
    }

    private void SetupStats()
    {
        unitStats[UnitKind.Rifleman] = new UnitStats("Rifleman", 70, 3.4f, 9, 3.2f, 0.65f, 100, 0.35f);
        unitStats[UnitKind.Tank] = new UnitStats("Light Tank", 185, 2.55f, 24, 4.1f, 0.95f, 250, 0.5f);
        unitStats[UnitKind.Harvester] = new UnitStats("Harvester", 165, 2.2f, 0, 0f, 0f, 300, 0.55f);

        buildingStats[BuildingKind.Command] = new BuildingStats("Command Center", 900, 0, new Vector2(2.35f, 2f));
        buildingStats[BuildingKind.Power] = new BuildingStats("Power Plant", 380, 200, new Vector2(1.85f, 1.65f));
        buildingStats[BuildingKind.Refinery] = new BuildingStats("Ore Refinery", 620, 400, new Vector2(2.35f, 1.85f));
        buildingStats[BuildingKind.Barracks] = new BuildingStats("Barracks", 420, 300, new Vector2(1.9f, 1.6f));
        buildingStats[BuildingKind.Factory] = new BuildingStats("War Factory", 580, 500, new Vector2(2.55f, 1.95f));
        buildingStats[BuildingKind.Turret] = new BuildingStats("Gun Turret", 300, 350, new Vector2(1.2f, 1.2f), 18, 4.4f, 0.76f);
    }

    private void SetupSprites()
    {
        sprites["white"] = CreateFilledSprite(8, 8, Color.white);
        sprites["selection"] = CreateRingSprite(96, new Color(0.78f, 1f, 0.55f, 0.95f));
        sprites["enemySelection"] = CreateRingSprite(96, new Color(1f, 0.42f, 0.38f, 0.95f));
        sprites["ore"] = CreateOreSprite();
        sprites["rifleman"] = CreateUnitSprite(UnitKind.Rifleman);
        sprites["tank"] = CreateUnitSprite(UnitKind.Tank);
        sprites["harvester"] = CreateUnitSprite(UnitKind.Harvester);
        sprites["command"] = CreateBuildingSprite(BuildingKind.Command);
        sprites["power"] = CreateBuildingSprite(BuildingKind.Power);
        sprites["refinery"] = CreateBuildingSprite(BuildingKind.Refinery);
        sprites["barracks"] = CreateBuildingSprite(BuildingKind.Barracks);
        sprites["factory"] = CreateBuildingSprite(BuildingKind.Factory);
        sprites["turret"] = CreateBuildingSprite(BuildingKind.Turret);
    }

    private void ShowMenu()
    {
        mode = GameMode.Menu;
        ClearWorld();
        RebuildCanvas();

        AddPanel(new Vector2(0f, 0f), new Vector2(Screen.width, Screen.height), new Color(0.08f, 0.12f, 0.09f, 1f), "MenuBackground");
        Vector2 topLeft = new Vector2(0f, 1f);
        Text title = AddText("RED ALERT RTS UNITY", new Vector2(70f, -72f), new Vector2(760f, 58f), 38, TextAnchor.MiddleLeft, Color.white, true, topLeft);
        title.name = "Title";
        AddText("Single-player campaign prototype", new Vector2(72f, -122f), new Vector2(760f, 34f), 18, TextAnchor.MiddleLeft, new Color(0.75f, 0.82f, 0.7f), false, topLeft);
        AddText("Mission 01: First Refinery", new Vector2(78f, -230f), new Vector2(520f, 34f), 24, TextAnchor.MiddleLeft, Color.white, true, topLeft);
        AddText("Establish the beachhead. Harvest ore, train a strike team, and destroy the hostile command center.", new Vector2(78f, -270f), new Vector2(720f, 70f), 16, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.73f), false, topLeft);
        AddButton("Start Mission", new Vector2(78f, -365f), new Vector2(172f, 48f), StartMissionOne, topLeft);
        AddText("Controls: drag-select units, right-click to move or attack, WASD moves the camera.", new Vector2(78f, -520f), new Vector2(900f, 32f), 15, TextAnchor.MiddleLeft, new Color(0.75f, 0.82f, 0.7f), false, topLeft);
    }

    private void StartMissionOne()
    {
        mode = GameMode.Playing;
        ClearWorld();
        RebuildCanvas();
        currentLevel = CreateMissionOne();
        resources = currentLevel.StartingResources;
        missionSeconds = 0f;
        waveIndex = 0;
        nextEntityId = 1;
        nextOreId = 1;
        pendingBuilding = null;
        selectedBuilding = null;
        selectedUnits.Clear();
        mainCamera.transform.position = new Vector3(10f, 8f, -10f);

        DrawTerrain(currentLevel);
        foreach (Tuple<Vector2, int> ore in currentLevel.Ore)
        {
            CreateOre(ore.Item1, ore.Item2);
        }

        foreach (Tuple<BuildingKind, Vector2> building in currentLevel.PlayerBuildings)
        {
            CreateBuilding(building.Item1, Team.Player, building.Item2);
        }

        foreach (Tuple<BuildingKind, Vector2> building in currentLevel.EnemyBuildings)
        {
            CreateBuilding(building.Item1, Team.Enemy, building.Item2);
        }

        foreach (Tuple<UnitKind, Vector2> unit in currentLevel.PlayerUnits)
        {
            CreateUnit(unit.Item1, Team.Player, unit.Item2);
        }

        foreach (Tuple<UnitKind, Vector2> unit in currentLevel.EnemyUnits)
        {
            CreateUnit(unit.Item1, Team.Enemy, unit.Item2);
        }

        CreateGameplayUi();
        AutoAssignHarvesters();
        ShowMessage(currentLevel.Briefing, 5f);
    }

    private LevelData CreateMissionOne()
    {
        LevelData level = new LevelData();
        level.Title = "Mission 01: First Refinery";
        level.Briefing = "Establish the beachhead. Harvest ore, train a strike team, and destroy the hostile command center.";
        level.Objective = "Destroy the enemy Command Center.";
        level.MapSize = new Vector2(40f, 28f);
        level.StartingResources = 850;

        level.PlayerBuildings.Add(new Tuple<BuildingKind, Vector2>(BuildingKind.Command, new Vector2(7.5f, 9f)));
        level.PlayerBuildings.Add(new Tuple<BuildingKind, Vector2>(BuildingKind.Refinery, new Vector2(10.7f, 8.2f)));
        level.PlayerBuildings.Add(new Tuple<BuildingKind, Vector2>(BuildingKind.Barracks, new Vector2(7.4f, 6.4f)));
        level.PlayerUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Harvester, new Vector2(12.8f, 8f)));
        level.PlayerUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Rifleman, new Vector2(5.8f, 11.1f)));
        level.PlayerUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Rifleman, new Vector2(6.6f, 11.25f)));
        level.PlayerUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Tank, new Vector2(9f, 11.25f)));

        level.EnemyBuildings.Add(new Tuple<BuildingKind, Vector2>(BuildingKind.Command, new Vector2(31.8f, 6.2f)));
        level.EnemyBuildings.Add(new Tuple<BuildingKind, Vector2>(BuildingKind.Barracks, new Vector2(29f, 4f)));
        level.EnemyBuildings.Add(new Tuple<BuildingKind, Vector2>(BuildingKind.Turret, new Vector2(26.7f, 6.25f)));
        level.EnemyUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Rifleman, new Vector2(27.2f, 4.5f)));
        level.EnemyUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Rifleman, new Vector2(28f, 3.8f)));
        level.EnemyUnits.Add(new Tuple<UnitKind, Vector2>(UnitKind.Tank, new Vector2(30f, 7.5f)));

        level.Ore.Add(new Tuple<Vector2, int>(new Vector2(16f, 8.8f), 1600));
        level.Ore.Add(new Tuple<Vector2, int>(new Vector2(17.3f, 7.9f), 1400));
        level.Ore.Add(new Tuple<Vector2, int>(new Vector2(18.8f, 9.2f), 1300));
        level.Ore.Add(new Tuple<Vector2, int>(new Vector2(24.8f, 1.9f), 1900));
        level.Ore.Add(new Tuple<Vector2, int>(new Vector2(26.2f, 0.8f), 1500));

        level.Waves.Add(new Wave(70f, "Enemy scouts are moving on your base.", UnitKind.Rifleman, UnitKind.Rifleman, UnitKind.Tank));
        level.Waves.Add(new Wave(150f, "Enemy armor wave detected.", UnitKind.Tank, UnitKind.Tank, UnitKind.Rifleman));
        return level;
    }

    private void DrawTerrain(LevelData level)
    {
        GameObject ground = new GameObject("Ground");
        worldObjects.Add(ground);
        SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = sprites["white"];
        groundRenderer.color = groundColor;
        groundRenderer.drawMode = SpriteDrawMode.Sliced;
        groundRenderer.size = level.MapSize;
        ground.transform.position = new Vector3(level.MapSize.x / 2f, level.MapSize.y / 2f, 1f);

        for (int x = 0; x <= Mathf.RoundToInt(level.MapSize.x); x++)
        {
            CreateLine("GridX", new Vector2(x, 0f), new Vector2(x, level.MapSize.y), new Color(0.16f, 0.21f, 0.17f, 0.55f), 0.015f);
        }

        for (int y = 0; y <= Mathf.RoundToInt(level.MapSize.y); y++)
        {
            CreateLine("GridY", new Vector2(0f, y), new Vector2(level.MapSize.x, y), new Color(0.16f, 0.21f, 0.17f, 0.55f), 0.015f);
        }

        CreatePatch(new Vector2(8.5f, 5.7f), new Vector2(7.5f, 2.7f));
        CreatePatch(new Vector2(24.5f, 11f), new Vector2(9.5f, 3.1f));
        CreatePatch(new Vector2(30.5f, 3.2f), new Vector2(10f, 2.8f));
    }

    private void CreatePatch(Vector2 position, Vector2 size)
    {
        GameObject patch = new GameObject("TerrainPatch");
        worldObjects.Add(patch);
        SpriteRenderer renderer = patch.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateEllipseSprite(128, 64, new Color(0.13f, 0.2f, 0.14f, 0.6f));
        renderer.sortingOrder = -3;
        patch.transform.position = new Vector3(position.x, position.y, 0.8f);
        patch.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void CreateLine(string name, Vector2 start, Vector2 end, Color color, float width)
    {
        GameObject lineObject = new GameObject(name);
        worldObjects.Add(lineObject);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(start.x, start.y, 0.6f));
        line.SetPosition(1, new Vector3(end.x, end.y, 0.6f));
        line.startWidth = width;
        line.endWidth = width;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
    }

    private OreNode CreateOre(Vector2 position, int amount)
    {
        GameObject root = new GameObject("Ore");
        worldObjects.Add(root);
        root.transform.position = new Vector3(position.x, position.y, 0f);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites["ore"];
        renderer.color = oreColor;
        renderer.sortingOrder = 1;
        root.transform.localScale = Vector3.one * 0.8f;

        OreNode node = new OreNode();
        node.Id = nextOreId++;
        node.Root = root;
        node.Renderer = renderer;
        node.Amount = amount;
        oreNodes.Add(node);
        return node;
    }

    private UnitEntity CreateUnit(UnitKind kind, Team team, Vector2 position)
    {
        UnitStats stats = unitStats[kind];
        GameObject root = new GameObject(team + " " + stats.Label);
        worldObjects.Add(root);
        root.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[SpriteKey(kind)];
        renderer.color = team == Team.Player ? playerColor : enemyColor;
        renderer.sortingOrder = 10;

        SpriteRenderer selection = CreateSelectionRenderer(root, stats.Radius * 2.2f, team);
        Transform hpFill = CreateHpBar(root, stats.Radius * 2f, stats.Radius + 0.38f);

        UnitEntity entity = new UnitEntity();
        entity.Id = nextEntityId++;
        entity.Kind = kind;
        entity.Team = team;
        entity.Root = root;
        entity.Renderer = renderer;
        entity.Selection = selection;
        entity.HpFill = hpFill;
        entity.Hp = stats.MaxHp;
        entity.MaxHp = stats.MaxHp;
        units.Add(entity);
        UpdateUnitDecorations(entity);
        return entity;
    }

    private BuildingEntity CreateBuilding(BuildingKind kind, Team team, Vector2 position)
    {
        BuildingStats stats = buildingStats[kind];
        GameObject root = new GameObject(team + " " + stats.Label);
        worldObjects.Add(root);
        root.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[SpriteKey(kind)];
        renderer.color = team == Team.Player ? playerColor : enemyColor;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = stats.Footprint;
        renderer.sortingOrder = 5;

        SpriteRenderer selection = CreateSelectionRenderer(root, Mathf.Max(stats.Footprint.x, stats.Footprint.y) * 1.25f, team);
        Transform hpFill = CreateHpBar(root, stats.Footprint.x, stats.Footprint.y / 2f + 0.35f);

        BuildingEntity entity = new BuildingEntity();
        entity.Id = nextEntityId++;
        entity.Kind = kind;
        entity.Team = team;
        entity.Root = root;
        entity.Renderer = renderer;
        entity.Selection = selection;
        entity.HpFill = hpFill;
        entity.Hp = stats.MaxHp;
        entity.MaxHp = stats.MaxHp;
        buildings.Add(entity);
        UpdateBuildingDecorations(entity);
        return entity;
    }

    private SpriteRenderer CreateSelectionRenderer(GameObject root, float size, Team team)
    {
        GameObject selectionObject = new GameObject("Selection");
        selectionObject.transform.SetParent(root.transform, false);
        selectionObject.transform.localPosition = new Vector3(0f, -0.08f, -0.1f);
        selectionObject.transform.localScale = new Vector3(size, size * 0.62f, 1f);
        SpriteRenderer selection = selectionObject.AddComponent<SpriteRenderer>();
        selection.sprite = sprites[team == Team.Player ? "selection" : "enemySelection"];
        selection.sortingOrder = 20;
        selection.enabled = false;
        return selection;
    }

    private Transform CreateHpBar(GameObject root, float width, float yOffset)
    {
        GameObject back = new GameObject("HpBarBack");
        back.transform.SetParent(root.transform, false);
        back.transform.localPosition = new Vector3(0f, yOffset, -0.15f);
        SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
        backRenderer.sprite = sprites["white"];
        backRenderer.color = new Color(0.04f, 0.06f, 0.05f, 0.95f);
        backRenderer.sortingOrder = 30;
        back.transform.localScale = new Vector3(width, 0.08f, 1f);

        GameObject fill = new GameObject("HpBarFill");
        fill.transform.SetParent(back.transform, false);
        fill.transform.localPosition = new Vector3(-0.5f, 0f, -0.05f);
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = sprites["white"];
        fillRenderer.color = playerColor;
        fillRenderer.sortingOrder = 31;
        fill.transform.localScale = new Vector3(1f, 1f, 1f);

        back.SetActive(false);
        return fill.transform;
    }

    private void HandleCamera()
    {
        Vector3 delta = Vector3.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) delta.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) delta.x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) delta.y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) delta.y += 1f;

        if (delta.sqrMagnitude > 0f)
        {
            mainCamera.transform.position += delta.normalized * (12f * Time.deltaTime);
        }

        Vector3 position = mainCamera.transform.position;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        position.x = Mathf.Clamp(position.x, halfWidth, currentLevel.MapSize.x - halfWidth);
        position.y = Mathf.Clamp(position.y, halfHeight, currentLevel.MapSize.y - halfHeight);
        position.z = -10f;
        mainCamera.transform.position = position;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            SelectHarvester();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            BuildingEntity command = FindPlayerBuilding(BuildingKind.Command);
            if (command != null)
            {
                mainCamera.transform.position = new Vector3(command.Root.transform.position.x, command.Root.transform.position.y, -10f);
            }
        }

        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (Input.GetMouseButtonDown(0) && !overUi)
        {
            if (pendingBuilding.HasValue)
            {
                TryPlaceBuilding(ScreenToWorld(Input.mousePosition));
                return;
            }

            dragStartScreen = Input.mousePosition;
            dragCurrentScreen = dragStartScreen;
            dragging = true;
            selectionBox.gameObject.SetActive(true);
            UpdateSelectionBox();
        }

        if (dragging && Input.GetMouseButton(0))
        {
            dragCurrentScreen = Input.mousePosition;
            UpdateSelectionBox();
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            selectionBox.gameObject.SetActive(false);
            if (Vector2.Distance(dragStartScreen, Input.mousePosition) > 8f)
            {
                SelectUnitsInScreenBox(dragStartScreen, Input.mousePosition);
            }
            else
            {
                SelectSingleAt(ScreenToWorld(Input.mousePosition));
            }
        }

        if (Input.GetMouseButtonDown(1) && !overUi)
        {
            if (pendingBuilding.HasValue)
            {
                CancelPlacement();
            }
            else
            {
                HandleRightClick(ScreenToWorld(Input.mousePosition));
            }
        }
    }

    private void UpdateSelectionBox()
    {
        Vector2 min = Vector2.Min(dragStartScreen, dragCurrentScreen);
        Vector2 max = Vector2.Max(dragStartScreen, dragCurrentScreen);
        RectTransform rect = selectionBox.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = min;
        rect.sizeDelta = max - min;
    }

    private void SelectUnitsInScreenBox(Vector2 a, Vector2 b)
    {
        ClearSelection();
        Rect rect = new Rect(Vector2.Min(a, b), Vector2.Max(a, b) - Vector2.Min(a, b));
        foreach (UnitEntity unit in units)
        {
            if (unit.Team != Team.Player || unit.Hp <= 0)
            {
                continue;
            }

            Vector3 screen = mainCamera.WorldToScreenPoint(unit.Root.transform.position);
            if (rect.Contains(screen))
            {
                SetUnitSelected(unit, true);
            }
        }

        ShowMessage(selectedUnits.Count + " units selected.", 1.1f);
    }

    private void SelectSingleAt(Vector2 world)
    {
        ClearSelection();
        UnitEntity unit = FindUnitAt(world, Team.Player);
        if (unit != null)
        {
            SetUnitSelected(unit, true);
            return;
        }

        BuildingEntity building = FindBuildingAt(world, Team.Player);
        if (building != null)
        {
            selectedBuilding = building;
            building.Selected = true;
            UpdateBuildingDecorations(building);
            ShowMessage(buildingStats[building.Kind].Label, 1.2f);
        }
    }

    private void ClearSelection()
    {
        foreach (UnitEntity unit in selectedUnits)
        {
            unit.Selected = false;
            UpdateUnitDecorations(unit);
        }
        selectedUnits.Clear();

        if (selectedBuilding != null)
        {
            selectedBuilding.Selected = false;
            UpdateBuildingDecorations(selectedBuilding);
            selectedBuilding = null;
        }
    }

    private void SetUnitSelected(UnitEntity unit, bool selected)
    {
        unit.Selected = selected;
        if (selected)
        {
            selectedUnits.Add(unit);
        }
        else
        {
            selectedUnits.Remove(unit);
        }
        UpdateUnitDecorations(unit);
    }

    private void HandleRightClick(Vector2 world)
    {
        if (selectedBuilding != null && selectedUnits.Count == 0)
        {
            selectedBuilding.RallyPoint = world;
            ShowPing(world, playerColor);
            ShowMessage("Rally point set.", 1.1f);
            return;
        }

        if (selectedUnits.Count == 0)
        {
            return;
        }

        UnitEntity targetUnit = FindUnitAt(world, Team.Enemy);
        BuildingEntity targetBuilding = targetUnit == null ? FindBuildingAt(world, Team.Enemy) : null;
        OreNode ore = targetUnit == null && targetBuilding == null ? FindOreAt(world) : null;

        foreach (UnitEntity unit in selectedUnits)
        {
            if (targetUnit != null)
            {
                unit.AttackUnit = targetUnit;
                unit.AttackBuilding = null;
                unit.HarvestTarget = null;
                unit.LastOrderWasAttack = true;
            }
            else if (targetBuilding != null)
            {
                unit.AttackUnit = null;
                unit.AttackBuilding = targetBuilding;
                unit.HarvestTarget = null;
                unit.LastOrderWasAttack = true;
            }
            else if (ore != null && unit.Kind == UnitKind.Harvester)
            {
                unit.HarvestTarget = ore;
                unit.AttackUnit = null;
                unit.AttackBuilding = null;
                unit.MoveTarget = ore.Root.transform.position;
                unit.LastOrderWasAttack = false;
            }
            else
            {
                unit.AttackUnit = null;
                unit.AttackBuilding = null;
                unit.HarvestTarget = null;
                unit.LastOrderWasAttack = false;
            }
        }

        if (targetUnit == null && targetBuilding == null && ore == null)
        {
            IssueMoveOrder(new List<UnitEntity>(selectedUnits), world);
            ShowPing(world, playerColor);
        }
        else
        {
            ShowPing(world, targetUnit != null || targetBuilding != null ? enemyColor : oreColor);
        }
    }

    private void IssueMoveOrder(List<UnitEntity> orderedUnits, Vector2 target)
    {
        int columns = Mathf.CeilToInt(Mathf.Sqrt(orderedUnits.Count));
        const float spacing = 0.78f;
        for (int i = 0; i < orderedUnits.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Vector2 offset = new Vector2((col - (columns - 1) * 0.5f) * spacing, row * spacing);
            orderedUnits[i].MoveTarget = target + offset;
        }
    }

    private void UpdateUnits()
    {
        foreach (UnitEntity unit in units)
        {
            if (unit.Hp <= 0)
            {
                continue;
            }

            unit.Cooldown = Mathf.Max(0f, unit.Cooldown - Time.deltaTime);
            unit.GatherCooldown = Mathf.Max(0f, unit.GatherCooldown - Time.deltaTime);
            if (unit.Kind == UnitKind.Harvester)
            {
                UpdateHarvester(unit);
            }
            else
            {
                UpdateCombatUnit(unit);
            }

            MoveUnit(unit);
            UpdateUnitDecorations(unit);
        }
    }

    private void UpdateCombatUnit(UnitEntity unit)
    {
        UnitStats stats = unitStats[unit.Kind];
        if (unit.AttackUnit != null && unit.AttackUnit.Hp <= 0)
        {
            unit.AttackUnit = null;
        }

        if (unit.AttackBuilding != null && unit.AttackBuilding.Hp <= 0)
        {
            unit.AttackBuilding = null;
        }

        if (unit.AttackUnit == null && unit.AttackBuilding == null)
        {
            object nearest = FindNearestEnemy(unit.Root.transform.position, unit.Team, unit.LastOrderWasAttack ? 5.8f : 4.2f);
            unit.AttackUnit = nearest as UnitEntity;
            unit.AttackBuilding = nearest as BuildingEntity;
        }

        Vector2? targetPosition = null;
        if (unit.AttackUnit != null)
        {
            targetPosition = unit.AttackUnit.Root.transform.position;
        }
        else if (unit.AttackBuilding != null)
        {
            targetPosition = unit.AttackBuilding.Root.transform.position;
        }

        if (!targetPosition.HasValue)
        {
            return;
        }

        float distance = Vector2.Distance(unit.Root.transform.position, targetPosition.Value);
        if (distance <= stats.Range)
        {
            unit.MoveTarget = null;
            if (unit.Cooldown <= 0f)
            {
                if (unit.AttackUnit != null)
                {
                    DealDamage(unit.AttackUnit, stats.Damage);
                }
                else if (unit.AttackBuilding != null)
                {
                    DealDamage(unit.AttackBuilding, stats.Damage);
                }

                DrawShot(unit.Root.transform.position, targetPosition.Value, unit.Kind == UnitKind.Tank ? new Color(1f, 0.78f, 0.28f) : Color.white);
                unit.Cooldown = stats.FireSeconds;
            }
        }
        else if (unit.LastOrderWasAttack)
        {
            unit.MoveTarget = targetPosition.Value;
        }
    }

    private void UpdateHarvester(UnitEntity unit)
    {
        if (unit.Cargo >= OreCargo)
        {
            BuildingEntity refinery = FindNearestBuilding(unit.Root.transform.position, Team.Player, BuildingKind.Refinery);
            if (refinery == null)
            {
                return;
            }

            unit.MoveTarget = refinery.Root.transform.position;
            if (Vector2.Distance(unit.Root.transform.position, refinery.Root.transform.position) < 1.2f)
            {
                resources += unit.Cargo;
                unit.Cargo = 0;
                ShowMessage("Ore delivered.", 0.8f);
                if (unit.HarvestTarget != null && unit.HarvestTarget.Amount > 0)
                {
                    unit.MoveTarget = unit.HarvestTarget.Root.transform.position;
                }
            }
            return;
        }

        if (unit.HarvestTarget == null || unit.HarvestTarget.Amount <= 0)
        {
            unit.HarvestTarget = FindNearestOre(unit.Root.transform.position);
            if (unit.HarvestTarget != null)
            {
                unit.MoveTarget = unit.HarvestTarget.Root.transform.position;
            }
        }

        if (unit.HarvestTarget == null)
        {
            return;
        }

        float distance = Vector2.Distance(unit.Root.transform.position, unit.HarvestTarget.Root.transform.position);
        if (distance > 0.85f)
        {
            return;
        }

        unit.MoveTarget = null;
        if (unit.GatherCooldown <= 0f)
        {
            int gathered = Mathf.Min(30, unit.HarvestTarget.Amount, OreCargo - unit.Cargo);
            unit.Cargo += gathered;
            unit.HarvestTarget.Amount -= gathered;
            unit.HarvestTarget.Renderer.color = Color.Lerp(new Color(0.4f, 0.35f, 0.12f, 0.45f), oreColor, Mathf.Clamp01(unit.HarvestTarget.Amount / 1400f));
            unit.GatherCooldown = 0.65f;
        }
    }

    private void MoveUnit(UnitEntity unit)
    {
        if (!unit.MoveTarget.HasValue)
        {
            return;
        }

        Vector2 position = unit.Root.transform.position;
        Vector2 target = unit.MoveTarget.Value;
        float distance = Vector2.Distance(position, target);
        if (distance < 0.08f)
        {
            unit.MoveTarget = null;
            return;
        }

        UnitStats stats = unitStats[unit.Kind];
        Vector2 next = Vector2.MoveTowards(position, target, stats.Speed * Time.deltaTime);
        unit.Root.transform.position = new Vector3(next.x, next.y, 0f);
    }

    private void UpdateBuildings()
    {
        foreach (BuildingEntity building in buildings)
        {
            if (building.Hp <= 0)
            {
                continue;
            }

            building.Cooldown = Mathf.Max(0f, building.Cooldown - Time.deltaTime);
            BuildingStats stats = buildingStats[building.Kind];
            if (stats.Damage > 0 && building.Cooldown <= 0f)
            {
                object target = FindNearestEnemy(building.Root.transform.position, building.Team, stats.Range);
                if (target is UnitEntity targetUnit)
                {
                    DealDamage(targetUnit, stats.Damage);
                    DrawShot(building.Root.transform.position, targetUnit.Root.transform.position, new Color(1f, 0.76f, 0.26f));
                    building.Cooldown = stats.FireSeconds;
                }
                else if (target is BuildingEntity targetBuilding)
                {
                    DealDamage(targetBuilding, stats.Damage);
                    DrawShot(building.Root.transform.position, targetBuilding.Root.transform.position, new Color(1f, 0.76f, 0.26f));
                    building.Cooldown = stats.FireSeconds;
                }
            }

            UpdateBuildingDecorations(building);
        }
    }

    private void CleanupDestroyed()
    {
        for (int i = units.Count - 1; i >= 0; i--)
        {
            if (units[i].Hp > 0)
            {
                continue;
            }

            selectedUnits.Remove(units[i]);
            if (units[i].Root != null)
            {
                Destroy(units[i].Root);
            }
            units.RemoveAt(i);
        }

        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i].Hp > 0)
            {
                continue;
            }

            if (selectedBuilding == buildings[i])
            {
                selectedBuilding = null;
            }
            if (buildings[i].Root != null)
            {
                Destroy(buildings[i].Root);
            }
            buildings.RemoveAt(i);
        }

        for (int i = oreNodes.Count - 1; i >= 0; i--)
        {
            if (oreNodes[i].Amount > 0)
            {
                continue;
            }

            if (oreNodes[i].Root != null)
            {
                Destroy(oreNodes[i].Root);
            }
            oreNodes.RemoveAt(i);
        }
    }

    private void UpdateWaves()
    {
        if (waveIndex >= currentLevel.Waves.Count)
        {
            return;
        }

        Wave wave = currentLevel.Waves[waveIndex];
        if (missionSeconds < wave.At)
        {
            return;
        }

        waveIndex++;
        ShowMessage(wave.Message, 4f);
        BuildingEntity enemyCommand = FindEnemyBuilding(BuildingKind.Command);
        BuildingEntity playerCommand = FindPlayerBuilding(BuildingKind.Command);
        if (enemyCommand == null || playerCommand == null)
        {
            return;
        }

        for (int i = 0; i < wave.Units.Length; i++)
        {
            UnitEntity unit = CreateUnit(wave.Units[i], Team.Enemy, (Vector2)enemyCommand.Root.transform.position + new Vector2(1.4f + i * 0.55f, -1.2f));
            unit.AttackBuilding = playerCommand;
            unit.LastOrderWasAttack = true;
        }
    }

    private void CheckVictory()
    {
        if (FindPlayerBuilding(BuildingKind.Command) == null)
        {
            FinishMission(false, "Your Command Center has been destroyed.");
            return;
        }

        if (FindEnemyBuilding(BuildingKind.Command) == null)
        {
            FinishMission(true, "Enemy Command Center destroyed.");
        }
    }

    private void FinishMission(bool won, string detail)
    {
        mode = GameMode.Ended;
        CancelPlacement();
        ClearSelection();
        AddPanel(new Vector2(0f, 0f), new Vector2(540f, 260f), new Color(0.08f, 0.12f, 0.09f, 0.96f), "ResultPanel");
        AddText(won ? "MISSION COMPLETE" : "MISSION FAILED", new Vector2(0f, 72f), new Vector2(480f, 48f), 30, TextAnchor.MiddleCenter, won ? new Color(0.85f, 1f, 0.7f) : new Color(1f, 0.73f, 0.7f), true);
        AddText(detail, new Vector2(0f, 18f), new Vector2(470f, 42f), 16, TextAnchor.MiddleCenter, Color.white, false);
        AddButton("Replay", new Vector2(-88f, -66f), new Vector2(156f, 46f), StartMissionOne);
        AddButton("Campaign", new Vector2(88f, -66f), new Vector2(156f, 46f), ShowMenu);
    }

    private void CreateGameplayUi()
    {
        AddPanel(new Vector2(0f, 0f), new Vector2(Screen.width, 56f), panelColor, "TopBar", new Vector2(0.5f, 1f));
        AddPanel(new Vector2(0f, 0f), new Vector2(Screen.width, 116f), panelColor, "BottomBar", new Vector2(0.5f, 0f));

        resourceText = AddText("", new Vector2(22f, -18f), new Vector2(190f, 34f), 18, TextAnchor.MiddleLeft, Color.white, true, new Vector2(0f, 1f));
        objectiveText = AddText("", new Vector2(220f, -18f), new Vector2(760f, 34f), 15, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.73f), false, new Vector2(0f, 1f));
        timerText = AddText("", new Vector2(-24f, -18f), new Vector2(140f, 34f), 16, TextAnchor.MiddleRight, Color.white, true, new Vector2(1f, 1f));
        hintText = AddText("Drag select | Right-click move/attack/harvest | WASD camera | Esc cancel build", new Vector2(22f, 18f), new Vector2(720f, 28f), 13, TextAnchor.MiddleLeft, new Color(0.73f, 0.79f, 0.69f), false, new Vector2(0f, 0f));
        messageText = AddText("", new Vector2(0f, -72f), new Vector2(900f, 42f), 17, TextAnchor.MiddleCenter, Color.white, true, new Vector2(0.5f, 1f));
        messageText.gameObject.SetActive(false);

        float x = 22f;
        AddCommandButton("Rifleman\n$100", x, delegate { TrainUnit(UnitKind.Rifleman); });
        x += 118f;
        AddCommandButton("Tank\n$250", x, delegate { TrainUnit(UnitKind.Tank); });
        x += 118f;
        AddCommandButton("Harvester\n$300", x, delegate { TrainUnit(UnitKind.Harvester); });
        x += 136f;
        AddCommandButton("Power\n$200", x, delegate { BeginBuildingPlacement(BuildingKind.Power); });
        x += 112f;
        AddCommandButton("Refinery\n$400", x, delegate { BeginBuildingPlacement(BuildingKind.Refinery); });
        x += 118f;
        AddCommandButton("Barracks\n$300", x, delegate { BeginBuildingPlacement(BuildingKind.Barracks); });
        x += 122f;
        AddCommandButton("Factory\n$500", x, delegate { BeginBuildingPlacement(BuildingKind.Factory); });
        x += 118f;
        AddCommandButton("Turret\n$350", x, delegate { BeginBuildingPlacement(BuildingKind.Turret); });

        GameObject mapObject = new GameObject("MiniMap");
        mapObject.transform.SetParent(canvas.transform, false);
        minimap = mapObject.AddComponent<RawImage>();
        minimap.color = Color.white;
        RectTransform mapRect = minimap.rectTransform;
        mapRect.anchorMin = new Vector2(1f, 0f);
        mapRect.anchorMax = new Vector2(1f, 0f);
        mapRect.pivot = new Vector2(1f, 0f);
        mapRect.anchoredPosition = new Vector2(-22f, 10f);
        mapRect.sizeDelta = new Vector2(172f, 104f);
        minimapTexture = new Texture2D(172, 104, TextureFormat.RGBA32, false);
        minimap.filterMode = FilterMode.Point;
        minimap.texture = minimapTexture;

        GameObject selectionObject = new GameObject("SelectionBox");
        selectionObject.transform.SetParent(canvas.transform, false);
        selectionBox = selectionObject.AddComponent<Image>();
        selectionBox.color = new Color(0.65f, 0.9f, 0.5f, 0.18f);
        selectionBox.gameObject.SetActive(false);

        UpdateUi();
    }

    private void AddCommandButton(string label, float x, UnityEngine.Events.UnityAction action)
    {
        Button button = AddButton(label, new Vector2(x, 60f), new Vector2(104f, 56f), action, new Vector2(0f, 0f));
        commandButtons.Add(button);
    }

    private void UpdateUi()
    {
        if (resourceText == null)
        {
            return;
        }

        resourceText.text = "Ore: $" + resources;
        objectiveText.text = currentLevel.Title + "  |  " + currentLevel.Objective;
        TimeSpan span = TimeSpan.FromSeconds(missionSeconds);
        timerText.text = string.Format("{0:00}:{1:00}", span.Minutes, span.Seconds);

        if (commandButtons.Count >= 8)
        {
            commandButtons[0].interactable = CanTrain(UnitKind.Rifleman);
            commandButtons[1].interactable = CanTrain(UnitKind.Tank);
            commandButtons[2].interactable = CanTrain(UnitKind.Harvester);
            commandButtons[3].interactable = CanStartBuilding(BuildingKind.Power);
            commandButtons[4].interactable = CanStartBuilding(BuildingKind.Refinery);
            commandButtons[5].interactable = CanStartBuilding(BuildingKind.Barracks);
            commandButtons[6].interactable = CanStartBuilding(BuildingKind.Factory);
            commandButtons[7].interactable = CanStartBuilding(BuildingKind.Turret);
        }

        if (Time.time >= minimapRefresh && minimapTexture != null)
        {
            minimapRefresh = Time.time + 0.2f;
            DrawMiniMap();
        }
    }

    private bool CanTrain(UnitKind kind)
    {
        if (resources < unitStats[kind].Cost)
        {
            return false;
        }

        BuildingKind producer = kind == UnitKind.Rifleman ? BuildingKind.Barracks : BuildingKind.Factory;
        return FindPlayerBuilding(producer) != null;
    }

    private void TrainUnit(UnitKind kind)
    {
        if (!CanTrain(kind))
        {
            return;
        }

        BuildingKind producerKind = kind == UnitKind.Rifleman ? BuildingKind.Barracks : BuildingKind.Factory;
        BuildingEntity producer = selectedBuilding != null && selectedBuilding.Kind == producerKind ? selectedBuilding : FindPlayerBuilding(producerKind);
        if (producer == null)
        {
            return;
        }

        resources -= unitStats[kind].Cost;
        Vector2 spawn = (Vector2)producer.Root.transform.position + UnityEngine.Random.insideUnitCircle.normalized * 1.75f;
        UnitEntity unit = CreateUnit(kind, Team.Player, spawn);
        if (producer.RallyPoint.HasValue)
        {
            unit.MoveTarget = producer.RallyPoint.Value;
        }
        if (kind == UnitKind.Harvester)
        {
            AssignHarvester(unit);
        }
        ShowMessage(unitStats[kind].Label + " ready.", 1.1f);
    }

    private bool CanStartBuilding(BuildingKind kind)
    {
        return resources >= buildingStats[kind].Cost && FindPlayerBuilding(BuildingKind.Command) != null;
    }

    private void BeginBuildingPlacement(BuildingKind kind)
    {
        if (!CanStartBuilding(kind))
        {
            return;
        }

        CancelPlacement();
        pendingBuilding = kind;
        placementGhost = new GameObject("PlacementGhost");
        SpriteRenderer renderer = placementGhost.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[SpriteKey(kind)];
        renderer.color = new Color(playerColor.r, playerColor.g, playerColor.b, 0.65f);
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = buildingStats[kind].Footprint;
        renderer.sortingOrder = 40;
        ShowMessage("Place " + buildingStats[kind].Label + ".", 1.4f);
    }

    private void UpdatePlacementGhost()
    {
        if (!pendingBuilding.HasValue || placementGhost == null)
        {
            return;
        }

        Vector2 world = ScreenToWorld(Input.mousePosition);
        placementGhost.transform.position = new Vector3(world.x, world.y, -0.2f);
        SpriteRenderer renderer = placementGhost.GetComponent<SpriteRenderer>();
        renderer.color = CanPlaceBuilding(pendingBuilding.Value, world) ? new Color(0.62f, 0.9f, 0.5f, 0.65f) : new Color(0.9f, 0.25f, 0.22f, 0.65f);
    }

    private void TryPlaceBuilding(Vector2 world)
    {
        if (!pendingBuilding.HasValue)
        {
            return;
        }

        BuildingKind kind = pendingBuilding.Value;
        if (!CanPlaceBuilding(kind, world))
        {
            ShowMessage("Cannot build there.", 1.1f);
            return;
        }

        resources -= buildingStats[kind].Cost;
        BuildingEntity building = CreateBuilding(kind, Team.Player, world);
        if (kind == BuildingKind.Refinery)
        {
            UnitEntity harvester = CreateUnit(UnitKind.Harvester, Team.Player, world + Vector2.right * 1.7f);
            AssignHarvester(harvester);
        }

        CancelPlacement();
        ClearSelection();
        selectedBuilding = building;
        building.Selected = true;
        UpdateBuildingDecorations(building);
        ShowMessage(buildingStats[kind].Label + " online.", 1.2f);
    }

    private bool CanPlaceBuilding(BuildingKind kind, Vector2 world)
    {
        BuildingStats stats = buildingStats[kind];
        if (world.x < stats.Footprint.x / 2f || world.y < stats.Footprint.y / 2f || world.x > currentLevel.MapSize.x - stats.Footprint.x / 2f || world.y > currentLevel.MapSize.y - stats.Footprint.y / 2f)
        {
            return false;
        }

        bool nearFriendly = false;
        foreach (BuildingEntity building in buildings)
        {
            if (building.Team == Team.Player && Vector2.Distance(world, building.Root.transform.position) < 5.4f)
            {
                nearFriendly = true;
                break;
            }
        }

        if (!nearFriendly)
        {
            return false;
        }

        foreach (BuildingEntity building in buildings)
        {
            BuildingStats other = buildingStats[building.Kind];
            bool overlapX = Mathf.Abs(world.x - building.Root.transform.position.x) < (stats.Footprint.x + other.Footprint.x) / 2f + 0.25f;
            bool overlapY = Mathf.Abs(world.y - building.Root.transform.position.y) < (stats.Footprint.y + other.Footprint.y) / 2f + 0.25f;
            if (overlapX && overlapY)
            {
                return false;
            }
        }

        return true;
    }

    private void CancelPlacement()
    {
        pendingBuilding = null;
        if (placementGhost != null)
        {
            Destroy(placementGhost);
            placementGhost = null;
        }
    }

    private void AutoAssignHarvesters()
    {
        foreach (UnitEntity unit in units)
        {
            if (unit.Team == Team.Player && unit.Kind == UnitKind.Harvester)
            {
                AssignHarvester(unit);
            }
        }
    }

    private void AssignHarvester(UnitEntity unit)
    {
        unit.HarvestTarget = FindNearestOre(unit.Root.transform.position);
        if (unit.HarvestTarget != null)
        {
            unit.MoveTarget = unit.HarvestTarget.Root.transform.position;
        }
    }

    private void SelectHarvester()
    {
        foreach (UnitEntity unit in units)
        {
            if (unit.Team == Team.Player && unit.Kind == UnitKind.Harvester)
            {
                ClearSelection();
                SetUnitSelected(unit, true);
                mainCamera.transform.position = new Vector3(unit.Root.transform.position.x, unit.Root.transform.position.y, -10f);
                return;
            }
        }
    }

    private void DealDamage(UnitEntity target, int amount)
    {
        target.Hp = Mathf.Max(0, target.Hp - amount);
        if (target.Hp <= 0)
        {
            AddExplosion(target.Root.transform.position);
        }
    }

    private void DealDamage(BuildingEntity target, int amount)
    {
        target.Hp = Mathf.Max(0, target.Hp - amount);
        if (target.Hp <= 0)
        {
            AddExplosion(target.Root.transform.position);
        }
    }

    private object FindNearestEnemy(Vector2 from, Team team, float range)
    {
        object best = null;
        float bestDistance = range;
        foreach (UnitEntity unit in units)
        {
            if (unit.Team == team || unit.Hp <= 0)
            {
                continue;
            }

            float distance = Vector2.Distance(from, unit.Root.transform.position);
            if (distance < bestDistance)
            {
                best = unit;
                bestDistance = distance;
            }
        }

        foreach (BuildingEntity building in buildings)
        {
            if (building.Team == team || building.Hp <= 0)
            {
                continue;
            }

            float distance = Vector2.Distance(from, building.Root.transform.position);
            if (distance < bestDistance)
            {
                best = building;
                bestDistance = distance;
            }
        }

        return best;
    }

    private UnitEntity FindUnitAt(Vector2 world, Team team)
    {
        foreach (UnitEntity unit in units)
        {
            if (unit.Team != team)
            {
                continue;
            }

            if (Vector2.Distance(world, unit.Root.transform.position) <= unitStats[unit.Kind].Radius + 0.16f)
            {
                return unit;
            }
        }

        return null;
    }

    private BuildingEntity FindBuildingAt(Vector2 world, Team team)
    {
        foreach (BuildingEntity building in buildings)
        {
            if (building.Team != team)
            {
                continue;
            }

            BuildingStats stats = buildingStats[building.Kind];
            Vector2 position = building.Root.transform.position;
            if (Mathf.Abs(world.x - position.x) <= stats.Footprint.x / 2f && Mathf.Abs(world.y - position.y) <= stats.Footprint.y / 2f)
            {
                return building;
            }
        }

        return null;
    }

    private OreNode FindOreAt(Vector2 world)
    {
        foreach (OreNode node in oreNodes)
        {
            if (node.Amount > 0 && Vector2.Distance(world, node.Root.transform.position) < 0.85f)
            {
                return node;
            }
        }

        return null;
    }

    private OreNode FindNearestOre(Vector2 from)
    {
        OreNode best = null;
        float bestDistance = float.MaxValue;
        foreach (OreNode node in oreNodes)
        {
            if (node.Amount <= 0)
            {
                continue;
            }

            float distance = Vector2.Distance(from, node.Root.transform.position);
            if (distance < bestDistance)
            {
                best = node;
                bestDistance = distance;
            }
        }

        return best;
    }

    private BuildingEntity FindNearestBuilding(Vector2 from, Team team, BuildingKind kind)
    {
        BuildingEntity best = null;
        float bestDistance = float.MaxValue;
        foreach (BuildingEntity building in buildings)
        {
            if (building.Team != team || building.Kind != kind)
            {
                continue;
            }

            float distance = Vector2.Distance(from, building.Root.transform.position);
            if (distance < bestDistance)
            {
                best = building;
                bestDistance = distance;
            }
        }

        return best;
    }

    private BuildingEntity FindPlayerBuilding(BuildingKind kind)
    {
        foreach (BuildingEntity building in buildings)
        {
            if (building.Team == Team.Player && building.Kind == kind && building.Hp > 0)
            {
                return building;
            }
        }

        return null;
    }

    private BuildingEntity FindEnemyBuilding(BuildingKind kind)
    {
        foreach (BuildingEntity building in buildings)
        {
            if (building.Team == Team.Enemy && building.Kind == kind && building.Hp > 0)
            {
                return building;
            }
        }

        return null;
    }

    private void UpdateUnitDecorations(UnitEntity unit)
    {
        unit.Selection.enabled = unit.Selected;
        bool showHp = unit.Selected || unit.Hp < unit.MaxHp;
        unit.HpFill.parent.gameObject.SetActive(showHp);
        unit.HpFill.localScale = new Vector3(Mathf.Clamp01((float)unit.Hp / unit.MaxHp), 1f, 1f);
        SpriteRenderer fillRenderer = unit.HpFill.GetComponent<SpriteRenderer>();
        fillRenderer.color = unit.Team == Team.Player ? playerColor : enemyColor;
    }

    private void UpdateBuildingDecorations(BuildingEntity building)
    {
        building.Selection.enabled = building.Selected;
        bool showHp = building.Selected || building.Hp < building.MaxHp;
        building.HpFill.parent.gameObject.SetActive(showHp);
        building.HpFill.localScale = new Vector3(Mathf.Clamp01((float)building.Hp / building.MaxHp), 1f, 1f);
        SpriteRenderer fillRenderer = building.HpFill.GetComponent<SpriteRenderer>();
        fillRenderer.color = building.Team == Team.Player ? playerColor : enemyColor;
    }

    private void DrawShot(Vector2 from, Vector2 to, Color color)
    {
        GameObject shot = new GameObject("Shot");
        LineRenderer line = shot.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(from.x, from.y, -0.3f));
        line.SetPosition(1, new Vector3(to.x, to.y, -0.3f));
        line.startWidth = 0.06f;
        line.endWidth = 0.02f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        Destroy(shot, 0.12f);
    }

    private void ShowPing(Vector2 world, Color color)
    {
        GameObject ping = new GameObject("OrderPing");
        ping.transform.position = new Vector3(world.x, world.y, -0.25f);
        SpriteRenderer renderer = ping.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites["selection"];
        renderer.color = color;
        renderer.sortingOrder = 60;
        StartCoroutine(FadeAndScale(ping, 0.45f, 0.3f, 1.2f));
    }

    private void AddExplosion(Vector2 world)
    {
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = new Vector3(world.x, world.y, -0.35f);
        SpriteRenderer renderer = explosion.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFilledCircleSprite(64, new Color(1f, 0.65f, 0.24f, 0.9f));
        renderer.sortingOrder = 70;
        StartCoroutine(FadeAndScale(explosion, 0.32f, 0.25f, 1.35f));
    }

    private System.Collections.IEnumerator FadeAndScale(GameObject target, float duration, float startScale, float endScale)
    {
        float t = 0f;
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        while (t < duration && target != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            target.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);
            if (renderer != null)
            {
                Color c = renderer.color;
                c.a = 1f - p;
                renderer.color = c;
            }
            yield return null;
        }

        if (target != null)
        {
            Destroy(target);
        }
    }

    private Vector2 ScreenToWorld(Vector3 screen)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(screen);
        return new Vector2(world.x, world.y);
    }

    private void DrawMiniMap()
    {
        Color empty = new Color(0.04f, 0.06f, 0.05f, 1f);
        Color[] pixels = new Color[minimapTexture.width * minimapTexture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = empty;
        }

        DrawMiniMapDots(pixels, oreNodes, oreColor, 2);
        foreach (BuildingEntity building in buildings)
        {
            PlotMiniMap(pixels, building.Root.transform.position, building.Team == Team.Player ? playerColor : enemyColor, 3);
        }
        foreach (UnitEntity unit in units)
        {
            PlotMiniMap(pixels, unit.Root.transform.position, unit.Team == Team.Player ? playerColor : enemyColor, 1);
        }

        Vector3 camMin = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 camMax = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        DrawMiniMapRect(pixels, camMin, camMax, Color.white);

        minimapTexture.SetPixels(pixels);
        minimapTexture.Apply(false);
    }

    private void DrawMiniMapDots(Color[] pixels, List<OreNode> nodes, Color color, int radius)
    {
        foreach (OreNode node in nodes)
        {
            PlotMiniMap(pixels, node.Root.transform.position, color, radius);
        }
    }

    private void PlotMiniMap(Color[] pixels, Vector2 world, Color color, int radius)
    {
        int x = Mathf.RoundToInt(world.x / currentLevel.MapSize.x * (minimapTexture.width - 1));
        int y = Mathf.RoundToInt(world.y / currentLevel.MapSize.y * (minimapTexture.height - 1));
        for (int yy = -radius; yy <= radius; yy++)
        {
            for (int xx = -radius; xx <= radius; xx++)
            {
                int px = x + xx;
                int py = y + yy;
                if (px < 0 || py < 0 || px >= minimapTexture.width || py >= minimapTexture.height)
                {
                    continue;
                }
                pixels[py * minimapTexture.width + px] = color;
            }
        }
    }

    private void DrawMiniMapRect(Color[] pixels, Vector2 min, Vector2 max, Color color)
    {
        int x0 = Mathf.Clamp(Mathf.RoundToInt(min.x / currentLevel.MapSize.x * (minimapTexture.width - 1)), 0, minimapTexture.width - 1);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(max.x / currentLevel.MapSize.x * (minimapTexture.width - 1)), 0, minimapTexture.width - 1);
        int y0 = Mathf.Clamp(Mathf.RoundToInt(min.y / currentLevel.MapSize.y * (minimapTexture.height - 1)), 0, minimapTexture.height - 1);
        int y1 = Mathf.Clamp(Mathf.RoundToInt(max.y / currentLevel.MapSize.y * (minimapTexture.height - 1)), 0, minimapTexture.height - 1);
        for (int x = x0; x <= x1; x++)
        {
            pixels[y0 * minimapTexture.width + x] = color;
            pixels[y1 * minimapTexture.width + x] = color;
        }
        for (int y = y0; y <= y1; y++)
        {
            pixels[y * minimapTexture.width + x0] = color;
            pixels[y * minimapTexture.width + x1] = color;
        }
    }

    private void ShowMessage(string text, float seconds)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = text;
        messageText.gameObject.SetActive(true);
        CancelInvoke("HideMessage");
        Invoke("HideMessage", seconds);
    }

    private void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void RebuildCanvas()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }

        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GameObject canvasObject = new GameObject("Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
        canvasObject.AddComponent<GraphicRaycaster>();
        commandButtons.Clear();
        resourceText = null;
        objectiveText = null;
        timerText = null;
        messageText = null;
        hintText = null;
        minimap = null;
        minimapTexture = null;
    }

    private Image AddPanel(Vector2 anchoredPosition, Vector2 size, Color color, string name, Vector2? anchor = null)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(canvas.transform, false);
        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        RectTransform rect = image.rectTransform;
        Vector2 anchorPoint = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = anchorPoint;
        rect.anchorMax = anchorPoint;
        rect.pivot = anchorPoint;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return image;
    }

    private Text AddText(string text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color, bool bold, Vector2? anchor = null)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(canvas.transform, false);
        Text uiText = textObject.AddComponent<Text>();
        uiText.text = text;
        uiText.font = uiFont;
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = color;
        uiText.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = uiText.rectTransform;
        Vector2 anchorPoint = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = anchorPoint;
        rect.anchorMax = anchorPoint;
        rect.pivot = anchorPoint;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return uiText;
    }

    private Button AddButton(string label, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction action, Vector2? anchor = null)
    {
        GameObject buttonObject = new GameObject("Button " + label);
        buttonObject.transform.SetParent(canvas.transform, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.24f, 0.18f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.24f, 0.18f, 1f);
        colors.highlightedColor = new Color(0.28f, 0.36f, 0.26f, 1f);
        colors.pressedColor = new Color(0.68f, 0.24f, 0.22f, 1f);
        colors.disabledColor = new Color(0.12f, 0.14f, 0.12f, 0.72f);
        button.colors = colors;
        button.onClick.AddListener(action);

        RectTransform rect = button.GetComponent<RectTransform>();
        Vector2 anchorPoint = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = anchorPoint;
        rect.anchorMax = anchorPoint;
        rect.pivot = anchorPoint;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = AddText(label, Vector2.zero, size, 14, TextAnchor.MiddleCenter, Color.white, true, new Vector2(0.5f, 0.5f));
        text.transform.SetParent(buttonObject.transform, false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private void ClearWorld()
    {
        CancelPlacement();
        foreach (GameObject obj in worldObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        worldObjects.Clear();
        units.Clear();
        buildings.Clear();
        oreNodes.Clear();
        selectedUnits.Clear();
        selectedBuilding = null;
    }

    private string SpriteKey(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Rifleman: return "rifleman";
            case UnitKind.Tank: return "tank";
            case UnitKind.Harvester: return "harvester";
            default: return "rifleman";
        }
    }

    private string SpriteKey(BuildingKind kind)
    {
        switch (kind)
        {
            case BuildingKind.Command: return "command";
            case BuildingKind.Power: return "power";
            case BuildingKind.Refinery: return "refinery";
            case BuildingKind.Barracks: return "barracks";
            case BuildingKind.Factory: return "factory";
            case BuildingKind.Turret: return "turret";
            default: return "command";
        }
    }

    private Sprite CreateFilledSprite(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private Sprite CreateFilledCircleSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.42f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? color : Color.clear);
            }
        }
        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private Sprite CreateRingSprite(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outer = size * 0.42f;
        float inner = size * 0.36f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= outer && distance >= inner ? color : Color.clear);
            }
        }
        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private Sprite CreateEllipseSprite(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(width / 2f, height / 2f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - center.x) / (width * 0.48f);
                float dy = (y - center.y) / (height * 0.48f);
                texture.SetPixel(x, y, dx * dx + dy * dy <= 1f ? color : Color.clear);
            }
        }
        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private Sprite CreateOreSprite()
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Fill(texture, Color.clear);
        FillPolygon(texture, new[] { new Vector2(14, 68), new Vector2(29, 34), new Vector2(52, 17), new Vector2(78, 34), new Vector2(86, 68), new Vector2(65, 84), new Vector2(31, 86) }, new Color(0.78f, 0.68f, 0.22f));
        FillPolygon(texture, new[] { new Vector2(29, 34), new Vector2(52, 17), new Vector2(55, 48), new Vector2(29, 68) }, new Color(0.95f, 0.83f, 0.36f));
        FillPolygon(texture, new[] { new Vector2(55, 48), new Vector2(78, 34), new Vector2(86, 68), new Vector2(65, 84) }, new Color(0.62f, 0.49f, 0.15f));
        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private Sprite CreateUnitSprite(UnitKind kind)
    {
        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Fill(texture, Color.clear);
        Color bright = new Color(0.95f, 0.98f, 0.91f);
        Color mid = new Color(0.78f, 0.84f, 0.73f);
        Color dark = new Color(0.22f, 0.25f, 0.21f);

        if (kind == UnitKind.Rifleman)
        {
            FillCircle(texture, 48, 43, 18, bright);
            FillRect(texture, 42, 58, 12, 24, mid);
            FillRect(texture, 24, 44, 20, 8, bright);
            FillRect(texture, 52, 44, 30, 7, dark);
            FillCircle(texture, 48, 40, 7, dark);
        }
        else if (kind == UnitKind.Tank)
        {
            FillRect(texture, 18, 20, 60, 56, bright);
            FillRect(texture, 12, 25, 12, 46, mid);
            FillRect(texture, 72, 25, 12, 46, mid);
            FillRect(texture, 30, 30, 36, 34, new Color(0.98f, 1f, 0.93f));
            FillCircle(texture, 48, 47, 13, mid);
            FillRect(texture, 45, 4, 7, 40, bright);
            FillRect(texture, 41, 2, 15, 9, dark);
        }
        else
        {
            FillRect(texture, 16, 24, 64, 48, bright);
            FillRect(texture, 20, 28, 24, 18, new Color(0.98f, 1f, 0.94f));
            FillRect(texture, 48, 30, 24, 34, mid);
            FillRect(texture, 10, 18, 76, 12, new Color(0.9f, 0.73f, 0.23f));
            FillCircle(texture, 29, 75, 6, dark);
            FillCircle(texture, 67, 75, 6, dark);
        }

        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private Sprite CreateBuildingSprite(BuildingKind kind)
    {
        Texture2D texture = new Texture2D(128, 112, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Fill(texture, Color.clear);
        Color bright = new Color(0.93f, 0.97f, 0.89f);
        Color mid = new Color(0.75f, 0.82f, 0.7f);
        Color dark = new Color(0.21f, 0.25f, 0.2f);

        if (kind == BuildingKind.Command)
        {
            FillRect(texture, 12, 38, 104, 58, bright);
            FillPolygon(texture, new[] { new Vector2(26, 38), new Vector2(64, 12), new Vector2(102, 38) }, new Color(0.98f, 1f, 0.94f));
            FillRect(texture, 56, 18, 16, 58, mid);
            FillCircle(texture, 64, 24, 8, dark);
            FillRect(texture, 45, 72, 38, 24, dark);
        }
        else if (kind == BuildingKind.Power)
        {
            FillRect(texture, 20, 46, 88, 48, bright);
            FillPolygon(texture, new[] { new Vector2(38, 46), new Vector2(52, 14), new Vector2(76, 46) }, new Color(0.98f, 1f, 0.94f));
            FillPolygon(texture, new[] { new Vector2(63, 18), new Vector2(50, 55), new Vector2(66, 55), new Vector2(57, 88), new Vector2(84, 42), new Vector2(66, 42), new Vector2(78, 18) }, new Color(0.93f, 0.74f, 0.22f));
        }
        else if (kind == BuildingKind.Refinery)
        {
            FillRect(texture, 10, 48, 78, 44, bright);
            FillRect(texture, 28, 60, 42, 22, mid);
            FillRect(texture, 88, 28, 26, 64, new Color(0.98f, 1f, 0.94f));
            FillCircle(texture, 101, 72, 10, new Color(0.78f, 0.68f, 0.22f));
        }
        else if (kind == BuildingKind.Barracks)
        {
            FillRect(texture, 18, 42, 92, 48, bright);
            FillPolygon(texture, new[] { new Vector2(22, 42), new Vector2(64, 14), new Vector2(106, 42) }, new Color(0.98f, 1f, 0.94f));
            FillRect(texture, 52, 56, 26, 34, dark);
            FillRect(texture, 30, 58, 18, 18, mid);
            FillRect(texture, 84, 58, 12, 18, mid);
        }
        else if (kind == BuildingKind.Factory)
        {
            FillRect(texture, 12, 38, 112, 54, bright);
            FillPolygon(texture, new[] { new Vector2(12, 38), new Vector2(38, 38), new Vector2(52, 18), new Vector2(68, 38), new Vector2(88, 38), new Vector2(104, 18), new Vector2(124, 38) }, new Color(0.98f, 1f, 0.94f));
            FillRect(texture, 28, 60, 40, 32, dark);
            FillRect(texture, 78, 54, 34, 14, mid);
            FillRect(texture, 84, 74, 22, 12, mid);
        }
        else
        {
            FillCircle(texture, 64, 66, 26, bright);
            FillCircle(texture, 64, 66, 14, mid);
            FillRect(texture, 60, 18, 8, 48, bright);
            FillRect(texture, 55, 16, 18, 10, dark);
        }

        FillRect(texture, 8, 92, 112, 8, new Color(0.58f, 0.65f, 0.54f));
        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0, 0, 128, 112), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private void Fill(Texture2D texture, Color color)
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    private void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                if (xx >= 0 && yy >= 0 && xx < texture.width && yy < texture.height)
                {
                    texture.SetPixel(xx, texture.height - yy - 1, color);
                }
            }
        }
    }

    private void FillCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= radius * radius && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                {
                    texture.SetPixel(x, texture.height - y - 1, color);
                }
            }
        }
    }

    private void FillPolygon(Texture2D texture, Vector2[] points, Color color)
    {
        int minX = texture.width;
        int minY = texture.height;
        int maxX = 0;
        int maxY = 0;
        foreach (Vector2 point in points)
        {
            minX = Mathf.Min(minX, Mathf.FloorToInt(point.x));
            minY = Mathf.Min(minY, Mathf.FloorToInt(point.y));
            maxX = Mathf.Max(maxX, Mathf.CeilToInt(point.x));
            maxY = Mathf.Max(maxY, Mathf.CeilToInt(point.y));
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (PointInPolygon(new Vector2(x, y), points) && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                {
                    texture.SetPixel(x, texture.height - y - 1, color);
                }
            }
        }
    }

    private bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }
}
