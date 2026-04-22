using UnityEngine;

/// <summary>
/// 플레이어 유닛 베이스
///
/// [턴 구조] 턴당 최대 2행동 — 이동 / 평타 / 스킬 각 1회씩 사용 가능
///   · 2행동 소진 → 자동 턴 종료
///   · 1행동 후 Space → 수동 턴 종료
/// </summary>
public abstract class PlayerUnit : Unit
{
    protected SkillBase skill1;
    protected SkillBase skill2;

    // ── 턴 행동 추적 ─────────────────────────────────────────────────────
    protected bool hasMovedThisTurn     = false;
    protected bool hasAttackedThisTurn  = false;
    protected bool hasUsedSkillThisTurn = false;

    /// <summary>이번 턴에 사용한 행동 수 (0~2)</summary>
    public int  ActionsUsed { get; protected set; } = 0;

    /// <summary>2행동 모두 소진 — 더 이상 입력 불가</summary>
    public bool HasActedThisTurn  => ActionsUsed >= 2;

    public bool CanMove     => !hasMovedThisTurn     && ActionsUsed < 2;
    public bool CanAttack   => !hasAttackedThisTurn  && ActionsUsed < 2;
    public bool CanUseSkill => !hasUsedSkillThisTurn && ActionsUsed < 2;

    /// <summary>투사체 색상 — 캐릭터별 오버라이드</summary>
    protected virtual Color AttackColor => Color.white;
    protected virtual float AttackSpeed => 12f;

    /// <summary>평타 도달 거리. 0 = 방향 직선 무제한, 1 = 인접 1칸 등</summary>
    public virtual int AttackReach => 0;

    // ── 턴 시작 시 행동 초기화 ───────────────────────────────────────────
    public virtual void StartTurn()
    {
        hasMovedThisTurn     = false;
        hasAttackedThisTurn  = false;
        hasUsedSkillThisTurn = false;
        ActionsUsed          = 0;
    }

    // ── 이동 ─────────────────────────────────────────────────────────────
    public bool TryMove(Vector2Int direction)
    {
        if (!CanMove) return false;

        // 이동봉쇄(스턴/속박) 상태 확인
        var seh = GetComponent<StatusEffectHandler>();
        if (seh != null && seh.IsImmobilized)
        {
            GameUI.Instance?.ShowNotify("이동 불가 상태!", 0.7f);
            return false;
        }

        Vector2Int target = GridPos + direction;
        Tile targetTile = BoardManager.Instance.GetTile(target);
        if (targetTile == null || targetTile.IsOccupied || targetTile.IsWall) return false;

        PlaceOnBoard(target);
        hasMovedThisTurn = true;
        ActionsUsed++;
        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        return true;
    }

    // ── 평타 — 방향 직선 발사, 항상 턴 소모 ─────────────────────────────
    public virtual bool TryAttackToward(Vector2Int targetPos)
    {
        if (!CanAttack) return false;

        Vector2Int dir   = GridUtil.SnapToCardinal(targetPos - GridPos);
        EnemyUnit  enemy = GridUtil.FindFirstEnemyInDir(GridPos, dir);

        Vector3 from = BoardManager.Instance.GridToWorld(GridPos);
        Vector3 to   = enemy != null
            ? BoardManager.Instance.GridToWorld(enemy.GridPos)
            : BoardManager.Instance.GridToWorld(GridUtil.GetFarEdge(GridPos, dir));

        EnemyUnit captured = enemy;
        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            if (captured != null && captured.IsAlive)
                Attack(captured);
            else
                EffectManager.Instance?.PlayAttack(to);
        });

        hasAttackedThisTurn = true;
        ActionsUsed++;
        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        return true;
    }

    // ── 스킬 ─────────────────────────────────────────────────────────────
    public bool TryUseSkill(int skillIndex, Vector2Int targetPos)
    {
        SkillBase skill = skillIndex == 1 ? skill1 : skill2;
        if (skill == null || !skill.CanUse()) return false;

        // 자유 스킬(E 원소변경 등): 행동 슬롯 소모 없이 즉시 사용
        if (skill.IsFree)
        {
            skill.Use(this, targetPos);
            return true;
        }

        if (!CanUseSkill) return false;

        skill.Use(this, targetPos);
        hasUsedSkillThisTurn = true;
        ActionsUsed++;
        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        return true;
    }

    public SkillBase GetSkill(int index) => index == 1 ? skill1 : skill2;

    protected override void OnHpChanged() => GameUI.Instance?.Refresh();
}
