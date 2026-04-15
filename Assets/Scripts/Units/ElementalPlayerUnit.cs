using UnityEngine;

/// <summary>
/// 1스테이지 — 원소마법사
/// 평타: 현재 원소 속성으로 1칸 공격
/// 스킬1: 원소변경 (불→땅→나무→물→불)
/// 스킬2: 원소집중 (강력한 단일 공격, 쿨타임 김)
/// </summary>
public class ElementalPlayerUnit : PlayerUnit
{
    public enum Element { Fire, Earth, Wood, Water }

    public Element CurrentElement { get; private set; } = Element.Fire;

    // 원소별 투사체 색상
    protected override Color AttackColor => CurrentElement switch
    {
        Element.Fire  => new Color(1f,  0.4f, 0.1f),
        Element.Earth => new Color(0.9f,0.7f, 0.1f),
        Element.Wood  => new Color(0.2f,0.85f,0.2f),
        Element.Water => new Color(0.2f,0.6f, 1f),
        _             => Color.white
    };

    protected override void Awake()
    {
        base.Awake();
        // 스킬 컴포넌트 연결
        skill1 = gameObject.AddComponent<Skill_ElementChange>();
        skill2 = gameObject.AddComponent<Skill_ElementFocus>();
    }

    /// <summary>
    /// 원소 속성 평타 — 원소에 따라 추가 효과 적용
    /// </summary>
    public override void Attack(Unit target)
    {
        if (target == null || !target.IsAlive) return;

        // 원소별 이펙트
        var fx = EffectManager.Instance;
        if (fx != null)
        {
            switch (CurrentElement)
            {
                case Element.Fire:  fx.PlayFireHit(target.transform.position);  break;
                case Element.Earth: fx.PlayEarthHit(target.transform.position); break;
                case Element.Wood:  fx.PlayWoodHit(target.transform.position);  break;
                case Element.Water: fx.PlayWaterHit(target.transform.position); break;
            }
        }

        target.TakeDamage(GetElementalDamage());
        ApplyElementEffect(target);
    }

    private int GetElementalDamage()
    {
        return CurrentElement switch
        {
            Element.Fire  => attackDamage - 1, // 낮음
            Element.Earth => attackDamage + 2, // 높음
            Element.Wood  => attackDamage - 1, // 낮음
            Element.Water => attackDamage,     // 보통
            _             => attackDamage
        };
    }

    private void ApplyElementEffect(Unit target)
    {
        switch (CurrentElement)
        {
            case Element.Fire:
                // 지속 화상 (DoT)
                target.GetComponent<StatusEffectHandler>()?.ApplyBurn(2, 3); // 2데미지 3턴
                Debug.Log($"[원소] 화상 적용!");
                break;
            case Element.Earth:
                // 추가 효과 없음 (높은 데미지만)
                Debug.Log($"[원소] 대지 공격 (고데미지)");
                break;
            case Element.Wood:
                // HP 회복
                Heal(2);
                Debug.Log($"[원소] 나무 공격 + HP 회복");
                break;
            case Element.Water:
                // 적 이동 봉쇄 1턴
                target.GetComponent<StatusEffectHandler>()?.ApplyImmobilize(1);
                Debug.Log($"[원소] 물 공격 + 이동 봉쇄");
                break;
        }
    }

    public void CycleElement()
    {
        CurrentElement = CurrentElement switch
        {
            Element.Fire  => Element.Earth,
            Element.Earth => Element.Wood,
            Element.Wood  => Element.Water,
            Element.Water => Element.Fire,
            _             => Element.Fire
        };
        Debug.Log($"[원소변경] 현재 원소: {CurrentElement}");
    }
}
