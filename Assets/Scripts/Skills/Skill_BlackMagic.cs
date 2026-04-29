using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Q — 흑마법
///   흑모드 전환 + 직선 관통 데미지 (모든 적 관통)
///   강화 (흑 게이지 100%) : 좌우 1칸 추가 관통 + 데미지 증가
///   쿨타임: 3턴
///   게이지 충전: 흑 +50
/// </summary>
public class Skill_BlackMagic : SkillBase
{
    public override SkillPreviewType PreviewType => SkillPreviewType.Directional;

    private void Awake()
    {
        skillName   = "흑마법";
        description = "흑모드 전환 + 직선 관통 공격 (강화 시 범위 확장)";
        maxCooldown = 2;
    }

    protected override bool OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (!(caster is BlackWhitePlayerUnit bw)) return false;

        // 1. 흑마법 모드 전환
        bw.SetMode(BlackWhitePlayerUnit.Mode.Black);

        // 2. 강화 여부 확인
        bool empowered = bw.BlackEmpowered;

        // 3. 방향 결정
        Vector2Int dir = GridUtil.SnapToCardinal(targetPos - caster.GridPos);

        if (empowered)
        {
            // 강화: 직선 + 좌우 1칸 (3줄 관통)
            FirePenetratingLine(caster, dir, damage: caster.attackDamage * 2);
            Vector2Int perpA = new Vector2Int(-dir.y, dir.x);
            Vector2Int perpB = new Vector2Int(dir.y, -dir.x);
            FirePenetratingLine(caster, dir, perpOffset: perpA, damage: caster.attackDamage * 2);
            FirePenetratingLine(caster, dir, perpOffset: perpB, damage: caster.attackDamage * 2);

            GameUI.Instance?.ShowNotify("⬛ 흑마법 강화 — 광폭 관통!", 1.5f);
            bw.OnEmpoweredSkillUsed(BlackWhitePlayerUnit.Mode.Black);
        }
        else
        {
            // 일반: 직선 관통
            FirePenetratingLine(caster, dir, damage: caster.attackDamage);
            GameUI.Instance?.ShowNotify("⬛ 흑마법 — 직선 관통!", 1.0f);
            bw.MarkSkillUsed(BlackWhitePlayerUnit.Mode.Black);
        }

        // 4. 게이지 충전 (강화가 아닐 때만)
        if (!empowered)
            bw.AddGauge(BlackWhitePlayerUnit.Mode.Black, 50f);

        EffectManager.Instance?.PlayFireHit(caster.transform.position);
        return true;
    }

    /// <summary>
    /// 지정 방향으로 관통 발사.
    /// perpOffset이 있으면 발사 시작점을 1칸 옆으로 이동 (강화 보조선).
    /// </summary>
    private void FirePenetratingLine(PlayerUnit caster, Vector2Int dir,
                                     Vector2Int perpOffset = default,
                                     int damage = 1)
    {
        Vector2Int startGrid = caster.GridPos + perpOffset;
        Vector3    from      = BoardManager.Instance.GridToWorld(startGrid);
        Vector3    to        = BoardManager.Instance.GridToWorld(
                                   GridUtil.GetFarEdge(startGrid, dir));

        // 직선 위 모든 적 수집 후 관통 데미지
        var targets = CollectEnemiesInLine(startGrid, dir);

        SkillProjectile.Fire(from, to, new Color(0.4f, 0.1f, 0.7f), speed: 18f, onHit: () =>
        {
            foreach (var enemy in targets)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                enemy.TakeDamage(damage, isCritical: targets.Count == 1);
                EffectManager.Instance?.PlayFireHit(enemy.transform.position);
            }
        });
    }

    private static List<EnemyUnit> CollectEnemiesInLine(Vector2Int start, Vector2Int dir)
    {
        var list = new List<EnemyUnit>();
        Vector2Int cur = start + dir;
        const int max  = 32;

        for (int i = 0; i < max; i++)
        {
            Tile tile = BoardManager.Instance.GetTile(cur);
            if (tile == null || tile.IsWall) break;

            if (tile.OccupiedUnit is EnemyUnit e)
                list.Add(e);

            cur += dir;
        }
        return list;
    }
}
