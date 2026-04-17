using UnityEngine;

/// <summary>
/// 보드 뒤에 배치되는 배경 이미지
/// Resources/Stage/Background_{stage} 를 로드해 보드 전체를 덮도록 크기 조정
/// 이미지가 없으면 투명 (단색 카메라 배경이 보임)
/// </summary>
public class BoardBackground : MonoBehaviour
{
    public static BoardBackground Instance { get; private set; }

    private SpriteRenderer sr;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = -2; // 타일(0,1) 보다 뒤
        sr.color            = Color.clear;
    }

    /// <summary>스테이지 배경 이미지 적용. stage=0 이면 초기화(투명)</summary>
    public void SetStage(int stage)
    {
        if (stage <= 0)
        {
            sr.sprite = null;
            sr.color  = Color.clear;
            return;
        }

        Sprite sp = LoadSprite($"Stage/Background_{stage}");
        if (sp == null)
        {
            sr.sprite = null;
            sr.color  = Color.clear;
            return;
        }

        sr.sprite = sp;
        sr.color  = Color.white;
        FitToBoard(sp);
    }

    private void FitToBoard(Sprite sp)
    {
        if (BoardManager.Instance == null) return;

        int   w = BoardManager.Instance.Width;
        int   h = BoardManager.Instance.Height;

        // 보드를 약간 넘치게(여백 0.7) 덮음
        float targetW = (w - 1) + 1.4f;
        float targetH = (h - 1) + 1.4f;

        float sprW = sp.bounds.size.x;
        float sprH = sp.bounds.size.y;

        // 비율 유지 + 보드 전체 커버 (cover 방식)
        float scaleX = targetW / sprW;
        float scaleY = targetH / sprH;
        float scale  = Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(scale, scale, 1f);

        // 보드 중심에 배치, z=1f (카메라 near=-100 이므로 양수도 렌더링됨)
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        transform.position = new Vector3(cx, cy, 1f);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
