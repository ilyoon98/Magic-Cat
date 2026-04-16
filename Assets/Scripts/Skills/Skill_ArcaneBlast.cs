using UnityEngine;

/// <summary>
/// 마력공격 — 마우스 방향을 4방향으로 스냅 후 직선 발사, 2배 데미지 (쿨타임 없음)
/// </summary>
public class Skill_ArcaneBlast : SkillBase
{
    public override SkillPreviewType PreviewType => SkillPreviewType.Directional;

    private void Awake()
    {
        skillName   = "마력공격";
        description = "직선 방향 강력한 비전 공격 (2배 데미지, 쿨타임 없음)";
        maxCooldown = 0;
    }

    protected override void OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        // 1. 4방향 스냅
        Vector2Int dir = GridUtil.SnapToCardinal(targetPos - caster.GridPos);

        // 2. 직선 첫 번째 적 탐색
        EnemyUnit capturedTarget = GridUtil.FindFirstEnemyInDir(caster.GridPos, dir);

        // 3. 투사체 목표
        Vector3 from = BoardManager.Instance.GridToWorld(caster.GridPos);
        Vector3 to   = capturedTarget != null
            ? BoardManager.Instance.GridToWorld(capturedTarget.GridPos)
            : BoardManager.Instance.GridToWorld(GridUtil.GetFarEdge(caster.GridPos, dir));

        int dmg = caster.attackDamage * 2;

        SkillProjectile.Fire(from, to, new Color(0.9f, 0.85f, 0.3f), speed: 16f, onHit: () =>
        {
            EffectManager.Instance?.PlayExplosion(to);
            if (capturedTarget != null && capturedTarget.IsAlive)
                capturedTarget.TakeDamage(dmg, isCritical: true);
        });
    }
}
