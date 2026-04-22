using UnityEngine;

/// <summary>
/// 상태이상 관리
///   화상(DoT)  — ApplyBurn(damage, turns)
///   스턴       — ApplyStun()       : 1턴 이동 불가
///   속박       — ApplyBind()       : 2턴 이동 불가
///   전도       — ApplyConductive() : 다음 원소 바닥 효과 2배 (1회 소비)
/// </summary>
public class StatusEffectHandler : MonoBehaviour
{
    private Unit unit;

    // 화상 (DoT)
    private int burnDamage;
    private int burnTurns;

    // 이동봉쇄 (스턴 / 속박 공통)
    private int immobilizeTurns;
    public bool IsImmobilized => immobilizeTurns > 0;

    // 전도 (1회용 — 다음 바닥 원소 효과 2배)
    private bool conductive;
    public bool IsConductive => conductive;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    // ── 화상 ──────────────────────────────────────────────────────────────
    public void ApplyBurn(int damagePerTurn, int turns)
    {
        burnDamage = damagePerTurn;
        burnTurns  = Mathf.Max(burnTurns, turns);
    }

    // ── 이동봉쇄 (내부 공통) ──────────────────────────────────────────────
    public void ApplyImmobilize(int turns)
    {
        immobilizeTurns = Mathf.Max(immobilizeTurns, turns);
        Debug.Log($"[상태이상] {unit.name} 이동봉쇄 {immobilizeTurns}턴");
    }

    /// <summary>스턴: 1턴 이동 불가</summary>
    public void ApplyStun() => ApplyImmobilize(1);

    /// <summary>속박: 2턴 이동 불가</summary>
    public void ApplyBind() => ApplyImmobilize(2);

    // ── 전도 ──────────────────────────────────────────────────────────────
    /// <summary>전도 적용 — 다음 원소 바닥 효과가 2배</summary>
    public void ApplyConductive()
    {
        conductive = true;
        Debug.Log($"[상태이상] {unit.name} 전도 적용");
    }

    /// <summary>전도 소비 — 발동 시 호출. 전도 상태였으면 true 반환 후 해제</summary>
    public bool ConsumeConductive()
    {
        if (!conductive) return false;
        conductive = false;
        Debug.Log($"[상태이상] {unit.name} 전도 소비 (2배 효과)");
        return true;
    }

    // ── 매 턴 시작 처리 ───────────────────────────────────────────────────
    public void TickEffects()
    {
        if (burnTurns > 0)
        {
            unit.TakeDamage(burnDamage);
            burnTurns--;
            Debug.Log($"[화상] {unit.name} {burnDamage} 화상 데미지, 남은 턴: {burnTurns}");
        }

        if (immobilizeTurns > 0)
            immobilizeTurns--;
    }

    public bool HasAnyEffect() => burnTurns > 0 || immobilizeTurns > 0 || conductive;
}
