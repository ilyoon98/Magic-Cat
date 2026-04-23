using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Goblin — 스피더 (최대 2칸 이동)
///
/// 핵심 동작:
///   - 경로 계산 후 최종 목적지를 타일에 즉시 선점
///     → 같은 턴에 다른 Goblin이 경로를 계획할 때 목적지가 이미 점유된 것으로 보여
///       겹치는 현상 방지
///   - 시각적 이동은 별도 코루틴(VisualMoveAlongPath)이 처리
/// </summary>
public class GoblinUnit : EnemyUnit
{
    protected override void Awake() { base.Awake(); }

    public override void ExecuteTurn(PlayerUnit player)
    {
        if (!IsAlive || player == null) return;

        // ① 공격 준비 완료 → 공격 실행
        if (willAttackNextTurn)
        {
            ClearWarning();
            willAttackNextTurn = false;
            GameUI.Instance?.ShowNotify($"⚠ {name} 공격!", 1.0f);

            if (IsInAttackRange(player))
            {
                Vector3 targetPos = BoardManager.Instance.GridToWorld(player.GridPos);
                EffectManager.Instance?.PlayAttack(targetPos);
                Attack(player);
            }
            return;
        }

        // ② 플레이어가 사거리 안 → 공격 준비 (이동 없음)
        if (IsInAttackRange(player))
        {
            willAttackNextTurn = true;
            return;
        }

        // ③ 플레이어가 사거리 밖 → 최대 2칸 이동
        var path = ComputeMovePath(player, maxSteps: 2);
        if (path.Count == 0) return;

        // ── 최종 목적지를 즉시 선점 ─────────────────────────────────────
        // MoveAlongPath는 코루틴이므로 첫 yield 전 step1만 선점된다.
        // 그 사이 다른 Goblin이 step2(최종 목적지)를 빈 칸으로 보고 같은 위치로
        // 이동하는 겹침 버그를 막기 위해, 논리 위치를 먼저 최종 목적지로 확정한다.
        Vector2Int originalPos = GridPos;               // 방향 계산을 위해 저장
        Vector2Int dest = path[path.Count - 1];
        BoardManager.Instance.GetTile(GridPos)?.ClearUnit();
        GridPos = dest;
        BoardManager.Instance.GetTile(dest)?.SetUnit(this);

        // ── 시각적 이동 코루틴 ─────────────────────────────────────────
        StartCoroutine(VisualMoveAlongPath(path, originalPos));
    }

    // ── 최대 N칸 이동 경로 계산 (타일 상태 부작용 없음) ─────────────────

    private List<Vector2Int> ComputeMovePath(PlayerUnit player, int maxSteps)
    {
        var path = new List<Vector2Int>();
        Vector2Int current = GridPos;

        for (int step = 0; step < maxSteps; step++)
        {
            Vector2Int? next = BFSNextStep(current, player.GridPos);
            if (next == null) break;

            Tile t = BoardManager.Instance.GetTile(next.Value);
            if (t == null || t.IsWall) break;

            // 플레이어 칸은 점령 불가
            if (next.Value == player.GridPos) break;
            if (t.IsOccupied) break;

            path.Add(next.Value);

            // 다음 스텝 BFS를 위해 타일 상태 임시 반영
            BoardManager.Instance.GetTile(current)?.ClearUnit();
            BoardManager.Instance.GetTile(next.Value)?.SetUnit(this);
            current = next.Value;
        }

        // 임시 반영 원복 (실제 선점은 ExecuteTurn에서 처리)
        if (path.Count > 0)
        {
            BoardManager.Instance.GetTile(current)?.ClearUnit();
            BoardManager.Instance.GetTile(GridPos)?.SetUnit(this);
        }

        return path;
    }

    // ── 시각적 이동만 담당하는 코루틴 ────────────────────────────────────
    // 논리 위치(GridPos)와 타일 점유는 ExecuteTurn에서 이미 확정됨.
    // 이 코루틴은 transform.position만 부드럽게 이동시키며, 방향도 갱신한다.

    private IEnumerator VisualMoveAlongPath(List<Vector2Int> path, Vector2Int startGridPos)
    {
        const float stepDuration = 0.12f;
        Vector2Int prevPos = startGridPos;
        foreach (var pos in path)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            UpdateFacing(prevPos, pos);

            Vector3 startPos  = transform.position;
            Vector3 targetPos = BoardManager.Instance.GridToWorld(pos);

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / stepDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos;
            prevPos = pos;
        }

        // 최종 위치(이미 GridPos에 확정됨)에서 바닥 오브젝트 발동
        FloorObjectManager.Instance?.OnUnitEnterTile(this, GridPos);
    }
}
