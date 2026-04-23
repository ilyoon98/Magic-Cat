using UnityEngine;
using System.Collections;
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
///
/// 서브클래스(GoblinUnit, CentaurusUnit 등)가 protected 멤버를 재사용할 수 있도록
/// 주요 필드/메서드를 protected 또는 virtual로 선언.
/// </summary>
public class EnemyUnit : Unit
{
    [HideInInspector] public int moveSpeed = 1;

    protected bool willAttackNextTurn = false;

    // ── 보스 전용 ────────────────────────────────────────────────────────
    private int  bossTurnCount = 0;
    private bool bossCharging  = false;

    // ── 공격 예고 하이라이트 ──────────────────────────────────────────────
    protected readonly List<Vector2Int> warningTiles = new List<Vector2Int>();

    // ── 스프라이트 방향 ──────────────────────────────────────────────────
    private int  _facingDir     = 1;    // 1 = 오른쪽, -1 = 왼쪽
    private bool _initialPlaced = false;

    protected override void Awake() { base.Awake(); }

    protected override void OnHpChanged()
    {
        GetComponent<EnemyHPDisplay>()?.UpdateDisplay();
    }

    public virtual void ExecuteTurn(PlayerUnit player)
    {
        if (!IsAlive || player == null) return;
        if (IsBoss) ExecuteBossTurn(player);
        else        ExecuteNormalTurn(player);
    }

    // ── 일반 적 AI ────────────────────────────────────────────────────────
    protected void ExecuteNormalTurn(PlayerUnit player)
    {
        // ① 공격 준비 완료 → 공격 실행
        if (willAttackNextTurn)
        {
            ClearWarning();
            willAttackNextTurn = false;
            GameUI.Instance?.ShowNotify($"⚠ {name} 공격!", 1.0f);

            // 사거리 확인 후 범위 안에 있을 때만 이펙트·데미지 처리
            if (IsInAttackRange(player))
            {
                Vector3 targetPos = BoardManager.Instance.GridToWorld(player.GridPos);
                EffectManager.Instance?.PlayAttack(targetPos);
                Attack(player);
            }
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
            if (tile != null && !tile.IsOccupied && !tile.IsWall)
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
                if (tile != null && !tile.IsOccupied && !tile.IsWall) PlaceOnBoard(next.Value);
            }
            return;
        }

        // ③ 일반 행동
        if (willAttackNextTurn)
        {
            ClearWarning();
            willAttackNextTurn = false;
            GameUI.Instance?.ShowNotify($"⚠ {name} 공격!", 1.0f);

            if (IsInAttackRange(player))
            {
                Vector3 targetPos = BoardManager.Instance.GridToWorld(player.GridPos);
                EffectManager.Instance?.PlayAttack(targetPos);
                Attack(player);
            }
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
            if (tile != null && !tile.IsOccupied && !tile.IsWall) PlaceOnBoard(next2.Value);
        }
    }

    // ── 공격 예고 하이라이트 ──────────────────────────────────────────────
    public virtual void RefreshWarning()
    {
        ClearWarning();
        if (!willAttackNextTurn && !bossCharging) return;

        // 4방향 직선으로 attackRange칸까지 표시 (실제 공격 범위와 동일한 직선 방향)
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            for (int i = 1; i <= attackRange; i++)
            {
                Vector2Int pos = GridPos + dir * i;
                Tile tile = BoardManager.Instance.GetTile(pos);
                if (tile == null || tile.IsWall) break;
                tile.SetHighlight(Tile.HighlightType.Danger);
                warningTiles.Add(pos);
                if (tile.IsOccupied) break; // 유닛에 막히면 그 너머는 표시 안 함
            }
        }
    }

    public virtual void ClearWarning()
    {
        foreach (var pos in warningTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        warningTiles.Clear();
    }

    // ── 스프라이트 방향 ──────────────────────────────────────────────────
    /// <summary>
    /// 이동 방향에 따라 스프라이트를 좌우 반전한다.
    /// 상하 이동 시에는 마지막 수평 방향을 유지한다.
    /// </summary>
    protected void UpdateFacing(Vector2Int from, Vector2Int to)
    {
        int dx = to.x - from.x;
        if (dx == 0) return;                    // 상하 이동 → 마지막 방향 유지
        _facingDir = dx > 0 ? 1 : -1;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = (_facingDir < 0);
    }

    /// <summary>PlaceOnBoard 오버라이드 — 이동 후 방향 갱신</summary>
    public override void PlaceOnBoard(Vector2Int pos)
    {
        Vector2Int oldPos = GridPos;
        base.PlaceOnBoard(pos);
        if (_initialPlaced) UpdateFacing(oldPos, pos);
        else                _initialPlaced = true;
    }

    // ── BFS 경로탐색 ──────────────────────────────────────────────────────
    protected Vector2Int? BFSNextStep(Vector2Int from, Vector2Int to)
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
                if (tile.IsWall) continue;
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

    /// <summary>
    /// 여러 타일을 순서대로 부드럽게 이동하는 코루틴 (서브클래스 공용).
    /// 각 타일 이동 후 바닥 오브젝트를 발동함.
    /// </summary>
    protected IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        const float stepDuration = 0.12f;
        Vector2Int prevPos = GridPos;
        foreach (var pos in path)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            UpdateFacing(prevPos, pos);

            Vector3 startPos  = transform.position;
            Vector3 targetPos = BoardManager.Instance.GridToWorld(pos);

            BoardManager.Instance.GetTile(GridPos)?.ClearUnit();
            GridPos = pos;
            BoardManager.Instance.GetTile(pos)?.SetUnit(this);

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / stepDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos;
            prevPos = pos;

            // 바닥 오브젝트 발동
            FloorObjectManager.Instance?.OnUnitEnterTile(this, pos);
        }
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
