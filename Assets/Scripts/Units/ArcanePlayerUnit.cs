using UnityEngine;

/// <summary>
/// 3스테이지 — 비전마법사
/// 평타: 직선 방향 금빛 투사체 (TryAttackToward가 처리)
/// 스킬1 [Q]: 순간이동 — 최대 3칸, 충전 최대 3회 (매 턴 자동 충전)
/// 스킬2 [E]: 마력공격 — 직선 방향, 2배 데미지
/// </summary>
public class ArcanePlayerUnit : PlayerUnit
{
    protected override Color AttackColor  => new Color(0.9f, 0.85f, 0.3f); // 금색
    protected override float AttackSpeed  => 14f;

    protected override void Awake()
    {
        base.Awake();
        attackRange = 999; // 사거리 무제한 (TryAttackToward가 방향 탐색 처리)
        skill1 = gameObject.AddComponent<Skill_Teleport>();
        skill2 = gameObject.AddComponent<Skill_ArcaneBlast>();
    }

    /// <summary>턴 시작: 순간이동 1회 자동 충전</summary>
    public override void StartTurn()
    {
        base.StartTurn();
        if (skill1 is Skill_Teleport teleport)
            teleport.RechargeOne();
    }

    /// <summary>비전 평타 — 투사체는 TryAttackToward가 발사, 여기선 데미지만 처리</summary>
    public override void Attack(Unit target)
    {
        if (target == null || !target.IsAlive) return;
        target.TakeDamage(attackDamage);
    }
}
