using UnityEngine;

/// <summary>
/// Q 스킬 — 원소포설: 원하는 빈 칸에 현재 원소를 바닥에 깔기
/// - 쿨타임 3턴 (행동 시 감소)
/// - 대상 타일: 비어 있는 아무 칸
/// - 한 칸에 원소 1개만 존재 (기존 원소 교체)
/// - 바닥 원소는 유닛이 밟는 순간 1회 발동 후 사라짐
///   불  → 화상(2데미지 × 2턴)
///   땅  → 스턴(1턴 이동 불가)
///   나무 → 속박(2턴 이동 불가)
///   물  → 전도(다음 원소 효과 2배)
///   전도 상태인 유닛이 밟으면 모든 효과 2배
/// </summary>
public class Skill_ElementPlace : SkillBase
{
    public override SkillPreviewType PreviewType  => SkillPreviewType.Teleport;

    /// <summary>Q 재입력 시 취소 (토글 방식). 확정은 클릭으로만.</summary>
    public override bool SameKeyConfirms => false;

    // 원소별 개별 쿨타임 (불=0, 땅=1, 나무=2, 물=3)
    private readonly int[] _elementCooldowns = new int[4];

    private ElementalPlayerUnit.Element CurrentElement =>
        GetComponent<ElementalPlayerUnit>()?.CurrentElement
        ?? ElementalPlayerUnit.Element.Fire;

    /// <summary>현재 원소의 쿨타임을 UI에 표시</summary>
    public override int DisplayCooldown => _elementCooldowns[(int)CurrentElement];

    private void Awake()
    {
        skillName   = "원소포설";
        description = "선택한 빈 칸에 현재 원소를 바닥에 깝니다. 쿨타임 3.";
        maxCooldown = 3;
    }

    public override bool CanUse()
    {
        if (CheatManager.Instance != null && CheatManager.Instance.ZeroCooldown) return true;
        return _elementCooldowns[(int)CurrentElement] <= 0;
    }

    protected override void ApplyCooldown()
    {
        _elementCooldowns[(int)CurrentElement] = maxCooldown;
    }

    /// <summary>행동 시 모든 원소 쿨타임 1씩 감소</summary>
    public override void ReduceCooldown(int amount)
    {
        for (int i = 0; i < _elementCooldowns.Length; i++)
            _elementCooldowns[i] = Mathf.Max(0, _elementCooldowns[i] - amount);
    }

    protected override bool OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        var elemental = caster as ElementalPlayerUnit;
        if (elemental == null) return false;

        // 대상 타일 유효성 검사 — 비어 있는 칸이어야 함 (벽·점유 불가)
        var tile = BoardManager.Instance.GetTile(targetPos);
        if (tile == null || tile.IsOccupied || tile.IsWall)
        {
            GameUI.Instance?.ShowNotify("빈 칸을 선택하세요", 0.7f);
            return false;
        }

        // 함정·하트 등 기존 바닥 오브젝트 위에는 설치 불가
        var existing = FloorObjectManager.Instance?.GetAt(targetPos);
        if (existing != null)
        {
            GameUI.Instance?.ShowNotify("이미 바닥 오브젝트가 있는 칸입니다", 0.7f);
            return false;
        }

        // ElementalPlayerUnit.Element → FloorObject.ElementType (값 동일)
        var elemType = (FloorObject.ElementType)(int)elemental.CurrentElement;
        FloorObjectManager.Instance?.SpawnElement(elemType, targetPos);

        string elemName = GetElementLabel(elemental.CurrentElement);
        GameUI.Instance?.ShowNotify(elemName + " 원소 포설!", 0.9f);
        Debug.Log($"[원소포설] {targetPos}에 {elemental.CurrentElement} 배치");
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
