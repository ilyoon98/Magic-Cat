using UnityEngine;

/// <summary>
/// 타일 위에 배치되는 바닥 오브젝트 (회복 하트 / 함정)
///
/// [회복 하트]
///   - 초록 하트 형태 (SpriteRenderer, sortingOrder=3)
///   - 플레이어가 밟으면 → HP +1 회복 + EffectManager.PlayExplosion + DamagePopup(heal) + 사라짐
///   - 적이 밟아도 아무 효과 없음
///
/// [함정]
///   - 빨간 X 타일 (SpriteRenderer, sortingOrder=3)
///   - 플레이어 또는 적이 밟으면 → 1 데미지 + EffectManager.PlayBlood + 발동됨
///   - 발동 후 → 1턴 쿨다운 (비활성 상태, 반투명 표시)
///   - 쿨다운 후 → 다시 활성화
///   - 활성화됐을 때 위에 유닛이 있으면 즉시 재발동
/// </summary>
public class FloorObject : MonoBehaviour
{
    public enum ObjectType { Heart, Trap }

    public ObjectType Type { get; private set; }
    public Vector2Int GridPos { get; private set; }

    // 함정 쿨다운: 0=활성, 양수=쿨다운 남은 턴
    private int cooldownTurns = 0;
    public bool IsActive => cooldownTurns == 0;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 3;
    }

    public void Init(ObjectType type, Vector2Int pos)
    {
        Type = type;
        GridPos = pos;
        UpdateVisual();
    }

    // 유닛이 이 타일에 들어왔을 때 호출
    public bool TryActivate(Unit unit)
    {
        if (Type == ObjectType.Heart)
        {
            // 플레이어만 회복
            if (!(unit is PlayerUnit)) return false;
            unit.Heal(1);
            EffectManager.Instance?.PlayExplosion(unit.transform.position);
            FloorObjectManager.Instance?.Remove(this);
            return true;
        }
        else // Trap
        {
            if (!IsActive) return false;
            unit.TakeDamage(1);
            // 플레이어가 사망하면 킬러 이름 설정
            if (!unit.IsAlive && unit is PlayerUnit)
                GameManager.LastKillerName = "Trap";
            EffectManager.Instance?.PlayBlood(unit.transform.position);
            StartCooldown();
            return true;
        }
    }

    // 턴 시작 시 호출 (TurnManager에서)
    public void OnTurnStart()
    {
        if (Type != ObjectType.Trap) return;
        if (cooldownTurns > 0)
        {
            cooldownTurns--;
            UpdateVisual();

            // 쿨다운 종료 → 활성화 → 위에 유닛이 있으면 즉시 발동
            if (cooldownTurns == 0)
            {
                var tile = BoardManager.Instance.GetTile(GridPos);
                if (tile != null && tile.IsOccupied && tile.OccupiedUnit != null)
                {
                    // 한 프레임 뒤에 발동 (이동 애니메이션 완료 후)
                    StartCoroutine(DelayedActivate(tile.OccupiedUnit));
                }
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
        // 발동 턴 → 1턴 대기 → 재발동
        // OnTurnStart()에서 매 턴 1씩 감소하므로 2로 설정
        // T1 발동 → cooldown=2
        // T2 시작: 2→1 (아직 비활성)
        // T3 시작: 1→0 (재활성, 위에 유닛 있으면 즉시 재발동)
        cooldownTurns = 2;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (sr == null) return;

        if (Type == ObjectType.Heart)
        {
            sr.sprite = MakeHeartSprite();
            sr.color = Color.white;
        }
        else // Trap
        {
            sr.sprite = MakeTrapSprite();
            sr.color = IsActive
                ? new Color(1f, 0.3f, 0.3f, 0.9f)
                : new Color(1f, 0.3f, 0.3f, 0.35f); // 쿨다운 중 반투명
        }
    }

    // 16x16 텍스처로 하트 스프라이트 생성
    private static Sprite MakeHeartSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];

        // 기본 투명
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // 하트 패턴 (32x32 픽셀 아트)
        Color heartColor = new Color(0.2f, 1f, 0.4f);
        // 상단 두 원
        DrawCircle(pixels, size, 9, 21, 6, heartColor);
        DrawCircle(pixels, size, 23, 21, 6, heartColor);
        // 하단 삼각형
        for (int y = 5; y < 21; y++)
        {
            int halfW = y - 4;
            int cx = 16;
            for (int x = cx - halfW; x <= cx + halfW; x++)
                if (x >= 0 && x < size)
                    pixels[y * size + x] = heartColor;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void DrawCircle(Color[] pixels, int texSize, int cx, int cy, int radius, Color color)
    {
        for (int y = 0; y < texSize; y++)
            for (int x = 0; x < texSize; x++)
            {
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= radius * radius)
                    pixels[y * texSize + x] = color;
            }
    }

    // X 패턴 함정 스프라이트 생성
    private static Sprite MakeTrapSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        Color trapColor = Color.white;
        int thick = 4;
        for (int i = 0; i < size; i++)
        {
            for (int t = -thick / 2; t <= thick / 2; t++)
            {
                // ↘ 대각선
                int x1 = Mathf.Clamp(i + t, 0, size - 1);
                int y1 = i;
                if (y1 >= 0 && y1 < size) pixels[y1 * size + x1] = trapColor;

                // ↙ 대각선
                int x2 = Mathf.Clamp(size - 1 - i + t, 0, size - 1);
                int y2 = i;
                if (y2 >= 0 && y2 < size) pixels[y2 * size + x2] = trapColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
