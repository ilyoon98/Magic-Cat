using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 홈/타이틀 화면 — 게임 시작 전 전체화면 오버레이
/// </summary>
public class TitleScreen : MonoBehaviour
{
    public static TitleScreen Instance { get; private set; }

    private GameObject panel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(Canvas canvas)
    {
        panel = new GameObject("TitlePanel");
        panel.transform.SetParent(canvas.transform, false);

        // ── 전체화면 배경 ─────────────────────────────────────────────────
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.09f, 1f);

        // ── 제목 텍스트 ───────────────────────────────────────────────────
        MakeText(panel.transform, "Title",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(700f, 120f),
            "고양이 마법사\nMagic Cats",
            56, new Color(0.75f, 0.88f, 1f), TextAnchor.MiddleCenter);

        // ── 서브타이틀 ────────────────────────────────────────────────────
        MakeText(panel.transform, "Subtitle",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -90f), new Vector2(600f, 50f),
            "3가지 마법으로 9개의 맵을 돌파하라",
            22, new Color(0.6f, 0.75f, 1f, 0.85f), TextAnchor.MiddleCenter);

        // ── 조작 안내 ─────────────────────────────────────────────────────
        MakeText(panel.transform, "Controls",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -165f), new Vector2(640f, 90f),
            "WASD / ↑↓←→  이동      마우스 클릭  공격\n" +
            "Q  스킬1      E  스킬2      Space  턴 넘기기\n" +
            "F1  치트 패널",
            18, new Color(0.65f, 0.75f, 0.85f), TextAnchor.MiddleCenter);

        // ── 게임 시작 버튼 ────────────────────────────────────────────────
        var btnGo = new GameObject("StartButton");
        btnGo.transform.SetParent(panel.transform, false);
        var brt = btnGo.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -270f);
        brt.sizeDelta = new Vector2(300f, 68f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.5f, 0.95f);
        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(OnStartClicked);
        MakeText(btnGo.transform, "StartLabel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "게임 시작", 34, Color.white, TextAnchor.MiddleCenter);

        // ── 갤러리 버튼 ───────────────────────────────────────────────────
        var galGo = new GameObject("GalleryButton");
        galGo.transform.SetParent(panel.transform, false);
        var grt = galGo.AddComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.5f, 0.5f);
        grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.anchoredPosition = new Vector2(0f, -360f);
        grt.sizeDelta = new Vector2(300f, 56f);
        var galImg = galGo.AddComponent<Image>();
        galImg.color = new Color(0.45f, 0.25f, 0.65f);
        var galBtn = galGo.AddComponent<Button>();
        galBtn.onClick.AddListener(() => { Hide(); GalleryScreen.Instance?.Show(); });
        MakeText(galGo.transform, "GalleryLabel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "🖼 갤러리", 28, Color.white, TextAnchor.MiddleCenter);

        // ── 설정 버튼 ─────────────────────────────────────────────────────
        var settGo = new GameObject("SettingsButton");
        settGo.transform.SetParent(panel.transform, false);
        var srt = settGo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 0.5f);
        srt.anchorMax = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, -436f);
        srt.sizeDelta = new Vector2(300f, 56f);
        var settImg = settGo.AddComponent<Image>();
        settImg.color = new Color(0.22f, 0.35f, 0.45f);
        var settBtn = settGo.AddComponent<Button>();
        settBtn.onClick.AddListener(() => SettingsPanel.Instance?.Show());
        MakeText(settGo.transform, "SettingsLabel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "⚙ 설정", 28, Color.white, TextAnchor.MiddleCenter);

        // ── 게임 종료 버튼 ────────────────────────────────────────────────
        var quitGo = new GameObject("QuitButton");
        quitGo.transform.SetParent(panel.transform, false);
        var qrt = quitGo.AddComponent<RectTransform>();
        qrt.anchorMin = new Vector2(0.5f, 0.5f);
        qrt.anchorMax = new Vector2(0.5f, 0.5f);
        qrt.anchoredPosition = new Vector2(0f, -508f);
        qrt.sizeDelta = new Vector2(300f, 56f);
        var quitImg = quitGo.AddComponent<Image>();
        quitImg.color = new Color(0.45f, 0.12f, 0.12f);
        var quitBtn = quitGo.AddComponent<Button>();
        quitBtn.onClick.AddListener(() => Application.Quit());
        MakeText(quitGo.transform, "QuitLabel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "✕ 종료", 28, Color.white, TextAnchor.MiddleCenter);

        // ── 장식 — 이모지 고양이 ──────────────────────────────────────────
        MakeText(panel.transform, "CatLeft",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-280f, 20f), new Vector2(120f, 120f),
            "🐱", 72, Color.white, TextAnchor.MiddleCenter);

        MakeText(panel.transform, "CatRight",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(280f, 20f), new Vector2(120f, 120f),
            "🐱", 72, Color.white, TextAnchor.MiddleCenter);
    }

    private void OnStartClicked()
    {
        panel.SetActive(false);
        StageSelectScreen.Instance?.Show();
    }

    // 외부에서 타이틀로 돌아올 때 사용
    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private Text MakeText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta,
        string content, int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        var txt = go.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = alignment;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        return txt;
    }
}
