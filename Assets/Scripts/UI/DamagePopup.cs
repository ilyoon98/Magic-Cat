using UnityEngine;

/// <summary>
/// 데미지/힐 숫자가 위로 떠오르며 사라지는 팝업
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private TextMesh textMesh;
    private float lifetime = 0.9f;
    private float elapsed;
    private Vector3 velocity = new Vector3(0f, 1.8f, 0f);

    public static void Create(Vector3 worldPos, int amount, bool isHeal = false, bool isCritical = false)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = worldPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.3f, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.fontSize = isCritical ? 52 : 40;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = isCritical ? FontStyle.Bold : FontStyle.Normal;

        if (isHeal)
        {
            tm.text = $"+{amount}";
            tm.color = new Color(0.3f, 1f, 0.4f);
        }
        else
        {
            tm.text = isCritical ? $"!!{amount}!!" : $"-{amount}";
            tm.color = isCritical ? new Color(1f, 0.3f, 0.1f) : new Color(1f, 0.85f, 0.2f);
        }

        tm.GetComponent<MeshRenderer>().sortingOrder = 20;

        var popup = go.AddComponent<DamagePopup>();
        popup.textMesh = tm;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity *= 0.88f;

        float alpha = Mathf.Clamp01(1f - (elapsed / lifetime));
        var c = textMesh.color;
        c.a = alpha;
        textMesh.color = c;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
