using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 맵의 모든 바닥 오브젝트 관리
/// - 스폰/제거
/// - 턴마다 함정 쿨다운 갱신
/// - 유닛이 타일에 착지할 때 발동
/// </summary>
public class FloorObjectManager : MonoBehaviour
{
    public static FloorObjectManager Instance { get; private set; }

    private readonly Dictionary<Vector2Int, FloorObject> objects = new Dictionary<Vector2Int, FloorObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>원소 바닥 타일 스폰</summary>
    public FloorObject SpawnElement(FloorObject.ElementType element, Vector2Int pos)
    {
        if (objects.ContainsKey(pos)) Remove(objects[pos]);

        var go  = new GameObject($"ElemFloor_{element}_{pos.x}_{pos.y}");
        var obj = go.AddComponent<FloorObject>();
        obj.InitElement(element, pos);

        Vector3 worldPos = BoardManager.Instance.GridToWorld(pos);
        worldPos.z = -0.5f;
        go.transform.position   = worldPos;
        // 땅 원소 벽은 타일 전체를 덮도록 크게, 나머지는 작게
        go.transform.localScale = element == FloorObject.ElementType.Earth
            ? Vector3.one * 0.95f
            : Vector3.one * 0.6f;

        objects[pos] = obj;
        return obj;
    }

    /// <summary>지정 위치에 오브젝트 스폰</summary>
    public FloorObject Spawn(FloorObject.ObjectType type, Vector2Int pos)
    {
        // 이미 있으면 교체
        if (objects.ContainsKey(pos)) Remove(objects[pos]);

        var go = new GameObject($"FloorObj_{type}_{pos.x}_{pos.y}");
        var obj = go.AddComponent<FloorObject>();
        obj.Init(type, pos);

        Vector3 worldPos = BoardManager.Instance.GridToWorld(pos);
        worldPos.z = -0.5f; // 타일(z=0)보다 살짝 앞
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * 0.55f; // 타일 안에 들어오게 크기 조절

        objects[pos] = obj;
        return obj;
    }

    /// <summary>오브젝트 제거</summary>
    public void Remove(FloorObject obj)
    {
        if (obj == null) return;
        objects.Remove(obj.GridPos);
        Destroy(obj.gameObject);
    }

    /// <summary>모든 오브젝트 제거 (맵 전환 시) — 땅 원소 벽 플래그도 해제</summary>
    public void ClearAll()
    {
        foreach (var obj in new List<FloorObject>(objects.Values))
        {
            if (obj == null) continue;
            // 땅 원소 벽 플래그를 먼저 해제 (OnDestroy보다 명시적으로)
            if (obj.Type == FloorObject.ObjectType.Element
                && obj.Element == FloorObject.ElementType.Earth)
            {
                BoardManager.Instance?.GetTile(obj.GridPos)?.SetWall(false);
            }
            Destroy(obj.gameObject);
        }
        objects.Clear();
    }

    /// <summary>해당 위치의 오브젝트 가져오기</summary>
    public FloorObject GetAt(Vector2Int pos)
    {
        objects.TryGetValue(pos, out var obj);
        return obj;
    }

    /// <summary>턴 시작 시 모든 함정 쿨다운 갱신</summary>
    public void OnTurnStart()
    {
        foreach (var obj in new List<FloorObject>(objects.Values))
            obj?.OnTurnStart();
    }

    /// <summary>유닛이 pos에 착지했을 때 호출 — 오브젝트 발동 시도</summary>
    public void OnUnitEnterTile(Unit unit, Vector2Int pos)
    {
        if (!objects.TryGetValue(pos, out var obj) || obj == null) return;
        obj.TryActivate(unit);
    }
}
