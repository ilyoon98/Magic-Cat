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
    /// start에서 dir 방향으로 직선 탐색 — 첫 번째 적(EnemyUnit) 반환.
    /// 벽(IsWall) 타일을 만나면 그 너머는 탐색하지 않고 null 반환.
    /// </summary>
    public static EnemyUnit FindFirstEnemyInDir(Vector2Int start, Vector2Int dir)
    {
        var pos = start + dir;
        while (BoardManager.Instance.IsInBounds(pos))
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) break;
            if (tile.IsWall) return null; // 벽에 막힘 — 그 너머 탐색 중단
            if (tile.IsOccupied && tile.OccupiedUnit is EnemyUnit e)
                return e;
            pos += dir;
        }
        return null;
    }

    /// <summary>
    /// start에서 dir 방향으로 보드 내 마지막 타일 좌표 반환.
    /// 벽(IsWall) 타일을 만나면 그 벽 위치에서 멈춤.
    /// </summary>
    public static Vector2Int GetFarEdge(Vector2Int start, Vector2Int dir)
    {
        var pos = start + dir;
        if (!BoardManager.Instance.IsInBounds(pos))
            return start;

        while (BoardManager.Instance.IsInBounds(pos))
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile != null && tile.IsWall) return pos; // 벽에서 멈춤
            if (!BoardManager.Instance.IsInBounds(pos + dir)) return pos; // 보드 끝
            pos += dir;
        }
        return pos;
    }

    /// <summary>
    /// from → to 직선(cardinal 방향) 경로에 벽 타일이 없는지 확인.
    /// 같은 행/열이 아닌 경우 항상 true 반환.
    /// </summary>
    public static bool HasClearLine(Vector2Int from, Vector2Int to)
    {
        if (from == to) return true;
        Vector2Int delta = to - from;
        // 같은 행 또는 같은 열인 경우만 차단 검사
        if (delta.x != 0 && delta.y != 0) return true;

        Vector2Int dir = SnapToCardinal(delta);
        Vector2Int pos = from + dir;
        while (pos != to)
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile != null && tile.IsWall) return false;
            pos += dir;
        }
        return true;
    }
}
