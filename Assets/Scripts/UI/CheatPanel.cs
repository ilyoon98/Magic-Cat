using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치트 UI 패널 — F1으로 표시/숨김
/// 버튼: 무적 / 쿨타임0 / 순간이동
/// </summary>
public class CheatPanel : MonoBehaviour
{
    public static CheatPanel Instance { get; private set; }

    private GameObject panel;
    private Button btnInvincible;
    private Button btnZeroCd;
    private Button btnTeleport;
    private Text   lblInvincible;
    private Text   lblZeroCd;
    private Text   lblTeleport;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // F1 키로 패널 토글
        if (UnityEngine.InputSystem.Keyboard.current?.f1Key.wasPressedThisFrame == true)
            panel.SetActive(!panel.activeSelf);

        // 치트 모드 변경 시 버튼 색 동기화
        RefreshButtonColors();
    }

    public void Build(Canvas canvas)
    {
        // ── 패널 배경 ─────────────────────────────────────────────────────
        panel = new GameObject("CheatPanel");
        panel.transform.SetParent(canvas.transform, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-110f, -200f);
        rt.sizeDelta = new Vector2(200f, 180f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // ── 제목 ──────────────────────────────────────────────────────────
        MakeLabel(panel.transform, "CHEAT [F1]",
            new Vector2(0f, 0.82f), new Vector2(1f, 1f), 16);

        // ── 버튼 3개 ──────────────────────────────────────────────────────
        (btnInvincible, lblInvincible) = MakeToggleButton(panel.transform,
            "무적", new Vector2(0f, 0.55f), new Vector2(1f, 0.80f),
            () =>
            {
                CheatManager.Instance?.ToggleInvincible();
                RefreshButtonColors();
            });

        (btnZeroCd, lblZeroCd) = MakeToggleButton(panel.transform,
            "쿨타임 0", new Vector2(0f, 0.28f), new Vector2(1f, 0.53f),
            () =>
            {
                CheatManager.Instance?.ToggleZeroCooldown();
                RefreshButtonColors();
            });

        (btnTeleport, lblTeleport) = MakeToggleButton(panel.transform,
            "순간이동", new Vector2(0f, 0.01f), new Vector2(1f, 0.26f),
            () =>
            {
                CheatManager.Instance?.ToggleTeleportMode();
                RefreshButtonColors();
                if (CheatManager.Instance != null && CheatManager.Instance.TeleportMode)
                    GameUI.Instance?.ShowNotify("순간이동 ON — 빈 칸 클릭", 1.5f);
            });

        panel.SetActive(false); // 기본 숨김
    }

    public void RefreshUI() => RefreshButtonColors();

    private void RefreshButtonColors()
    {
        if (CheatManager.Instance == null) return;

        SetButtonState(btnInvincible,  lblInvincible,  "무적",    CheatManager.Instance.Invincible);
        SetButtonState(btnZeroCd,      lblZeroCd,      "쿨타임 0", CheatManager.Instance.ZeroCooldown);
        SetButtonState(btnTeleport,    lblTeleport,    "순간이동", CheatManager.Instance.TeleportMode);
    }

    private void SetButtonState(Button btn, Text lbl, string label, bool on)
    {
        if (btn == null) return;
        btn.GetComponent<Image>().color = on
            ? new Color(0.2f, 0.8f, 0.3f, 0.9f)
            : new Color(0.25f, 0.25f, 0.3f, 0.9f);
        if (lbl != null) lbl.text = on ? $"[ON]  {label}" : $"[OFF] {label}";
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private void MakeLabel(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = new Color(0.7f, 0.85f, 1f);
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private (Button btn, Text lbl) MakeToggleButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(6f, 3f); rt.offsetMax = new Vector2(-6f, -3f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        var txt = textGo.AddComponent<Text>();
        txt.text      = $"[OFF] {label}";
        txt.fontSize  = 15;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return (btn, txt);
    }
}
