using UnityEngine;

/// <summary>
/// 타일 위에 배치되는 바닥 오브젝트
///
/// [Heart]   — 플레이어가 밟으면 HP +1 회복 후 사라짐
/// [Trap]    — 밟으면 1 데미지, 발동 후 1턴 쿨다운
/// [Element] — 원소포설 스킬로 생성. 원소별 효과:
///   불  → 화상 (DoT 1데미지 × 1턴), 밟는 순간 1회 발동 후 사라짐
///         전도 상태면 2데미지 × 1턴
///   땅  → 벽 생성 (영구 벽 타일, 이동 불가). 밟혀도 사라지지 않음
///   나무 → 전도 (다음 원소 효과 2배 1회), 밟는 순간 1회 발동 후 사라짐
///   물  → 밀쳐내기 (플레이어 반대 방향으로 벽/맵 끝까지), 1회 발동 후 사라짐
///         충돌 시 데미지 2. 전도 상태면 충돌 데미지 2배(4).
///         충돌 대상이 땅 원소 벽이면 추가로 2배. 부드럽게 슬라이드 이동.
/// </summary>
public class FloorObject : MonoBehaviour
{
    public enum ObjectType  { Heart, Trap, Element }
    public enum ElementType { Fire, Earth, Wood, Water }

    public ObjectType  Type    { get; private set; }
    public ElementType Element { get; private set; }
    public Vector2Int  GridPos { get; private set; }

    // Trap 쿨다운
    private int  cooldownTurns = 0;
    public  bool IsActive => cooldownTurns == 0;

    // 땅 원소 벽 지속 턴 (-1 = 영구)
    private int earthWallTurnsLeft = -1;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 3;
    }

    private void OnDestroy()
    {
        // 땅 원소 벽 제거 시 타일 IsWall 해제
        if (Type == ObjectType.Element && Element == ElementType.Earth
            && BoardManager.Instance != null)
        {
            BoardManager.Instance.GetTile(GridPos)?.SetWall(false);
        }
    }

    // ── 초기화 ────────────────────────────────────────────────────────────
    public void Init(ObjectType type, Vector2Int pos)
    {
        Type    = type;
        GridPos = pos;
        UpdateVisual();
    }

    public void InitElement(ElementType element, Vector2Int pos)
    {
        Type    = ObjectType.Element;
        Element = element;
        GridPos = pos;

        if (element == ElementType.Earth)
        {
            // 즉시 벽 타일로 변환 (영구)
            earthWallTurnsLeft = -1;
            BoardManager.Instance.GetTile(pos)?.SetWall(true);
        }

        UpdateVisual();
    }

    // ── 유닛 착지 시 호출 ─────────────────────────────────────────────────
    public bool TryActivate(Unit unit)
    {
        switch (Type)
        {
            case ObjectType.Heart:   return ActivateHeart(unit);
            case ObjectType.Trap:    return ActivateTrap(unit);
            case ObjectType.Element: return ActivateElement(unit);
        }
        return false;
    }

    private bool ActivateHeart(Unit unit)
    {
        if (!(unit is PlayerUnit)) return false;
        unit.Heal(1);
        EffectManager.Instance?.PlayExplosion(unit.transform.position);
        FloorObjectManager.Instance?.Remove(this);
        return true;
    }

    private bool ActivateTrap(Unit unit)
    {
        if (!IsActive) return false;
        unit.TakeDamage(1);
        if (!unit.IsAlive && unit is PlayerUnit)
            GameManager.LastKillerName = "Trap";
        EffectManager.Instance?.PlayBlood(unit.transform.position);
        StartCooldown();
        return true;
    }

    private bool ActivateElement(Unit unit)
    {
        // 땅 원소(벽)는 이동 불가이므로 발동 불가
        if (Element == ElementType.Earth) return false;

        var seh = unit.GetComponent<StatusEffectHandler>();
        if (seh == null) return false;

        // 전도 소비 → 효과 2배 여부
        bool doubled = seh.ConsumeConductive();
        int  mult    = doubled ? 2 : 1;

        switch (Element)
        {
            case ElementType.Fire:
                // 화상: 1데미지 × 1턴 (전도 시 2데미지 × 1턴)
                seh.ApplyBurn(1 * mult, 1);
                EffectManager.Instance?.PlayFireHit(unit.transform.position);
                Debug.Log($"[원소바닥] 불 — {unit.name} 화상 {1 * mult}×1턴" + (doubled ? " (전도!)" : ""));
                break;

            case ElementType.Wood:
                // 전도: 다음 원소 효과 2배 (전도 상태이면 갱신)
                seh.ApplyConductive();
                EffectManager.Instance?.PlayWoodHit(unit.transform.position);
                Debug.Log($"[원소바닥] 나무 — {unit.name} 전도 적용" + (doubled ? " (전도 갱신!)" : ""));
                break;

            case ElementType.Water:
                // 물: 적에게만 밀쳐내기 적용
                if (unit is EnemyUnit)
                {
                    EffectManager.Instance?.PlayWaterHit(unit.transform.position);
                    ApplyKnockback(unit, doubled);
                    Debug.Log($"[원소바닥] 물 — {unit.name} 밀쳐내기" + (doubled ? " (전도! 충돌 데미지 2배)" : ""));
                }
                break;
        }

        FloorObjectManager.Instance?.Remove(this);
        return true;
    }

    // ── 밀쳐내기 ─────────────────────────────────────────────────────────
    private void ApplyKnockback(Unit unit, bool doubled)
    {
        var player = TurnManager.Instance?.GetPlayer();
        if (player == null) return;

        // 플레이어 반대 방향 계산
        Vector2Int pushDir;
        if (player.GridPos == unit.GridPos)
            pushDir = Vector2Int.up;
        else
            pushDir = GridUtil.SnapToCardinal(unit.GridPos - player.GridPos);

        // 충돌 데미지: 기본 2, 전도 시 2배(4)
        int collisionDmg = doubled ? 4 : 2;

        // ── 경로 계산 (벽/맵 끝까지) ───────────────────────────────────
        var    movePath   = new System.Collections.Generic.List<Vector2Int>();
        int    pendingDmg = 0;
        string wallTag    = "";

        Vector2Int cur = unit.GridPos;
        const int  maxSearch = 32; // 맵 최대 크기보다 충분히 큰 값

        for (int i = 0; i < maxSearch; i++)
        {
            Vector2Int next     = cur + pushDir;
            Tile       nextTile = BoardManager.Instance.GetTile(next);

            // ① 맵 외곽
            if (nextTile == null)
            {
                pendingDmg = collisionDmg;
                wallTag    = "맵 끝";
                break;
            }

            // ② 벽/점유 타일 — 땅 원소 벽이면 추가 2배
            if (nextTile.IsWall || nextTile.IsOccupied)
            {
                bool isEarthWall = nextTile.IsWall && IsEarthWallAt(next);
                pendingDmg = isEarthWall ? collisionDmg * 2 : collisionDmg;
                wallTag    = isEarthWall ? "땅벽" : "벽";
                break;
            }

            // ③ 이동 가능 타일
            movePath.Add(next);
            cur = next;
        }

        // ── 부드러운 이동 애니메이션 후 충돌 데미지 처리 ───────────────
        int    finalDmg  = pendingDmg;
        string finalTag  = wallTag;
        bool   willHit   = pendingDmg > 0;
        string unitName  = unit.name;

        unit.StartSmoothKnockback(movePath, () =>
        {
            if (!willHit) return;
            if (unit == null || !unit.IsAlive) return;
            unit.TakeDamage(finalDmg);
            string extraTag = doubled && finalTag == "땅벽" ? " (전도+땅벽!)" :
                              doubled                        ? " (전도!)"     :
                              finalTag == "땅벽"             ? " (땅벽 2배!)" : "";
            GameUI.Instance?.ShowNotify($"💥 {unitName} {finalTag} 충돌! -{finalDmg}{extraTag}", 1.0f);
            Debug.Log($"[넉백] {unitName} {finalTag} 충돌 {finalDmg} 데미지{extraTag}");
        });

        Debug.Log($"[넉백] {unitName} 경로 {movePath.Count}칸" + (willHit ? $" → {wallTag} 충돌 예정" : " (충돌 없음)"));
    }

    private static bool IsEarthWallAt(Vector2Int pos)
    {
        var obj = FloorObjectManager.Instance?.GetAt(pos);
        return obj != null
            && obj.Type    == ObjectType.Element
            && obj.Element == ElementType.Earth;
    }

    // ── 턴 시작 호출 ──────────────────────────────────────────────────────
    public void OnTurnStart()
    {
        // Trap 쿨다운
        if (Type == ObjectType.Trap)
        {
            if (cooldownTurns > 0)
            {
                cooldownTurns--;
                UpdateVisual();
                if (cooldownTurns == 0)
                {
                    var tile = BoardManager.Instance.GetTile(GridPos);
                    if (tile != null && tile.IsOccupied && tile.OccupiedUnit != null)
                        StartCoroutine(DelayedActivate(tile.OccupiedUnit));
                }
            }
            return;
        }

        // 땅 원소 벽 지속 시간 관리 (earthWallTurnsLeft == -1 이면 영구)
        if (Type == ObjectType.Element && Element == ElementType.Earth
            && earthWallTurnsLeft > 0)
        {
            earthWallTurnsLeft--;
            if (earthWallTurnsLeft <= 0)
            {
                Debug.Log($"[땅벽] {GridPos} 벽 소멸");
                FloorObjectManager.Instance?.Remove(this); // OnDestroy가 SetWall(false) 호출
            }
        }
    }

    private System.Collections.IEnumerator DelayedActivate(Unit unit)
    {
        yield return new WaitForSeconds(0.15f);
        if (unit != null && unit.IsAlive && unit.GridPos == GridPos && IsActive)
            TryActivate(unit);
    }

    private void StartCooldown()
    {
        cooldownTurns = 2;
        UpdateVisual();
    }

    // ── 비주얼 ───────────────────────────────────────────────────────────
    private void UpdateVisual()
    {
        if (sr == null) return;

        switch (Type)
        {
            case ObjectType.Heart:
                sr.sprite = MakeHeartSprite();
                sr.color  = Color.white;
                break;

            case ObjectType.Trap:
                sr.sprite = MakeTrapSprite();
                sr.color  = IsActive
                    ? new Color(1f, 0.3f, 0.3f, 0.9f)
                    : new Color(1f, 0.3f, 0.3f, 0.35f);
                break;

            case ObjectType.Element:
                if (Element == ElementType.Earth)
                {
                    // 땅 벽 — 큰 사각형으로 타일 전체를 덮는 느낌
                    sr.sprite = MakeSquareSprite();
                    sr.color  = new Color(0.55f, 0.40f, 0.20f, 0.90f);
                }
                else
                {
                    sr.sprite = MakeDiamondSprite();
                    sr.color  = GetElementColor();
                }
                break;
        }
    }

    private Color GetElementColor()
    {
        return Element switch
        {
            ElementType.Fire  => new Color(1f,    0.40f, 0.10f, 0.85f),
            ElementType.Earth => new Color(0.55f, 0.40f, 0.20f, 0.85f),
            ElementType.Wood  => new Color(0.20f, 0.85f, 0.20f, 0.85f),
            ElementType.Water => new Color(0.20f, 0.60f, 1.00f, 0.85f),
            _                 => Color.white
        };
    }

    // ── 스프라이트 생성 ───────────────────────────────────────────────────
    private static Sprite MakeDiamondSprite()
    {
        int size = 32;
        var tex    = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int cx = size / 2, cy = size / 2, r = 12;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= r)
                pixels[y * size + x] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeSquareSprite()
    {
        int size = 32;
        var tex    = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        int margin = 2;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            pixels[y * size + x] = (x >= margin && x < size - margin
                                 && y >= margin && y < size - margin)
                ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeHeartSprite()
    {
        int size = 32;
        var tex    = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Color c = new Color(0.2f, 1f, 0.4f);
        DrawCircle(pixels, size, 9,  21, 6, c);
        DrawCircle(pixels, size, 23, 21, 6, c);
        for (int y = 5; y < 21; y++)
        {
            int hw = y - 4, cx2 = 16;
            for (int x = cx2 - hw; x <= cx2 + hw; x++)
                if (x >= 0 && x < size) pixels[y * size + x] = c;
        }

        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void DrawCircle(Color[] p, int ts, int cx, int cy, int r, Color c)
    {
        for (int y = 0; y < ts; y++)
        for (int x = 0; x < ts; x++)
        {
            int dx = x - cx, dy = y - cy;
            if (dx * dx + dy * dy <= r * r) p[y * ts + x] = c;
        }
    }

    private static Sprite MakeTrapSprite()
    {
        int size = 32;
        var tex    = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int thick = 4;
        for (int i = 0; i < size; i++)
        for (int t = -thick / 2; t <= thick / 2; t++)
        {
            int x1 = Mathf.Clamp(i + t, 0, size - 1);
            if (i >= 0 && i < size) pixels[i * size + x1] = Color.white;
            int x2 = Mathf.Clamp(size - 1 - i + t, 0, size - 1);
            if (i >= 0 && i < size) pixels[i * size + x2] = Color.white;
        }

        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
