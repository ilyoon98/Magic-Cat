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
    private Text  skill1CdText;
    private Text  skill2CdText;
    private int   currentStage = 1;

    // HP 기반 이미지 (현재 HP 값에 따라 자동 전환)
    // [0]=HP0(빈피), [1]=HP1, [2]=HP2, [3]=HP3(풀피)
    private Sprite[] currentHpSprites = new Sprite[4];

    // 우측 대형 초상화 (13:19 세로비율)
    private Image bigPortraitImage;

    // 최저 HP 추적 — 한 번 내려가면 회복해도 되돌아가지 않음
    private int lowestHpReached = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

        // ── 캐릭터 이름 (30pt) ────────────────────────────────────────────
        charNameText = MakeText(panel.rectTransform, "Name",
            new Vector2(0f, 0.77f), new Vector2(1f, 0.89f),
            "원소마법사", 22, Color.white);

        // ── 캐릭터 이미지 슬롯 (스프라이트 교체 가능) ───────────────────
        var portraitFrame = Make<Image>(panel.rectTransform, "PortraitFrame");
        SetRect(portraitFrame.rectTransform,
            new Vector2(0.05f, 0.51f), new Vector2(0.95f, 0.77f),
            Vector2.zero, Vector2.zero);
        portraitFrame.color = new Color(0.1f, 0.12f, 0.2f, 1f); // 어두운 배경

        portraitImage = Make<Image>(portraitFrame.rectTransform, "PortraitSprite");
        SetRect(portraitImage.rectTransform,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
            Vector2.zero, Vector2.zero);
        portraitImage.color = Color.white;
        portraitImage.preserveAspect = true;
        portraitImage.gameObject.SetActive(false); // 스프라이트 없으면 숨김

        // 이모지 폴백 (스프라이트 없을 때 표시)
        portraitFallback = MakeText(portraitFrame.rectTransform, "PortraitEmoji",
            Vector2.zero, Vector2.one,
            "🐱", 46, Color.white);

        // ── HP (30pt) ─────────────────────────────────────────────────────
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
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.255f),
            Vector2.zero, Vector2.zero);
        div.color = new Color(0.4f, 0.5f, 0.7f, 0.5f);

        // ── 스킬1 쿨타임 (18pt) ──────────────────────────────────────────
        skill1CdText = MakeText(panel.rectTransform, "S1",
            new Vector2(0f, 0.13f), new Vector2(1f, 0.25f),
            "[Q] 원소변경", 18, new Color(0.9f, 0.85f, 0.4f));

        // ── 스킬2 쿨타임 (18pt) ──────────────────────────────────────────
        skill2CdText = MakeText(panel.rectTransform, "S2",
            new Vector2(0f, 0.01f), new Vector2(1f, 0.13f),
            "[E] 원소집중", 18, new Color(0.9f, 0.6f, 0.3f));
    }

    // ── 우측 대형 초상화 빌드 ────────────────────────────────────────────
    private void BuildBigPortrait(Transform canvasRoot)
    {
        // 캔버스 1920×1080 기준, 우측 25% 영역 중앙 배치
        // 화면 x ≈ 1740, y ≈ 540 / 크기 420×648 (약 13:20 세로비율)
        var frame = Make<Image>(canvasRoot, "BigPortraitFrame");
        var frt = frame.rectTransform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.pivot     = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = new Vector2(780f, 0f);   // 화면 x≈1740, y=540
        frt.sizeDelta        = new Vector2(428f, 660f);
        frame.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);

        // 실제 이미지
        bigPortraitImage = Make<Image>(frame.rectTransform, "BigPortraitImage");
        var irt = bigPortraitImage.rectTransform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4f, 4f);
        irt.offsetMax = new Vector2(-4f, -4f);
        bigPortraitImage.color = Color.white;
        bigPortraitImage.preserveAspect = true;
        bigPortraitImage.gameObject.SetActive(false); // 스프라이트 없으면 숨김
    }

    // ── 스테이지별 HP 초상화 자동 로드 ─────────────────────────────────
    /// <summary>Resources/Portraits/Stage{n}_HP{0-3}.png 자동 로드</summary>
    private void LoadPortraitsForStage(int stage)
    {
        for (int i = 0; i < 4; i++)
        {
            string path = $"Portraits/Stage{stage}_HP{i}";

            // 1차: Sprite로 직접 로드 (Texture Type = Sprite 일 때)
            Sprite sp = Resources.Load<Sprite>(path);

            // 2차: Texture2D로 로드 후 변환 (Texture Type = Default 일 때)
            if (sp == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                    sp = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }

            currentHpSprites[i] = sp;

            if (sp != null) Debug.Log($"[Portrait] ✅ {path} 로드 성공");
            else            Debug.LogWarning($"[Portrait] ❌ {path} 로드 실패 — Resources/Portraits 폴더 확인 필요");
        }
    }

    private void ApplyHpPortrait(int hp)
    {
        // ① 최저 HP 갱신 (회복해도 되돌아가지 않음)
        lowestHpReached = Mathf.Min(lowestHpReached, hp);

        // ② 좌측 소형 — 현재 HP 이미지
        int curIdx = Mathf.Clamp(hp, 0, 3);
        ApplySmallPortrait(currentHpSprites[curIdx]);

        // ③ 우측 대형 — 최저 HP 이미지 고정
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

    // ── 외부 수동 교체 API (호출 시 좌우 모두 업데이트) ────────────────
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

        // HP 기반 초상화 자동 전환
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

        // ── 스킬 표시 ─────────────────────────────────────────────────────
        RefreshSkillDisplay(skill1CdText, player.GetSkill(1), "Q");
        RefreshSkillDisplay(skill2CdText, player.GetSkill(2), "E");
    }

    private void RefreshSkillDisplay(Text txt, SkillBase skill, string key)
    {
        if (skill == null) return;

        if (skill is Skill_Teleport tp)
        {
            txt.text  = $"[{key}] {skill.skillName} ⚡{tp.Charges}";
            txt.color = tp.Charges > 0
                ? new Color(0.9f, 0.85f, 0.4f)
                : new Color(0.5f, 0.5f, 0.5f);
            return;
        }

        if (skill.currentCooldown <= 0)
        {
            txt.text  = $"[{key}] {skill.skillName}";
            txt.color = new Color(0.9f, 0.85f, 0.4f);
        }
        else
        {
            txt.text  = $"[{key}] {skill.skillName} ⏳{skill.currentCooldown}";
            txt.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }

    public void SetStage(int stage)
    {
        currentStage    = stage;
        lowestHpReached = 3; // 스테이지 전환 시 최저 HP 리셋
        if (stageText != null) stageText.text = $"─ STAGE {stage} ─";
        LoadPortraitsForStage(stage);
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
}
