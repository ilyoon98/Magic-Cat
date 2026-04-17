using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 설정 패널 — TitleScreen에서 열림
/// BGM · SFX 볼륨 슬라이더
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance { get; private set; }

    private GameObject panel;
    private Slider     bgmSlider;
    private Slider     sfxSlider;
    private Text       bgmValText;
    private Text       sfxValText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(Transform canvasRoot)
    {
        // ── 전체화면 반투명 오버레이 ──────────────────────────────────────
        panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvasRoot, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

        // 클릭해도 아래로 통과하지 않게 버튼으로 막기
        panel.AddComponent<Button>().onClick.AddListener(() => { }); // 빈 버튼으로 레이캐스트 차단

        // ── 카드 ──────────────────────────────────────────────────────────
        var card = new GameObject("Card");
        card.transform.SetParent(panel.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin        = new Vector2(0.5f, 0.5f);
        crt.anchorMax        = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta        = new Vector2(500f, 360f);
        card.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.17f, 1f);

        // ── 제목 ──────────────────────────────────────────────────────────
        MakeLabel(card.transform, "Title",
            new Vector2(0f, 130f), new Vector2(500f, 60f),
            "⚙  설  정", 34, new Color(0.75f, 0.88f, 1f));

        // ── 구분선 ────────────────────────────────────────────────────────
        var divider = new GameObject("Divider");
        divider.transform.SetParent(card.transform, false);
        var drt = divider.AddComponent<RectTransform>();
        drt.anchorMin        = new Vector2(0.5f, 0.5f);
        drt.anchorMax        = new Vector2(0.5f, 0.5f);
        drt.anchoredPosition = new Vector2(0f, 100f);
        drt.sizeDelta        = new Vector2(440f, 2f);
        divider.AddComponent<Image>().color = new Color(0.3f, 0.4f, 0.6f, 0.5f);

        // ── BGM 행 ────────────────────────────────────────────────────────
        MakeLabel(card.transform, "BGMLabel",
            new Vector2(-170f, 45f), new Vector2(80f, 36f),
            "BGM", 22, new Color(0.75f, 0.85f, 1f));

        bgmSlider = MakeSlider(card.transform, "BGMSlider",
            new Vector2(20f, 45f), new Vector2(260f, 32f),
            AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : 0.8f,
            v =>
            {
                AudioManager.Instance?.SetBGMVolume(v);
                if (bgmValText != null) bgmValText.text = Mathf.RoundToInt(v * 100) + "%";
            });

        bgmValText = MakeLabel(card.transform, "BGMVal",
            new Vector2(195f, 45f), new Vector2(70f, 36f),
            Mathf.RoundToInt((AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : 0.8f) * 100) + "%",
            20, Color.white);

        // ── SFX 행 ────────────────────────────────────────────────────────
        MakeLabel(card.transform, "SFXLabel",
            new Vector2(-170f, -10f), new Vector2(80f, 36f),
            "SFX", 22, new Color(0.75f, 0.85f, 1f));

        sfxSlider = MakeSlider(card.transform, "SFXSlider",
            new Vector2(20f, -10f), new Vector2(260f, 32f),
            AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1.0f,
            v =>
            {
                AudioManager.Instance?.SetSFXVolume(v);
                if (sfxValText != null) sfxValText.text = Mathf.RoundToInt(v * 100) + "%";
            });

        sfxValText = MakeLabel(card.transform, "SFXVal",
            new Vector2(195f, -10f), new Vector2(70f, 36f),
            Mathf.RoundToInt((AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1.0f) * 100) + "%",
            20, Color.white);

        // ── 닫기 버튼 ─────────────────────────────────────────────────────
        var closeGo = new GameObject("CloseBtn");
        closeGo.transform.SetParent(card.transform, false);
        var closert = closeGo.AddComponent<RectTransform>();
        closert.anchorMin        = new Vector2(0.5f, 0.5f);
        closert.anchorMax        = new Vector2(0.5f, 0.5f);
        closert.anchoredPosition = new Vector2(0f, -120f);
        closert.sizeDelta        = new Vector2(200f, 52f);
        closeGo.AddComponent<Image>().color = new Color(0.28f, 0.28f, 0.38f);
        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(Hide);
        MakeLabel(closeGo.transform, "CloseLbl",
            Vector2.zero, Vector2.zero,
            "닫기", 24, Color.white, true);

        panel.SetActive(false);
    }

    public void Show()
    {
        if (AudioManager.Instance != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = AudioManager.Instance.BGMVolume;
                if (bgmValText != null) bgmValText.text = Mathf.RoundToInt(AudioManager.Instance.BGMVolume * 100) + "%";
            }
            if (sfxSlider != null)
            {
                sfxSlider.value = AudioManager.Instance.SFXVolume;
                if (sfxValText != null) sfxValText.text = Mathf.RoundToInt(AudioManager.Instance.SFXVolume * 100) + "%";
            }
        }
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }

    // ── 슬라이더 빌더 ────────────────────────────────────────────────────
    private Slider MakeSlider(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, float initialValue,
        UnityEngine.Events.UnityAction<float> onChange)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        // 배경 트랙
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.3f);
        bgRt.anchorMax = new Vector2(1f, 0.7f);
        bgRt.sizeDelta = Vector2.zero;
        bgGo.AddComponent<Image>().color = new Color(0.18f, 0.20f, 0.30f);

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.3f);
        faRt.anchorMax = new Vector2(1f, 0.7f);
        faRt.offsetMin = new Vector2(4f,  0f);
        faRt.offsetMax = new Vector2(-12f, 0f);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillArea.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.60f, 1f);

        // Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(8f,  0f);
        haRt.offsetMax = new Vector2(-8f, 0f);

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(handleArea.transform, false);
        var hRt = handleGo.AddComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(20f, 0f);
        hRt.anchorMin = new Vector2(0f, 0.1f);
        hRt.anchorMax = new Vector2(0f, 0.9f);
        var handleImg = handleGo.AddComponent<Image>();
        handleImg.color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect      = fillRt;
        slider.handleRect    = hRt;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = initialValue;
        slider.onValueChanged.AddListener(onChange);

        return slider;
    }

    // ── 레이블 헬퍼 (anchoredPosition 기반) ──────────────────────────────
    private Text MakeLabel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size,
        string content, int fontSize, Color color, bool stretch = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
        else
        {
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
        }
        var txt = go.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return txt;
    }
}
