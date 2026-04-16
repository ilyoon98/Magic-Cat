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

    // 잠금 상태 갱신용 참조
    private Button[]     startBtns    = new Button[3];
    private GameObject[] lockOverlays = new GameObject[3]; // 잠긴 카드 어두운 오버레이
    private Image[]      stageImgs    = new Image[3];      // 스테이지 배경 이미지

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

        // ── 스테이지 배경 이미지 (카드 전체 영역, 잠금시 숨김) ───────────────
        var imgGo = new GameObject("StageImage");
        imgGo.transform.SetParent(card.transform, false);
        var imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero; imgRt.anchorMax = Vector2.one; imgRt.sizeDelta = Vector2.zero;
        var stageImg = imgGo.AddComponent<Image>();
        stageImg.preserveAspect = false; // 카드에 꽉 채움
        // 이미지 로드 시도 (없으면 투명)
        Sprite stageSp = LoadStageImage(stage);
        if (stageSp != null) { stageImg.sprite = stageSp; stageImg.color = new Color(1f, 1f, 1f, 0.18f); }
        else stageImg.color = Color.clear;
        stageImgs[stage - 1] = stageImg;

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
            new Vector2(0, -72), new Vector2(316, 90),
            info.skills, 20, new Color(0.68f, 0.74f, 0.82f), TextAnchor.MiddleCenter);

        // ── HP 표시 ───────────────────────────────────────────────────────
        MakeText(card.transform, "HP",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 102), new Vector2(200, 34),
            "♥ ♥ ♥", 22, new Color(1f, 0.38f, 0.48f), TextAnchor.MiddleCenter);

        // ── 맵 구성 표시 ──────────────────────────────────────────────────
        MakeText(card.transform, "Maps",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 68), new Vector2(290, 32),
            "MAP 1  ·  MAP 2  ·  BOSS", 20,
            new Color(0.50f, 0.62f, 0.72f), TextAnchor.MiddleCenter);

        // ── 시작 버튼 ─────────────────────────────────────────────────────
        var startBtn = MakeButton(card.transform, "StartBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(300, 52));
        SetBtnStyle(startBtn, $"스테이지 {stage} 시작", info.color);
        int s = stage; // closure capture
        startBtn.onClick.AddListener(() => StartFromStage(s));
        startBtns[stage - 1] = startBtn;

        // ── 잠금 오버레이 (잠긴 스테이지에만 표시) ───────────────────────
        var lockGo = new GameObject("LockOverlay");
        lockGo.transform.SetParent(card.transform, false);
        var lrt2 = lockGo.AddComponent<RectTransform>();
        lrt2.anchorMin = Vector2.zero; lrt2.anchorMax = Vector2.one; lrt2.sizeDelta = Vector2.zero;
        lockGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        // 자물쇠 아이콘 + 텍스트
        MakeText(lockGo.transform, "LockText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 10), new Vector2(300, 120),
            "🔒", 64, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleCenter);
        MakeText(lockGo.transform, "LockSub",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -55), new Vector2(280, 56),
            "이전 스테이지 보스를\n클리어하면 해금됩니다", 20,
            new Color(0.6f, 0.6f, 0.65f), TextAnchor.MiddleCenter);
        lockOverlays[stage - 1] = lockGo;
    }

    public void Show()
    {
        RefreshLockState();
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    /// <summary>PlayerPrefs에서 잠금 상태를 읽어 카드 UI를 갱신</summary>
    private void RefreshLockState()
    {
        for (int i = 0; i < 3; i++)
        {
            bool unlocked = ProgressManager.IsStageUnlocked(i + 1);
            if (startBtns[i]    != null) startBtns[i].interactable    = unlocked;
            if (lockOverlays[i] != null) lockOverlays[i].SetActive(!unlocked);
            // 해금된 경우 이미지를 더 선명하게, 잠긴 경우 투명하게
            if (stageImgs[i] != null && stageImgs[i].sprite != null)
                stageImgs[i].color = unlocked
                    ? new Color(1f, 1f, 1f, 0.28f)
                    : new Color(1f, 1f, 1f, 0.06f);
        }
    }

    private void StartFromStage(int stage)
    {
        Hide();
        // 인트로 풍경 패닝 연출 후 게임 시작 (SpecialSceneController 내부에서 StartGame 호출)
        SpecialSceneController.Instance?.ShowStageIntro(stage);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private Sprite LoadStageImage(int stage)
    {
        string path = $"Stage/Stage{stage}";
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

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
