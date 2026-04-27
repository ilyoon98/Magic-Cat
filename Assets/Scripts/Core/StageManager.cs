using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 스테이지/맵 진행 관리
/// 구조: Stage 1(원소마법사) → Stage 2(흑백마법사) → Stage 3(비전마법사)
///       각 스테이지: Map1(일반) → Map2(일반) → Map3(보스)
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public int CurrentStage { get; private set; } = 1;
    public int CurrentMap   { get; private set; } = 1; // 1, 2 = 일반 / 3 = 보스

    public bool IsBossMap => CurrentMap == 3;
    public string MapLabel => IsBossMap ? "BOSS" : $"MAP {CurrentMap}";

    // 스테이지별 캐릭터 정보
    private static readonly (string name, string portrait, Color color)[] StageCharacters =
    {
        ("원소마법사", "🐱", new Color(0.35f, 0.65f, 1f)),   // Stage 1
        ("흑백마법사", "🌓", new Color(0.7f,  0.35f, 1f)),   // Stage 2
        ("비전마법사", "✨", new Color(0.95f, 0.85f, 0.3f)), // Stage 3
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 현재 맵 적 스폰 ───────────────────────────────────────────────────
    /// <param name="keepPlayerPosition">true = 플레이어 위치 유지, 적/오브젝트만 랜덤 배치</param>
    public void SpawnCurrentMap(bool keepPlayerPosition = false)
    {
        FloorObjectManager.Instance?.ClearAll();
        WallManager.Instance?.ClearAll();
        EnemyManager.Instance.Reset();

        var enemyTypes = EnemySpawnManager.Instance != null
            ? EnemySpawnManager.Instance.GetEnemyIndices(CurrentStage, CurrentMap)
            : GetEnemyTypesFallback(CurrentStage, CurrentMap);
        var floorTypes = GetFloorObjectTypes(CurrentStage, CurrentMap);

        var player   = TurnManager.Instance.GetPlayer();
        var occupied = new HashSet<Vector2Int>();

        // 벽 배치 — 플레이어/적 위치 선택 전에 occupied에 등록하여 겹침 방지
        var wallPositions = GetWallPositions(CurrentStage, CurrentMap);
        foreach (var wp in wallPositions)
        {
            occupied.Add(wp);
            WallManager.Instance?.PlaceWall(wp);
        }

        const int boardSize = 8;
        const int enemyMinDist = 4; // 적은 플레이어와 최소 4칸 거리

        if (keepPlayerPosition && player != null)
        {
            // 플레이어 현재 위치 유지 — 적/바닥 오브젝트만 랜덤 배치
            Vector2Int savedPos = player.GridPos;
            occupied.Add(savedPos);

            var enemyPos = PickEnemyPositions(enemyTypes.Count, savedPos, occupied, enemyMinDist, boardSize);
            var floorPos = PickRandomPositions(floorTypes.Count, occupied, boardSize);

            BoardManager.Instance.GetTile(savedPos)?.ClearUnit();
            player.PlaceOnBoard(savedPos);

            for (int i = 0; i < enemyTypes.Count; i++)
                SpawnEnemy(enemyTypes[i], enemyPos[i]);

            if (FloorObjectManager.Instance != null)
                for (int i = 0; i < floorTypes.Count; i++)
                    FloorObjectManager.Instance.Spawn(floorTypes[i], floorPos[i]);
        }
        else
        {
            // 플레이어: 바깥에서 2번째 라인(두 번째 링) 중 랜덤 배치
            Vector2Int playerPos = PickSecondRingPosition(occupied, boardSize);
            occupied.Add(playerPos);

            // 적: 플레이어와 최소 4칸 거리
            var enemyPos = PickEnemyPositions(enemyTypes.Count, playerPos, occupied, enemyMinDist, boardSize);

            // 바닥 오브젝트: 나머지 빈 칸 중 랜덤
            var floorPos = PickRandomPositions(floorTypes.Count, occupied, boardSize);

            if (player != null)
            {
                BoardManager.Instance.GetTile(player.GridPos)?.ClearUnit();
                player.PlaceOnBoard(playerPos);
            }

            for (int i = 0; i < enemyTypes.Count; i++)
                SpawnEnemy(enemyTypes[i], enemyPos[i]);

            if (FloorObjectManager.Instance != null)
                for (int i = 0; i < floorTypes.Count; i++)
                    FloorObjectManager.Instance.Spawn(floorTypes[i], floorPos[i]);
        }

        // 배경 이미지 적용
        BoardBackground.Instance?.SetStage(CurrentStage);
        ProgressManager.UnlockBackground(CurrentStage);

        // BGM 재생
        AudioManager.Instance?.PlayBGM($"BGM_Stage{CurrentStage}");

        // UI 갱신
        var (charName, portrait, _) = StageCharacters[CurrentStage - 1];
        PortraitPanel.Instance?.SetStage(CurrentStage);
        PortraitPanel.Instance?.SetCharacterInfo(charName, portrait);
        GameUI.Instance?.Refresh();
    }

    // ── 랜덤 위치 생성 헬퍼 ──────────────────────────────────────────────

    /// <summary>보드 안에서 아무 빈 칸 count개 랜덤 선택</summary>
    private static List<Vector2Int> PickRandomPositions(int count, HashSet<Vector2Int> occupied, int boardSize = 8)
    {
        var result = new List<Vector2Int>();
        int safety = 0;
        while (result.Count < count && safety++ < 10000)
        {
            var pos = new Vector2Int(Random.Range(0, boardSize), Random.Range(0, boardSize));
            if (occupied.Add(pos))
                result.Add(pos);
        }
        return result;
    }

    /// <summary>
    /// 바깥에서 2번째 링(second ring) 위에서 빈 칸 1개 랜덤 선택
    /// 8×8 기준: x 또는 y가 1 또는 6인 테두리 링
    /// </summary>
    private static Vector2Int PickSecondRingPosition(HashSet<Vector2Int> occupied, int boardSize = 8)
    {
        int inner = 1;                // 안쪽 인덱스
        int outer = boardSize - 2;   // 바깥쪽 인덱스 (8×8이면 6)

        var candidates = new List<Vector2Int>();
        for (int x = inner; x <= outer; x++)
        for (int y = inner; y <= outer; y++)
        {
            // 두 번째 링: 네 변 중 하나에 해당 (distFromEdge == 1)
            bool onRing = x == inner || x == outer || y == inner || y == outer;
            if (onRing && !occupied.Contains(new Vector2Int(x, y)))
                candidates.Add(new Vector2Int(x, y));
        }

        if (candidates.Count == 0)
        {
            // 폴백: 전체에서 랜덤
            var pos = new Vector2Int(Random.Range(0, boardSize), Random.Range(0, boardSize));
            return pos;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 플레이어로부터 최소 minDist 이상 떨어진 빈 칸 count개 선택.
    /// 충분한 자리가 없으면 거리 조건 완화.
    /// </summary>
    private static List<Vector2Int> PickEnemyPositions(
        int count, Vector2Int playerPos, HashSet<Vector2Int> occupied,
        int minDist = 4, int boardSize = 8)
    {
        var result = new List<Vector2Int>();

        // 1차 시도: minDist 이상인 칸 수집 후 셔플
        var farCandidates = new List<Vector2Int>();
        for (int x = 0; x < boardSize; x++)
        for (int y = 0; y < boardSize; y++)
        {
            var p = new Vector2Int(x, y);
            int dist = Mathf.Abs(p.x - playerPos.x) + Mathf.Abs(p.y - playerPos.y);
            if (dist >= minDist && !occupied.Contains(p))
                farCandidates.Add(p);
        }

        // 셔플 (Fisher-Yates)
        for (int i = farCandidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (farCandidates[i], farCandidates[j]) = (farCandidates[j], farCandidates[i]);
        }

        foreach (var p in farCandidates)
        {
            if (result.Count >= count) break;
            if (occupied.Add(p)) result.Add(p);
        }

        // 부족하면 거리 무관 폴백
        if (result.Count < count)
            result.AddRange(PickRandomPositions(count - result.Count, occupied, boardSize));

        return result;
    }

    // ── 특정 스테이지부터 초기화 (StageSelect에서 호출) ──────────────────
    public void InitStage(int stage)
    {
        CurrentStage = stage;
        CurrentMap   = 1;
        SwitchPlayerForStage(stage);           // 항상 교체 (같은 스테이지여도 새로 생성)
        BoardManager.Instance.ClearAllHighlights();
        SpawnCurrentMap();
    }

    // ── 맵 클리어 처리 ────────────────────────────────────────────────────
    public void OnMapCleared()
    {
        StartCoroutine(MapClearSequence());
    }

    private IEnumerator MapClearSequence()
    {
        // 즉시 입력 차단 (클리어 후 플레이어가 계속 행동하는 버그 방지)
        GameManager.Instance?.ChangeState(GameManager.GameState.MapClear);

        // 모든 맵(일반/보스) 클리어 기록 저장
        ProgressManager.UnlockMapClear(CurrentStage, CurrentMap);

        yield return new UnityEngine.WaitForSeconds(0.4f);

        bool isBoss = IsBossMap;
        bool isLast = CurrentStage >= 3 && isBoss;

        if (isLast)
        {
            // 전체 클리어 — SpecialScene
            ProgressManager.UnlockClear(CurrentStage);
            SpecialSceneController.Instance?.ShowAllClear();
        }
        else if (isBoss)
        {
            // 스테이지 클리어 — 다음 스테이지 해금 후 SpecialScene
            ProgressManager.UnlockClear(CurrentStage);
            ProgressManager.UnlockStage(CurrentStage + 1);
            SpecialSceneController.Instance?.ShowStageClear(CurrentStage);
        }
        else
        {
            // 일반 맵 클리어 — ResultScreen
            ResultScreen.Instance?.Show(ResultScreen.ResultType.MapClear);
        }
    }

    // ── 다음 맵으로 진행 (ResultScreen 버튼에서 호출) ────────────────────
    public void AdvanceMap()
    {
        int prevStage = CurrentStage;

        if (CurrentMap < 3)
        {
            CurrentMap++;
        }
        else
        {
            CurrentStage++;
            CurrentMap = 1;

            if (CurrentStage > 3) return; // AllClear — 더 이상 진행 없음
        }

        // 스테이지가 바뀌면 플레이어 캐릭터 교체
        bool stageChanged = (CurrentStage != prevStage);
        if (stageChanged)
            SwitchPlayerForStage(CurrentStage);

        BoardManager.Instance.ClearAllHighlights();
        // 같은 스테이지 내 맵 이동 시 플레이어 위치 유지
        SpawnCurrentMap(keepPlayerPosition: !stageChanged);
        GameManager.Instance.ChangeState(GameManager.GameState.PlayerTurn);
    }

    // ── 플레이어 캐릭터 교체 ─────────────────────────────────────────────
    private void SwitchPlayerForStage(int stage)
    {
        var oldPlayer = TurnManager.Instance.GetPlayer();

        // 기존 플레이어 타일 해제 후 제거
        if (oldPlayer != null)
        {
            BoardManager.Instance.GetTile(oldPlayer.GridPos)?.ClearUnit();
            Destroy(oldPlayer.gameObject);
        }

        var (_, _, color) = StageCharacters[stage - 1];

        // 새 플레이어 오브젝트 생성
        var go = new GameObject("Player");
        var sr = go.AddComponent<SpriteRenderer>();

        // Resources/Units/Player_Stage{N} 이미지 우선 사용, 없으면 원형 폴백
        Sprite playerSp = UnitSpriteCache.LoadSprite($"Units/Player_Stage{stage}");
        if (playerSp != null)
        {
            sr.sprite = playerSp;
            sr.color  = Color.white;
            go.transform.localScale = Vector3.one;
        }
        else
        {
            sr.sprite = UnitSpriteCache.CircleSprite;
            sr.color  = color;
            go.transform.localScale = Vector3.one * 0.72f;
        }
        sr.sortingOrder = 5;

        PlayerUnit newPlayer = stage switch
        {
            2 => go.AddComponent<BlackWhitePlayerUnit>(),
            3 => go.AddComponent<ArcanePlayerUnit>(),
            _ => go.AddComponent<ElementalPlayerUnit>()
        };

        newPlayer.maxHp        = 3;
        newPlayer.currentHp    = 3;   // Awake 타이밍 문제 보정
        newPlayer.attackDamage = 1;
        if (stage < 3) newPlayer.attackRange = 2; // 3스테이지는 ArcanePlayerUnit.Awake에서 999 설정

        go.AddComponent<StatusEffectHandler>();
        go.AddComponent<PlayerInputController>().Init(newPlayer);

        TurnManager.Instance.SetPlayer(newPlayer);
        newPlayer.PlaceOnBoard(Vector2Int.zero); // SpawnCurrentMap()에서 즉시 랜덤 재배치됨

        Debug.Log($"[StageManager] 플레이어 교체 → {StageCharacters[stage - 1].name}");
    }

    // ── 스테이지/맵별 적 종류 정의 — EnemySpawnManager 미로드 시 폴백 ─────
    private static List<int> GetEnemyTypesFallback(int stage, int map) => (stage, map) switch
    {
        // Stage 1: Goblin(1) / Orc(2) / Centaurus(3)
        (1, 1) => new List<int> { 1, 1 },            // 고블린 2마리
        (1, 2) => new List<int> { 1, 1, 2 },          // 고블린 2 + 오크 1
        (1, 3) => new List<int> { 3 },                // 센타우루스 (보스)
        // Stage 2: Slime(4) / BigSlime(5)
        (2, 1) => new List<int> { 4, 4, 4 },
        (2, 2) => new List<int> { 4, 4, 4, 4 },
        (2, 3) => new List<int> { 5, 4 },             // 보스
        // Stage 3: Slime(4) / BigSlime(5)
        (3, 1) => new List<int> { 4, 4, 4, 4 },
        (3, 2) => new List<int> { 4, 4, 4, 4, 4 },
        (3, 3) => new List<int> { 5, 4, 4 },          // 보스
        _      => new List<int> { 4 }
    };

    // ── 스테이지/맵별 벽 위치 정의 ──────────────────────────────────────────
    /// <summary>Stage 2 맵에 고정 배치되는 벽 타일 좌표 목록</summary>
    private static List<Vector2Int> GetWallPositions(int stage, int map) => (stage, map) switch
    {
        (2, 1) => new List<Vector2Int> { new Vector2Int(2, 3), new Vector2Int(5, 4) },
        (2, 2) => new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(5, 5), new Vector2Int(3, 5) },
        (2, 3) => new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 5), new Vector2Int(5, 2), new Vector2Int(5, 5) },
        _      => new List<Vector2Int>()
    };

    // ── 스테이지/맵별 바닥 오브젝트 종류 정의 (위치는 SpawnCurrentMap에서 랜덤 배정) ──
    private static List<FloorObject.ObjectType> GetFloorObjectTypes(int stage, int map)
    {
        var H = FloorObject.ObjectType.Heart;
        var T = FloorObject.ObjectType.Trap;

        return (stage, map) switch
        {
            (1, 1) => new List<FloorObject.ObjectType> { T, T, H },
            (1, 2) => new List<FloorObject.ObjectType> { T, T, H },
            (1, 3) => new List<FloorObject.ObjectType> { T, T, T, T, H },
            (2, 1) => new List<FloorObject.ObjectType> { T, T, H },
            (2, 2) => new List<FloorObject.ObjectType> { T, T, T, H },
            (2, 3) => new List<FloorObject.ObjectType> { T, T, T, T, H },
            (3, 1) => new List<FloorObject.ObjectType> { T, T, T, H },
            (3, 2) => new List<FloorObject.ObjectType> { T, T, T, T, H, H },
            (3, 3) => new List<FloorObject.ObjectType> { T, T, T, T, T, T, H, H },
            _      => new List<FloorObject.ObjectType>()
        };
    }

    // ── 적 스폰 — EnemySpawnManager에 위임 ───────────────────────────────
    private static void SpawnEnemy(int enemyIndex, Vector2Int pos)
    {
        if (EnemySpawnManager.Instance != null)
            EnemySpawnManager.Instance.SpawnEnemy(enemyIndex, pos);
        else
            Debug.LogError("[StageManager] EnemySpawnManager 인스턴스 없음 — " +
                           "SceneBootstrapper에서 EnemySpawnManager를 추가했는지 확인해주세요.");
    }
}
