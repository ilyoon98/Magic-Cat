using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 그림자 손 (스피더) — INDEX 8
///
/// GoblinUnit과 동일하게 최대 2칸 이동.
/// 공격 준비·공격은 기본 EnemyUnit AI 사용.
/// </summary>
public class ShadowHandUnit : EnemyUnit
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

        // ② 플레이어가 사거리 안 → 공격 준비
        if (IsInAttackRange(player))
        {
            willAttackNextTurn = true;
            return;
        }

        // ③ 사거리 밖 → 최대 2칸 이동 (스피더)
        var path = ComputeMovePath(player, maxSteps: 2);
        if (path.Count == 0) return;

        Vector2Int originalPos = GridPos;
        Vector2Int dest        = path[path.Count - 1];

        BoardManager.Instance.GetTile(GridPos)?.ClearUnit();
        GridPos = dest;
        BoardManager.Instance.GetTile(dest)?.SetUnit(this);

        StartCoroutine(VisualMove(path, originalPos));
    }

    // ── 경로 계산 (타일 임시 반영 후 원복) ─────────────────────────────────
    private List<Vector2Int> ComputeMovePath(PlayerUnit player, int maxSteps)
    {
        var path    = new List<Vector2Int>();
        Vector2Int cur = GridPos;

        for (int step = 0; step < maxSteps; step++)
        {
            Vector2Int? next = BFSNextStep(cur, player.GridPos);
            if (next == null) break;

            Tile t = BoardManager.Instance.GetTile(next.Value);
            if (t == null || t.IsWall) break;
            if (next.Value == player.GridPos) break;
            if (t.IsOccupied) break;

            path.Add(next.Value);

            BoardManager.Instance.GetTile(cur)?.ClearUnit();
            BoardManager.Instance.GetTile(next.Value)?.SetUnit(this);
            cur = next.Value;
        }

        // 임시 반영 원복
        if (path.Count > 0)
        {
            BoardManager.Instance.GetTile(cur)?.ClearUnit();
            BoardManager.Instance.GetTile(GridPos)?.SetUnit(this);
        }
        return path;
    }

    // ── 시각적 이동 코루틴 ────────────────────────────────────────────────
    private IEnumerator VisualMove(List<Vector2Int> path, Vector2Int startPos)
    {
        const float stepDuration = 0.10f; // 스피더 — 고블린보다 약간 빠르게
        Vector2Int prev = startPos;

        foreach (var pos in path)
        {
            if (this == null || !gameObject.activeInHierarchy) yield break;

            UpdateFacing(prev, pos);

            Vector3 from = transform.position;
            Vector3 to   = BoardManager.Instance.GridToWorld(pos);

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                if (this == null || !gameObject.activeInHierarchy) yield break;
                transform.position = Vector3.Lerp(from, to, elapsed / stepDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = to;
            prev = pos;
        }

        FloorObjectManager.Instance?.OnUnitEnterTile(this, GridPos);
    }
}
