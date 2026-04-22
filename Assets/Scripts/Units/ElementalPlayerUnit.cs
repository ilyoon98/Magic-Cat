using UnityEngine;

/// <summary>
/// 1스테이지 — 원소마법사
///
/// [행동 구조]
///   평타: 현재 원소 속성으로 직선 발사. 대상 없어도 공격 발사.
///   Q   : 원소포설 — 원하는 빈 칸에 원소를 깔기. 쿨타임 3. (Q 재입력으로 취소)
///   E   : 원소변경 — 불→땅→나무→물→불 순환. 행동 소모 없음(예외)
///
/// [원소 포설 효과] — 유닛이 밟는 순간 발동 후 사라짐
///   불  → 화상(DoT 2턴)
///   땅  → 스턴(1턴 이동 불가)
///   나무 → 속박(2턴 이동 불가)
///   물  → 전도(다음 원소 효과 2배)
/// </summary>
public class ElementalPlayerUnit : PlayerUnit
{
    public enum Element { Fire, Earth, Wood, Water }

    public Element CurrentElement { get; private set; } = Element.Fire;

    // 투사체 색상 — 원소별
    protected override Color AttackColor => CurrentElement switch
    {
        Element.Fire  => new Color(1f,  0.4f, 0.1f),
        Element.Earth => new Color(0.9f,0.7f, 0.1f),
        Element.Wood  => new Color(0.2f,0.85f,0.2f),
        Element.Water => new Color(0.2f,0.6f, 1f),
        _             => Color.white
    };

    protected override float AttackSpeed => 16f;

    protected override void Awake()
    {
        base.Awake();
        skill1 = gameObject.AddComponent<Skill_ElementPlace>();   // Q
        skill2 = gameObject.AddComponent<Skill_ElementChange>();  // E (free)
    }

    // ── 평타 데미지/이펙트 — 원소 비주얼 적용 (상태이상은 바닥 원소로) ──────
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

        target.TakeDamage(attackDamage);
    }

    // ── 원소 순환 ─────────────────────────────────────────────────────────
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
