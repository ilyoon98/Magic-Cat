using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 암흑 거인 (보스) — INDEX 6
///
/// 3턴 주기 5×5 광역 패턴.
///
/// [턴 1 — 이동]
///   BFS 1칸 이동
///
/// [턴 2 — 경고]
///   5×5 사각 범위 Danger 하이라이트
///
/// [턴 3 — 공격]
///   · 5×5 범위 내 플레이어 → attackDamage(3) 피해
///   · 5×5 범위 내 벽 → DarkGiant→벽 방향으로 WallManager.LaunchWall
///
/// 5×5 범위: 상하좌우 각 2칸 (attackRange=2), 마름모 아닌 정사각형
/// </summary>
public class DarkGiantUnit : EnemyUnit
{
    private int giantTurn = 0;

    // 경고 하이라이트 타일 목록
    private readonly List<Vector2Int> aoeDangerTiles = new List<Vector2Int>();

    protected override void Awake() { base.Awake(); }

    // ── 턴 진입점 ────────────────────────────────────────────────────────────
    public override void ExecuteTurn(PlayerUnit player)
    {
        if (!IsAlive || player == null) return;
        giantTurn++;

        switch (giantTurn % 3)
        {
            case 1: ExecuteMoveTurn(player);    break; // 이동
            case 2: ExecuteWarningTurn(player); break; // 경고
            case 0: ExecuteAttackTurn(player);  break; // 공격
        }
    }

    // ── 턴 1: 이동 ──────────────────────────────────────────────────────────
    private void ExecuteMoveTurn(PlayerUnit player)
    {
        ClearAoeDanger();
        Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
        if (next != null)
        {
            Tile tile = BoardManager.Instance.GetTile(next.Value);
            if (tile != null && !tile.IsOccupied && !tile.IsWall)
                PlaceOnBoard(next.Value);
        }
    }

    // ── 턴 2: 5×5 경고 ──────────────────────────────────────────────────────
    private void ExecuteWarningTurn(PlayerUnit player)
    {
        ClearAoeDanger();
        HighlightAoeRange();
        GameUI.Instance?.ShowNotify($"💀 {name} 광역 공격 예고!", 1.5f);
    }

    // ── 턴 3: 5×5 공격 + 벽 발사 ─────────────────────────────────────────────
    private void ExecuteAttackTurn(PlayerUnit player)
    {
        ClearAoeDanger();
        GameUI.Instance?.ShowNotify($"💀 {name} 광역 공격!", 1.2f);

        // ① 플레이어 피해
        if (IsInAoeRange(player.GridPos))
        {
            GameManager.LastKillerName = gameObject.name.Replace(" ", "");
            EffectManager.Instance?.PlayExplosion(player.transform.position);
            player.TakeDamage(attackDamage);
        }

        // ② 5×5 범위 내 벽 발사
        if (WallManager.Instance != null)
        {
            // 현재 벽 위치 목록을 먼저 복사 (발사 도중 dict가 바뀌므로)
            var wallPositions = WallManager.Instance.GetWallPositions();
            foreach (var wp in wallPositions)
            {
                if (!IsInAoeRange(wp)) continue;

                Vector2Int dir = GridUtil.SnapToCardinal(wp - GridPos);

                // 벽 발사 이펙트
                EffectManager.Instance?.PlayExplosion(
                    BoardManager.Instance.GridToWorld(wp));

                WallManager.Instance.LaunchWall(wp, dir, attackDamage, player);
            }
        }
    }

    // ── 5×5 사각 범위 확인 ──────────────────────────────────────────────────
    private bool IsInAoeRange(Vector2Int pos)
    {
        return Mathf.Abs(pos.x - GridPos.x) <= attackRange &&
               Mathf.Abs(pos.y - GridPos.y) <= attackRange;
    }

    // ── 5×5 경고 하이라이트 ─────────────────────────────────────────────────
    private void HighlightAoeRange()
    {
        for (int dx = -attackRange; dx <= attackRange; dx++)
        {
            for (int dy = -attackRange; dy <= attackRange; dy++)
            {
                if (dx == 0 && dy == 0) continue; // 자기 자신 제외

                Vector2Int pos  = GridPos + new Vector2Int(dx, dy);
                Tile        tile = BoardManager.Instance.GetTile(pos);
                if (tile == null || tile.IsWall) continue;

                tile.SetHighlight(Tile.HighlightType.Danger);
                aoeDangerTiles.Add(pos);
            }
        }
    }

    // ── 경고 하이라이트 해제 ─────────────────────────────────────────────────
    private void ClearAoeDanger()
    {
        foreach (var pos in aoeDangerTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        aoeDangerTiles.Clear();
    }

    public override void RefreshWarning()
    {
        base.RefreshWarning();
        // 경고 턴(% 3 == 2) 이후 리프레시 시 Danger 재표시
        foreach (var pos in aoeDangerTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.Danger);
    }

    protected override void OnDeath()
    {
        ClearAoeDanger();
        base.OnDeath();
    }
}
