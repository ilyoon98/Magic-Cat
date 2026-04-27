using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Stage 2 맵의 이동 가능한 벽 오브젝트 관리.
///
/// 벽은 타일 위에 배치된 물리 오브젝트로,
/// · 유닛 이동 불가 (Tile.IsWall = true)
/// · DarkGiant 스킬 발동 시 공격 방향으로 슬라이드
/// · 슬라이드 경로에서 플레이어를 만나면 데미지
/// · 몬스터는 그냥 통과 (데미지 없음)
/// · 다른 벽 또는 보드 끝 직전 타일에서 정지
/// </summary>
public class WallManager : MonoBehaviour
{
    public static WallManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, GameObject> walls =
        new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── 벽 배치 ─────────────────────────────────────────────────────────────
    public void PlaceWall(Vector2Int pos)
    {
        Tile tile = BoardManager.Instance.GetTile(pos);
        if (tile == null) return;

        // 이미 벽이 있으면 제거 후 재배치
        if (walls.ContainsKey(pos))
        {
            Destroy(walls[pos]);
            walls.Remove(pos);
        }

        tile.SetWall(true);
        walls[pos] = CreateWallVisual(pos);
    }

    // ── 전체 벽 제거 (맵 전환 시) ────────────────────────────────────────────
    public void ClearAll()
    {
        foreach (var kv in walls)
        {
            BoardManager.Instance.GetTile(kv.Key)?.SetWall(false);
            if (kv.Value != null) Destroy(kv.Value);
        }
        walls.Clear();
    }

    // ── 현재 배치된 벽 위치 목록 반환 ────────────────────────────────────────
    public List<Vector2Int> GetWallPositions()
    {
        return new List<Vector2Int>(walls.Keys);
    }

    // ── 벽 발사 ─────────────────────────────────────────────────────────────
    /// <summary>
    /// wallPos의 벽을 dir 방향으로 발사한다.
    /// · 플레이어를 만나면 damage 피해 후 해당 유닛 앞 타일에서 정지
    /// · 몬스터를 만나면 피해 없이 해당 유닛 앞 타일에서 정지
    /// · 다른 벽이나 보드 끝에 닿으면 그 직전 타일에서 정지
    /// </summary>
    public void LaunchWall(Vector2Int wallPos, Vector2Int dir, int damage, PlayerUnit player)
    {
        if (!walls.ContainsKey(wallPos)) return;

        Vector2Int stopPos  = wallPos;           // 최종 정착 위치
        Vector2Int checkPos = wallPos + dir;     // 현재 검사 중인 위치

        while (BoardManager.Instance.IsInBounds(checkPos))
        {
            Tile tile = BoardManager.Instance.GetTile(checkPos);
            if (tile == null) break;

            // 다른 벽에 막힘 → stopPos(직전)에서 멈춤
            if (tile.IsWall) break;

            // 유닛에 막힘
            if (tile.IsOccupied)
            {
                // 플레이어면 데미지
                if (tile.OccupiedUnit is PlayerUnit)
                {
                    GameManager.LastKillerName = "WallLaunch";
                    player.TakeDamage(damage);
                }
                // 몬스터/기타는 데미지 없음
                break; // 유닛 앞(stopPos)에서 정지
            }

            // 빈 타일 → 이동 가능
            stopPos  = checkPos;
            checkPos += dir;
        }

        // 이동이 없으면 벽은 제자리
        if (stopPos == wallPos) return;

        // 소스 타일 해제
        BoardManager.Instance.GetTile(wallPos)?.SetWall(false);
        GameObject visual = walls[wallPos];
        walls.Remove(wallPos);

        // 목적지 타일 설정
        BoardManager.Instance.GetTile(stopPos)?.SetWall(true);
        walls[stopPos] = visual;

        // 시각적 위치 업데이트 (즉시 이동)
        if (visual != null)
        {
            Vector3 dest = BoardManager.Instance.GridToWorld(stopPos);
            dest.z = -0.3f;
            visual.transform.position = dest;
        }
    }

    // ── 시각 오브젝트 생성 ────────────────────────────────────────────────────
    private GameObject CreateWallVisual(Vector2Int pos)
    {
        var go = new GameObject($"Wall_{pos.x}_{pos.y}");
        go.transform.SetParent(transform);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateSquareSprite();
        sr.color        = new Color(0.55f, 0.45f, 0.30f); // 석조 갈색
        sr.sortingOrder = 3;                               // 타일(1)보다 위, 유닛(5)보다 아래

        Vector3 worldPos = BoardManager.Instance.GridToWorld(pos);
        worldPos.z = -0.3f;
        go.transform.position   = worldPos;
        go.transform.localScale = Vector3.one * 0.80f;

        return go;
    }

    private static Sprite CreateSquareSprite()
    {
        var tex    = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }
}
