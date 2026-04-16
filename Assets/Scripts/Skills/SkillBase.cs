using UnityEngine;

/// <summary>
/// 모든 스킬의 베이스 클래스
/// </summary>
public abstract class SkillBase : MonoBehaviour
{
    /// <summary>스킬 미리보기 타입 — PlayerInputController에서 확인용</summary>
    public enum SkillPreviewType { None, Directional, Teleport }
    /// <summary>기본값: 즉시 발동 (미리보기 없음). 방향/이동 스킬은 오버라이드</summary>
    public virtual SkillPreviewType PreviewType => SkillPreviewType.None;

    [Header("Skill Info")]
    public string skillName;
    [TextArea] public string description;
    public int maxCooldown;
    public int currentCooldown { get; private set; }

    public virtual bool CanUse()
    {
        // 치트: 쿨타임 0
        if (CheatManager.Instance != null && CheatManager.Instance.ZeroCooldown) return true;
        return currentCooldown <= 0;
    }

    public void Use(PlayerUnit caster, Vector2Int targetPos)
    {
        if (!CanUse()) return;
        OnUse(caster, targetPos);
        // 치트: 쿨타임 0이면 쿨타임 세팅 안 함
        if (CheatManager.Instance == null || !CheatManager.Instance.ZeroCooldown)
            currentCooldown = maxCooldown;
    }

    protected abstract void OnUse(PlayerUnit caster, Vector2Int targetPos);

    // 이동 또는 행동 시 1 감소
    public void ReduceCooldown(int amount)
    {
        currentCooldown = Mathf.Max(0, currentCooldown - amount);
    }

    // 턴 시작 시 호출 (현재는 이동/행동 기반이라 별도 처리)
    public void TickCooldown() { /* 기획서: 이동 or 행동 시 감소 */ }
}
