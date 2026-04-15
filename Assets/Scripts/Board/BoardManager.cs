using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Board Settings")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private float tileSize = 1f;

    public int Width  => width;
    public int Height => height;

    [Header("Prefabs")]
    [SerializeField] private GameObject tilePrefab;

    public void SetTilePrefab(GameObject prefab) { tilePrefab = prefab; }

    private Tile[,] tiles;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BuildBoard()
    {
        // 기존 타일 정리
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(x, y));
                GameObject go = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                go.SetActive(true); // 프리팹이 비활성 상태여도 타일은 활성화
                go.name = $"Tile_{x}_{y}";

                Tile tile = go.GetComponent<Tile>();
                tile.Init(new Vector2Int(x, y));
                tiles[x, y] = tile;
            }
        }

        CenterCamera();
        Debug.Log($"[BoardManager] {width}x{height} 보드 생성 완료");
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / tileSize), Mathf.RoundToInt(worldPos.y / tileSize));
    }

    public Tile GetTile(Vector2Int pos)
    {
        if (!IsInBounds(pos)) return null;
        return tiles[pos.x, pos.y];
    }

    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    // 인접한 4방향 타일 반환 (상하좌우 +모양)
    public List<Tile> GetAdjacentTiles(Vector2Int pos)
    {
        var result = new List<Tile>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Tile t = GetTile(pos + dir);
            if (t != null) result.Add(t);
        }
        return result;
    }

    // 지정 범위 내 타일 반환 (맨해튼 거리 기준)
    public List<Tile> GetTilesInRange(Vector2Int center, int range)
    {
        var result = new List<Tile>();
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) <= range && (x != 0 || y != 0))
                {
                    Tile t = GetTile(center + new Vector2Int(x, y));
                    if (t != null) result.Add(t);
                }
            }
        }
        return result;
    }

    // 전체 타일 하이라이트 초기화
    public void ClearAllHighlights()
    {
        foreach (var tile in tiles)
            tile.SetHighlight(Tile.HighlightType.None);
    }

    // 보드 크기 변경 (스테이지별)
    public void ResizeBoard(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        BuildBoard();
    }

    private void CenterCamera()
    {
        float boardCX  = (width  - 1) * tileSize / 2f;
        float boardCY  = (height - 1) * tileSize / 2f;
        float orthoSize = Mathf.Max(width, height) * tileSize / 2f + 1f;

        // 우측 초상화 UI 공간 확보 — 카메라를 오른쪽으로 오프셋하면
        // 보드가 화면 왼쪽으로 이동하여 오른쪽 25% 영역이 비워짐
        float halfW    = orthoSize * Camera.main.aspect;
        float xOffset  = halfW * 0.22f; // 화면 너비의 11% 만큼 보드를 좌측으로

        Camera.main.transform.position = new Vector3(boardCX + xOffset, boardCY, -10f);
        Camera.main.orthographicSize   = orthoSize;
    }
}
