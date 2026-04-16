using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int GridPos { get; private set; }
    public Unit OccupiedUnit { get; private set; }
    public bool IsOccupied => OccupiedUnit != null;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = new Color(0.8f, 0.85f, 0.9f);
    [SerializeField] private Color highlightColor = new Color(0.5f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private Color skillColor = new Color(0.25f, 1f, 0.65f);   // 스킬 경로 미리보기 (청록)

    private void Awake()
    {
        // 코드로 생성된 경우 [SerializeField]가 null → Inner 자식에서 자동 탐색
        if (spriteRenderer == null)
        {
            var inner = transform.Find("Inner");
            if (inner != null) spriteRenderer = inner.GetComponent<SpriteRenderer>();
            else               spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void Init(Vector2Int pos)
    {
        GridPos = pos;
        SetHighlight(HighlightType.None);
    }

    public enum HighlightType { None, Move, Attack, Selected, Danger, Skill }

    public void SetHighlight(HighlightType type)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = type switch
        {
            HighlightType.Move     => highlightColor,
            HighlightType.Attack   => dangerColor,
            HighlightType.Danger   => dangerColor,
            HighlightType.Selected => selectedColor,
            HighlightType.Skill    => skillColor,    // 스킬 경로 미리보기 (청록)
            _                      => normalColor
        };
    }

    public void SetUnit(Unit unit)
    {
        OccupiedUnit = unit;
    }

    public void ClearUnit()
    {
        OccupiedUnit = null;
    }
}
