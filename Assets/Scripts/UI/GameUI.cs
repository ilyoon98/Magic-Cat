using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 HUD (Screen Space)
/// - 상단: 턴 상태
/// - 하단 중앙: 행동 현황 + 턴 종료 버튼
/// - 중앙: 이벤트 알림 (적 공격 등)
/// </summary>
public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    private Text turnText;
    private Text actionAttackText;
    private Text actionMoveText;
    private Text actionSkillText;
    private Button endTurnButton;
    private Text notifyText;
    private float notifyTimer;

    // (구) 맵 클리어 패널 → ResultScreen으로 대체됨

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
        // ━━━ 좌측: 캐릭터 정보 패널 (같은 캔버스에 붙임) ━━━━━━━━━━━━━━━━━━━
        var portraitPanelGo = new GameObject("PortraitPanel");
        portraitPanelGo.transform.SetParent(canvas.transform, false);
        var portraitPanel = portraitPanelGo.AddComponent<PortraitPanel>();
        portraitPanel.Build(canvas.transform);

        // ━━━ 상단: 턴 표시 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        var topPanel = MakePanel(canvas.transform, "TopPanel",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -38f), new Vector2(0f, 70f),
            new Color(0f, 0f, 0f, 0.6f));

        turnText = MakeText(topPanel.transform, "TurnText",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "플레이어 턴", 32, Color.white, TextAnchor.MiddleCenter);

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

        // ━━━ 우측 하단: 턴 종료 버튼 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        endTurnButton = MakeButton(canvas.transform, "EndTurnBtn",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-95f, 50f), new Vector2(165f, 58f),
            new Color(0.2f, 0.5f, 0.95f));
        endTurnButton.GetComponentInChildren<Text>().text = "턴 종료\n[Space]";
        endTurnButton.GetComponentInChildren<Text>().fontSize = 19;
        endTurnButton.onClick.AddListener(() => TurnManager.Instance?.SkipTurn());

        // ━━━ 중앙: 이벤트 알림 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        notifyText = MakeText(canvas.transform, "NotifyText",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -100f), new Vector2(450f, 50f),
            "", 28, new Color(1f, 0.35f, 0.35f), TextAnchor.MiddleCenter);

        // 맵 클리어 패널은 ResultScreen.cs 로 이전됨
    }

    // ResultScreen으로 이전 — 하위 호환용 스텁
    public void ShowClearPanel(string msg) { }
    public void HideClearPanel()           { }

    public void Refresh()
    {
        if (TurnManager.Instance == null) return;

        bool isPlayerTurn = TurnManager.Instance.CurrentPhase == TurnManager.TurnPhase.PlayerTurn;
        var player = TurnManager.Instance.GetPlayer();

        turnText.text = isPlayerTurn
            ? $"[ 플레이어 턴  {TurnManager.Instance.TurnCount} ]"
            : "[ 적 턴 ... ]";
        turnText.color = isPlayerTurn ? Color.white : new Color(1f, 0.45f, 0.45f);

        // 1행동 시스템 — 행동 완료 여부로 표시
        if (player != null)
        {
            bool acted = player.HasActedThisTurn;
            SetActionText(actionAttackText, "⚔ 공격", acted);
            SetActionText(actionMoveText,   "👣 이동", acted);
            SetActionText(actionSkillText,  "✦ 스킬",  acted);
        }

        bool ipt = isPlayerTurn;
        endTurnButton.interactable = ipt;
        endTurnButton.GetComponent<Image>().color = ipt
            ? new Color(0.2f, 0.5f, 0.95f)
            : new Color(0.18f, 0.18f, 0.22f);

        // PortraitPanel도 함께 갱신 (모든 플레이어 타입 지원)
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
