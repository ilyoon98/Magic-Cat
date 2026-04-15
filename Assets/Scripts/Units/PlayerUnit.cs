using UnityEngine;

/// <summary>
/// 플레이어 유닛 베이스
/// 턴당 행동 1개 (이동 OR 공격 OR 스킬)
/// 공격: 클릭 방향을 4방향으로 스냅 → 직선으로 투사체 발사 (사거리 무제한)
///        적이 없어도 발사되며 턴 종료
/// </summary>
public abstract class PlayerUnit : Unit
{
    protected SkillBase skill1;
    protected SkillBase skill2;

    public bool HasActedThisTurn { get; private set; }

    /// <summary>투사체 색상 — 캐릭터별 오버라이드</summary>
    protected virtual Color AttackColor => Color.white;
    protected virtual float AttackSpeed => 12f;

    public virtual void StartTurn()
    {
        HasActedThisTurn = false;
    }

    // ── 이동 ─────────────────────────────────────────────────────────────
    public bool TryMove(Vector2Int direction)
    {
        if (HasActedThisTurn) return false;

        Vector2Int target = GridPos + direction;
        Tile targetTile = BoardManager.Instance.GetTile(target);
        if (targetTile == null || targetTile.IsOccupied) return false;

        PlaceOnBoard(target);
        HasActedThisTurn = true;

        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        return true;
    }

    // ── 공격 — 방향 직선 발사, 사거리 무제한, 항상 턴 소모 ─────────────
    public bool TryAttackToward(Vector2Int targetPos)
    {
        if (HasActedThisTurn) return false;

        // 1. 클릭 방향을 4방향으로 스냅
        Vector2Int dir = GridUtil.SnapToCardinal(targetPos - GridPos);

        // 2. 해당 방향 직선 위 첫 번째 적 탐색
        EnemyUnit enemy = GridUtil.FindFirstEnemyInDir(GridPos, dir);

        // 3. 투사체 목표 좌표 결정
        Vector3 from = BoardManager.Instance.GridToWorld(GridPos);
        Vector3 to   = enemy != null
            ? BoardManager.Instance.GridToWorld(enemy.GridPos)
            : BoardManager.Instance.GridToWorld(GridUtil.GetFarEdge(GridPos, dir));

        // 4. 투사체 발사 (항상)
        EnemyUnit captured = enemy;
        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            if (captured != null && captured.IsAlive)
                Attack(captured);              // 캐릭터별 공격 효과 + 데미지
            else
                EffectManager.Instance?.PlayAttack(to); // 빈 공간 히트 이펙트
        });

        HasActedThisTurn = true;
        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        return true;
    }

    // ── 스킬 ─────────────────────────────────────────────────────────────
    public bool TryUseSkill(int skillIndex, Vector2Int targetPos)
    {
        if (HasActedThisTurn) return false;

        SkillBase skill = skillIndex == 1 ? skill1 : skill2;
        if (skill == null || !skill.CanUse()) return false;

        skill.Use(this, targetPos);
        HasActedThisTurn = true;

        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        return true;
    }

    public SkillBase GetSkill(int index) => index == 1 ? skill1 : skill2;

    protected override void OnHpChanged() => GameUI.Instance?.Refresh();
}
