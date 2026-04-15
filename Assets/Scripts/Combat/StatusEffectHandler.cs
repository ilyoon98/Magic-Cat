using UnityEngine;

/// <summary>
/// 상태이상 관리: 화상(DoT), 이동봉쇄
/// </summary>
public class StatusEffectHandler : MonoBehaviour
{
    private Unit unit;

    // 화상
    private int burnDamage;
    private int burnTurns;

    // 이동봉쇄
    private int immobilizeTurns;
    public bool IsImmobilized => immobilizeTurns > 0;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public void ApplyBurn(int damagePerTurn, int turns)
    {
        burnDamage = damagePerTurn;
        burnTurns = Mathf.Max(burnTurns, turns); // 가장 긴 지속시간 유지
    }

    public void ApplyImmobilize(int turns)
    {
        immobilizeTurns = Mathf.Max(immobilizeTurns, turns);
        Debug.Log($"[상태이상] {unit.name} 이동봉쇄 {immobilizeTurns}턴");
    }

    /// <summary>
    /// 매 턴 시작 시 호출
    /// </summary>
    public void TickEffects()
    {
        // 화상 처리
        if (burnTurns > 0)
        {
            unit.TakeDamage(burnDamage);
            burnTurns--;
            Debug.Log($"[화상] {unit.name} {burnDamage} 화상 데미지, 남은 턴: {burnTurns}");
        }

        // 이동봉쇄 감소
        if (immobilizeTurns > 0)
        {
            immobilizeTurns--;
        }
    }

    public bool HasAnyEffect() => burnTurns > 0 || immobilizeTurns > 0;
}
