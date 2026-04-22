using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 유닛 AI
///
/// [AI 턴 행동 우선순위] — 턴마다 1가지만 수행
///   1. 공격 준비 완료(willAttackNextTurn=true) → 공격 실행 후 턴 종료
///   2. 플레이어가 사거리 안 → 공격 준비 (이동 없음) 후 턴 종료
///   3. 플레이어가 사거리 밖 → 1칸 이동 후 턴 종료
///
/// [보스 추가 패턴]
///   3턴 주기로 충전 → 다음 턴 2배 데미지 강화 공격
/// </summary>
public class EnemyUnit : Unit
{
    [HideInInspector] public int moveSpeed = 1; // 현재 AI에서 1칸만 이동하므로 참고용

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
        // ① 공격 준비 완료 → 공격 실행
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

        // ② 플레이어가 사거리 안 → 공격 준비 (이동 없음)
        if (IsInAttackRange(player))
        {
            willAttackNextTurn = true;
            return;
        }

        // ③ 플레이어가 사거리 밖 → 1칸 이동
        Vector2Int? next = BFSNextStep(GridPos, player.GridPos);
        if (next != null)
        {
            Tile tile = BoardManager.Instance.GetTile(next.Value);
            if (tile != null && !tile.IsOccupied)
                PlaceOnBoard(next.Value);
        }
    }

    // ── 보스 AI ───────────────────────────────────────────────────────────
    private void ExecuteBossTurn(PlayerUnit player)
    {
        bossTurnCount++;

        // ① 충전 완료 → 강화 공격 (2배 데미지)
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

        // ② 3턴마다 충전 시작 + 1칸 이동
        if (bossTurnCount % 3 == 0)
        {
            bossCharging       = true;
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

        // ③ 일반 행동 — 새 AI 우선순위 적용
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

        if (IsInAttackRange(player))
        {
            willAttackNextTurn = true;
            return;
        }

        Vector2Int? next2 = BFSNextStep(GridPos, player.GridPos);
        if (next2 != null)
        {
            var tile = BoardManager.Instance.GetTile(next2.Value);
            if (tile != null && !tile.IsOccupied) PlaceOnBoard(next2.Value);
        }
    }

    // ── 공격 예고 하이라이트 ──────────────────────────────────────────────
    public void RefreshWarning()
    {
        ClearWarning();
        if (!willAttackNextTurn && !bossCharging) return;

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
                if (tile.IsWall) continue;                         // 벽 타일 우회
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

    // ── 공격 오버라이드 — 킬러 이름 기록 ────────────────────────────────
    public override void Attack(Unit target)
    {
        GameManager.LastKillerName = gameObject.name.Replace(" ", "");
        base.Attack(target);
    }

    protected override void OnDeath()
    {
        ClearWarning();
        base.OnDeath();
        EnemyManager.Instance?.OnEnemyDefeated(this);
    }
}
