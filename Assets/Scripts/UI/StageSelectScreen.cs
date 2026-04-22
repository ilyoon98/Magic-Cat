using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    private Text[]       mapsClearTxts = new Text[3];      // 맵별 클리어 현황 텍스트

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!panel.activeSelf) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Hide();
            TitleScreen.Instance?.Show();
        }
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
        float[] xPos = { -570f, 0f, 570f };
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
        crt.anchoredPosition = new Vector2(xOffset, 0f);
        crt.sizeDelta        = new Vector2(540, 720);
        card.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.17f, 1f);

        // ── 스테이지 배경 이미지 (카드 전체 영역, 잠금시 숨김) ───────────────
        var imgGo = new GameObject("StageImage");
        imgGo.transform.SetParent(card.transform, false);
        var imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero; imgRt.anchorMax = Vector2.one; imgRt.sizeDelta = Vector2.zero;
        var stageImg = imgGo.AddComponent<Image>();
        stageImg.preserveAspect = true; // 비율 유지, 중앙 정렬
        // 이미지 로드 시도 (색상은 RefreshLockState에서 결정)
        Sprite stageSp = LoadStageImage(stage);
        if (stageSp != null) stageImg.sprite = stageSp;
        stageImg.color = Color.clear; // Show() → RefreshLockState()에서 갱신
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
            new Vector2(0, -55), new Vector2(440, 50),
            $"STAGE {stage}", 28,
            new Color(info.color.r, info.color.g, info.color.b, 0.9f), TextAnchor.MiddleCenter);

        // ── 캐릭터 이모지 ─────────────────────────────────────────────────
        MakeText(card.transform, "Emoji",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 160), new Vector2(220, 140),
            info.emoji, 80, Color.white, TextAnchor.MiddleCenter);

        // ── 캐릭터 이름 ───────────────────────────────────────────────────
        MakeText(card.transform, "CharName",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 40), new Vector2(440, 54),
            info.name, 34, Color.white, TextAnchor.MiddleCenter);

        // ── 스킬 설명 ─────────────────────────────────────────────────────
        MakeText(card.transform, "Skills",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -90), new Vector2(460, 120),
            info.skills, 22, new Color(0.68f, 0.74f, 0.82f), TextAnchor.MiddleCenter);

        // ── HP 표시 ───────────────────────────────────────────────────────
        MakeText(card.transform, "HP",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 138), new Vector2(280, 40),
            "♥ ♥ ♥", 26, new Color(1f, 0.38f, 0.48f), TextAnchor.MiddleCenter);

        // ── 맵 구성 + 클리어 표시 (동적 갱신용 참조 보관) ─────────────────
        var mapsText = MakeText(card.transform, "Maps",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 96), new Vector2(460, 36),
            GetMapsLabel(stage), 21,
            new Color(0.50f, 0.62f, 0.72f), TextAnchor.MiddleCenter);
        mapsText.supportRichText = true;
        mapsClearTxts[stage - 1] = mapsText;

        // ── 시작 버튼 ─────────────────────────────────────────────────────
        var startBtn = MakeButton(card.transform, "StartBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 38), new Vector2(440, 60));
        SetBtnStyle(startBtn, $"스테이지 {stage} 시작", info.color);
        int s = stage; // closure capture
        startBtn.onClick.AddListener(() => StartFromStage(s));
        startBtns[stage - 1] = startBtn;

        // ── 잠금 오버레이 (잠긴 스테이지에만 표시) ───────────────────────
        var lockGo = new GameObject("LockOverlay");
        lockGo.transform.SetParent(card.transform, false);
        var lrt2 = lockGo.AddComponent<RectTransform>();
        lrt2.anchorMin = Vector2.zero; lrt2.anchorMax = Vector2.one; lrt2.sizeDelta = Vector2.zero;
        lockGo.AddComponent<Image>().color = new Color(0f, 0f, 0.05f, 0.82f);

        // 잠금 배지 카드 (가운데)
        var badge = new GameObject("LockBadge");
        badge.transform.SetParent(lockGo.transform, false);
        var badgeRt = badge.AddComponent<RectTransform>();
        badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRt.anchoredPosition = Vector2.zero;
        badgeRt.sizeDelta = new Vector2(270, 140);
        badge.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.95f);

        MakeText(badge.transform, "LockText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 28), new Vector2(250, 64),
            "🔒", 48, new Color(0.85f, 0.85f, 1f), TextAnchor.MiddleCenter);
        MakeText(badge.transform, "LockSub",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -28), new Vector2(250, 52),
            "이전 보스 클리어 시 해금", 17,
            new Color(0.65f, 0.65f, 0.75f), TextAnchor.MiddleCenter);
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
        bool showImages = CheatManager.Instance != null && CheatManager.Instance.ShowSensitiveImages;

        for (int i = 0; i < 3; i++)
        {
            int  stage    = i + 1;
            bool unlocked = ProgressManager.IsStageUnlocked(stage);

            if (startBtns[i]    != null) startBtns[i].interactable    = unlocked;
            if (lockOverlays[i] != null) lockOverlays[i].SetActive(!unlocked);

            // 이미지: 해금 + ShowSensitiveImages 모두 충족해야 표시
            if (stageImgs[i] != null && stageImgs[i].sprite != null)
            {
                if (!unlocked || !showImages)
                    stageImgs[i].color = Color.clear;
                else
                    stageImgs[i].color = new Color(1f, 1f, 1f, 0.28f);
            }

            // 맵별 클리어 표시 갱신
            if (mapsClearTxts[i] != null)
                mapsClearTxts[i].text = GetMapsLabel(stage);
        }
    }

    /// <summary>맵 클리어 상태를 반영한 표시 문자열 생성</summary>
    private static string GetMapsLabel(int stage)
    {
        string m1   = ProgressManager.IsMapCleared(stage, 1) ? "<color=#66ff99>MAP 1 ✓</color>" : "MAP 1";
        string m2   = ProgressManager.IsMapCleared(stage, 2) ? "<color=#66ff99>MAP 2 ✓</color>" : "MAP 2";
        string boss = ProgressManager.IsMapCleared(stage, 3) ? "<color=#ffdd55>BOSS ✓</color>"  : "BOSS";
        return $"{m1}  ·  {m2}  ·  {boss}";
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
        string path = $"Stage/Landscape_{stage}";
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
