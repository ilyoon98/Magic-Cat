using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 캐릭터 정보 UI
/// - 상단 바 좌측: 스테이지 · 캐릭터 이름 · HP · 상태 (GameUI의 TopPanel에 주입)
/// - 우측 대형 초상화 (HP에 따라 교체)
/// - 좌하단 스킬 바 (Q / E)
/// </summary>
public class PortraitPanel : MonoBehaviour
{
    public static PortraitPanel Instance { get; private set; }

    // ── 상단 바에 주입되는 텍스트 ─────────────────────────────────────────
    private Text stageText;
    private Text charNameText;
    private Text hpText;
    private Text statusText;

    // ── 스킬 아이콘 시스템 ────────────────────────────────────────────────
    private Image[] skillFillRings = new Image[2];
    private Image[] skillIconImgs  = new Image[2];
    private Text[]  skillNameTxts  = new Text[2];
    private Text[]  skillCdTxts    = new Text[2];

    // ── 우측 대형 초상화 ──────────────────────────────────────────────────
    private Image bigPortraitImage;

    // ── 흑/백 게이지 UI (2스테이지 전용) ────────────────────────────────
    private GameObject gaugeRoot;
    private UnityEngine.UI.Image blackGaugeFill;
    private UnityEngine.UI.Image whiteGaugeFill;

    // ── HP 기반 초상화 ────────────────────────────────────────────────────
    private Sprite[] currentHpSprites = new Sprite[4];
    private int      lowestHpReached  = 3;
    private int      currentStage     = 1;

    private static readonly Color[] SkillRingColors =
    {
        new Color(0.95f, 0.85f, 0.25f), // Q — 노란색
        new Color(0.30f, 0.80f, 1.00f), // E — 하늘색
    };

    private Sprite circleSprite;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        circleSprite = MakeCircleSprite(64);
    }

    // topBar = GameUI가 미리 생성해서 넘겨주는 TopPanel의 Transform
    public void Build(Transform canvasRoot, Transform topBar)
    {
        BuildBigPortrait(canvasRoot);
        BuildBottomSkillBar(canvasRoot);
        BuildGaugeBar(canvasRoot);   // 흑/백 게이지 (2스테이지 전용, 초기 비표시)
        if (topBar != null) BuildTopBarInfo(topBar);
    }

    // ── 상단 바 좌측 정보 영역 ───────────────────────────────────────────
    private void BuildTopBarInfo(Transform topBar)
    {
        // 상단 바의 0% ~ 42% 범위를 사용
        var root = new GameObject("TopBarInfo");
        root.transform.SetParent(topBar, false);
        var rrt = root.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0f, 0f);
        rrt.anchorMax = new Vector2(0.42f, 1f);
        rrt.offsetMin = new Vector2(14f, 0f);
        rrt.offsetMax = Vector2.zero;

        // STAGE X (왼쪽 15%)
        stageText = MakeTopText(root.transform, "Stage",
            new Vector2(0f, 0f), new Vector2(0.20f, 1f),
            "STAGE 1", 19, new Color(0.65f, 0.82f, 1f));

        // 구분선
        MakeDivider(root.transform, 0.205f);

        // 캐릭터 이름 (15~35%)
        charNameText = MakeTopText(root.transform, "CharName",
            new Vector2(0.215f, 0f), new Vector2(0.44f, 1f),
            "원소마법사", 19, Color.white);

        // 구분선
        MakeDivider(root.transform, 0.445f);

        // HP 하트 (35~65%)
        hpText = MakeTopText(root.transform, "HP",
            new Vector2(0.455f, 0f), new Vector2(0.70f, 1f),
            "♥♥♥", 50, new Color(0.4f, 1f, 0.5f));

        // 구분선
        MakeDivider(root.transform, 0.705f);

        // 원소/모드 상태 (65~100%)
        statusText = MakeTopText(root.transform, "Status",
            new Vector2(0.715f, 0f), new Vector2(1.00f, 1f),
            "🔥 불", 18, new Color(1f, 0.65f, 0.3f));
    }

    private void MakeDivider(Transform parent, float anchorX)
    {
        var go = new GameObject("Div");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(anchorX, 0.15f);
        rt.anchorMax = new Vector2(anchorX + 0.003f, 0.85f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = new Color(0.4f, 0.5f, 0.7f, 0.45f);
    }

    // ── 좌하단 스킬 바 ────────────────────────────────────────────────────
    private void BuildBottomSkillBar(Transform canvasRoot)
    {
        var bar = new GameObject("SkillBar");
        bar.transform.SetParent(canvasRoot, false);
        var brt = bar.AddComponent<RectTransform>();
        brt.anchorMin        = new Vector2(0f, 0f);
        brt.anchorMax        = new Vector2(0f, 0f);
        brt.pivot            = new Vector2(0f, 0f);
        brt.anchoredPosition = new Vector2(10f, 10f);
        brt.sizeDelta        = new Vector2(470f, 200f);
        var bg = bar.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.04f, 0.08f, 0.88f);

        // 테두리
        var border = new GameObject("Border");
        border.transform.SetParent(bar.transform, false);
        var bord = border.AddComponent<RectTransform>();
        bord.anchorMin = Vector2.zero; bord.anchorMax = Vector2.one;
        bord.offsetMin = new Vector2(1f, 1f); bord.offsetMax = new Vector2(-1f, -1f);
        border.AddComponent<Image>().color = new Color(0.3f, 0.45f, 0.7f, 0.3f);

        BuildSkillIconSlot(bar.transform, 0, "Q",
            new Vector2(0f, 0f), new Vector2(0.5f, 1f));
        BuildSkillIconSlot(bar.transform, 1, "E",
            new Vector2(0.5f, 0f), new Vector2(1f, 1f));
    }

    // ── 스킬 아이콘 슬롯 빌더 ────────────────────────────────────────────
    private void BuildSkillIconSlot(Transform parent, int idx, string key,
                                     Vector2 anchorMin, Vector2 anchorMax)
    {
        var slot = new GameObject($"Skill{key}Slot");
        slot.transform.SetParent(parent, false);
        var srt = slot.AddComponent<RectTransform>();
        srt.anchorMin = anchorMin; srt.anchorMax = anchorMax;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

        // [Q] / [E] 키 레이블 (상단)
        var keyLbl = new GameObject($"Key{key}");
        keyLbl.transform.SetParent(slot.transform, false);
        var klrt = keyLbl.AddComponent<RectTransform>();
        klrt.anchorMin = new Vector2(0f, 0.84f); klrt.anchorMax = new Vector2(1f, 1f);
        klrt.offsetMin = klrt.offsetMax = Vector2.zero;
        var kTxt = keyLbl.AddComponent<Text>();
        kTxt.text = $"[{key}]"; kTxt.fontSize = 32; kTxt.color = new Color(0.55f, 0.68f, 0.88f);
        kTxt.alignment = TextAnchor.MiddleCenter;
        kTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 원형 아이콘 (중앙 ~ 살짝 위)
        var iconRoot = new GameObject($"IconRoot{idx}");
        iconRoot.transform.SetParent(slot.transform, false);
        var irrt = iconRoot.AddComponent<RectTransform>();
        irrt.anchorMin        = new Vector2(0.5f, 0.55f);
        irrt.anchorMax        = new Vector2(0.5f, 0.55f);
        irrt.pivot            = new Vector2(0.5f, 0.5f);
        irrt.anchoredPosition = Vector2.zero;
        irrt.sizeDelta        = new Vector2(110f, 110f);

        // ① 배경 원
        var bgCircle = new GameObject("BG");
        bgCircle.transform.SetParent(iconRoot.transform, false);
        var bgRt = bgCircle.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        var bgImg = bgCircle.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        bgImg.color  = new Color(0.18f, 0.20f, 0.28f);

        // ② 쿨타운 링 (Radial360)
        var ringGo = new GameObject("Ring");
        ringGo.transform.SetParent(iconRoot.transform, false);
        var rrt = ringGo.AddComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.sizeDelta = Vector2.zero;
        var ring = ringGo.AddComponent<Image>();
        ring.sprite     = circleSprite;
        ring.color      = SkillRingColors[idx];
        ring.type       = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillAmount = 1f;
        skillFillRings[idx] = ring;

        // ③ 중심 어두운 원
        var centerGo = new GameObject("Center");
        centerGo.transform.SetParent(iconRoot.transform, false);
        var crt2 = centerGo.AddComponent<RectTransform>();
        crt2.anchorMin = crt2.anchorMax = new Vector2(0.5f, 0.5f);
        crt2.pivot     = new Vector2(0.5f, 0.5f);
        crt2.sizeDelta = new Vector2(78f, 78f);
        var cImg = centerGo.AddComponent<Image>();
        cImg.sprite = circleSprite;
        cImg.color  = new Color(0.07f, 0.08f, 0.13f);

        // ④ 스킬 아이콘 이미지
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(iconRoot.transform, false);
        var irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot     = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(62f, 62f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        skillIconImgs[idx] = iconImg;

        // 스킬 이름 (하단 18%)
        var nameGo = new GameObject($"SkillName{idx}");
        nameGo.transform.SetParent(slot.transform, false);
        var nrt = nameGo.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0.00f); nrt.anchorMax = new Vector2(1f, 0.28f);
        nrt.offsetMin = nrt.offsetMax = Vector2.zero;
        var nameTxt = nameGo.AddComponent<Text>();
        nameTxt.text      = "---";
        nameTxt.fontSize  = 30;
        nameTxt.color     = new Color(0.80f, 0.84f, 0.92f);
        nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTxt.supportRichText = true;
        skillNameTxts[idx] = nameTxt;

        // 쿨타임 텍스트 (스킬 이름 아래 — 슬롯 최하단 10%)
        // 이름 텍스트 아래 공간 없으므로 이름에 합쳐서 두 줄 표시
        skillCdTxts[idx] = nameTxt; // 쿨타임 정보는 이름 텍스트에 두 번째 줄로 표시
    }

    // ── 흑/백 게이지 바 (SkillBar 바로 위, 2스테이지에서만 표시) ────────
    private void BuildGaugeBar(Transform canvasRoot)
    {
        // 전체 루트 (SkillBar 위쪽)
        gaugeRoot = new GameObject("GaugeBar");
        gaugeRoot.transform.SetParent(canvasRoot, false);
        var grt = gaugeRoot.AddComponent<RectTransform>();
        grt.anchorMin        = new Vector2(0f, 0f);
        grt.anchorMax        = new Vector2(0f, 0f);
        grt.pivot            = new Vector2(0f, 0f);
        grt.anchoredPosition = new Vector2(10f, 215f); // SkillBar(y=10+200=210) 위 5px
        grt.sizeDelta        = new Vector2(470f, 28f);
        gaugeRoot.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.80f);

        // 흑 게이지 (왼쪽 절반)
        blackGaugeFill = BuildGaugeSlot(gaugeRoot.transform, "BlackGauge",
            new Vector2(0f, 0f), new Vector2(0.5f, 1f),
            new Color(0.35f, 0.10f, 0.55f),   // 어두운 보라
            new Color(0.60f, 0.20f, 0.90f),   // 채워진 보라
            "⬛ 흑");

        // 백 게이지 (오른쪽 절반)
        whiteGaugeFill = BuildGaugeSlot(gaugeRoot.transform, "WhiteGauge",
            new Vector2(0.5f, 0f), new Vector2(1f, 1f),
            new Color(0.30f, 0.30f, 0.40f),   // 어두운 회색
            new Color(0.85f, 0.85f, 1.00f),   // 채워진 흰빛
            "⬜ 백");

        gaugeRoot.SetActive(false); // 2스테이지 진입 시 SetStage에서 활성화
    }

    private Image BuildGaugeSlot(Transform parent, string name,
                                  Vector2 anchorMin, Vector2 anchorMax,
                                  Color bgColor, Color fillColor, string label)
    {
        // 배경
        var bg = new GameObject($"{name}BG");
        bg.transform.SetParent(parent, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = anchorMin; bgRt.anchorMax = anchorMax;
        bgRt.offsetMin = new Vector2(2f, 2f); bgRt.offsetMax = new Vector2(-2f, -2f);
        bg.AddComponent<Image>().color = bgColor;

        // 채움 바 (Filled)
        var fill = new GameObject($"{name}Fill");
        fill.transform.SetParent(bg.transform, false);
        var frt = fill.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(0.5f, 1f); // 50% 초기값
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type  = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.5f;

        // 레이블
        var lbl = new GameObject($"{name}Label");
        lbl.transform.SetParent(bg.transform, false);
        var lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = lbl.AddComponent<Text>();
        txt.text      = label;
        txt.fontSize  = 13;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return fillImg;
    }

    // ── 우측 대형 초상화 ──────────────────────────────────────────────────
    private void BuildBigPortrait(Transform canvasRoot)
    {
        var frame = new GameObject("BigPortraitFrame");
        frame.transform.SetParent(canvasRoot, false);
        var frt = frame.AddComponent<RectTransform>();
        frt.anchorMin        = new Vector2(1f, 0f);
        frt.anchorMax        = new Vector2(1f, 1f);
        frt.pivot            = new Vector2(1f, 0.5f);
        frt.anchoredPosition = new Vector2(-58f, -40f); // 좌측 50, 아래 40
        frt.sizeDelta        = new Vector2(460f, -56f); // 상하로 56 줄임 (pivot 중앙이므로 각 28씩)
        frame.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        bigPortraitImage = new GameObject("BigPortraitImage").AddComponent<Image>();
        bigPortraitImage.transform.SetParent(frame.transform, false);
        var irt = bigPortraitImage.rectTransform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4f,  4f);
        irt.offsetMax = new Vector2(-4f, -4f);
        bigPortraitImage.color = Color.white;
        bigPortraitImage.preserveAspect = true;
        bigPortraitImage.gameObject.SetActive(false);
    }

    // ── 인게임 전용 초상화 로드 ──────────────────────────────────────────
    // 우선순위: Characters/Stage{N}_HP{H} → 없으면 Portraits/Stage{N}_HP{H} 폴백
    // (갤러리는 Portraits/ 를 독립적으로 로드)
    private void LoadPortraitsForStage(int stage)
    {
        for (int i = 0; i < 4; i++)
        {
            string[] paths =
            {
                $"Characters/Stage{stage}_HP{i}",   // 인게임 전용 경로 우선
                $"Portraits/Stage{stage}_HP{i}",    // 갤러리 경로 폴백
            };
            Sprite sp = null;
            foreach (var path in paths)
            {
                sp = Resources.Load<Sprite>(path);
                if (sp == null)
                {
                    Texture2D tex = Resources.Load<Texture2D>(path);
                    if (tex != null)
                        sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), 100f);
                }
                if (sp != null) break;
            }
            currentHpSprites[i] = sp;
        }
    }

    // ── 스킬 아이콘 로드 ─────────────────────────────────────────────────
    private void LoadSkillIcons(int stage)
    {
        string[] keys = { "Q", "E" };
        for (int i = 0; i < 2; i++)
        {
            string path = $"SkillIcons/Stage{stage}{keys[i]}";
            Sprite sp = Resources.Load<Sprite>(path);
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
            }
            if (skillIconImgs[i] != null)
            {
                skillIconImgs[i].sprite = sp;
                skillIconImgs[i].color  = sp != null ? Color.white : new Color(0.5f, 0.55f, 0.6f);
            }
        }
    }

    private void ApplyHpPortrait(int hp)
    {
        if (hp < lowestHpReached)
        {
            lowestHpReached = hp;
            ProgressManager.UnlockGallery(currentStage, hp);
            GalleryScreen.Instance?.RefreshAll();
        }
        else
        {
            lowestHpReached = Mathf.Min(lowestHpReached, hp);
        }

        int lowIdx = Mathf.Clamp(lowestHpReached, 0, 3);
        ApplyBigPortrait(currentHpSprites[lowIdx]);
    }

    private void ApplyBigPortrait(Sprite sprite)
    {
        if (bigPortraitImage == null) return;

        // 민감 이미지 숨기기: ShowSensitiveImages가 꺼져 있으면 초상화 비표시
        bool show = CheatManager.Instance == null || CheatManager.Instance.ShowSensitiveImages;
        if (!show || sprite == null)
        {
            bigPortraitImage.gameObject.SetActive(false);
            return;
        }

        bigPortraitImage.sprite = sprite;
        bigPortraitImage.gameObject.SetActive(true);
    }

    public void SetPortraitSprite(Sprite sprite) => ApplyBigPortrait(sprite);

    // ── Refresh ───────────────────────────────────────────────────────────
    public void Refresh(PlayerUnit player)
    {
        if (player == null) return;

        if (stageText != null)
            stageText.text = $"STAGE {currentStage}";

        // HP 하트
        if (hpText != null)
        {
            string hearts = "";
            for (int i = 0; i < player.maxHp; i++)
                hearts += i < player.currentHp ? "♥" : "♡";
            hpText.text  = hearts;
            hpText.color = player.currentHp <= 1
                ? new Color(1f, 0.3f, 0.3f)
                : new Color(0.4f, 1f, 0.5f);
        }

        ApplyHpPortrait(player.currentHp);

        // 캐릭터별 상태
        if (statusText != null)
        {
            if (player is ElementalPlayerUnit ep)
            {
                statusText.gameObject.SetActive(true);
                (string label, Color col) = ep.CurrentElement switch
                {
                    ElementalPlayerUnit.Element.Fire  => ("🔥 불",  new Color(1f, 0.5f, 0.2f)),
                    ElementalPlayerUnit.Element.Earth => ("⬡ 땅",  new Color(0.9f, 0.8f, 0.2f)),
                    ElementalPlayerUnit.Element.Wood  => ("✿ 나무", new Color(0.3f, 0.9f, 0.3f)),
                    ElementalPlayerUnit.Element.Water => ("◈ 물",  new Color(0.3f, 0.7f, 1f)),
                    _                                 => ("?", Color.white)
                };
                statusText.text  = label;
                statusText.color = col;
            }
            else if (player is BlackWhitePlayerUnit bw)
            {
                statusText.gameObject.SetActive(true);
                if (bw.CurrentMode == BlackWhitePlayerUnit.Mode.Black)
                { statusText.text = "⬛ 흑"; statusText.color = new Color(0.65f, 0.3f, 1f); }
                else
                { statusText.text = "⬜ 백"; statusText.color = new Color(0.9f, 0.9f, 1f); }
            }
            else if (player is ArcanePlayerUnit)
            {
                statusText.gameObject.SetActive(true);
                var tp = player.GetSkill(1) as Skill_Teleport;
                int charges = tp != null ? tp.Charges    : 0;
                int maxCh   = tp != null ? tp.maxCharges : 3;
                statusText.text  = $"⚡×{charges}/{maxCh}";
                statusText.color = new Color(0.9f, 0.85f, 0.3f);
            }
            else
            {
                statusText.gameObject.SetActive(false);
            }
        }

        // 스킬 아이콘
        RefreshSkillIcon(0, player.GetSkill(1), "Q");
        RefreshSkillIcon(1, player.GetSkill(2), "E");

        // 흑/백 게이지 갱신 (2스테이지 BlackWhitePlayerUnit)
        if (player is BlackWhitePlayerUnit bw)
        {
            if (blackGaugeFill != null) blackGaugeFill.fillAmount = bw.BlackGauge / 100f;
            if (whiteGaugeFill != null) whiteGaugeFill.fillAmount = bw.WhiteGauge / 100f;
        }
    }

    private void RefreshSkillIcon(int idx, SkillBase skill, string key)
    {
        if (skill == null)
        {
            if (skillNameTxts[idx] != null) skillNameTxts[idx].text = "---";
            if (skillFillRings[idx] != null) skillFillRings[idx].fillAmount = 0f;
            return;
        }

        // 충전형 스킬 (Skill_Teleport)
        if (skill is Skill_Teleport tp)
        {
            float fill = tp.maxCharges > 0 ? (float)tp.Charges / tp.maxCharges : 1f;
            if (skillFillRings[idx] != null)
            {
                skillFillRings[idx].fillAmount = fill;
                skillFillRings[idx].color = tp.Charges > 0
                    ? SkillRingColors[idx] : new Color(0.35f, 0.35f, 0.4f);
            }
            if (skillNameTxts[idx] != null)
                skillNameTxts[idx].text = tp.Charges > 0
                    ? $"{skill.skillName}\n⚡×{tp.Charges}"
                    : $"{skill.skillName}\n충전중";
            return;
        }

        // 일반 쿨타임 스킬 (원소별 개별 쿨타임 스킬은 DisplayCooldown이 현재 원소 기준으로 오버라이드됨)
        int cd    = skill.DisplayCooldown;
        int maxCd = skill.maxCooldown;
        float fillAmt = maxCd <= 0 ? 1f : 1f - (cd / (float)maxCd);

        if (skillFillRings[idx] != null)
        {
            skillFillRings[idx].fillAmount = fillAmt;
            skillFillRings[idx].color = cd <= 0
                ? SkillRingColors[idx] : new Color(0.35f, 0.35f, 0.4f);
        }

        if (skillNameTxts[idx] != null)
        {
            skillNameTxts[idx].text = cd <= 0
                ? $"{skill.skillName}\n<color=#88EE88>준비됨</color>"
                : $"{skill.skillName}\n<color=#CC8866>⏳{cd}턴</color>";
        }
    }

    public void SetStage(int stage)
    {
        currentStage    = stage;
        lowestHpReached = 3;
        if (stageText != null) stageText.text = $"STAGE {stage}";
        LoadPortraitsForStage(stage);
        LoadSkillIcons(stage);

        // 흑/백 게이지는 2스테이지에서만 표시
        if (gaugeRoot != null)
            gaugeRoot.SetActive(stage == 2);
    }

    public void SetCharacterInfo(string charName, string portrait)
    {
        if (charNameText != null) charNameText.text = charName;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private Text MakeTopText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        string content, int fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        return txt;
    }

    private static Sprite MakeCircleSprite(int size)
    {
        var tex    = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float c = size / 2f, r = c - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                pixels[y * size + x] = dx * dx + dy * dy <= r * r
                    ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
