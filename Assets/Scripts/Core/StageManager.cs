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
    public void SpawnCurrentMap()
    {
        EnemyManager.Instance.Reset();

        var spawnList = GetSpawnList(CurrentStage, CurrentMap);
        foreach (var (dataIndex, pos) in spawnList)
            SpawnEnemy(EnemyDataTable.Get(dataIndex), pos);

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
        if (CurrentStage != prevStage)
            SwitchPlayerForStage(CurrentStage);

        BoardManager.Instance.ClearAllHighlights();
        SpawnCurrentMap();
        GameManager.Instance.ChangeState(GameManager.GameState.PlayerTurn);
    }

    // ── 플레이어 캐릭터 교체 ─────────────────────────────────────────────
    private void SwitchPlayerForStage(int stage)
    {
        var oldPlayer = TurnManager.Instance.GetPlayer();
        Vector2Int spawnPos = oldPlayer != null ? oldPlayer.GridPos : Vector2Int.zero;

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
            go.transform.localScale = Vector3.one * 0.95f;
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
        newPlayer.attackDamage = 1;
        if (stage < 3) newPlayer.attackRange = 2; // 3스테이지는 ArcanePlayerUnit.Awake에서 999 설정

        go.AddComponent<StatusEffectHandler>();
        go.AddComponent<PlayerInputController>().Init(newPlayer);

        TurnManager.Instance.SetPlayer(newPlayer);
        newPlayer.PlaceOnBoard(spawnPos);

        Debug.Log($"[StageManager] 플레이어 교체 → {StageCharacters[stage - 1].name}");
    }

    // ── 스폰 리스트 정의 ──────────────────────────────────────────────────
    private List<(int, Vector2Int)> GetSpawnList(int stage, int map)
    {
        return (stage, map) switch
        {
            // ── Stage 1 ──────────────────────────────────────────────────
            (1, 1) => new List<(int, Vector2Int)>
            {
                (1, new Vector2Int(4, 3)),
                (1, new Vector2Int(6, 5)),
            },
            (1, 2) => new List<(int, Vector2Int)>
            {
                (1, new Vector2Int(3, 3)),
                (1, new Vector2Int(5, 5)),
                (1, new Vector2Int(7, 2)),
            },
            (1, 3) => new List<(int, Vector2Int)> // 보스
            {
                (2, new Vector2Int(4, 4)),
            },

            // ── Stage 2 ──────────────────────────────────────────────────
            (2, 1) => new List<(int, Vector2Int)>
            {
                (1, new Vector2Int(3, 4)),
                (1, new Vector2Int(5, 3)),
                (1, new Vector2Int(6, 6)),
            },
            (2, 2) => new List<(int, Vector2Int)>
            {
                (1, new Vector2Int(2, 2)),
                (1, new Vector2Int(4, 6)),
                (1, new Vector2Int(6, 3)),
                (1, new Vector2Int(7, 7)),
            },
            (2, 3) => new List<(int, Vector2Int)>
            {
                (2, new Vector2Int(4, 4)),
                (1, new Vector2Int(3, 3)),
            },

            // ── Stage 3 ──────────────────────────────────────────────────
            (3, 1) => new List<(int, Vector2Int)>
            {
                (1, new Vector2Int(3, 3)),
                (1, new Vector2Int(5, 5)),
                (1, new Vector2Int(7, 1)),
                (1, new Vector2Int(1, 7)),
            },
            (3, 2) => new List<(int, Vector2Int)>
            {
                (1, new Vector2Int(2, 2)),
                (1, new Vector2Int(6, 2)),
                (1, new Vector2Int(2, 6)),
                (1, new Vector2Int(6, 6)),
                (1, new Vector2Int(4, 4)),
            },
            (3, 3) => new List<(int, Vector2Int)>
            {
                (2, new Vector2Int(4, 4)),
                (1, new Vector2Int(2, 4)),
                (1, new Vector2Int(6, 4)),
            },

            _ => new List<(int, Vector2Int)> { (1, new Vector2Int(4, 4)) }
        };
    }

    // ── 적 스폰 ───────────────────────────────────────────────────────────
    private void SpawnEnemy(EnemyDataTable.EnemyData data, Vector2Int pos)
    {
        Color fallbackColor = data.IsBoss
            ? new Color(0.85f, 0.2f, 0.2f)
            : new Color(1f, 0.45f, 0.45f);
        float fallbackScale = data.IsBoss ? 0.85f : 0.65f;

        var go = new GameObject(data.Name);
        var sr = go.AddComponent<SpriteRenderer>();

        // Resources/Units/{이름(공백제거)} 이미지 우선 사용, 없으면 원형 폴백
        string spriteName = data.Name.Replace(" ", "");
        Sprite enemySp = UnitSpriteCache.LoadSprite($"Units/{spriteName}");
        if (enemySp != null)
        {
            sr.sprite = enemySp;
            sr.color  = Color.white;
            go.transform.localScale = Vector3.one * (data.IsBoss ? 1.05f : 0.90f);
        }
        else
        {
            sr.sprite = UnitSpriteCache.CircleSprite;
            sr.color  = fallbackColor;
            go.transform.localScale = Vector3.one * fallbackScale;
        }
        sr.sortingOrder = 5;

        var enemy = go.AddComponent<EnemyUnit>();
        enemy.maxHp        = data.HP;
        enemy.attackDamage = data.Damage;
        enemy.attackRange  = data.AttackRange;
        enemy.moveSpeed    = data.MoveSpeed;
        go.AddComponent<StatusEffectHandler>();

        var hpDisplay = go.AddComponent<EnemyHPDisplay>();
        hpDisplay.Init(enemy);

        if (data.IsBoss) EnemyManager.Instance.SpawnBoss(enemy);
        else             EnemyManager.Instance.RegisterEnemy(enemy);

        enemy.PlaceOnBoard(pos);
    }
}
