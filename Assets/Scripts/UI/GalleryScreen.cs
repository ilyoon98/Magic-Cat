using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 갤러리 화면 — 스테이지 선택 → 해당 스테이지 이미지 목록
///
/// 스테이지별 8칸 구성:
///   Row 1 캐릭터 초상화: HP3 HP2 HP1 HP0
///   Row 2 스테이지·패배: 배경 클리어 Slime패배 BigSlime패배
///
/// 해금 조건:
///   HP3 초상화  — 항상 해금
///   HP2/1/0     — 해당 체력에 도달 시
///   배경        — 해당 스테이지 첫 진입 시
///   클리어      — 보스 클리어 시
///   Slime 패배  — Slime에게 패배 시
///   BigSlime 패배 — BigSlime에게 패배 시
/// </summary>
public class GalleryScreen : MonoBehaviour
{
    public static GalleryScreen Instance { get; private set; }

    // ── 데이터 정의 ──────────────────────────────────────────────────────
    private static readonly string[] StageNames  = { "원소마법사", "흑백마법사", "비전마법사" };
    private static readonly string[] KillerKeys  = { "Slime", "BigSlime", "Trap" };
    private static readonly string[] KillerNames = { "슬라임 패배", "빅슬라임 패배", "기타 패배" };

    // 행 1: 초상화 HP3·2·1·0·(빈칸) / 행 2: 배경·클리어·적패배×3
    private const int COLS = 5;
    private const int ROWS = 2;

    // ── UI 참조 ───────────────────────────────────────────────────────────
    private GameObject stageSelectPanel;   // 스테이지 선택 화면
    private GameObject detailPanel;        // 스테이지별 상세 화면

    // 상세: 셀 이미지·잠금 (row, col)
    private Image[,]      cellImgs  = new Image[ROWS, COLS];
    private GameObject[,] lockIcons = new GameObject[ROWS, COLS];
    private Text          detailTitle;

    private int currentStage = 0; // 현재 보고 있는 스테이지 (1~3)

    // 전체화면 감상
    private GameObject fullViewPanel;
    private Image      fullViewImage;

    // 잠금 셀 클릭 시 토스트 알림
    private Text  toastText;
    private float toastTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // 토스트 타이머
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f && toastText != null)
                toastText.text = "";
        }

        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // 우선순위: 전체화면 감상 → 상세 → 스테이지 선택 → 타이틀
        if (fullViewPanel != null && fullViewPanel.activeSelf)
        {
            fullViewPanel.SetActive(false);
        }
        else if (detailPanel != null && detailPanel.activeSelf)
        {
            detailPanel.SetActive(false);
            stageSelectPanel.SetActive(true);
        }
        else if (stageSelectPanel != null && stageSelectPanel.activeSelf)
        {
            Hide();
            TitleScreen.Instance?.Show();
        }
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

    // ── 스테이지 선택 패널 ────────────────────────────────────────────────
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

        // 스테이지 카드 3개
        float[] xPos = { -430f, 0f, 430f };
        Color[] colors =
        {
            new Color(0.35f, 0.65f, 1.00f),
            new Color(0.70f, 0.35f, 1.00f),
            new Color(0.95f, 0.85f, 0.30f),
        };
        for (int i = 0; i < 3; i++)
        {
            int stage = i + 1;
            var card = new GameObject($"StageCard{stage}");
            card.transform.SetParent(stageSelectPanel.transform, false);
            var crt = card.AddComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(xPos[i], 20f);
            crt.sizeDelta = new Vector2(340f, 280f);
            card.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.17f, 1f);

            // 컬러 바
            var bar = new GameObject("Bar"); bar.transform.SetParent(card.transform, false);
            var brt = bar.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0,1); brt.anchorMax = new Vector2(1,1);
            brt.pivot = new Vector2(0.5f,1f); brt.anchoredPosition = Vector2.zero; brt.sizeDelta = new Vector2(0,8);
            bar.AddComponent<Image>().color = colors[i];

            MakeText(card.transform, "StageLbl",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0, 60), new Vector2(300, 50),
                $"STAGE {stage}", 30, colors[i], TextAnchor.MiddleCenter);

            MakeText(card.transform, "CharLbl",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0, 10), new Vector2(300, 40),
                StageNames[i], 24, Color.white, TextAnchor.MiddleCenter);

            MakeText(card.transform, "CountLbl",
                new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
                new Vector2(0, -35), new Vector2(300, 32),
                "8장", 18, new Color(0.6f,0.7f,0.8f), TextAnchor.MiddleCenter);

            var btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors2 = btn.colors;
            colors2.highlightedColor = new Color(1.2f,1.2f,1.2f,1f);
            btn.colors = colors2;
            btn.onClick.AddListener(() => OpenDetail(stage));
        }

        // 닫기
        var closeBtn = MakeButton(stageSelectPanel.transform, "CloseBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(200, 52));
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

        // 행 레이블
        string[] rowLabels = { "캐릭터 초상화", "스테이지 · 패배" };
        float[]  rowY      = { 230f, -28f };
        for (int r = 0; r < ROWS; r++)
        {
            MakeText(detailPanel.transform, $"RowLabel{r}",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(62f, rowY[r] + 88f), new Vector2(160f, 30f),
                rowLabels[r], 16, new Color(0.55f, 0.65f, 0.8f), TextAnchor.MiddleCenter);
        }

        // 열 레이블 행1 (HP)
        string[] hpLabels    = { "HP 3 (풀피)", "HP 2", "HP 1", "HP 0", "" };
        string[] otherLabels = { "배경", "클리어", "슬라임 패배", "빅슬라임 패배", "기타 패배" };
        float[]  colX        = { -510f, -255f, 0f, 255f, 510f };

        for (int c = 0; c < COLS; c++)
        {
            // 행1 헤더
            MakeText(detailPanel.transform, $"HpHdr{c}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(colX[c], 330f), new Vector2(220f, 28f),
                hpLabels[c], 16, new Color(1f, 0.5f, 0.55f), TextAnchor.MiddleCenter);

            // 행2 헤더
            MakeText(detailPanel.transform, $"OtherHdr{c}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(colX[c], 60f), new Vector2(220f, 28f),
                otherLabels[c], 16, new Color(0.7f, 0.85f, 0.7f), TextAnchor.MiddleCenter);
        }

        // 셀 그리드 빌드
        float[] cellY = { 190f, -70f };
        for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
                BuildCell(detailPanel.transform, r, c, colX[c], cellY[r]);

        // 뒤로
        var backBtn = MakeButton(detailPanel.transform, "BackBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(200, 52));
        SetBtnStyle(backBtn, "← 뒤로", new Color(0.28f, 0.28f, 0.33f));
        backBtn.onClick.AddListener(() =>
        {
            detailPanel.SetActive(false);
            stageSelectPanel.SetActive(true);
        });

        // 잠금 클릭 토스트 (상세 패널 상단)
        toastText = MakeText(detailPanel.transform, "ToastText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -130), new Vector2(600, 44),
            "", 22, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
    }

    private void BuildCell(Transform parent, int row, int col, float xPos, float yPos)
    {
        var cell = new GameObject($"Cell_{row}_{col}");
        cell.transform.SetParent(parent, false);
        var crt = cell.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(xPos, yPos);
        crt.sizeDelta = new Vector2(160f, 230f);
        cell.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.20f);

        // 이미지 슬롯
        var imgGo = new GameObject("Img"); imgGo.transform.SetParent(cell.transform, false);
        var irt = imgGo.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4,4); irt.offsetMax = new Vector2(-4,-4);
        var img = imgGo.AddComponent<Image>();
        img.preserveAspect = true;
        cellImgs[row, col] = img;

        // 잠금 아이콘
        var lockGo = new GameObject("Lock"); lockGo.transform.SetParent(cell.transform, false);
        var lrt = lockGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        lockGo.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.13f, 0.92f);
        MakeText(lockGo.transform, "LockEmoji",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "🔒", 38, new Color(0.5f, 0.5f, 0.6f), TextAnchor.MiddleCenter);
        lockIcons[row, col] = lockGo;

        // 클릭 버튼
        int r = row, c = col;
        var btn = cell.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => OnCellClick(r, c));
    }

    // ── 전체화면 패널 ─────────────────────────────────────────────────────
    private void BuildFullViewPanel(Transform canvasRoot)
    {
        fullViewPanel = new GameObject("GalleryFullView");
        fullViewPanel.transform.SetParent(canvasRoot, false);
        var rt = fullViewPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        fullViewPanel.AddComponent<Image>().color = new Color(0,0,0, 0.94f);

        fullViewImage = new GameObject("FullImg").AddComponent<Image>();
        fullViewImage.transform.SetParent(fullViewPanel.transform, false);
        var irt = fullViewImage.rectTransform;
        irt.anchorMin = new Vector2(0.08f, 0.08f);
        irt.anchorMax = new Vector2(0.92f, 0.92f);
        irt.sizeDelta = Vector2.zero;
        fullViewImage.preserveAspect = true;

        var closeBtn = MakeButton(fullViewPanel.transform, "FVClose",
            new Vector2(1f,1f), new Vector2(-70,-70), new Vector2(120,50));
        SetBtnStyle(closeBtn, "✕ 닫기", new Color(0.5f,0.2f,0.2f));
        closeBtn.onClick.AddListener(() => fullViewPanel.SetActive(false));
        fullViewPanel.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 공개 API
    // ═══════════════════════════════════════════════════════════════════════
    public void Show()
    {
        stageSelectPanel.SetActive(true);
        detailPanel.SetActive(false);
        fullViewPanel.SetActive(false);
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
        RefreshDetail();
    }

    /// <summary>갤러리 상태 전체 갱신 (PortraitPanel·CheatPanel 등 외부에서 호출)</summary>
    public void RefreshAll()
    {
        if (currentStage > 0 && detailPanel.activeSelf)
            RefreshDetail();
    }

    /// <summary>CheatPanel 토글 시 즉시 반영용 (RefreshAll 별칭)</summary>
    public void RefreshImageBlocker() => RefreshAll();

    private void RefreshDetail()
    {
        int s = currentStage;
        int[] hpOrder = { 3, 2, 1, 0 };

        // Row 0: 초상화 HP3·2·1·0
        for (int c = 0; c < 4; c++)
        {
            int hp      = hpOrder[c];
            bool unlocked = ProgressManager.IsGalleryUnlocked(s, hp);
            SetCell(0, c, unlocked,
                unlocked ? LoadSprite($"Portraits/Stage{s}_HP{hp}") : null);
        }

        // Row 1 col 0: 배경
        {
            bool unlocked = ProgressManager.IsBackgroundUnlocked(s);
            SetCell(1, 0, unlocked,
                unlocked ? LoadSprite($"Stage/Background_{s}") : null);
        }
        // Row 1 col 1: 클리어
        {
            bool unlocked = ProgressManager.IsClearUnlocked(s);
            SetCell(1, 1, unlocked,
                unlocked ? LoadSprite($"Stage/StageClear_{s}") : null);
        }
        // Row 1 col 2·3·4: 몬스터/기타 패배
        for (int k = 0; k < KillerKeys.Length && k < 3; k++)
        {
            bool unlocked = ProgressManager.IsDefeatUnlocked(s, KillerKeys[k]);
            SetCell(1, 2 + k, unlocked,
                unlocked ? LoadSprite($"Defeat/GameOver_Stage{s}_{KillerKeys[k]}") : null);
        }
    }

    private void SetCell(int row, int col, bool unlocked, Sprite sprite)
    {
        lockIcons[row, col].SetActive(!unlocked);

        bool showImages = CheatManager.Instance != null && CheatManager.Instance.ShowSensitiveImages;

        if (!unlocked)
        {
            // 잠금: 이미지 숨김
            cellImgs[row, col].sprite = null;
            cellImgs[row, col].color  = Color.clear;
        }
        else if (!showImages)
        {
            // 해금됐지만 이미지 보기 OFF: 셀 배경만 표시 (이미지는 투명 처리)
            cellImgs[row, col].sprite = null;
            cellImgs[row, col].color  = Color.clear;
        }
        else if (sprite != null)
        {
            // 해금 + 이미지 보기 ON + 이미지 있음
            cellImgs[row, col].sprite = sprite;
            cellImgs[row, col].color  = Color.white;
        }
        else
        {
            // 해금 + 이미지 보기 ON + 이미지 파일 없음
            cellImgs[row, col].sprite = null;
            cellImgs[row, col].color  = new Color(0.2f, 0.25f, 0.4f);
        }
    }

    private void OnCellClick(int row, int col)
    {
        // 잠금 상태
        if (lockIcons[row, col] != null && lockIcons[row, col].activeSelf)
        {
            ShowToast("🔒 아직 열리지 않은 이미지입니다");
            return;
        }

        var img = cellImgs[row, col];
        if (img.sprite == null)
        {
            ShowToast("이미지 파일을 찾을 수 없습니다");
            return;
        }
        fullViewImage.sprite = img.sprite;
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
        return Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f));
    }

    private void SetBtnStyle(Button btn, string label, Color color)
    {
        btn.GetComponent<Image>().color = color;
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
