using UnityEngine;

/// <summary>
/// 백마법 — 백마법 모드 전환 + 즉시 회복 (쿨타임 4)
/// 이후 평타: 데미지 + 자신 회복
/// </summary>
public class Skill_WhiteMagic : SkillBase
{
    private void Awake()
    {
        skillName   = "백마법";
        description = "백마법 모드 전환: 평타가 회복 공격으로 변경 + 즉시 HP+2";
        maxCooldown = 4;
    }

    protected override void OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (caster is BlackWhitePlayerUnit bw)
            bw.SetMode(BlackWhitePlayerUnit.Mode.White);

        // 즉시 회복
        caster.Heal(2);

        // 전환 이펙트 (녹색 빛)
        EffectManager.Instance?.PlayWoodHit(caster.transform.position);
        GameUI.Instance?.ShowNotify("⬜ 백마법 모드 + HP 회복", 1.0f);
    }
}
