using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 암흑 거인 (보스) — INDEX 6
///
/// 2턴 주기 광역 패턴.
///
/// [홀수 턴] 예고 or 이동
///   플레이어가 attackRange 안 → 십자 범위 Danger 예고 + 이동 없음
///   사거리 밖 → BFS 1칸 이동
///
/// [짝수 턴] 광역 공격 or 이동
///   예고했으면 → 십자 범위 내 플레이어 데미지
///   예고 없었으면 → BFS 1칸 이동
///
/// 십자 범위: 상하좌우 각 attackRange칸 (현재 2칸)
/// </summary>
public class DarkGiantUnit : EnemyUnit
{
    private int  giantTurn    = 0;
    private bool aoeWarned    = false;

    // 예고 하이라이트 타일 목록
    private readonly List<Vector2Int> aoeDangerTiles = new List<Vector2Int>();

    protected override void Awake() { base.Awake(); }

    // ── 턴 진입점 ────────────────────────────────────────────────────────
    public override void ExecuteTurn(PlayerUnit player)
    {
        if (!IsAlive || player == null) return;
        giantTurn++;

        if (giantTurn % 2 == 1) ExecuteWarningOrMoveTurn(player);
        else                     ExecuteAoeOrMoveTurn(player);
    }

    // ── 홀수 턴: 사거리 안이면 예고, 아니면 이동 ─────────────────────────
    private void ExecuteWarningOrMoveTurn(PlayerUnit player)
    {
        ClearAoeDanger();
        aoeWarned = false;

        if (!IsInAttackRange(player))
        {
            // 사거리 밖 → 1칸 이동
            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next != null)
            {
                Tile tile = BoardManager.Instance.GetTile(next.Value);
                if (tile != null && !tile.IsOccupied && !tile.IsWall)
                    PlaceOnBoard(next.Value);
            }
            return;
        }

        // 사거리 안 → 십자 범위 Danger 예고
        aoeWarned = true;
        HighlightAoeRange();
        GameUI.Instance?.ShowNotify($"💀 {name} 광역 공격 예고!", 1.5f);
    }

    // ── 짝수 턴: 예고 있으면 광역 공격, 없으면 이동 ─────────────────────
    private void ExecuteAoeOrMoveTurn(PlayerUnit player)
    {
        ClearAoeDanger();

        if (!aoeWarned)
        {
            // 예고 없었음 → 1칸 이동
            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next != null)
            {
                Tile tile = BoardManager.Instance.GetTile(next.Value);
                if (tile != null && !tile.IsOccupied && !tile.IsWall)
                    PlaceOnBoard(next.Value);
            }
            return;
        }

        // 광역 공격 실행
        aoeWarned = false;
        GameUI.Instance?.ShowNotify($"💀 {name} 광역 공격!", 1.2f);

        // 십자 범위 내 플레이어 타격
        if (IsInAoeRange(player.GridPos))
        {
            EffectManager.Instance?.PlayExplosion(player.transform.position);
            player.TakeDamage(attackDamage);
        }
    }

    // ── 십자 범위 계산 ────────────────────────────────────────────────────
    private bool IsInAoeRange(Vector2Int pos)
    {
        // 같은 행 또는 같은 열, attackRange 이내
        bool sameRow = pos.y == GridPos.y &&
                       Mathf.Abs(pos.x - GridPos.x) <= attackRange;
        bool sameCol = pos.x == GridPos.x &&
                       Mathf.Abs(pos.y - GridPos.y) <= attackRange;
        return sameRow || sameCol;
    }

    private void HighlightAoeRange()
    {
        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in dirs)
        {
            for (int i = 1; i <= attackRange; i++)
            {
                Vector2Int pos  = GridPos + dir * i;
                Tile        tile = BoardManager.Instance.GetTile(pos);
                if (tile == null || tile.IsWall) break;

                tile.SetHighlight(Tile.HighlightType.Danger);
                aoeDangerTiles.Add(pos);
            }
        }
    }

    // ── 예고 하이라이트 해제 ─────────────────────────────────────────────
    private void ClearAoeDanger()
    {
        foreach (var pos in aoeDangerTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        aoeDangerTiles.Clear();
    }

    public override void RefreshWarning()
    {
        base.RefreshWarning();
        foreach (var pos in aoeDangerTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.Danger);
    }

    protected override void OnDeath()
    {
        ClearAoeDanger();
        base.OnDeath();
    }
}
