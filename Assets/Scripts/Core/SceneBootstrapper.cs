using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class SceneBootstrapper : MonoBehaviour
{
    // 보드 설정 (BoardManager와 동일하게)
    private const int BoardWidth  = 8;
    private const int BoardHeight = 8;
    private const float TileSize  = 1f;

    private void Awake()
    {
        SetupCamera();
        SetupManagers();
        SetupBoard();
        SetupUI();
        SetupTestEntities();
        // StartGame()은 타이틀화면의 "게임 시작" 버튼에서 호출
        // (타이틀 없이 바로 시작하려면 아래 주석 해제)
        // GameManager.Instance.StartGame();
    }

    // ── 0. 카메라 & 환경 ─────────────────────────────────────────────────
    private void SetupCamera()
    {
        var cam = Camera.main;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
        cam.nearClipPlane = -100f;
        cam.farClipPlane  =  100f;

        var light = FindFirstObjectByType<Light>();
        if (light != null) light.gameObject.SetActive(false);

        var volume = FindFirstObjectByType<Volume>();
        if (volume != null) volume.gameObject.SetActive(false);
    }

    // ── 1. 매니저 ─────────────────────────────────────────────────────────
    private void SetupManagers()
    {
        var boardGo = new GameObject("BoardManager");
        var boardManager = boardGo.AddComponent<BoardManager>();

        new GameObject("EnemyManager").AddComponent<EnemyManager>();
        var turnManager = new GameObject("TurnManager").AddComponent<TurnManager>();

        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.boardManager = boardManager;
        gm.turnManager  = turnManager;

        new GameObject("EffectManager").AddComponent<EffectManager>();
        new GameObject("StageManager").AddComponent<StageManager>();
        new GameObject("CheatManager").AddComponent<CheatManager>();
    }

    // ── 2. 보드 ───────────────────────────────────────────────────────────
    private void SetupBoard()
    {
        BoardManager.Instance.SetTilePrefab(CreateTilePrefab());
        BoardManager.Instance.BuildBoard();
    }

    private GameObject CreateTilePrefab()
    {
        var go = new GameObject("TilePrefab");

        var bgSr = go.AddComponent<SpriteRenderer>();
        bgSr.sprite     = CreateSquareSprite();
        bgSr.color      = new Color(0.08f, 0.09f, 0.14f); // 격자선
        bgSr.sortingOrder = 0;

        var inner = new GameObject("Inner");
        inner.transform.SetParent(go.transform);
        inner.transform.localPosition = Vector3.zero;
        inner.transform.localScale    = Vector3.one * 0.86f;

        var innerSr = inner.AddComponent<SpriteRenderer>();
        innerSr.sprite     = CreateSquareSprite();
        innerSr.color      = new Color(0.30f, 0.38f, 0.55f); // 타일 본체
        innerSr.sortingOrder = 1;

        go.AddComponent<Tile>();
        go.SetActive(false);
        return go;
    }

    // ── 3. UI ─────────────────────────────────────────────────────────────
    private void SetupUI()
    {
        // ── EventSystem (UI 버튼 클릭 감지에 필수) ───────────────────────
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>(); // New Input System 전용
        }

        // ── Screen Space HUD ─────────────────────────────────────────────
        var canvasGo = new GameObject("GameCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<GameUI>().Build(canvas);

        // 치트 패널
        canvasGo.AddComponent<CheatPanel>().Build(canvas);

        // 결과 화면 (게임오버 / 맵클리어)
        canvasGo.AddComponent<ResultScreen>().Build(canvas.transform);

        // 스테이지 선택 화면
        canvasGo.AddComponent<StageSelectScreen>().Build(canvas.transform);

        // 타이틀 화면 (맨 위에 그려지도록 마지막에 추가)
        canvasGo.AddComponent<TitleScreen>().Build(canvas);
    }

    // ── 4. 유닛 배치 ──────────────────────────────────────────────────────
    private void SetupTestEntities()
    {
        // 플레이어
        var playerGo = CreateCircleObject("Player", new Color(0.35f, 0.65f, 1f));
        playerGo.transform.localScale = Vector3.one * 0.72f;
        var player = playerGo.AddComponent<ElementalPlayerUnit>();
        player.maxHp        = 3;
        player.attackDamage = 1;
        player.attackRange  = 2;
        playerGo.AddComponent<StatusEffectHandler>();

        TurnManager.Instance.SetPlayer(player);
        playerGo.AddComponent<PlayerInputController>().Init(player);
        player.PlaceOnBoard(new Vector2Int(0, 0));
        // 적 스폰은 GameManager.StartGame(stage) → StageManager.InitStage()에서 처리
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────
    private GameObject CreateCircleObject(string unitName, Color color)
    {
        var go = new GameObject(unitName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateCircleSprite();
        sr.color        = color;
        sr.sortingOrder = 5;
        return go;
    }

    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        var tex    = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float c = size / 2f, r = c - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                pixels[y * size + x] = dx * dx + dy * dy <= r * r
                    ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
