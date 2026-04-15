using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 결과 화면 — MapClear / StageClear / AllClear / GameOver
/// ResultScreen.Instance.Show(type) 으로 호출
/// </summary>
public class ResultScreen : MonoBehaviour
{
    public static ResultScreen Instance { get; private set; }

    public enum ResultType { MapClear, StageClear, AllClear, GameOver }

    private GameObject panel;
    private Image      bgImage;
    private Text       emojiText;
    private Text       mainText;
    private Text       subText;
    private Button     primaryBtn;
    private Button     secondaryBtn;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(Transform canvasRoot)
    {
        panel = new GameObject("ResultScreen");
        panel.transform.SetParent(canvasRoot, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        bgImage = panel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.88f);

        // ── 이모지 ────────────────────────────────────────────────────────
        emojiText = MakeText(panel.transform, "Emoji",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 175), new Vector2(200, 130),
            "", 80, Color.white, TextAnchor.MiddleCenter);

        // ── 메인 텍스트 ───────────────────────────────────────────────────
        mainText = MakeText(panel.transform, "MainText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 60), new Vector2(850, 110),
            "", 66, Color.white, TextAnchor.MiddleCenter);

        // ── 서브 텍스트 ───────────────────────────────────────────────────
        subText = MakeText(panel.transform, "SubText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -35), new Vector2(700, 55),
            "", 26, new Color(0.75f, 0.75f, 0.82f), TextAnchor.MiddleCenter);

        // ── 버튼 2개 ─────────────────────────────────────────────────────
        primaryBtn   = MakeButton(panel.transform, "PrimaryBtn",   new Vector2(-135, -150));
        secondaryBtn = MakeButton(panel.transform, "SecondaryBtn", new Vector2( 135, -150));

        // 타이틀 버튼은 항상 고정
        SetBtnStyle(secondaryBtn, "타이틀로", new Color(0.28f, 0.28f, 0.34f));
        secondaryBtn.onClick.AddListener(GoTitle);

        panel.SetActive(false);
    }

    // ── 결과 타입에 따라 내용 세팅 후 표시 ───────────────────────────────
    public void Show(ResultType type)
    {
        panel.SetActive(true);
        primaryBtn.onClick.RemoveAllListeners();

        var sm       = StageManager.Instance;
        int   stage  = sm != null ? sm.CurrentStage : 1;
        string map   = sm != null ? sm.MapLabel     : "";

        switch (type)
        {
            // ── 일반 맵 클리어 ─────────────────────────────────────────
            case ResultType.MapClear:
                bgImage.color   = new Color(0.03f, 0.09f, 0.18f, 0.90f);
                emojiText.text  = "✅";
                mainText.text   = "MAP CLEAR!";
                mainText.color  = new Color(0.40f, 1.00f, 0.55f);
                subText.text    = $"Stage {stage} · {map} 클리어!";
                SetBtnStyle(primaryBtn, "다음 맵 →", new Color(0.15f, 0.55f, 0.95f));
                primaryBtn.onClick.AddListener(() => { Hide(); StageManager.Instance.AdvanceMap(); });
                break;

            // ── 보스 맵(스테이지) 클리어 ──────────────────────────────
            case ResultType.StageClear:
                bgImage.color   = new Color(0.10f, 0.08f, 0.01f, 0.92f);
                emojiText.text  = "⭐";
                mainText.text   = $"STAGE {stage} CLEAR!";
                mainText.color  = new Color(1.00f, 0.88f, 0.25f);
                subText.text    = "다음 스테이지가 해금됩니다";
                SetBtnStyle(primaryBtn, "계속하기 →", new Color(0.80f, 0.60f, 0.10f));
                primaryBtn.onClick.AddListener(() => { Hide(); StageManager.Instance.AdvanceMap(); });
                break;

            // ── 전체 클리어 ────────────────────────────────────────────
            case ResultType.AllClear:
                bgImage.color   = new Color(0.06f, 0.02f, 0.14f, 0.93f);
                emojiText.text  = "🎉";
                mainText.text   = "ALL CLEAR!!";
                mainText.color  = new Color(1.00f, 0.82f, 0.30f);
                subText.text    = "모든 스테이지를 클리어했습니다!";
                SetBtnStyle(primaryBtn, "처음부터", new Color(0.55f, 0.15f, 0.90f));
                primaryBtn.onClick.AddListener(GoRestart);
                break;

            // ── 게임오버 ───────────────────────────────────────────────
            case ResultType.GameOver:
                bgImage.color   = new Color(0.14f, 0.02f, 0.02f, 0.93f);
                emojiText.text  = "💀";
                mainText.text   = "GAME OVER";
                mainText.color  = new Color(1.00f, 0.28f, 0.28f);
                subText.text    = $"Stage {stage} · {map}에서 쓰러졌습니다";
                SetBtnStyle(primaryBtn, "재시작", new Color(0.85f, 0.18f, 0.18f));
                primaryBtn.onClick.AddListener(GoRestart);
                break;
        }
    }

    public void Hide() => panel?.SetActive(false);

    private void GoTitle()
    {
        Hide();
        TitleScreen.Instance?.Show();
    }

    private void GoRestart()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
    }

    // ── 버튼 스타일 적용 ─────────────────────────────────────────────────
    private void SetBtnStyle(Button btn, string label, Color color)
    {
        btn.GetComponent<Image>().color           = color;
        btn.GetComponentInChildren<Text>().text   = label;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────
    private Button MakeButton(Transform parent, string name, Vector2 anchoredPos)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(235, 62);
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var txt = lgo.AddComponent<Text>();
        txt.fontSize  = 26;
        txt.color     = Color.white;
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
