using UnityEngine;

/// <summary>
/// E 스킬 — 원소변경: 불→땅→나무→물→불 순환
/// 예외: 행동 슬롯 소모 없음 (IsFree = true)
/// </summary>
public class Skill_ElementChange : SkillBase
{
    /// <summary>행동 소모 없는 자유 스킬</summary>
    public override bool IsFree => true;

    private void Awake()
    {
        skillName   = "원소변경";
        description = "원소를 불→땅→나무→물→불 순서로 순환합니다. 행동 소모 없음.";
        maxCooldown = 0;
    }

    protected override bool OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (caster is ElementalPlayerUnit elemental)
        {
            elemental.CycleElement();
            string elemLabel = GetElementLabel(elemental.CurrentElement);
            GameUI.Instance?.ShowNotify($"원소 변경: {elemLabel}", 0.8f);
        }
        return true;
    }

    private static string GetElementLabel(ElementalPlayerUnit.Element e)
    {
        if (e == ElementalPlayerUnit.Element.Fire)  return "불";
        if (e == ElementalPlayerUnit.Element.Earth) return "땅";
        if (e == ElementalPlayerUnit.Element.Wood)  return "나무";
        if (e == ElementalPlayerUnit.Element.Water) return "물";
        return "?";
    }
}
