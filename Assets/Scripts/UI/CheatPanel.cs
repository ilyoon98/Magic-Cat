using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치트 UI 패널 — F1으로 표시/숨김
/// 버튼: 무적 / 쿨타임0 / 순간이동 / 스테이지 잠금해제 / 스테이지 초기화
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
        if (UnityEngine.InputSystem.Keyboard.current?.f1Key.wasPressedThisFrame == true)
            panel.SetActive(!panel.activeSelf);

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
        rt.anchoredPosition = new Vector2(-110f, -310f); // 메뉴 버튼 아래
        rt.sizeDelta = new Vector2(200f, 280f);          // 버튼 5개에 맞게 확장

        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        // ── 제목 ──────────────────────────────────────────────────────────
        MakeLabel(panel.transform, "CHEAT [F1]",
            new Vector2(0f, 0.88f), new Vector2(1f, 1f), 15);

        // ── 토글 버튼 3개 (치트) ──────────────────────────────────────────
        (btnInvincible, lblInvincible) = MakeToggleButton(panel.transform,
            "무적", new Vector2(0f, 0.70f), new Vector2(1f, 0.87f),
            () => { CheatManager.Instance?.ToggleInvincible(); RefreshButtonColors(); });

        (btnZeroCd, lblZeroCd) = MakeToggleButton(panel.transform,
            "쿨타임 0", new Vector2(0f, 0.52f), new Vector2(1f, 0.69f),
            () => { CheatManager.Instance?.ToggleZeroCooldown(); RefreshButtonColors(); });

        (btnTeleport, lblTeleport) = MakeToggleButton(panel.transform,
            "순간이동", new Vector2(0f, 0.34f), new Vector2(1f, 0.51f),
            () =>
            {
                CheatManager.Instance?.ToggleTeleportMode();
                RefreshButtonColors();
                if (CheatManager.Instance != null && CheatManager.Instance.TeleportMode)
                    GameUI.Instance?.ShowNotify("순간이동 ON — 빈 칸 클릭", 1.5f);
            });

        // ── 구분선 ────────────────────────────────────────────────────────
        var div = new GameObject("Div"); div.transform.SetParent(panel.transform, false);
        var drt = div.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.05f, 0.325f); drt.anchorMax = new Vector2(0.95f, 0.33f);
        drt.sizeDelta = Vector2.zero;
        div.AddComponent<Image>().color = new Color(0.35f, 0.45f, 0.65f, 0.6f);

        // ── 스테이지 잠금해제 버튼 ────────────────────────────────────────
        MakeActionButton(panel.transform, "스테이지 잠금해제",
            new Vector2(0f, 0.17f), new Vector2(1f, 0.32f),
            new Color(0.2f, 0.5f, 0.2f, 0.9f),
            () =>
            {
                ProgressManager.UnlockAll();
                GameUI.Instance?.ShowNotify("✅ 전 스테이지 해금", 1.5f);
            });

        // ── 스테이지 초기화 버튼 ──────────────────────────────────────────
        MakeActionButton(panel.transform, "스테이지 초기화",
            new Vector2(0f, 0.01f), new Vector2(1f, 0.16f),
            new Color(0.55f, 0.18f, 0.18f, 0.9f),
            () =>
            {
                ProgressManager.ResetAll();
                GameUI.Instance?.ShowNotify("🗑 진행 데이터 초기화", 1.5f);
            });

        panel.SetActive(false);
    }

    public void RefreshUI() => RefreshButtonColors();

    private void RefreshButtonColors()
    {
        if (CheatManager.Instance == null) return;
        SetButtonState(btnInvincible, lblInvincible, "무적",     CheatManager.Instance.Invincible);
        SetButtonState(btnZeroCd,     lblZeroCd,     "쿨타임 0", CheatManager.Instance.ZeroCooldown);
        SetButtonState(btnTeleport,   lblTeleport,   "순간이동", CheatManager.Instance.TeleportMode);
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
        t.text = text; t.fontSize = fontSize;
        t.color = new Color(0.7f, 0.85f, 1f);
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        txt.text = $"[OFF] {label}"; txt.fontSize = 14;
        txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return (btn, txt);
    }

    private Button MakeActionButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Btn");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(6f, 3f); rt.offsetMax = new Vector2(-6f, -3f);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var txt = textGo.AddComponent<Text>();
        txt.text = label; txt.fontSize = 13;
        txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return btn;
    }
}
