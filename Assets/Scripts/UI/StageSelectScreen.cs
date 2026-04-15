using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 선택 화면 — 타이틀에서 게임 시작 시 표시
/// 3개 카드: 원소마법사(1) / 흑백마법사(2) / 비전마법사(3)
/// </summary>
public class StageSelectScreen : MonoBehaviour
{
    public static StageSelectScreen Instance { get; private set; }

    private static readonly (string name, string emoji, string skills, Color color)[] StageInfo =
    {
        ("원소마법사",  "🐱",
         "Q: 원소변경\nE: 원소집중 (3배 데미지, 쿨4)",
         new Color(0.35f, 0.65f, 1.00f)),

        ("흑백마법사",  "🌓",
         "Q: 흑마법 모드 (화상)\nE: 백마법 모드 (쿨4·자가회복)",
         new Color(0.70f, 0.35f, 1.00f)),

        ("비전마법사",  "✨",
         "Q: 순간이동 (충전×3)\nE: 마력공격 (2배 데미지)",
         new Color(0.95f, 0.85f, 0.30f)),
    };

    private GameObject panel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(Transform canvasRoot)
    {
        panel = new GameObject("StageSelectScreen");
        panel.transform.SetParent(canvasRoot, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 1f);

        // ── 타이틀 ────────────────────────────────────────────────────────
        MakeText(panel.transform, "Title",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -88), new Vector2(700, 80),
            "스테이지 선택", 48, new Color(0.75f, 0.88f, 1f), TextAnchor.MiddleCenter);

        MakeText(panel.transform, "Sub",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -150), new Vector2(550, 42),
            "플레이할 캐릭터를 선택하세요", 22, new Color(0.60f, 0.70f, 0.80f), TextAnchor.MiddleCenter);

        // ── 스테이지 카드 3개 ─────────────────────────────────────────────
        float[] xPos = { -430f, 0f, 430f };
        for (int i = 0; i < 3; i++)
            BuildCard(panel.transform, i + 1, xPos[i]);

        // ── 뒤로 버튼 ─────────────────────────────────────────────────────
        var backBtn = MakeButton(panel.transform, "BackBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 58), new Vector2(180, 50));
        SetBtnStyle(backBtn, "← 뒤로", new Color(0.28f, 0.28f, 0.33f));
        backBtn.onClick.AddListener(() => { Hide(); TitleScreen.Instance?.Show(); });

        panel.SetActive(false);
    }

    private void BuildCard(Transform parent, int stage, float xOffset)
    {
        var info = StageInfo[stage - 1];

        // ── 카드 배경 ─────────────────────────────────────────────────────
        var card = new GameObject($"Stage{stage}Card");
        card.transform.SetParent(parent, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(xOffset, 10f);
        crt.sizeDelta        = new Vector2(360, 480);
        card.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.17f, 1f);

        // ── 상단 컬러 바 ──────────────────────────────────────────────────
        var bar = new GameObject("TopBar");
        bar.transform.SetParent(card.transform, false);
        var brt = bar.AddComponent<RectTransform>();
        brt.anchorMin       = new Vector2(0, 1);
        brt.anchorMax       = new Vector2(1, 1);
        brt.pivot           = new Vector2(0.5f, 1f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta       = new Vector2(0, 8);
        bar.AddComponent<Image>().color = info.color;

        // ── STAGE 번호 ────────────────────────────────────────────────────
        MakeText(card.transform, "StageNum",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -42), new Vector2(300, 40),
            $"STAGE {stage}", 24,
            new Color(info.color.r, info.color.g, info.color.b, 0.9f), TextAnchor.MiddleCenter);

        // ── 캐릭터 이모지 ─────────────────────────────────────────────────
        MakeText(card.transform, "Emoji",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 108), new Vector2(200, 120),
            info.emoji, 70, Color.white, TextAnchor.MiddleCenter);

        // ── 캐릭터 이름 ───────────────────────────────────────────────────
        MakeText(card.transform, "CharName",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 22), new Vector2(300, 46),
            info.name, 30, Color.white, TextAnchor.MiddleCenter);

        // ── 스킬 설명 ─────────────────────────────────────────────────────
        MakeText(card.transform, "Skills",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -72), new Vector2(316, 80),
            info.skills, 16, new Color(0.68f, 0.74f, 0.82f), TextAnchor.MiddleCenter);

        // ── HP 표시 ───────────────────────────────────────────────────────
        MakeText(card.transform, "HP",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 102), new Vector2(200, 34),
            "♥ ♥ ♥", 22, new Color(1f, 0.38f, 0.48f), TextAnchor.MiddleCenter);

        // ── 맵 구성 표시 ──────────────────────────────────────────────────
        MakeText(card.transform, "Maps",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 68), new Vector2(290, 28),
            "MAP 1  ·  MAP 2  ·  BOSS", 15,
            new Color(0.50f, 0.62f, 0.72f), TextAnchor.MiddleCenter);

        // ── 시작 버튼 ─────────────────────────────────────────────────────
        var startBtn = MakeButton(card.transform, "StartBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(300, 52));
        SetBtnStyle(startBtn, $"스테이지 {stage} 시작", info.color);
        int s = stage; // closure capture
        startBtn.onClick.AddListener(() => StartFromStage(s));
    }

    public void Show() => panel.SetActive(true);
    public void Hide() => panel.SetActive(false);

    private void StartFromStage(int stage)
    {
        Hide();
        GameManager.Instance?.StartGame(stage);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private void SetBtnStyle(Button btn, string label, Color color)
    {
        btn.GetComponent<Image>().color         = color;
        btn.GetComponentInChildren<Text>().text = label;
    }

    private Button MakeButton(Transform parent, string name,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var txt = lgo.AddComponent<Text>();
        txt.fontSize  = 22; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return btn;
    }

    private Text MakeText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        string content, int fontSize, Color color, TextAnchor alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        var txt = go.AddComponent<Text>();
        txt.text      = content;   txt.fontSize = fontSize;
        txt.color     = color;     txt.alignment = alignment;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        return txt;
    }
}
