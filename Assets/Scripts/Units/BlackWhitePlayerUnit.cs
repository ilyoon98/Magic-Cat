using UnityEngine;

/// <summary>
/// 2스테이지 — 흑백마법사
/// 평타: 현재 모드에 따라 효과 변경
///   흑마법 모드: 데미지 + 화상 DoT (3턴)
///   백마법 모드: 데미지 + 자신 HP+1 회복
/// 스킬1 [Q]: 흑마법 — 흑마법 모드 전환 (쿨타임 없음)
/// 스킬2 [E]: 백마법 — 백마법 모드 전환 + 즉시 HP+2 (쿨타임 4)
/// 모드 전환: 흑/백 동시 적용 불가 (덮어씌움)
/// </summary>
public class BlackWhitePlayerUnit : PlayerUnit
{
    public enum Mode { Black, White }

    public Mode CurrentMode { get; private set; } = Mode.White;

    protected override Color AttackColor => CurrentMode == Mode.Black
        ? new Color(0.5f, 0.2f, 0.9f)   // 흑마법 — 보라
        : new Color(0.9f, 0.9f, 1f);    // 백마법 — 흰빛

    protected override void Awake()
    {
        base.Awake();
        skill1 = gameObject.AddComponent<Skill_BlackMagic>();
        skill2 = gameObject.AddComponent<Skill_WhiteMagic>();
    }

    /// <summary>
    /// 모드에 따라 다른 평타 효과
    /// </summary>
    public override void Attack(Unit target)
    {
        if (target == null || !target.IsAlive) return;

        switch (CurrentMode)
        {
            case Mode.Black:
                // 흑마법: 데미지 + 화상 DoT
                EffectManager.Instance?.PlayFireHit(target.transform.position);
                target.TakeDamage(attackDamage);
                target.GetComponent<StatusEffectHandler>()?.ApplyBurn(1, 3);
                Debug.Log($"[흑마법] {target.name}에게 화상 3턴 적용");
                break;

            case Mode.White:
                // 백마법: 데미지 + 자신 회복
                EffectManager.Instance?.PlayWoodHit(target.transform.position);
                target.TakeDamage(attackDamage);
                Heal(1);
                Debug.Log($"[백마법] {target.name} 공격 + 자신 HP 회복");
                break;
        }
    }

    /// <summary>
    /// 모드 전환 (흑/백 동시 적용 불가)
    /// </summary>
    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        GameUI.Instance?.Refresh();
        Debug.Log($"[흑백마법사] 모드 전환 → {mode}");
    }
}
