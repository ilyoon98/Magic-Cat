using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 페이드 + 특별 씬 컨트롤러
///
/// [게임오버]
///   FadeToBlack → GameOver 이미지 → 페이드인 → "메뉴로" 버튼
///
/// [스테이지 클리어]
///   FadeToBlack → StageClear_{n} 이미지 → 페이드인 → "다음 맵" + "타이틀로" 버튼
///   "다음 맵" 클릭 → Landscape_{n} 이미지 좌→우 패닝 → AdvanceMap()
///
/// [올 클리어]
///   FadeToBlack → AllClear 이미지 → 페이드인 → "처음부터" + "타이틀로" 버튼
/// </summary>
public class SpecialSceneController : MonoBehaviour
{
    public static SpecialSceneController Instance { get; private set; }

    // ── UI 요소 ───────────────────────────────────────────────────────────
    private Image  fadeOverlay;       // 전체화면 검정 페이드용

    private GameObject sceneRoot;    // 특별 씬 루트
    private Image      sceneImage;   // 메인 일러스트
    private Button     btn1, btn2;   // 하단 버튼

    private GameObject landscapeRoot; // 풍경 씬 루트
    private Image      landscapeImg;
    private RectTransform landscapeRt;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Build(Transform canvasRoot)
    {
        // ── ① 특별 씬 패널 ────────────────────────────────────────────────
        sceneRoot = MakeFullPanel(canvasRoot, "SpecialSceneRoot", Color.black);

        sceneImage = MakeImage(sceneRoot.transform, "SceneImage", Vector2.zero, Vector2.one);
        sceneImage.preserveAspect = true;
        sceneImage.raycastTarget = false;

        // 하단 버튼 영역
        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(sceneRoot.transform, false);
        var brrt = btnRow.AddComponent<RectTransform>();
        brrt.anchorMin = brrt.anchorMax = new Vector2(0.5f, 0f);
        brrt.anchoredPosition = new Vector2(0, 80);
        brrt.sizeDelta = new Vector2(640, 66);

        btn1 = MakeButton(btnRow.transform, "Btn1", new Vector2(-165, 0), new Vector2(280, 60));
        btn2 = MakeButton(btnRow.transform, "Btn2", new Vector2( 165, 0), new Vector2(280, 60));

        sceneRoot.SetActive(false);

        // ── ② 풍경 씬 패널 ────────────────────────────────────────────────
        landscapeRoot = MakeFullPanel(canvasRoot, "LandscapeRoot", Color.black);

        var limgGo = new GameObject("LandscapeImage");
        limgGo.transform.SetParent(landscapeRoot.transform, false);
        landscapeRt = limgGo.AddComponent<RectTransform>();
        landscapeRt.anchorMin = new Vector2(0, 0.5f);
        landscapeRt.anchorMax = new Vector2(0, 0.5f);
        landscapeRt.pivot     = new Vector2(0, 0.5f);
        landscapeRt.anchoredPosition = Vector2.zero;
        landscapeRt.sizeDelta = new Vector2(3840, 1080);
        landscapeImg = limgGo.AddComponent<Image>();
        landscapeImg.raycastTarget = false;

        landscapeRoot.SetActive(false);

        // ── ③ 페이드 오버레이 (가장 마지막 = 최상단) ───────────────────────
        var fadGo = new GameObject("FadeOverlay");
        fadGo.transform.SetParent(canvasRoot, false);
        var frt = fadGo.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.sizeDelta = Vector2.zero;
        fadeOverlay = fadGo.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.raycastTarget = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 공개 진입점
    // ═══════════════════════════════════════════════════════════════════════

    public void ShowGameOver(int stage)         => StartCoroutine(GameOverSeq(stage));
    public void ShowStageClear(int stage)       => StartCoroutine(StageClearSeq(stage));
    public void ShowAllClear()                  => StartCoroutine(AllClearSeq());

    // ── 게임오버 ──────────────────────────────────────────────────────────
    private IEnumerator GameOverSeq(int stage)
    {
        yield return StartCoroutine(FadeToBlack(0.55f));

        SetupScene(LoadScene("GameOver") ?? LoadScene($"GameOver_{stage}"),
                   new Color(0.12f, 0.02f, 0.02f));

        SetBtn(btn1, "메뉴로 돌아가기", new Color(0.75f, 0.18f, 0.18f), GoTitle);
        btn2.gameObject.SetActive(false);

        sceneRoot.SetActive(true);
        yield return StartCoroutine(FadeIn(0.5f));
    }

    // ── 스테이지 클리어 ───────────────────────────────────────────────────
    private IEnumerator StageClearSeq(int stage)
    {
        yield return StartCoroutine(FadeToBlack(0.55f));

        SetupScene(LoadScene($"StageClear_{stage}"), new Color(0.08f, 0.06f, 0.01f));

        SetBtn(btn1, "다음 맵 →", new Color(0.78f, 0.58f, 0.10f),
               () => StartCoroutine(LandscapeSeq(stage)));
        SetBtn(btn2, "타이틀로",   new Color(0.28f, 0.28f, 0.33f), GoTitle);
        btn2.gameObject.SetActive(true);

        sceneRoot.SetActive(true);
        yield return StartCoroutine(FadeIn(0.5f));
    }

    // ── 올 클리어 ─────────────────────────────────────────────────────────
    private IEnumerator AllClearSeq()
    {
        yield return StartCoroutine(FadeToBlack(0.55f));

        SetupScene(LoadScene("AllClear"), new Color(0.05f, 0.02f, 0.12f));

        SetBtn(btn1, "처음부터",  new Color(0.50f, 0.12f, 0.88f), GoRestart);
        SetBtn(btn2, "타이틀로",  new Color(0.28f, 0.28f, 0.33f), GoTitle);
        btn2.gameObject.SetActive(true);

        sceneRoot.SetActive(true);
        yield return StartCoroutine(FadeIn(0.5f));
    }

    // ── 풍경 패닝 씬 (좌→우) ─────────────────────────────────────────────
    private IEnumerator LandscapeSeq(int stage)
    {
        // 버튼 비활성화, 페이드 아웃
        btn1.interactable = false;
        btn2.interactable = false;
        yield return StartCoroutine(FadeToBlack(0.4f));
        sceneRoot.SetActive(false);

        // 풍경 이미지 설정
        Sprite landSp = LoadScene($"Landscape_{stage}");
        if (landSp != null)
        {
            landscapeImg.sprite = landSp;
            landscapeImg.color  = Color.white;
            // 높이를 1080에 맞추고 비율대로 너비 계산
            float ratio = (float)landSp.texture.width / landSp.texture.height;
            landscapeRt.sizeDelta = new Vector2(1080f * ratio, 1080f);
        }
        else
        {
            // 이미지 없으면 그라디언트 색상 배경 (시각적 폴백)
            landscapeImg.color = new Color(0.15f, 0.22f, 0.38f);
            landscapeRt.sizeDelta = new Vector2(3840, 1080);
        }
        landscapeRt.anchoredPosition = Vector2.zero;

        landscapeRoot.SetActive(true);
        yield return StartCoroutine(FadeIn(0.4f));

        // 좌→우 패닝 (SmoothStep으로 부드럽게)
        float imageW  = landscapeRt.sizeDelta.x;
        float screenW = 1920f;
        float maxPan  = Mathf.Max(0f, imageW - screenW);
        float duration = Mathf.Clamp(maxPan / 400f, 3f, 6f); // 이미지 너비에 따라 자동 조절

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            landscapeRt.anchoredPosition = new Vector2(-maxPan * t, 0f);
            yield return null;
        }

        // 페이드 아웃 → 다음 맵
        yield return StartCoroutine(FadeToBlack(0.5f));
        landscapeRoot.SetActive(false);

        // 다음 맵 전환 후 페이드 인
        StageManager.Instance.AdvanceMap();
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(0.4f));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 내부 헬퍼
    // ═══════════════════════════════════════════════════════════════════════

    private void SetupScene(Sprite sp, Color bgColor)
    {
        sceneRoot.GetComponent<Image>().color = bgColor;
        if (sp != null) { sceneImage.sprite = sp; sceneImage.color = Color.white; }
        else              sceneImage.color  = Color.clear;
        btn1.interactable = true;
        btn2.interactable = true;
    }

    private void SetBtn(Button btn, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        btn.onClick.RemoveAllListeners();
        btn.GetComponent<Image>().color = color;
        btn.GetComponentInChildren<Text>().text = label;
        btn.onClick.AddListener(action);
    }

    private void GoTitle()
    {
        StartCoroutine(FadeAndDo(0.4f, () =>
        {
            sceneRoot.SetActive(false);
            landscapeRoot.SetActive(false);
            TitleScreen.Instance?.Show();
        }));
    }

    private void GoRestart() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);

    // ── Resources 로드 (Sprite → Texture2D 폴백) ──────────────────────────
    private Sprite LoadScene(string name)
    {
        Sprite sp = Resources.Load<Sprite>($"Scenes/{name}");
        if (sp != null) return sp;
        Texture2D tex = Resources.Load<Texture2D>($"Scenes/{name}");
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    // ── 페이드 코루틴 ─────────────────────────────────────────────────────
    private IEnumerator FadeToBlack(float dur)
    {
        fadeOverlay.raycastTarget = true;
        yield return StartCoroutine(LerpAlpha(fadeOverlay, 0f, 1f, dur));
    }

    private IEnumerator FadeIn(float dur)
    {
        yield return StartCoroutine(LerpAlpha(fadeOverlay, 1f, 0f, dur));
        fadeOverlay.raycastTarget = false;
    }

    private IEnumerator LerpAlpha(Image img, float from, float to, float dur)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            img.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t));
            yield return null;
        }
        img.color = new Color(0, 0, 0, to);
    }

    private IEnumerator FadeAndDo(float dur, System.Action act)
    {
        yield return StartCoroutine(FadeToBlack(dur));
        act?.Invoke();
        yield return StartCoroutine(FadeIn(0.35f));
    }

    // ── UI 빌더 헬퍼 ──────────────────────────────────────────────────────
    private GameObject MakeFullPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private Image MakeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.sizeDelta = Vector2.zero;
        return go.AddComponent<Image>();
    }

    private Button MakeButton(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        var txt = lgo.AddComponent<Text>();
        txt.fontSize  = 26; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return btn;
    }
}
