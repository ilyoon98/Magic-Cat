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

    /// <summary>true이면 행동 슬롯을 소모하지 않음 (E 원소변경 등 예외 스킬)</summary>
    public virtual bool IsFree => false;

    /// <summary>
    /// true  : 스킬 미리보기 중 같은 키 재입력 시 확정 (기본값)
    /// false : 같은 키 재입력 시 취소 — Q 원소포설처럼 토글 방식인 경우
    /// </summary>
    public virtual bool SameKeyConfirms => true;

    [Header("Skill Info")]
    public string skillName;
    [TextArea] public string description;
    public int maxCooldown;
    public int currentCooldown { get; protected set; }

    /// <summary>UI에 표시할 쿨다운 값. 원소별 개별 쿨타임 스킬에서 오버라이드.</summary>
    public virtual int DisplayCooldown => currentCooldown;

    public virtual bool CanUse()
    {
        // 치트: 쿨타임 0
        if (CheatManager.Instance != null && CheatManager.Instance.ZeroCooldown) return true;
        return currentCooldown <= 0;
    }

    /// <summary>
    /// 스킬 사용 시도. OnUse가 false를 반환하면 쿨타임·행동 소모 없이 실패 처리.
    /// </summary>
    public bool Use(PlayerUnit caster, Vector2Int targetPos)
    {
        if (!CanUse()) return false;
        bool success = OnUse(caster, targetPos);
        if (success && (CheatManager.Instance == null || !CheatManager.Instance.ZeroCooldown))
            ApplyCooldown();
        return success;
    }

    /// <summary>Use() 성공 후 쿨타임 적용. 원소별 개별 쿨타임 스킬에서 오버라이드.</summary>
    protected virtual void ApplyCooldown()
    {
        currentCooldown = maxCooldown;
    }

    /// <summary>
    /// 스킬 실제 동작. true = 성공(행동 소모), false = 실패(행동 소모 없음).
    /// </summary>
    protected abstract bool OnUse(PlayerUnit caster, Vector2Int targetPos);

    // 이동 또는 행동 시 1 감소
    public virtual void ReduceCooldown(int amount)
    {
        currentCooldown = Mathf.Max(0, currentCooldown - amount);
    }

    // 턴 시작 시 호출 (현재는 이동/행동 기반이라 별도 처리)
    public void TickCooldown() { /* 기획서: 이동 or 행동 시 감소 */ }
}
