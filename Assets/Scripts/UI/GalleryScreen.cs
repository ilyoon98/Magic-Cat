using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 갤러리 화면 — 스테이지 선택 → 해당 스테이지 이미지 목록
///
/// Row 0 (세로 이미지, 387×516): HP3 HP2 HP1 HP0
/// Row 1 (가로 이미지, 265×180): 스테이지입장 배경 클리어 슬라임패배 빅슬라임패배 기타패배
/// </summary>
public class GalleryScreen : MonoBehaviour
{
    public static GalleryScreen Instance { get; private set; }

    private static readonly string[] StageNames  = { "원소마법사", "흑백마법사", "비전마법사" };

    private const int COLS = 8;
    private const int ROWS = 2;

    // Row 0: 세로 초상화 (4칸)
    private const float CW0 = 387f, CH0 = 516f, CY0 = 75f;
    private static readonly float[] CX0 = { -605f, -202f, 202f, 605f };

    // Row 1: 가로 스테이지/패배 이미지 (3 고정 + 5 패배, 총 8칸)
    private const float CW1 = 180f, CH1 = 160f, CY1 = -345f;
    private static readonly float[] CX1 = { -672f, -480f, -288f, -96f, 96f, 288f, 480f, 672f };

    // ── UI 참조 ───────────────────────────────────────────────────────────
    private GameObject stageSelectPanel;
    private GameObject detailPanel;

    // 갤러리 스테이지 선택 카드 배경 이미지 (ShowSensitiveImages 갱신용)
    private readonly Image[]  cardBgImgs    = new Image[3];
    private readonly Sprite[] cardBgSprites = new Sprite[3];

    private Image[,]      cellImgs  = new Image[ROWS, COLS];
    private GameObject[,] lockIcons = new GameObject[ROWS, COLS];
    private Text          detailTitle;

    // 패배 이미지 슬롯 (최대 5개, 스테이지별 활성/비활성)
    private readonly GameObject[] defeatCellGOs  = new GameObject[5];
    private readonly Text[]       defeatHdrTexts = new Text[5];

    private int currentStage = 0;

    private GameObject fullViewPanel;
    private Image      fullViewImage;
    private Image      fullViewLockedOverlay;
    private Text       fullViewLockedText;
    private Button     fvPrevBtn, fvNextBtn;
    private Text       fvPageText;

    private int fullViewIndex;
    // 현재 스테이지의 모든 셀 순서 (row, col)
    private readonly List<(int row, int col)> fullViewOrder = new List<(int, int)>();

    private Text  toastText;
    private float toastTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f && toastText != null) toastText.text = "";
        }

        if (Keyboard.current == null) return;

        // 전체화면 감상 중 ← → 키 네비게이션
        if (fullViewPanel != null && fullViewPanel.activeSelf)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)  NavigateFullView(-1);
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame) NavigateFullView(1);
        }

        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (fullViewPanel != null && fullViewPanel.activeSelf)
            fullViewPanel.SetActive(false);
        else if (detailPanel != null && detailPanel.activeSelf)
        { detailPanel.SetActive(false); stageSelectPanel.SetActive(true); }
        else if (stageSelectPanel != null && stageSelectPanel.activeSelf)
        { Hide(); TitleScreen.Instance?.Show(); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Build
    // ═══════════════════════════════════════════════════════════════════════
    public void Build(Transform canvasRoot)
    {
        BuildStageSelectPanel(canvasRoot);
        BuildDetailPanel(canvasRoot);
        BuildFullViewPanel(canvasRoot);
        stageSelectPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    // ── 갤러리 스테이지 선택 패널 ─────────────────────────────────────────
    private void BuildStageSelectPanel(Transform canvasRoot)
    {
        stageSelectPanel = new GameObject("GalleryStageSelect");
        stageSelectPanel.transform.SetParent(canvasRoot, false);
        var rt = stageSelectPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        stageSelectPanel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 1f);

        MakeText(stageSelectPanel.transform, "Title",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -70), new Vector2(600, 70),
            "🖼  갤 러 리", 48, new Color(0.85f, 0.88f, 1f), TextAnchor.MiddleCenter);

        MakeText(stageSelectPanel.transform, "Sub",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -130), new Vector2(700, 38),
            "스테이지를 선택하세요", 22, new Color(0.55f, 0.65f, 0.75f), TextAnchor.MiddleCenter);

        float[] xPos = { -570f, 0f, 570f };
        Color[] colors = {
            new Color(0.35f, 0.65f, 1.00f),
            new Color(0.70f, 0.35f, 1.00f),
            new Color(0.95f, 0.85f, 0.30f),
        };
        string[] emojis = { "🐱", "🌓", "✨" };

        for (int i = 0; i < 3; i++)
        {
            int stage = i + 1;
            var card = new GameObject($"StageCard{stage}");
            card.transform.SetParent(stageSelectPanel.transform, false);
            var crt = card.AddComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(xPos[i], 0f);
            crt.sizeDelta = new Vector2(540f, 720f);
            card.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.17f, 1f);

            // 배경 랜드스케이프 이미지 (ShowSensitiveImages 상태에 따라 Show()에서 갱신)
            var bgGo = new GameObject("BgImg"); bgGo.transform.SetParent(card.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.preserveAspect = true;
            bgImg.color = Color.clear; // Show()의 RefreshCardBgImages()에서 결정
            Sprite bgSp = LoadSprite($"Stage/Landscape_{stage}");
            if (bgSp != null) bgImg.sprite = bgSp;
            cardBgImgs[i]    = bgImg;
            cardBgSprites[i] = bgSp;

            // 상단 컬러 바
            var bar = new GameObject("Bar"); bar.transform.SetParent(card.transform, false);
            var brt = bar.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0,1); brt.anchorMax = new Vector2(1,1);
            brt.pivot = new Vector2(0.5f,1f); brt.anchoredPosition = Vector2.zero; brt.sizeDelta = new Vector2(0,8);
            bar.AddComponent<Image>().color = colors[i];

            MakeText(card.transform, "StageLbl",
                new Vector2(0.5f,1f), new Vector2(0.5f,1f),
                new Vector2(0,-52), new Vector2(440,48),
                $"STAGE {stage}", 30, colors[i], TextAnchor.MiddleCenter);

            MakeText(card.transform, "Emoji",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0,120), new Vector2(200,130),
                emojis[i], 76, Color.white, TextAnchor.MiddleCenter);

            MakeText(card.transform, "CharLbl",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0,20), new Vector2(440,48),
                StageNames[i], 30, Color.white, TextAnchor.MiddleCenter);

            MakeText(card.transform, "CountLbl",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0,-50), new Vector2(380,38),
                "10장", 22, new Color(0.6f,0.7f,0.8f), TextAnchor.MiddleCenter);

            var btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors2 = btn.colors;
            colors2.highlightedColor = new Color(1.2f,1.2f,1.2f,1f);
            btn.colors = colors2;
            btn.onClick.AddListener(() => OpenDetail(stage));
        }

        var closeBtn = MakeButton(stageSelectPanel.transform, "CloseBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 58), new Vector2(200, 52));
        SetBtnStyle(closeBtn, "닫기", new Color(0.28f, 0.28f, 0.33f));
        closeBtn.onClick.AddListener(() => { Hide(); TitleScreen.Instance?.Show(); });
    }

    // ── 상세 패널 ─────────────────────────────────────────────────────────
    private void BuildDetailPanel(Transform canvasRoot)
    {
        detailPanel = new GameObject("GalleryDetail");
        detailPanel.transform.SetParent(canvasRoot, false);
        var rt = detailPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        detailPanel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 1f);

        detailTitle = MakeText(detailPanel.transform, "DetailTitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -65), new Vector2(800, 64),
            "", 38, new Color(0.85f, 0.88f, 1f), TextAnchor.MiddleCenter);

        // ── Row 0: 세로 초상화 이미지 ─────────────────────────────────────
        MakeText(detailPanel.transform, "Row0Label",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0f, CY0 + CH0 * 0.5f + 55f), new Vector2(500f, 30f),
            "── 캐릭터 초상화 ──", 17,
            new Color(0.65f, 0.78f, 0.95f), TextAnchor.MiddleCenter);

        string[] hpLabels = { "HP 3 (풀피)", "HP 2", "HP 1", "HP 0" };
        for (int c = 0; c < 4; c++)
        {
            MakeText(detailPanel.transform, $"HpHdr{c}",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(CX0[c], CY0 + CH0 * 0.5f + 26f), new Vector2(360f, 26f),
                hpLabels[c], 16, new Color(1f, 0.5f, 0.55f), TextAnchor.MiddleCenter);

            BuildCell(detailPanel.transform, 0, c, CX0[c], CY0, CW0, CH0);
        }

        // ── Row 1: 가로 스테이지/패배 이미지 ─────────────────────────────
        MakeText(detailPanel.transform, "Row1Label",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(0f, CY1 + CH1 * 0.5f + 52f), new Vector2(700f, 30f),
            "── 스테이지 이미지 · 패배 이미지 ──", 17,
            new Color(0.65f, 0.88f, 0.65f), TextAnchor.MiddleCenter);

        // 고정 3칸: 스테이지 입장, 배경, 클리어
        string[] row1FixedLabels = { "스테이지 입장", "배경", "클리어" };
        for (int c = 0; c < 3; c++)
        {
            MakeText(detailPanel.transform, $"Row1Hdr{c}",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(CX1[c], CY1 + CH1 * 0.5f + 26f), new Vector2(220f, 26f),
                row1FixedLabels[c], 15, new Color(0.7f, 0.85f, 0.7f), TextAnchor.MiddleCenter);
            BuildCell(detailPanel.transform, 1, c, CX1[c], CY1, CW1, CH1);
        }

        // 패배 5칸 (스테이지별 동적): 초기 위치는 임시, RefreshDetail()에서 재조정
        for (int k = 0; k < 5; k++)
        {
            var hdr = MakeText(detailPanel.transform, $"Row1DefeatHdr{k}",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(CX1[3 + k], CY1 + CH1 * 0.5f + 26f), new Vector2(220f, 26f),
                "", 15, new Color(1f, 0.75f, 0.55f), TextAnchor.MiddleCenter);
            defeatHdrTexts[k] = hdr;

            var cellGO = BuildCell(detailPanel.transform, 1, 3 + k, CX1[3 + k], CY1, CW1, CH1);
            defeatCellGOs[k] = cellGO;
            cellGO.SetActive(false);       // RefreshDetail()에서 활성화
            hdr.gameObject.SetActive(false);
        }

        // 뒤로 버튼
        var backBtn = MakeButton(detailPanel.transform, "BackBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 58), new Vector2(200, 52));
        SetBtnStyle(backBtn, "← 뒤로", new Color(0.28f, 0.28f, 0.33f));
        backBtn.onClick.AddListener(() =>
        {
            detailPanel.SetActive(false);
            stageSelectPanel.SetActive(true);
        });

        // 잠금 클릭 토스트
        toastText = MakeText(detailPanel.transform, "ToastText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -130), new Vector2(600, 44),
            "", 22, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
    }

    private GameObject BuildCell(Transform parent, int row, int col,
                                  float xPos, float yPos, float w, float h)
    {
        var cell = new GameObject($"Cell_{row}_{col}");
        cell.transform.SetParent(parent, false);
        var crt = cell.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(xPos, yPos);
        crt.sizeDelta = new Vector2(w, h);
        cell.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.20f);

        // 이미지 슬롯
        var imgGo = new GameObject("Img"); imgGo.transform.SetParent(cell.transform, false);
        var irt = imgGo.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4, 4); irt.offsetMax = new Vector2(-4, -4);
        var img = imgGo.AddComponent<Image>();
        img.preserveAspect = true;
        cellImgs[row, col] = img;

        // 잠금 오버레이
        var lockGo = new GameObject("Lock"); lockGo.transform.SetParent(cell.transform, false);
        var lrt = lockGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        lockGo.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.13f, 0.92f);
        MakeText(lockGo.transform, "LockEmoji",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "🔒", 36, new Color(0.5f, 0.5f, 0.6f), TextAnchor.MiddleCenter);
        lockIcons[row, col] = lockGo;

        // 클릭 버튼
        int r = row, c = col;
        var btn = cell.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => OnCellClick(r, c));

        return cell;
    }

    // ── 전체화면 감상 패널 ────────────────────────────────────────────────
    private void BuildFullViewPanel(Transform canvasRoot)
    {
        fullViewPanel = new GameObject("GalleryFullView");
        fullViewPanel.transform.SetParent(canvasRoot, false);
        var rt = fullViewPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        fullViewPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.96f);

        // ── 메인 이미지 ───────────────────────────────────────────────────
        fullViewImage = new GameObject("FullImg").AddComponent<Image>();
        fullViewImage.transform.SetParent(fullViewPanel.transform, false);
        var irt = fullViewImage.rectTransform;
        irt.anchorMin = new Vector2(0.08f, 0.08f);
        irt.anchorMax = new Vector2(0.92f, 0.92f);
        irt.sizeDelta = Vector2.zero;
        fullViewImage.preserveAspect = true;

        // ── 잠금/비공개 오버레이 (이미지 영역 위) ────────────────────────
        var lockOvGo = new GameObject("LockedOverlay");
        lockOvGo.transform.SetParent(fullViewPanel.transform, false);
        var lovrt = lockOvGo.AddComponent<RectTransform>();
        lovrt.anchorMin = new Vector2(0.08f, 0.08f);
        lovrt.anchorMax = new Vector2(0.92f, 0.92f);
        lovrt.sizeDelta = Vector2.zero;
        fullViewLockedOverlay = lockOvGo.AddComponent<Image>();
        fullViewLockedOverlay.color = new Color(0.04f, 0.04f, 0.08f, 1f);

        var lockMsgGo = new GameObject("LockMsg");
        lockMsgGo.transform.SetParent(lockOvGo.transform, false);
        var lmrt = lockMsgGo.AddComponent<RectTransform>();
        lmrt.anchorMin = lmrt.anchorMax = new Vector2(0.5f, 0.5f);
        lmrt.anchoredPosition = Vector2.zero;
        lmrt.sizeDelta = new Vector2(800f, 180f);
        fullViewLockedText = lockMsgGo.AddComponent<Text>();
        fullViewLockedText.font           = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fullViewLockedText.fontSize       = 34;
        fullViewLockedText.alignment      = TextAnchor.MiddleCenter;
        fullViewLockedText.color          = new Color(0.55f, 0.58f, 0.72f);
        fullViewLockedText.supportRichText = true;
        // 기본 잠금 메시지 — ShowFullViewAt()에서 상황에 맞게 덮어씀
        fullViewLockedText.text =
            "🔒  봉인된 기억\n<size=22><color=#666880>아직 공개되지 않은 이야기입니다\n모험을 계속하면 열립니다</color></size>";

        // ── 이전 버튼 ◀ ──────────────────────────────────────────────────
        fvPrevBtn = MakeButton(fullViewPanel.transform, "FVPrev",
            new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(72f, 130f));
        SetBtnStyle(fvPrevBtn, "◀", new Color(0.15f, 0.18f, 0.28f, 0.88f));
        fvPrevBtn.GetComponentInChildren<Text>().fontSize = 30;
        fvPrevBtn.onClick.AddListener(() => NavigateFullView(-1));

        // ── 다음 버튼 ▶ ──────────────────────────────────────────────────
        fvNextBtn = MakeButton(fullViewPanel.transform, "FVNext",
            new Vector2(1f, 0.5f), new Vector2(-44f, 0f), new Vector2(72f, 130f));
        SetBtnStyle(fvNextBtn, "▶", new Color(0.15f, 0.18f, 0.28f, 0.88f));
        fvNextBtn.GetComponentInChildren<Text>().fontSize = 30;
        fvNextBtn.onClick.AddListener(() => NavigateFullView(1));

        // ── 페이지 표시 (하단 중앙) ───────────────────────────────────────
        fvPageText = MakeText(fullViewPanel.transform, "FVPage",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(300f, 40f),
            "", 20, new Color(0.55f, 0.6f, 0.75f), TextAnchor.MiddleCenter);

        // ── 닫기 버튼 ────────────────────────────────────────────────────
        var closeBtn = MakeButton(fullViewPanel.transform, "FVClose",
            new Vector2(1f, 1f), new Vector2(-70f, -70f), new Vector2(120f, 50f));
        SetBtnStyle(closeBtn, "✕ 닫기", new Color(0.5f, 0.2f, 0.2f));
        closeBtn.onClick.AddListener(() => fullViewPanel.SetActive(false));

        fullViewPanel.SetActive(false);
    }

    // ── 풀뷰 순서 목록 빌드 ──────────────────────────────────────────────
    private void BuildFullViewOrder()
    {
        fullViewOrder.Clear();
        // Row 0: 초상화 HP3·2·1·0 (4개)
        for (int c = 0; c < 4; c++)
            fullViewOrder.Add((0, c));
        // Row 1 고정 3칸: 스테이지 입장·배경·클리어
        for (int c = 0; c < 3; c++)
            fullViewOrder.Add((1, c));
        // Row 1 패배 칸: 활성화된 것만 포함
        for (int k = 0; k < 5; k++)
            if (defeatCellGOs[k] != null && defeatCellGOs[k].activeSelf)
                fullViewOrder.Add((1, 3 + k));
    }

    // ── 인덱스 지정 풀뷰 표시 ────────────────────────────────────────────
    private void ShowFullViewAt(int index)
    {
        if (fullViewOrder.Count == 0) return;
        fullViewIndex = ((index % fullViewOrder.Count) + fullViewOrder.Count) % fullViewOrder.Count;

        var (row, col) = fullViewOrder[fullViewIndex];
        bool locked     = lockIcons[row, col] != null && lockIcons[row, col].activeSelf;
        bool showImages = CheatManager.Instance == null || CheatManager.Instance.ShowSensitiveImages;
        var  img        = cellImgs[row, col];

        if (locked)
        {
            fullViewImage.gameObject.SetActive(false);
            fullViewLockedOverlay.gameObject.SetActive(true);
            fullViewLockedText.text =
                "🔒  봉인된 기억\n<size=22><color=#666880>아직 공개되지 않은 이야기입니다\n모험을 계속하면 열립니다</color></size>";
        }
        else if (!showImages || img == null || img.sprite == null)
        {
            fullViewImage.gameObject.SetActive(false);
            fullViewLockedOverlay.gameObject.SetActive(true);
            fullViewLockedText.text = locked
                ? "🔒  봉인된 기억\n<size=22><color=#666880>아직 공개되지 않은 이야기입니다\n모험을 계속하면 열립니다</color></size>"
                : "<size=26><color=#888899>이미지 파일이 없습니다</color></size>";
        }
        else
        {
            fullViewImage.sprite = img.sprite;
            fullViewImage.gameObject.SetActive(true);
            fullViewLockedOverlay.gameObject.SetActive(false);
        }

        if (fvPageText != null)
            fvPageText.text = $"{fullViewIndex + 1}  /  {fullViewOrder.Count}";
    }

    // ── 이전/다음 네비게이션 ─────────────────────────────────────────────
    private void NavigateFullView(int dir) => ShowFullViewAt(fullViewIndex + dir);

    // ═══════════════════════════════════════════════════════════════════════
    // 공개 API
    // ═══════════════════════════════════════════════════════════════════════
    public void Show()
    {
        RefreshCardBgImages();
        stageSelectPanel.SetActive(true);
        detailPanel.SetActive(false);
        fullViewPanel.SetActive(false);
    }

    /// <summary>갤러리 스테이지 선택 카드의 Landscape 배경을 ShowSensitiveImages 상태에 맞게 갱신</summary>
    private void RefreshCardBgImages()
    {
        bool showImages = CheatManager.Instance != null && CheatManager.Instance.ShowSensitiveImages;
        for (int i = 0; i < 3; i++)
        {
            if (cardBgImgs[i] == null) continue;
            if (showImages && cardBgSprites[i] != null)
                cardBgImgs[i].color = new Color(1f, 1f, 1f, 0.22f);
            else
                cardBgImgs[i].color = Color.clear;
        }
    }

    public void Hide()
    {
        stageSelectPanel.SetActive(false);
        detailPanel.SetActive(false);
        fullViewPanel.SetActive(false);
    }

    private void OpenDetail(int stage)
    {
        currentStage = stage;
        detailTitle.text = $"STAGE {stage}  ·  {StageNames[stage - 1]}";
        stageSelectPanel.SetActive(false);
        detailPanel.SetActive(true);
        RefreshDetail(); // BuildFullViewOrder()는 RefreshDetail() 마지막에 호출됨
    }

    /// <summary>외부 (CheatPanel 등)에서 호출 — 이미지 표시 상태 즉시 갱신</summary>
    public void RefreshAll()
    {
        RefreshCardBgImages();
        if (currentStage > 0 && detailPanel != null && detailPanel.activeSelf)
            RefreshDetail();
    }

    public void RefreshImageBlocker() => RefreshAll();

    private void RefreshDetail()
    {
        int s = currentStage;

        // ── Row 0: 초상화 HP3·2·1·0 ──────────────────────────────────────
        int[] hpOrder = { 3, 2, 1, 0 };
        for (int c = 0; c < 4; c++)
        {
            int hp        = hpOrder[c];
            bool unlocked = ProgressManager.IsGalleryUnlocked(s, hp);
            SetCell(0, c, unlocked,
                unlocked ? LoadSprite($"Portraits/Stage{s}_HP{hp}") : null);
        }

        // ── Row 1 col 0: 스테이지 입장 (Landscape) ───────────────────────
        {
            bool unlocked = ProgressManager.IsStageUnlocked(s);
            SetCell(1, 0, unlocked,
                unlocked ? LoadSprite($"Stage/Landscape_{s}") : null);
        }
        // ── Row 1 col 1: 배경 ────────────────────────────────────────────
        {
            bool unlocked = ProgressManager.IsBackgroundUnlocked(s);
            SetCell(1, 1, unlocked,
                unlocked ? LoadSprite($"Stage/Background_{s}") : null);
        }
        // ── Row 1 col 2: 클리어 ──────────────────────────────────────────
        {
            bool unlocked = ProgressManager.IsClearUnlocked(s);
            SetCell(1, 2, unlocked,
                unlocked ? LoadSprite($"Stage/StageClear_{s}") : null);
        }
        // ── Row 1 col 3~7: 패배 이미지 (스테이지별 활성/비활성) ─────────
        // 위치는 BuildDetailPanel()에서 CX1[3~7]로 고정 — 여기서 재배치하지 않음
        var killerKeys  = GetStageKillerKeys(s);
        int activeCount = Mathf.Min(killerKeys.Count, 5);

        for (int k = 0; k < 5; k++)
        {
            if (defeatCellGOs[k] == null) continue;

            bool active = k < activeCount;
            defeatCellGOs[k].SetActive(active);
            if (defeatHdrTexts[k] != null) defeatHdrTexts[k].gameObject.SetActive(active);
            if (!active) continue;

            defeatHdrTexts[k].text = killerKeys[k].displayLabel;

            bool unlocked = ProgressManager.IsDefeatUnlocked(s, killerKeys[k].key);
            SetCell(1, 3 + k, unlocked,
                unlocked ? LoadSprite($"Defeat/GameOver_Stage{s}_{killerKeys[k].key}") : null);
        }

        // 패배 슬롯 활성 여부가 결정된 뒤 풀뷰 순서 재구성
        BuildFullViewOrder();
    }

    // ── 스테이지별 패배 이미지 키 목록 ───────────────────────────────────────

    private struct KillerKeyEntry
    {
        public string key;          // 영문 식별 키 (LastKillerName · 이미지 파일명 공용)
        public string displayLabel; // 갤러리 헤더 한국어 표시 이름
    }

    private List<KillerKeyEntry> GetStageKillerKeys(int stage)
    {
        // EnemySpawnManager 가 있으면 CSV 기반 동적 조회
        if (EnemySpawnManager.Instance != null)
        {
            var list    = new List<KillerKeyEntry>();
            var indices = EnemySpawnManager.Instance.GetUniqueEnemyIndices(stage);
            foreach (int idx in indices)
            {
                string k = EnemyDataTable.Contains(idx)
                    ? EnemyDataTable.Get(idx).Name.Replace(" ", "")
                    : $"Monster{idx}";
                list.Add(new KillerKeyEntry { key = k, displayLabel = KoreanLabel(k) + " 패배" });
            }
            list.Add(new KillerKeyEntry { key = "Trap", displayLabel = "함정 패배" });
            return list;
        }

        // EnemySpawnManager 가 없을 때 하드코딩 폴백
        return GetFallbackKillerKeys(stage);
    }

    /// <summary>
    /// EnemySpawnManager 가 씬에 없을 때 사용하는 정적 폴백.
    /// 기획서 v1.4 기준 스테이지별 몬스터 목록을 하드코딩.
    /// </summary>
    private static List<KillerKeyEntry> GetFallbackKillerKeys(int stage)
    {
        string[] keys = stage switch
        {
            1 => new[] { "Orc", "Goblin", "Centaur" },
            2 => new[] { "Slime", "Eyeball", "ShadowHand", "DarkGiant" },
            3 => new string[0], // TODO: 3스테이지 몬스터 미정
            _ => new string[0]
        };

        var list = new List<KillerKeyEntry>();
        foreach (var k in keys)
            list.Add(new KillerKeyEntry { key = k, displayLabel = KoreanLabel(k) + " 패배" });
        list.Add(new KillerKeyEntry { key = "Trap", displayLabel = "함정 패배" });
        return list;
    }

    /// <summary>영문 식별 키 → 갤러리 표시용 한국어 이름</summary>
    private static string KoreanLabel(string key) => key switch
    {
        "Orc"        => "오크",
        "Goblin"     => "고블린",
        "Centaur"    => "켄타우로스",
        "Slime"      => "슬라임",
        "Eyeball"    => "아이볼",
        "ShadowHand" => "그림자 손",
        "DarkGiant"  => "암흑 거인",
        _            => key   // 미등록 키는 영문 그대로
    };

    private void SetCell(int row, int col, bool unlocked, Sprite sprite)
    {
        if (cellImgs[row, col] == null) return; // 미사용 슬롯 방어

        lockIcons[row, col].SetActive(!unlocked);

        bool showImages = CheatManager.Instance != null && CheatManager.Instance.ShowSensitiveImages;

        if (!unlocked || !showImages)
        {
            cellImgs[row, col].sprite = null;
            cellImgs[row, col].color  = Color.clear;
        }
        else if (sprite != null)
        {
            cellImgs[row, col].sprite = sprite;
            cellImgs[row, col].color  = Color.white;
        }
        else
        {
            // 해금됐지만 파일 없음 → 어두운 채움
            cellImgs[row, col].sprite = null;
            cellImgs[row, col].color  = new Color(0.2f, 0.25f, 0.4f);
        }
    }

    private void OnCellClick(int row, int col)
    {
        // 풀뷰 순서에서 해당 셀 인덱스 찾기
        int idx = fullViewOrder.IndexOf((row, col));
        if (idx < 0) return;

        ShowFullViewAt(idx);
        fullViewPanel.SetActive(true);
    }

    private void ShowToast(string msg, float duration = 2f)
    {
        if (toastText == null) return;
        toastText.text = msg;
        toastTimer = duration;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private static Sprite LoadSprite(string path)
    {
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
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var txt = lgo.AddComponent<Text>();
        txt.fontSize = 22; txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return btn;
    }

    private Text MakeText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
        string content, int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var txt = go.AddComponent<Text>();
        txt.text = content; txt.fontSize = fontSize; txt.color = color;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        return txt;
    }
}
