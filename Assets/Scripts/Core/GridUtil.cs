using UnityEngine;

/// <summary>
/// 그리드 계산 공용 유틸 (방향 스냅, 직선 탐색, 보드 끝 좌표)
/// </summary>
public static class GridUtil
{
    /// <summary>
    /// 임의 벡터를 4방향(상하좌우) 중 가장 가까운 방향으로 스냅
    /// </summary>
    public static Vector2Int SnapToCardinal(Vector2Int delta)
    {
        if (delta == Vector2Int.zero) return Vector2Int.right;
        return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, delta.y > 0 ? 1 : -1);
    }

    /// <summary>
    /// start에서 dir 방향으로 직선 탐색 — 첫 번째 적(EnemyUnit) 반환
    /// </summary>
    public static EnemyUnit FindFirstEnemyInDir(Vector2Int start, Vector2Int dir)
    {
        var pos = start + dir;
        while (BoardManager.Instance.IsInBounds(pos))
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile != null && tile.IsOccupied && tile.OccupiedUnit is EnemyUnit e)
                return e;
            pos += dir;
        }
        return null;
    }

    /// <summary>
    /// start에서 dir 방향으로 보드 내 마지막 타일 좌표 반환 (보드 끝)
    /// </summary>
    public static Vector2Int GetFarEdge(Vector2Int start, Vector2Int dir)
    {
        var pos = start + dir;
        if (!BoardManager.Instance.IsInBounds(pos))
            return start; // 이미 끝

        while (BoardManager.Instance.IsInBounds(pos + dir))
            pos += dir;
        return pos;
    }
}
