using UnityEngine;

/// <summary>
/// 슬라임 (노말) — INDEX 4
///
/// 기본 AI는 EnemyUnit과 동일.
/// 사망 시 소형슬라임 2마리로 분열.
/// </summary>
public class SlimeUnit : EnemyUnit
{
    protected override void Awake() { base.Awake(); }

    protected override void OnDeath()
    {
        // 분열 — 소형슬라임 2마리를 인접 빈 칸에 스폰
        SpawnSplitSlimes();
        base.OnDeath(); // ClearWarning + EnemyManager 해제 + SetActive(false)
    }

    private void SpawnSplitSlimes()
    {
        if (EnemySpawnManager.Instance == null) return;

        Vector2Int[] dirs =
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        int spawned = 0;
        foreach (var dir in dirs)
        {
            if (spawned >= 2) break;

            Vector2Int pos  = GridPos + dir;
            Tile        tile = BoardManager.Instance.GetTile(pos);
            if (tile == null || tile.IsOccupied || tile.IsWall) continue;

            EnemySpawnManager.Instance.SpawnEnemy(5, pos); // INDEX 5 = 소형슬라임
            spawned++;
        }

        if (spawned > 0)
            GameUI.Instance?.ShowNotify($"💧 {name} 분열!", 1.0f);
    }
}
