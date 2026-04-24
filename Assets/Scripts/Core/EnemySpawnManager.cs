using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰 전담 매니저.
///
/// 역할:
///   1) Resources/Table/EnemySpawn.csv 파싱 → 스테이지·라운드별 배치 데이터 제공
///   2) Inspector에서 MonsterINDEX별 프리팹 연결 (미설정 시 런타임 폴백 생성)
///   3) SpawnEnemy(index, pos) 로 단일 적 스폰
///
/// EnemySpawn.csv 헤더:
///   Stage, Round, MonsterINDEX, Count
///
/// 사용법 (StageManager에서):
///   var indices = EnemySpawnManager.Instance.GetEnemyIndices(stage, round);
///   EnemySpawnManager.Instance.SpawnEnemy(index, pos);
///
/// 프리팹 연결 (Inspector):
///   - Hierarchy에서 EnemySpawnManager 오브젝트 선택
///   - "Prefab Entries" 리스트에 INDEX와 프리팹 추가
///   - 프리팹에는 SpriteRenderer가 있어야 함 (EnemyUnit 컴포넌트는 자동 추가)
/// </summary>
public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    // ── Inspector: 프리팹 매핑 ────────────────────────────────────────────

    [Serializable]
    public class PrefabEntry
    {
        [Tooltip("Enemy.csv의 INDEX와 동일한 값")]
        public int        index;
        [Tooltip("해당 몬스터의 프리팹 (SpriteRenderer 포함)")]
        public GameObject prefab;
    }

    [Header("몬스터 프리팹 연결 (INDEX → Prefab)")]
    [SerializeField] private List<PrefabEntry> prefabEntries = new List<PrefabEntry>();

    // ── 내부 데이터 ───────────────────────────────────────────────────────

    // key: (stage, round) / value: (MonsterINDEX, Count) 목록
    private Dictionary<(int stage, int round), List<(int index, int count)>> _spawnTable;
    // Inspector 매핑 → 빠른 조회 딕셔너리
    private Dictionary<int, GameObject> _prefabMap;

    // ── 초기화 ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 프리팹 맵 구성
        _prefabMap = new Dictionary<int, GameObject>();
        foreach (var e in prefabEntries)
            if (e != null && e.prefab != null)
                _prefabMap[e.index] = e.prefab;

        LoadSpawnTable();
    }

    // ── 스폰 테이블 로드 ──────────────────────────────────────────────────

    private void LoadSpawnTable()
    {
        _spawnTable = new Dictionary<(int, int), List<(int, int)>>();

        var asset = Resources.Load<TextAsset>("Table/EnemySpawn");
        if (asset == null)
        {
            Debug.LogError("[EnemySpawnManager] Resources/Table/EnemySpawn.csv 를 찾을 수 없음. " +
                           "Assets/Resources/Table/ 폴더에 EnemySpawn.csv 를 넣어주세요.");
            return;
        }

        var rows = CsvReader.Parse(asset.text);
        if (rows.Count < 2)
        {
            Debug.LogError("[EnemySpawnManager] EnemySpawn.csv 에 데이터 행이 없음");
            return;
        }

        string[] header = rows[0];
        int cStage = FindCol(header, "Stage");
        int cRound = FindCol(header, "Round");
        int cIdx   = FindCol(header, "MonsterINDEX");
        int cCount = FindCol(header, "Count");

        if (cStage < 0 || cRound < 0 || cIdx < 0)
        {
            Debug.LogError("[EnemySpawnManager] 필수 헤더(Stage, Round, MonsterINDEX)를 찾을 수 없음");
            return;
        }

        for (int r = 1; r < rows.Count; r++)
        {
            string[] row = rows[r];
            if (!int.TryParse(SafeGet(row, cStage), out int stage) || stage <= 0) continue;
            if (!int.TryParse(SafeGet(row, cRound), out int round) || round <= 0) continue;
            if (!int.TryParse(SafeGet(row, cIdx),   out int mIdx)  || mIdx  <= 0) continue;
            int count = cCount >= 0 && int.TryParse(SafeGet(row, cCount), out int c) ? c : 1;
            if (count <= 0) count = 1;

            var key = (stage, round);
            if (!_spawnTable.ContainsKey(key))
                _spawnTable[key] = new List<(int, int)>();
            _spawnTable[key].Add((mIdx, count));
        }

        Debug.Log($"[EnemySpawnManager] 스폰 테이블 로드 완료 — {_spawnTable.Count}맵 정의");
    }

    // ── 공개 API ─────────────────────────────────────────────────────────

    /// <summary>
    /// 해당 스테이지·라운드의 적 INDEX 목록 반환.
    /// Count만큼 INDEX를 펼쳐서 반환한다.
    /// ex) (MonsterINDEX=1, Count=2) → [1, 1]
    /// 데이터가 없으면 빈 리스트 반환.
    /// </summary>
    public List<int> GetEnemyIndices(int stage, int round)
    {
        if (_spawnTable == null) return new List<int>();

        var key = (stage, round);
        if (!_spawnTable.TryGetValue(key, out var entries))
        {
            Debug.LogWarning($"[EnemySpawnManager] Stage{stage} Round{round} 데이터 없음 — " +
                              "EnemySpawn.csv 에 해당 행을 추가해주세요.");
            return new List<int>();
        }

        var result = new List<int>();
        foreach (var (idx, cnt) in entries)
            for (int i = 0; i < cnt; i++)
                result.Add(idx);
        return result;
    }

    /// <summary>
    /// 단일 적을 지정 위치에 스폰한다.
    /// Inspector에 프리팹이 연결돼 있으면 Instantiate, 없으면 런타임으로 생성.
    /// </summary>
    public EnemyUnit SpawnEnemy(int enemyIndex, Vector2Int pos)
    {
        if (!EnemyDataTable.Contains(enemyIndex))
        {
            Debug.LogError($"[EnemySpawnManager] EnemyDataTable에 INDEX {enemyIndex} 없음");
            return null;
        }

        var data = EnemyDataTable.Get(enemyIndex);
        GameObject go = BuildEnemyObject(enemyIndex, data);

        // ── 로직 컴포넌트 추가 (INDEX 기반 서브클래스 선택) ──────────────
        EnemyUnit enemy = enemyIndex switch
        {
            1 => go.GetComponent<GoblinUnit>()     ?? go.AddComponent<GoblinUnit>(),     // 스피더
            3 => go.GetComponent<CentaurusUnit>()  ?? go.AddComponent<CentaurusUnit>(),  // 돌진 보스
            4 => go.GetComponent<SlimeUnit>()      ?? go.AddComponent<SlimeUnit>(),      // 분열 슬라임
            5 => go.GetComponent<SmallSlimeUnit>() ?? go.AddComponent<SmallSlimeUnit>(), // 소형 슬라임
            6 => go.GetComponent<DarkGiantUnit>()  ?? go.AddComponent<DarkGiantUnit>(),  // 광역 보스
            7 => go.GetComponent<EyeballUnit>()    ?? go.AddComponent<EyeballUnit>(),    // 레인저
            8 => go.GetComponent<ShadowHandUnit>() ?? go.AddComponent<ShadowHandUnit>(), // 스피더
            _ => go.GetComponent<EnemyUnit>()      ?? go.AddComponent<EnemyUnit>()       // 기본 AI
        };

        // ── 스탯 적용 ─────────────────────────────────────────────────────
        // AddComponent 시점에 Awake()가 즉시 실행되어 currentHp = maxHp(기본값 3)으로
        // 고정되므로, maxHp를 외부에서 바꾼 뒤 currentHp도 함께 초기화해야 한다.
        enemy.maxHp        = data.HP;
        enemy.currentHp    = data.HP;   // ← Awake 타이밍 문제 보정
        enemy.attackDamage = data.Damage;
        enemy.attackRange  = data.AttackRange;
        enemy.moveSpeed    = data.MoveSpeed;

        // ── 보조 컴포넌트 ─────────────────────────────────────────────────
        if (go.GetComponent<StatusEffectHandler>() == null)
            go.AddComponent<StatusEffectHandler>();

        var hpDisplay = go.GetComponent<EnemyHPDisplay>() ?? go.AddComponent<EnemyHPDisplay>();
        hpDisplay.Init(enemy);

        // ── EnemyManager 등록 ─────────────────────────────────────────────
        if (data.IsBoss) EnemyManager.Instance.SpawnBoss(enemy);
        else             EnemyManager.Instance.RegisterEnemy(enemy);

        enemy.PlaceOnBoard(pos);
        return enemy;
    }

    // ── 오브젝트 생성 (프리팹 우선, 없으면 런타임 폴백) ──────────────────

    private GameObject BuildEnemyObject(int enemyIndex, EnemyDataTable.EnemyData data)
    {
        // ① Inspector에 프리팹이 연결된 경우
        if (_prefabMap.TryGetValue(enemyIndex, out var prefab) && prefab != null)
        {
            var go = Instantiate(prefab);
            go.name = data.Name; // 이름은 Enemy.csv EnemyName 사용
            return go;
        }

        // ② 프리팹 미설정 — 런타임 폴백 생성
        // 이름은 Enemy.csv의 EnemyName 컬럼 값을 사용 (하드코딩 아님)
        var fallbackGo = new GameObject(data.Name);
        var sr = fallbackGo.AddComponent<SpriteRenderer>();

        // 스프라이트 경로: Assets/Resources/Units/{EnemyName(공백제거)}.png
        // ① CSV 이름 그대로 시도 (영어 파일명이면 바로 로드)
        // ② 없으면 INDEX 기반 GetEnglishSpriteName() 영어 폴백
        string spriteName = data.Name.Replace(" ", "");
        string spritePath = $"Units/{spriteName}";
        Sprite sp = UnitSpriteCache.LoadSprite(spritePath);

        if (sp == null)
        {
            string englishName = GetEnglishSpriteName(enemyIndex);
            if (englishName != null)
            {
                spritePath = $"Units/{englishName}";
                sp = UnitSpriteCache.LoadSprite(spritePath);
            }
        }

        if (sp != null)
        {
            sr.sprite = sp;
            sr.color  = Color.white;
            // Scale 별도 지정 불필요 — UnitSpriteCache.LoadSprite가
            // ppu = 이미지 높이로 설정해 자동으로 1타일 높이가 됨.
            // 보스는 1.15배로 살짝 크게 표시
            fallbackGo.transform.localScale = data.IsBoss ? Vector3.one * 1.15f : Vector3.one;
        }
        else
        {
            // 스프라이트 없음 → 원형 폴백 + INDEX별 고유 색상
            Debug.Log($"[EnemySpawnManager] 스프라이트 없음 (INDEX={enemyIndex}, " +
                      $"경로: Assets/Resources/{spritePath}.png) → 원형 폴백 사용");

            sr.sprite = UnitSpriteCache.CircleSprite;
            sr.color  = GetIndexColor(enemyIndex);
            fallbackGo.transform.localScale = Vector3.one * (data.IsBoss ? 0.85f : 0.65f);
        }
        sr.sortingOrder = 5;

        return fallbackGo;
    }

    /// <summary>
    /// INDEX별 기본 식별 색상.
    /// 스프라이트가 없을 때 원형 폴백에 적용된다.
    /// Enemy.csv에 새 몬스터를 추가하면 이 표에도 항목을 추가할 수 있다.
    /// </summary>
    private static Color GetIndexColor(int index) => index switch
    {
        1 => new Color(0.35f, 0.85f, 0.35f),  // 고블린      — 초록
        2 => new Color(0.55f, 0.30f, 0.75f),  // 오크        — 보라
        3 => new Color(0.90f, 0.20f, 0.20f),  // 켄타우로스  — 진빨강 (보스)
        4 => new Color(0.25f, 0.80f, 0.95f),  // 슬라임      — 하늘
        5 => new Color(0.55f, 0.90f, 1.00f),  // 소형슬라임  — 연하늘
        6 => new Color(0.10f, 0.05f, 0.20f),  // 암흑거인    — 진보라 (보스)
        7 => new Color(0.95f, 0.60f, 0.10f),  // 아이볼      — 주황
        8 => new Color(0.20f, 0.20f, 0.30f),  // 그림자손    — 짙은 회청
        _ => new Color(1.00f, 0.45f, 0.45f),  // 기타        — 분홍
    };

    // ── 스프라이트 이름 폴백 (INDEX → 영어 파일명) ───────────────────────
    /// <summary>
    /// CSV EnemyName이 한글이어서 영어 파일명과 불일치할 때,
    /// enemyIndex를 키로 영어 파일명을 반환한다.
    /// Resources/Units/에 영어 파일명 PNG(Goblin.png 등)가 있으면 정상 로드.
    /// </summary>
    private static string GetEnglishSpriteName(int enemyIndex) => enemyIndex switch
    {
        1 => "Goblin",
        2 => "Orc",
        3 => "Centaurus",
        4 => "Slime",
        5 => "SmallSlime",
        6 => "DarkGiant",
        7 => "Eyeball",
        8 => "ShadowHand",
        _ => null,
    };

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private static int FindCol(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string SafeGet(string[] row, int col) =>
        col >= 0 && col < row.Length ? row[col].Trim() : "";
}
