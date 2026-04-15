using UnityEngine;

/// <summary>
/// 적 머리 위에 HP를 표시하는 TextMesh
/// </summary>
public class EnemyHPDisplay : MonoBehaviour
{
    private TextMesh textMesh;
    private Unit unit;
    private Vector3 offset = new Vector3(0f, 0.55f, 0f);

    public void Init(Unit u)
    {
        unit = u;

        var go = new GameObject("HPDisplay");
        go.transform.SetParent(transform);
        go.transform.localPosition = offset;

        textMesh = go.AddComponent<TextMesh>();
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.065f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // 항상 UI 위에 표시되도록
        go.GetComponent<MeshRenderer>().sortingOrder = 10;

        UpdateDisplay();
    }

    private void LateUpdate()
    {
        if (unit == null) return;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (textMesh == null || unit == null) return;
        int hp = unit.currentHp;
        int max = unit.maxHp;

        // HP에 따라 색상 변경
        textMesh.color = hp > max / 2f
            ? Color.white
            : hp > 0 ? new Color(1f, 0.6f, 0.2f) : Color.red;

        textMesh.text = $"♥{hp}/{max}";
    }
}
