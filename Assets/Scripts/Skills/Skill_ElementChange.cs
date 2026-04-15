using UnityEngine;

/// <summary>
/// 스킬1 — 원소변경: 평타 속성을 불→땅→나무→물→불 순환
/// 쿨타임 없음 (행동 슬롯 소모)
/// </summary>
public class Skill_ElementChange : SkillBase
{
    private void Awake()
    {
        skillName = "원소변경";
        description = "평타 속성을 불→땅→나무→물→불 순서로 순환합니다.";
        maxCooldown = 0;
    }

    protected override void OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (caster is ElementalPlayerUnit elemental)
            elemental.CycleElement();
    }
}
