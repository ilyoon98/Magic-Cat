using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 HUD (Screen Space)
/// - 상단: 턴 상태 (좌측 포트레이트 패널 오른쪽부터)
/// - 하단 중앙: 행동 현황 + 턴 종료 버튼
/// - 우상단: 메뉴 버튼
/// - 중앙: 이벤트 알림 (적 공격 등)
/// </summary>
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    private Text   turnText;
    private Text   actionAttackText;
    private Text   actionMoveText;
    private Text   actionSkillText;
    private Button endTurnButton;
    private Text   actionCountText;   // EndTurnBtn 위쪽 "행동 X/2" 표시
    private Text   notifyText;
    private float  notifyTimer;

    // 인게임 메뉴 패널
    private GameObject inGameMenuPanel;
    public static bool IsMenuOpen => Instance != null && Instance.inGameMenuPanel != null
                                  && Instance.inGameMenuPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (notifyTimer > 0f)
        {
            notifyTimer -= Time.deltaTime;
            if (notifyTimer <= 0f) notifyText.text = "";
        }
    }

    public void Build(Canvas canvas)
    {
        // ━━━ 상단 바: 전체 너비. PortraitPanel이 좌측 42%를 채움 ━━━━━━━━━━
        var topPanel = MakePanel(canvas.transform, "TopPanel",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -34f), new Vector2(0f, 68f),
            new Color(0f, 0f, 0f, 0.62f));

        // ━━━ 캐릭터 정보 패널 (사이드바 없음 — 상단 바 + 우측 초상화 + 좌하단 스킬 바) ━━━━━━━━━━
        var portraitPanelGo = new GameObject("PortraitPanel");
        portraitPanelGo.transform.SetParent(canvas.transform, false);
        var portraitPanel = portraitPanelGo.AddComponent<PortraitPanel>();
        portraitPanel.Build(canvas.transform, topPanel.transform);

        // 턴 표시 (상단 바 우측 58%)
        turnText = MakeText(topPanel.transform, "TurnText",
            new Vector2(0.42f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
            "플레이어 턴", 28, Color.white, TextAnchor.MiddleCenter);

        // ━━━ 하단: 행동 현황 패널 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var actionPanel = MakePanel(canvas.transform, "ActionPanel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 50f), new Vector2(380f, 86f),
            new Color(0f, 0f, 0f, 0.65f));

        actionAttackText = MakeText(actionPanel.transform, "AtkText",
            new Vector2(0f, 0f), new Vector2(0.33f, 1f),
            Vector2.zero, Vector2.zero,
            "⚔ 공격\n○", 22, Color.white, TextAnchor.MiddleCenter);

        actionMoveText = MakeText(actionPanel.transform, "MoveText",
            new Vector2(0.33f, 0f), new Vector2(0.66f, 1f),
            Vector2.zero, Vector2.zero,
            "👣 이동\n○", 22, Color.white, TextAnchor.MiddleCenter);

        actionSkillText = MakeText(actionPanel.transform, "SkillText",
            new Vector2(0.66f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            "✦ 스킬\n○", 22, Color.white, TextAnchor.MiddleCenter);

        // ━━━ 좌측 하단: 턴 종료 버튼 (SkillBar 위) ━━━━━━━━━━━━━━━━━━━━━━━━━
        endTurnButton = MakeButton(canvas.transform, "EndTurnBtn",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(125f, 270f), new Vector2(230f, 100f),
            new Color(0.2f, 0.5f, 0.95f));
        endTurnButton.GetComponentInChildren<Text>().text = "턴 종료\n[Space]";
        endTurnButton.GetComponentInChildren<Text>().fontSize = 30;
        endTurnButton.onClick.AddListener(() => TurnManager.Instance?.SkipTurn());

        // 버튼 위쪽: 행동 X/2 카운터 텍스트
        var actGo = new GameObject("ActionCount");
        actGo.transform.SetParent(endTurnButton.transform, false);
        var art = actGo.AddComponent<RectTransform>();
        art.anchorMin        = new Vector2(0f, 1f);
        art.anchorMax        = new Vector2(1f, 1f);
        art.pivot            = new Vector2(0.5f, 0f);
        art.anchoredPosition = new Vector2(0f, 8f);
        art.sizeDelta        = new Vector2(0f, 34f);
        actionCountText = actGo.AddComponent<Text>();
        actionCountText.text      = "행동 0 / 2";
        actionCountText.fontSize  = 22;
        actionCountText.color     = new Color(0.75f, 0.85f, 1f);
        actionCountText.alignment = TextAnchor.MiddleCenter;
        actionCountText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        actionCountText.supportRichText = true;

        // ━━━ 우상단: 메뉴 버튼 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var menuBtn = MakeButton(canvas.transform, "MenuBtn",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-52f, -34f), new Vector2(88f, 44f),
            new Color(0.22f, 0.22f, 0.28f));
        menuBtn.GetComponentInChildren<Text>().text = "≡ 메뉴";
        menuBtn.GetComponentInChildren<Text>().fontSize = 18;
        menuBtn.onClick.AddListener(OpenMenu);

        // ━━━ 중앙: 이벤트 알림 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        notifyText = MakeText(canvas.transform, "NotifyText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -100f), new Vector2(450f, 50f),
            "", 28, new Color(1f, 0.35f, 0.35f), TextAnchor.MiddleCenter);

        // ━━━ 인게임 메뉴 패널 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        BuildInGameMenu(canvas.transform);
    }

    // ── 인게임 메뉴 빌드 ─────────────────────────────────────────────────
    private void BuildInGameMenu(Transform canvasRoot)
    {
        // 전체화면 반투명 오버레이
        inGameMenuPanel = new GameObject("InGameMenu");
        inGameMenuPanel.transform.SetParent(canvasRoot, false);
        var rt = inGameMenuPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        inGameMenuPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // 메뉴 카드
        var card = new GameObject("MenuCard");
        card.transform.SetParent(inGameMenuPanel.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(340f, 320f);
        crt.anchoredPosition = Vector2.zero;
        card.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.14f, 1f);

        // 제목
        MakeText(card.transform, "MenuTitle",
            new Vector2(0f, 0.78f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero,
            "⏸  일시정지", 30, new Color(0.75f, 0.88f, 1f), TextAnchor.MiddleCenter);

        // 구분선
        var div = new GameObject("Div"); div.transform.SetParent(card.transform, false);
        var drt = div.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.05f, 0.755f); drt.anchorMax = new Vector2(0.95f, 0.76f);
        drt.sizeDelta = Vector2.zero;
        div.AddComponent<Image>().color = new Color(0.3f, 0.4f, 0.6f, 0.5f);

        // ── 계속하기 버튼
        var resumeBtn = MakeButton(card.transform, "ResumeBtn",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 68f), new Vector2(268f, 56f),
            new Color(0.15f, 0.55f, 0.95f));
        resumeBtn.GetComponentInChildren<Text>().text = "▶  계속하기";
        resumeBtn.GetComponentInChildren<Text>().fontSize = 22;
        resumeBtn.onClick.AddListener(CloseMenu);

        // ── 타이틀로 버튼
        var titleBtn = MakeButton(card.transform, "TitleBtn",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(268f, 56f),
            new Color(0.28f, 0.28f, 0.35f));
        titleBtn.GetComponentInChildren<Text>().text = "🏠  타이틀로";
        titleBtn.GetComponentInChildren<Text>().fontSize = 22;
        titleBtn.onClick.AddListener(GoTitle);

        // ── 스테이지 선택 버튼
        var selectBtn = MakeButton(card.transform, "SelectBtn",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -68f), new Vector2(268f, 56f),
            new Color(0.22f, 0.28f, 0.40f));
        selectBtn.GetComponentInChildren<Text>().text = "🗂  스테이지 선택";
        selectBtn.GetComponentInChildren<Text>().fontSize = 22;
        selectBtn.onClick.AddListener(GoStageSelect);

        inGameMenuPanel.SetActive(false);
    }

    // ── 메뉴 열기/닫기 ───────────────────────────────────────────────────
    public void OpenMenu()
    {
        GameManager.Instance?.Pause();
        inGameMenuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        inGameMenuPanel.SetActive(false);
        GameManager.Instance?.Resume();
    }

    private void GoTitle()
    {
        inGameMenuPanel.SetActive(false);
        // Resume() 대신 Idle로 전환 — PlayerTurn 상태로 복귀하면
        // 타이틀 화면에서도 PlayerInputController가 입력을 처리하는 버그 발생
        TurnManager.Instance?.Reset();
        GameManager.Instance?.ChangeState(GameManager.GameState.Idle);
        TitleScreen.Instance?.Show();
    }

    private void GoStageSelect()
    {
        inGameMenuPanel.SetActive(false);
        TurnManager.Instance?.Reset();
        GameManager.Instance?.ChangeState(GameManager.GameState.Idle);
        StageSelectScreen.Instance?.Show();
    }

    // ResultScreen으로 이전 — 하위 호환용 스텁
    public void ShowClearPanel(string msg) { }
    public void HideClearPanel()           { }

    public void Refresh()
    {
        if (TurnManager.Instance == null) return;

        bool isPlayerTurn = TurnManager.Instance.CurrentPhase == TurnManager.TurnPhase.PlayerTurn
                         && GameManager.Instance?.CurrentState == GameManager.GameState.PlayerTurn;
        var player = TurnManager.Instance.GetPlayer();

        turnText.color = isPlayerTurn ? Color.white : new Color(1f, 0.45f, 0.45f);

        if (player != null)
        {
            // TurnText: 턴 번호만 표시 (행동 카운터는 EndTurnBtn 위쪽으로 이동)
            turnText.text = isPlayerTurn
                ? $"[ 플레이어 턴  {TurnManager.Instance.TurnCount} ]"
                : "[ 적 턴 ... ]";

            SetActionText(actionAttackText, "⚔ 공격", !player.CanAttack);
            SetActionText(actionMoveText,   "👣 이동", !player.CanMove);
            SetActionText(actionSkillText,  "✦ 스킬",  !player.CanUseSkill);

            // 행동 카운터 — EndTurnBtn 위
            if (actionCountText != null)
                actionCountText.text = $"행동  {player.ActionsUsed} / 2";
        }
        else
        {
            turnText.text = isPlayerTurn
                ? $"[ 플레이어 턴  {TurnManager.Instance.TurnCount} ]"
                : "[ 적 턴 ... ]";
        }

        endTurnButton.interactable = isPlayerTurn;

        // EndTurnBtn 색: 남은 행동 수에 따라 변경
        if (!isPlayerTurn || player == null)
        {
            endTurnButton.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        }
        else
        {
            int remaining = 2 - player.ActionsUsed;
            endTurnButton.GetComponent<Image>().color = remaining switch
            {
                2 => new Color(0.22f, 0.38f, 0.68f), // 2개 남음 — 어두운 파랑
                1 => new Color(0.78f, 0.50f, 0.10f), // 1개 남음 — 주황
                _ => new Color(0.18f, 0.62f, 0.28f), // 0개 남음 — 초록 (턴 종료 유도)
            };
        }

        if (player != null)
            PortraitPanel.Instance?.Refresh(player);
    }

    private void SetActionText(Text txt, string label, bool done)
    {
        txt.text = done
            ? $"<color=#555>{label}\n●</color>"
            : $"{label}\n○";
    }

    public void ShowNotify(string msg, float duration = 1.5f)
    {
        notifyText.text = msg;
        notifyTimer = duration;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private Image MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private Text MakeText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        string content, int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.supportRichText = true;
        return text;
    }

    private Button MakeButton(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var panel = MakePanel(parent, name, anchorMin, anchorMax, anchoredPos, sizeDelta, color);
        var btn = panel.gameObject.AddComponent<Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(panel.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.supportRichText = true;
        return btn;
    }
}
