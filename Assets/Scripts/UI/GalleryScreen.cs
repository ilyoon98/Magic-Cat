using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 갤러리 화면 — 해금된 캐릭터 일러스트 열람
///
/// 구성: 3스테이지 × 4 HP = 12장
/// HP3(풀피) → 항상 해금
/// HP2,1,0   → 게임 중 해당 체력에 도달 시 해금
///
/// 해금된 이미지는 클릭하면 전체화면으로 감상 가능
/// </summary>
public class GalleryScreen : MonoBehaviour
{
    public static GalleryScreen Instance { get; private set; }

    private GameObject panel;
    private GameObject fullViewPanel;
    private Image      fullViewImage;

    // 각 셀의 Image 참조 (갱신용)
    private Image[,]   cellImages  = new Image[3, 4];  // [stage-1, hp]
    private GameObject[,] lockIcons = new GameObject[3, 4];

    // 스테이지별 캐릭터 이름
    private static readonly string[] StageNames = { "원소마법사", "흑백마법사", "비전마법사" };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(Transform canvasRoot)
    {
        // ── 배경 패널 ─────────────────────────────────────────────────────
        panel = new GameObject("GalleryScreen");
        panel.transform.SetParent(canvasRoot, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 1f);

        // ── 제목 ──────────────────────────────────────────────────────────
        MakeText(panel.transform, "GalleryTitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -70), new Vector2(600, 70),
            "갤 러 리", 48, new Color(0.85f, 0.88f, 1f), TextAnchor.MiddleCenter);

        MakeText(panel.transform, "GallerySub",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -130), new Vector2(700, 38),
            "체력이 낮아질수록 이미지가 해금됩니다", 20,
            new Color(0.55f, 0.65f, 0.75f), TextAnchor.MiddleCenter);

        // ── 이미지 그리드 (3스테이지 × 4HP) ─────────────────────────────
        // HP 순서: 3(풀) → 2 → 1 → 0(사망 직전)
        int[] hpOrder = { 3, 2, 1, 0 };
        string[] hpLabels = { "❤❤❤", "❤❤", "❤", "빈피" };

        // 열 헤더 (스테이지명)
        float[] stageX = { -570f, 0f, 570f };
        for (int s = 0; s < 3; s++)
        {
            MakeText(panel.transform, $"StageHeader{s}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(stageX[s], 245), new Vector2(330, 38),
                $"Stage {s + 1} · {StageNames[s]}", 20,
                new Color(0.75f, 0.85f, 1f), TextAnchor.MiddleCenter);
        }

        // 행 헤더 (HP)
        float[] hpY = { 175f, 25f, -125f, -275f };
        for (int h = 0; h < 4; h++)
        {
            MakeText(panel.transform, $"HPHeader{h}",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(72, hpY[h]), new Vector2(100, 34),
                hpLabels[h], 18, new Color(1f, 0.5f, 0.55f), TextAnchor.MiddleCenter);
        }

        // 셀 빌드
        for (int s = 0; s < 3; s++)
        {
            for (int h = 0; h < 4; h++)
            {
                int hp = hpOrder[h];
                BuildCell(panel.transform, s, h, hp, stageX[s], hpY[h]);
            }
        }

        // ── 닫기 버튼 ─────────────────────────────────────────────────────
        var closeBtn = MakeButton(panel.transform, "CloseBtn",
            new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(200, 52));
        SetBtnStyle(closeBtn, "닫기", new Color(0.28f, 0.28f, 0.33f));
        closeBtn.onClick.AddListener(Hide);

        // ── 전체화면 감상 패널 ────────────────────────────────────────────
        fullViewPanel = new GameObject("FullView");
        fullViewPanel.transform.SetParent(panel.transform, false);
        var frt = fullViewPanel.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.sizeDelta = Vector2.zero;
        fullViewPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.94f);

        fullViewImage = new GameObject("FullImg").AddComponent<Image>();
        fullViewImage.transform.SetParent(fullViewPanel.transform, false);
        var irt = fullViewImage.rectTransform;
        irt.anchorMin = new Vector2(0.1f, 0.1f);
        irt.anchorMax = new Vector2(0.9f, 0.9f);
        irt.sizeDelta = Vector2.zero;
        fullViewImage.preserveAspect = true;

        var fvClose = MakeButton(fullViewPanel.transform, "FVClose",
            new Vector2(1f, 1f), new Vector2(-70, -70), new Vector2(120, 50));
        SetBtnStyle(fvClose, "✕ 닫기", new Color(0.5f, 0.2f, 0.2f));
        fvClose.onClick.AddListener(() => fullViewPanel.SetActive(false));

        fullViewPanel.SetActive(false);
        panel.SetActive(false);
    }

    private void BuildCell(Transform parent, int stageIdx, int hpIdx, int hp,
                            float xPos, float yPos)
    {
        // 셀 배경
        var cell = new GameObject($"Cell_S{stageIdx + 1}_HP{hp}");
        cell.transform.SetParent(parent, false);
        var crt = cell.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(xPos, yPos);
        crt.sizeDelta = new Vector2(290, 130);
        cell.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.20f);

        // 이미지 슬롯
        var imgSlot = new GameObject("ImgSlot");
        imgSlot.transform.SetParent(cell.transform, false);
        var irt = imgSlot.AddComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 0f); irt.anchorMax = new Vector2(0.55f, 1f);
        irt.offsetMin = new Vector2(4, 4); irt.offsetMax = new Vector2(-2, -4);
        var img = imgSlot.AddComponent<Image>();
        img.preserveAspect = true;
        cellImages[stageIdx, hpIdx] = img;

        // 잠금 아이콘 (잠긴 상태)
        var lockGo = new GameObject("LockIcon");
        lockGo.transform.SetParent(cell.transform, false);
        var lrt = lockGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var lockImg = lockGo.AddComponent<Image>();
        lockImg.color = new Color(0.08f, 0.09f, 0.14f, 0.9f);
        MakeText(lockGo.transform, "LockEmoji",
            new Vector2(0f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero,
            "🔒", 36, new Color(0.5f, 0.5f, 0.6f), TextAnchor.MiddleCenter);
        lockIcons[stageIdx, hpIdx] = lockGo;

        // 설명 텍스트
        string hpLabel = hp switch { 3 => "풀피", 2 => "HP 2", 1 => "HP 1", _ => "HP 0" };
        MakeText(cell.transform, "Label",
            new Vector2(0.55f, 0.5f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            hpLabel, 18, new Color(0.75f, 0.8f, 0.9f), TextAnchor.MiddleCenter);

        // 클릭 버튼 (해금된 경우)
        int s = stageIdx + 1, h = hp; // capture
        var btn = cell.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => OnCellClick(s, h));
    }

    // ── 공개 API ──────────────────────────────────────────────────────────
    public void Show()
    {
        RefreshAll();
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    /// <summary>HP가 낮아질 때 PortraitPanel에서 호출</summary>
    public void RefreshAll()
    {
        int[] hpOrder = { 3, 2, 1, 0 };
        for (int s = 0; s < 3; s++)
        {
            for (int hIdx = 0; hIdx < 4; hIdx++)
            {
                int hp        = hpOrder[hIdx];
                bool unlocked = ProgressManager.IsGalleryUnlocked(s + 1, hp);

                lockIcons[s, hIdx].SetActive(!unlocked);

                if (unlocked)
                {
                    // 이미지 로드 (미리 로드되지 않은 경우)
                    if (cellImages[s, hIdx].sprite == null)
                    {
                        var sprite = LoadPortrait(s + 1, hp);
                        cellImages[s, hIdx].sprite = sprite;
                        cellImages[s, hIdx].color  = sprite != null ? Color.white : new Color(0.2f, 0.25f, 0.4f);
                    }
                }
                else
                {
                    cellImages[s, hIdx].sprite = null;
                    cellImages[s, hIdx].color  = Color.clear;
                }
            }
        }
    }

    private void OnCellClick(int stage, int hp)
    {
        if (!ProgressManager.IsGalleryUnlocked(stage, hp)) return;

        var sprite = LoadPortrait(stage, hp);
        if (sprite == null) return;

        fullViewImage.sprite = sprite;
        fullViewPanel.SetActive(true);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private Sprite LoadPortrait(int stage, int hp)
    {
        string path = $"Portraits/Stage{stage}_HP{hp}";
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private void SetBtnStyle(Button btn, string label, Color color)
    {
        btn.GetComponent<Image>().color = color;
        btn.GetComponentInChildren<Text>().text = label;
    }

    private Button MakeButton(Transform parent, string name,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var txt = lgo.AddComponent<Text>();
        txt.fontSize = 22; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
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
        txt.text = content; txt.fontSize = fontSize;
        txt.color = color; txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        return txt;
    }
}
