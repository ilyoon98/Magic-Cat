using UnityEngine;

/// <summary>
/// 흑마법 — 흑마법 모드로 전환 (쿨타임 없음, 행동 소모)
/// 이후 평타: 데미지 + 화상 DoT
/// </summary>
public class Skill_BlackMagic : SkillBase
{
    private void Awake()
    {
        skillName   = "흑마법";
        description = "흑마법 모드 전환: 평타가 화상 DoT 공격으로 변경";
        maxCooldown = 0;
    }

    protected override bool OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (caster is BlackWhitePlayerUnit bw)
            bw.SetMode(BlackWhitePlayerUnit.Mode.Black);

        // 전환 이펙트 (어두운 불꽃)
        EffectManager.Instance?.PlayFireHit(caster.transform.position);
        GameUI.Instance?.ShowNotify("⬛ 흑마법 모드", 1.0f);
        return true;
    }
}
