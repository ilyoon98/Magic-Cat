using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 유닛 AI
///
/// [공격 흐름]
///  이동 턴: 사정거리 진입 → willAttackNextTurn = true, 빨간 타일 표시, 이동 불가
///  공격 턴: willAttackNextTurn이 true이면 무조건 공격 실행 (플레이어 위치 무관)
///           사정거리 안이면 실제 데미지, 밖이면 공격 허공 (턴은 소모)
///
/// [보스]
///  3턴 주기로 충전 → 다음 턴 2배 데미지 (사거리는 Excel 그대로)
/// </summary>
public class EnemyUnit : Unit
{
    [HideInInspector] public int moveSpeed = 1;

    private bool willAttackNextTurn = false;

    // ── 보스 전용 ────────────────────────────────────────────────────────
    private int  bossTurnCount = 0;
    private bool bossCharging  = false;

    // ── 공격 예고 하이라이트 ──────────────────────────────────────────────
    private readonly List<Vector2Int> warningTiles = new List<Vector2Int>();

    protected override void Awake() { base.Awake(); }

    protected override void OnHpChanged()
    {
        GetComponent<EnemyHPDisplay>()?.UpdateDisplay();
    }

    public void ExecuteTurn(PlayerUnit player)
    {
        if (!IsAlive || player == null) return;
        if (IsBoss) ExecuteBossTurn(player);
        else        ExecuteNormalTurn(player);
    }

    // ── 일반 적 AI ────────────────────────────────────────────────────────
    private void ExecuteNormalTurn(PlayerUnit player)
    {
        // willAttackNextTurn이면 무조건 공격 (플레이어가 도망가도 실행)
        if (willAttackNextTurn)
        {
            ClearWarning();
            willAttackNextTurn = false;
            GameUI.Instance?.ShowNotify($"⚠ {name} 공격!", 1.0f);

            Vector3 targetPos = BoardManager.Instance.GridToWorld(player.GridPos);
            EffectManager.Instance?.PlayAttack(targetPos); // 공격 이펙트는 항상

            if (IsInAttackRange(player))
                Attack(player); // 사정거리 안이면 데미지, 밖이면 허공

            return; // 공격 후 턴 종료
        }

        // 이동
        bool movedIntoRange = false;
        for (int step = 0; step < moveSpeed; step++)
        {
            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next == null) break;
            Tile tile = BoardManager.Instance.GetTile(next.Value);
            if (tile == null || tile.IsOccupied) break;
            PlaceOnBoard(next.Value);

            // 이동 중 사정거리 진입 → 즉시 멈추고 공격 예고
            if (IsInAttackRange(player))
            {
                movedIntoRange = true;
                break;
            }
        }

        // 이동 후 사정거리 확인 → 다음 턴 공격 예고 세팅
        if (movedIntoRange || IsInAttackRange(player))
            willAttackNextTurn = true;
    }

    // ── 보스 AI ───────────────────────────────────────────────────────────
    private void ExecuteBossTurn(PlayerUnit player)
    {
        bossTurnCount++;

        // ① 충전 완료 → 강화 공격 (사거리는 Excel 값 그대로, 데미지만 2배)
        if (bossCharging)
        {
            bossCharging = false;
            ClearWarning();
            GameUI.Instance?.ShowNotify($"💥 {name} 강화 공격!", 1.5f);

            Vector3 targetPos = BoardManager.Instance.GridToWorld(player.GridPos);
            EffectManager.Instance?.PlayExplosion(targetPos);

            if (IsInAttackRange(player))
                player.TakeDamage(attackDamage * 2);

            return;
        }

        // ② 3턴마다 충전 시작 (이동도 1칸)
        if (bossTurnCount % 3 == 0)
        {
            bossCharging = true;
            willAttackNextTurn = false;
            ClearWarning();
            GameUI.Instance?.ShowNotify($"⚡ {name} 충전 중...", 1.5f);

            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next != null)
            {
                var tile = BoardManager.Instance.GetTile(next.Value);
                if (tile != null && !tile.IsOccupied) PlaceOnBoard(next.Value);
            }
            return;
        }

        // ③ 일반 행동 (일반 적과 동일)
        if (willAttackNextTurn)
        {
            ClearWarning();
            willAttackNextTurn = false;
            GameUI.Instance?.ShowNotify($"⚠ {name} 공격!", 1.0f);

            Vector3 targetPos = BoardManager.Instance.GridToWorld(player.GridPos);
            EffectManager.Instance?.PlayAttack(targetPos);

            if (IsInAttackRange(player))
                Attack(player);
            return;
        }

        for (int step = 0; step < moveSpeed; step++)
        {
            Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
            if (next == null) break;
            Tile tile = BoardManager.Instance.GetTile(next.Value);
            if (tile == null || tile.IsOccupied) break;
            PlaceOnBoard(next.Value);
            if (IsInAttackRange(player)) break;
        }

        if (IsInAttackRange(player))
            willAttackNextTurn = true;
    }

    // ── 공격 예고 하이라이트 ──────────────────────────────────────────────
    public void RefreshWarning()
    {
        ClearWarning();
        if (!willAttackNextTurn && !bossCharging) return;

        // 사거리 내 타일 빨간색 표시 (보스도 Excel 값 그대로)
        var tiles = BoardManager.Instance.GetTilesInRange(GridPos, attackRange);
        foreach (var tile in tiles)
        {
            tile.SetHighlight(Tile.HighlightType.Danger);
            warningTiles.Add(tile.GridPos);
        }
    }

    public void ClearWarning()
    {
        foreach (var pos in warningTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        warningTiles.Clear();
    }

    // ── BFS 경로탐색 ──────────────────────────────────────────────────────
    private Vector2Int? BFSNextStep(Vector2Int from, Vector2Int to)
    {
        if (from == to) return null;

        var queue   = new Queue<Vector2Int>();
        var visited = new Dictionary<Vector2Int, Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        queue.Enqueue(from);
        visited[from] = from;

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var next = cur + dir;
                if (visited.ContainsKey(next)) continue;
                if (!BoardManager.Instance.IsInBounds(next)) continue;

                var tile = BoardManager.Instance.GetTile(next);
                if (tile.IsOccupied && next != to) continue;

                visited[next] = cur;

                if (next == to)
                {
                    var step = next;
                    while (visited[step] != from) step = visited[step];
                    return step;
                }

                queue.Enqueue(next);
            }
        }
        return null;
    }

    protected override void OnDeath()
    {
        ClearWarning();
        base.OnDeath();
        EnemyManager.Instance?.OnEnemyDefeated(this);
    }
}
