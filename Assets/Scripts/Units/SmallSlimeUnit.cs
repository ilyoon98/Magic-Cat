using UnityEngine;

/// <summary>
/// 소형슬라임 — INDEX 5
///
/// 슬라임(INDEX 4) 사망 시 분열 생성되는 소형 유닛.
/// 분열 없음. 기본 EnemyUnit AI 그대로 사용.
/// </summary>
public class SmallSlimeUnit : EnemyUnit
{
    protected override void Awake() { base.Awake(); }

    // 분열 없이 일반 사망 처리만
    protected override void OnDeath()
    {
        base.OnDeath();
    }
}
