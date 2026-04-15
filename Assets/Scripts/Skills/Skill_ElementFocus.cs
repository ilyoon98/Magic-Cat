using UnityEngine;

/// <summary>
/// 스킬2 — 원소집중: 마우스 방향을 4방향으로 스냅 후 직선 발사
/// 첫 번째 적 명중 시 원소 속성 크리티컬 데미지
/// </summary>
public class Skill_ElementFocus : SkillBase
{
    private int damageMultiplier = 3;

    private void Awake()
    {
        skillName   = "원소집중";
        description = "원소 에너지를 직선으로 집중 발사. 사거리 무제한.";
        maxCooldown = 4;
    }

    protected override void OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        // 1. 클릭 방향을 4방향으로 스냅
        Vector2Int dir = GridUtil.SnapToCardinal(targetPos - caster.GridPos);

        // 2. 직선 위 첫 번째 적 탐색
        EnemyUnit capturedTarget = GridUtil.FindFirstEnemyInDir(caster.GridPos, dir);

        // 3. 투사체 목표 결정
        Vector3 from = BoardManager.Instance.GridToWorld(caster.GridPos);
        Vector3 to   = capturedTarget != null
            ? BoardManager.Instance.GridToWorld(capturedTarget.GridPos)
            : BoardManager.Instance.GridToWorld(GridUtil.GetFarEdge(caster.GridPos, dir));

        int damage = caster.attackDamage * damageMultiplier;

        SkillProjectile.Fire(from, to, new Color(1f, 0.7f, 0.1f), speed: 14f, onHit: () =>
        {
            EffectManager.Instance?.PlayExplosion(to);
            if (capturedTarget != null && capturedTarget.IsAlive)
            {
                string tName = capturedTarget.name;
                capturedTarget.TakeDamage(damage, isCritical: true);
                Debug.Log($"[원소집중] {tName}에게 {damage} 크리티컬!");
            }
        });
    }
}
