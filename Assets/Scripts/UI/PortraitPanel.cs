using UnityEngine;
using UnityEngine.UI;

public class PortraitPanel : MonoBehaviour
{
    public static PortraitPanel Instance { get; private set; }

    private Text  stageText;
    private Text  charNameText;
    private Image portraitImage;    // ← 캐릭터 스프라이트 이미지 슬롯
    private Text  portraitFallback; // 스프라이트 없을 때 이모지 표시
    private Text  hpText;
    private Text  statusText;
    private int   currentStage = 1;

    // ── 스킬 아이콘 시스템 ────────────────────────────────────────────────
    private Image[] skillFillRings = new Image[2]; // 원형 쿨다운 링
    private Image[] skillIconImgs  = new Image[2]; // 스킬 아이콘
    private Text[]  skillNameTxts  = new Text[2];  // 스킬 이름
    private Text[]  skillCdTxts    = new Text[2];  // 남은 쿨타임 / 충전 수

    // HP 기반 이미지 (현재 HP 값에 따라 자동 전환)
    // [0]=HP0(빈피), [1]=HP1, [2]=HP2, [3]=HP3(풀피)
    private Sprite[] currentHpSprites = new Sprite[4];

    // 우측 대형 초상화 (13:19 세로비율)
    private Image bigPortraitImage;

    // 최저 HP 추적 — 한 번 내려가면 회복해도 되돌아가지 않음
    private int lowestHpReached = 3;

    // 스킬 아이콘 링 색상
    private static readonly Color[] SkillRingColors =
    {
        new Color(0.95f, 0.85f, 0.25f), // Q — 노란색
        new Color(0.30f, 0.80f, 1.00f), // E — 하늘색
    };

    private Sprite circleSprite; // 공유 원형 스프라이트

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        circleSprite = MakeCircleSprite(64);
    }

    public void Build(Transform canvasRoot)
    {
        // ── 우측 대형 초상화 (13:19 세로비율) ────────────────────────────
        BuildBigPortrait(canvasRoot);

        // ── 패널 배경 ─────────────────────────────────────────────────────
        var panel = Make<Image>(canvasRoot, "PortraitPanel");
        SetRect(panel.rectTransform,
            new Vector2(0f, 0.05f), new Vector2(0f, 0.95f),
            new Vector2(10f, 0f),   new Vector2(165f, 0f));
        panel.color = new Color(0.04f, 0.06f, 0.11f, 0.90f);

        var border = Make<Image>(panel.rectTransform, "Border");
        SetRect(border.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(2f, 2f), new Vector2(-2f, -2f));
        border.color = new Color(0.3f, 0.5f, 0.85f, 0.35f);

        // ── 스테이지 텍스트 (21pt) ────────────────────────────────────────
        stageText = MakeText(panel.rectTransform, "Stage",
            new Vector2(0f, 0.89f), new Vector2(1f, 1f),
            "STAGE 1", 21, new Color(0.7f, 0.85f, 1f));

        // ── 캐릭터 이름 (22pt) ────────────────────────────────────────────
        charNameText = MakeText(panel.rectTransform, "Name",
            new Vector2(0f, 0.77f), new Vector2(1f, 0.89f),
            "원소마법사", 22, Color.white);

        // ── 캐릭터 이미지 슬롯 (스프라이트 교체 가능) ───────────────────
        var portraitFrame = Make<Image>(panel.rectTransform, "PortraitFrame");
        SetRect(portraitFrame.rectTransform,
            new Vector2(0.05f, 0.51f), new Vector2(0.95f, 0.77f),
            Vector2.zero, Vector2.zero);
        portraitFrame.color = new Color(0.1f, 0.12f, 0.2f, 1f);

        portraitImage = Make<Image>(portraitFrame.rectTransform, "PortraitSprite");
        SetRect(portraitImage.rectTransform,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
            Vector2.zero, Vector2.zero);
        portraitImage.color = Color.white;
        portraitImage.preserveAspect = true;
        portraitImage.gameObject.SetActive(false);

        // 이모지 폴백 (스프라이트 없을 때 표시)
        portraitFallback = MakeText(portraitFrame.rectTransform, "PortraitEmoji",
            Vector2.zero, Vector2.one,
            "🐱", 46, Color.white);

        // ── HP ────────────────────────────────────────────────────────────
        hpText = MakeText(panel.rectTransform, "HP",
            new Vector2(0f, 0.39f), new Vector2(1f, 0.51f),
            "♥♥♥", 28, new Color(0.4f, 1f, 0.5f));

        // ── 캐릭터별 상태 (원소/모드/충전) ───────────────────────────────
        statusText = MakeText(panel.rectTransform, "Status",
            new Vector2(0f, 0.28f), new Vector2(1f, 0.39f),
            "🔥 불", 21, new Color(1f, 0.65f, 0.3f));

        // ── 구분선 ───────────────────────────────────────────────────────
        var div = Make<Image>(panel.rectTransform, "Divider");
        SetRect(div.rectTransform,
            new Vector2(0.05f, 0.265f), new Vector2(0.95f, 0.270f),
            Vector2.zero, Vector2.zero);
        div.color = new Color(0.4f, 0.5f, 0.7f, 0.5f);

        // ── 스킬 아이콘 (Q = 왼쪽, E = 오른쪽) ──────────────────────────
        // 하단 26% 영역을 두 컬럼으로 분할
        BuildSkillIconSlot(panel.rectTransform, 0, "Q",
            new Vector2(0.00f, 0.00f), new Vector2(0.50f, 0.265f));
        BuildSkillIconSlot(panel.rectTransform, 1, "E",
            new Vector2(0.50f, 0.00f), new Vector2(1.00f, 0.265f));
    }

    // ── 스킬 아이콘 슬롯 빌더 ────────────────────────────────────────────
    private void BuildSkillIconSlot(Transform parent, int idx, string key,
                                     Vector2 anchorMin, Vector2 anchorMax)
    {
        // 슬롯 컨테이너
        var slot = new GameObject($"Skill{key}Slot");
        slot.transform.SetParent(parent, false);
        var srt = slot.AddComponent<RectTransform>();
        srt.anchorMin = anchorMin; srt.anchorMax = anchorMax;
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

        // 키 레이블 (Q / E) — 슬롯 상단
        var keyLbl = new GameObject($"Key{key}");
        keyLbl.transform.SetParent(slot.transform, false);
        var klrt = keyLbl.AddComponent<RectTransform>();
        klrt.anchorMin = new Vector2(0f, 0.82f); klrt.anchorMax = new Vector2(1f, 1f);
        klrt.offsetMin = klrt.offsetMax = Vector2.zero;
        var kTxt = keyLbl.AddComponent<Text>();
        kTxt.text = $"[{key}]"; kTxt.fontSize = 14; kTxt.color = new Color(0.6f, 0.7f, 0.85f);
        kTxt.alignment = TextAnchor.MiddleCenter;
        kTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ── 원형 아이콘 영역 (앵커 중단) ─────────────────────────────────
        var iconRoot = new GameObject($"IconRoot{idx}");
        iconRoot.transform.SetParent(slot.transform, false);
        var irrt = iconRoot.AddComponent<RectTransform>();
        irrt.anchorMin = new Vector2(0.5f, 0.42f); irrt.anchorMax = new Vector2(0.5f, 0.42f);
        irrt.pivot     = new Vector2(0.5f, 0.5f);
        irrt.anchoredPosition = Vector2.zero;
        irrt.sizeDelta = new Vector2(58f, 58f);

        // ① 배경 원 (회색)
        var bgCircle = new GameObject("BG");
        bgCircle.transform.SetParent(iconRoot.transform, false);
        var bgRt = bgCircle.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        var bgImg = bgCircle.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        bgImg.color  = new Color(0.20f, 0.22f, 0.30f);

        // ② 쿨타운 링 (Radial360 채우기)
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

        // ③ 중심 어두운 원 (링 효과를 위해 안쪽 가림)
        var centerGo = new GameObject("Center");
        centerGo.transform.SetParent(iconRoot.transform, false);
        var crt2 = centerGo.AddComponent<RectTransform>();
        crt2.anchorMin = crt2.anchorMax = new Vector2(0.5f, 0.5f);
        crt2.pivot     = new Vector2(0.5f, 0.5f);
        crt2.sizeDelta = new Vector2(42f, 42f);
        var cImg = centerGo.AddComponent<Image>();
        cImg.sprite = circleSprite;
        cImg.color  = new Color(0.07f, 0.08f, 0.13f);

        // ④ 스킬 아이콘 이미지 (중심 위)
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(iconRoot.transform, false);
        var irt = iconGo.AddComponent<RectTransform>();
        irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot     = new Vector2(0.5f, 0.5f);
        irt.sizeDelta = new Vector2(32f, 32f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;
        skillIconImgs[idx] = iconImg;

        // ── 스킬 이름 텍스트 ─────────────────────────────────────────────
        var nameGo = new GameObject($"SkillName{idx}");
        nameGo.transform.SetParent(slot.transform, false);
        var nrt = nameGo.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0.18f); nrt.anchorMax = new Vector2(1f, 0.38f);
        nrt.offsetMin = nrt.offsetMax = Vector2.zero;
        var nameTxt = nameGo.AddComponent<Text>();
        nameTxt.text      = "---";
        nameTxt.fontSize  = 13;
        nameTxt.color     = new Color(0.85f, 0.88f, 0.95f);
        nameTxt.alignment = TextAnchor.MiddleCenter;
        nameTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTxt.supportRichText = true;
        skillNameTxts[idx] = nameTxt;

        // ── 쿨타임 / 충전 텍스트 ─────────────────────────────────────────
        var cdGo = new GameObject($"SkillCD{idx}");
        cdGo.transform.SetParent(slot.transform, false);
        var cdrt = cdGo.AddComponent<RectTransform>();
        cdrt.anchorMin = new Vector2(0f, 0.00f); cdrt.anchorMax = new Vector2(1f, 0.18f);
        cdrt.offsetMin = cdrt.offsetMax = Vector2.zero;
        var cdTxt = cdGo.AddComponent<Text>();
        cdTxt.text      = "";
        cdTxt.fontSize  = 13;
        cdTxt.color     = new Color(0.6f, 0.65f, 0.75f);
        cdTxt.alignment = TextAnchor.MiddleCenter;
        cdTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skillCdTxts[idx] = cdTxt;
    }

    // ── 우측 대형 초상화 빌드 ────────────────────────────────────────────
    private void BuildBigPortrait(Transform canvasRoot)
    {
        var frame = Make<Image>(canvasRoot, "BigPortraitFrame");
        var frt = frame.rectTransform;
        // 앵커를 우측 중앙으로 설정 → sizeDelta로 크기 제어, anchoredPosition으로 여백 조정
        frt.anchorMin = frt.anchorMax = new Vector2(1f, 0.5f);
        frt.pivot     = new Vector2(1f, 0.5f);  // 우측 기준 정렬
        frt.anchoredPosition = new Vector2(-8f, 0f);  // 우측 여백 8px
        frt.sizeDelta        = new Vector2(370f, 570f);
        frame.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        bigPortraitImage = Make<Image>(frame.rectTransform, "BigPortraitImage");
        var irt = bigPortraitImage.rectTransform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4f, 4f);
        irt.offsetMax = new Vector2(-4f, -4f);
        bigPortraitImage.color = Color.white;
        bigPortraitImage.preserveAspect = true;
        bigPortraitImage.gameObject.SetActive(false);
    }

    // ── 스테이지별 HP 초상화 자동 로드 ─────────────────────────────────
    private void LoadPortraitsForStage(int stage)
    {
        for (int i = 0; i < 4; i++)
        {
            string path = $"Portraits/Stage{stage}_HP{i}";
            Sprite sp = Resources.Load<Sprite>(path);
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                    sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            currentHpSprites[i] = sp;
            if (sp != null) Debug.Log($"[Portrait] ✅ {path}");
            else            Debug.LogWarning($"[Portrait] ❌ {path} — Resources/Portraits 확인");
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

        int curIdx = Mathf.Clamp(hp, 0, 3);
        ApplySmallPortrait(currentHpSprites[curIdx]);

        int lowIdx = Mathf.Clamp(lowestHpReached, 0, 3);
        ApplyBigPortrait(currentHpSprites[lowIdx]);
    }

    private void ApplySmallPortrait(Sprite sprite)
    {
        if (sprite == null)
        {
            portraitImage.gameObject.SetActive(false);
            portraitFallback.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.sprite = sprite;
            portraitImage.gameObject.SetActive(true);
            portraitFallback.gameObject.SetActive(false);
        }
    }

    private void ApplyBigPortrait(Sprite sprite)
    {
        if (bigPortraitImage == null) return;
        if (sprite == null) { bigPortraitImage.gameObject.SetActive(false); return; }
        bigPortraitImage.sprite = sprite;
        bigPortraitImage.gameObject.SetActive(true);
    }

    public void SetPortraitSprite(Sprite sprite)
    {
        ApplySmallPortrait(sprite);
        ApplyBigPortrait(sprite);
    }

    public void Refresh(PlayerUnit player)
    {
        if (player == null) return;

        stageText.text = $"─ STAGE {currentStage} ─";

        // ── HP ────────────────────────────────────────────────────────────
        string hearts = "";
        for (int i = 0; i < player.maxHp; i++)
            hearts += i < player.currentHp ? "♥" : "♡";
        hpText.text  = hearts;
        hpText.color = player.currentHp <= 1
            ? new Color(1f, 0.3f, 0.3f)
            : new Color(0.4f, 1f, 0.5f);

        ApplyHpPortrait(player.currentHp);

        // ── 캐릭터별 상태 ─────────────────────────────────────────────────
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
            {
                statusText.text  = "⬛ 흑마법";
                statusText.color = new Color(0.65f, 0.3f, 1f);
            }
            else
            {
                statusText.text  = "⬜ 백마법";
                statusText.color = new Color(0.9f, 0.9f, 1f);
            }
        }
        else if (player is ArcanePlayerUnit)
        {
            statusText.gameObject.SetActive(true);
            var teleport = player.GetSkill(1) as Skill_Teleport;
            int charges = teleport != null ? teleport.Charges    : 0;
            int maxCh   = teleport != null ? teleport.maxCharges : 3;
            statusText.text  = $"⚡ ×{charges}/{maxCh}";
            statusText.color = new Color(0.9f, 0.85f, 0.3f);
        }
        else
        {
            statusText.gameObject.SetActive(false);
        }

        // ── 스킬 아이콘 갱신 ─────────────────────────────────────────────
        RefreshSkillIcon(0, player.GetSkill(1), "Q");
        RefreshSkillIcon(1, player.GetSkill(2), "E");
    }

    // ── 스킬 아이콘 갱신 ─────────────────────────────────────────────────
    private void RefreshSkillIcon(int idx, SkillBase skill, string key)
    {
        if (skill == null)
        {
            if (skillNameTxts[idx] != null) skillNameTxts[idx].text = "---";
            if (skillCdTxts[idx]   != null) skillCdTxts[idx].text   = "";
            if (skillFillRings[idx] != null) skillFillRings[idx].fillAmount = 0f;
            return;
        }

        // 스킬 이름
        if (skillNameTxts[idx] != null)
            skillNameTxts[idx].text = skill.skillName;

        // 충전형 스킬 (Skill_Teleport)
        if (skill is Skill_Teleport tp)
        {
            float fill = tp.maxCharges > 0 ? (float)tp.Charges / tp.maxCharges : 1f;
            if (skillFillRings[idx] != null)
            {
                skillFillRings[idx].fillAmount = fill;
                skillFillRings[idx].color = tp.Charges > 0
                    ? SkillRingColors[idx]
                    : new Color(0.35f, 0.35f, 0.4f);
            }
            if (skillCdTxts[idx] != null)
                skillCdTxts[idx].text = tp.Charges > 0 ? $"⚡×{tp.Charges}" : "충전중";
            return;
        }

        // 일반 쿨타임 스킬
        int cd    = skill.currentCooldown;
        int maxCd = skill.maxCooldown;

        float fillAmt = maxCd <= 0 ? 1f : 1f - (cd / (float)maxCd);
        if (skillFillRings[idx] != null)
        {
            skillFillRings[idx].fillAmount = fillAmt;
            skillFillRings[idx].color = cd <= 0
                ? SkillRingColors[idx]
                : new Color(0.35f, 0.35f, 0.4f);
        }

        if (skillCdTxts[idx] != null)
        {
            if (cd <= 0)
            {
                skillCdTxts[idx].text  = "준비됨";
                skillCdTxts[idx].color = new Color(0.5f, 0.9f, 0.5f);
            }
            else
            {
                skillCdTxts[idx].text  = $"⏳{cd}턴";
                skillCdTxts[idx].color = new Color(0.7f, 0.5f, 0.4f);
            }
        }
    }

    public void SetStage(int stage)
    {
        currentStage    = stage;
        lowestHpReached = 3;
        if (stageText != null) stageText.text = $"─ STAGE {stage} ─";
        LoadPortraitsForStage(stage);
        LoadSkillIcons(stage);
    }

    public void SetCharacterInfo(string charName, string portrait)
    {
        if (charNameText     != null) charNameText.text     = charName;
        if (portraitFallback != null) portraitFallback.text = portrait;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private T Make<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go.AddComponent<T>();
    }

    private void SetRect(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
    }

    private Text MakeText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        string content, int fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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
