using UnityEngine;

/// <summary>
/// 아이볼 (레인저) — INDEX 7
///
/// 원거리 AI. attackRange = 3 (Enemy.csv 기준).
///
/// [AI 행동 우선순위]
///   1. 공격 준비 완료 → 공격 실행
///   2. 플레이어가 attackRange 안 + 시야(직선) 확보 → 공격 준비
///   3. 플레이어가 사거리 밖 → BFS 1칸 이동
///
/// 기본 EnemyUnit AI와 동일하지만,
/// IsInAttackRange가 이미 직선 시야 체크를 포함하므로 그대로 상속.
/// attackRange를 CSV에서 3으로 설정하면 2~3칸 커버.
/// </summary>
public class EyeballUnit : EnemyUnit
{
    protected override void Awake() { base.Awake(); }

    // 기본 ExecuteNormalTurn 그대로 사용
    // attackRange=3이 CSV에서 설정되므로 별도 override 불필요
}
