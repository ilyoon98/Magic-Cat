using UnityEngine;

/// <summary>
/// 런타임 생성 스프라이트를 캐시해서 재사용
/// LoadSprite() — Resources에서 유닛 이미지 로드 (없으면 null)
/// </summary>
public static class UnitSpriteCache
{
    private static Sprite _circleSprite;

    public static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite == null) _circleSprite = CreateCircle();
            return _circleSprite;
        }
    }

    /// <summary>
    /// Resources/{path} 에서 스프라이트를 로드합니다.
    /// Sprite 타입 → 직접 반환, Default 타입 → Texture2D로 로드 후 변환.
    /// 파일이 없으면 null 반환.
    /// pixelsPerUnit 은 이미지 장변 길이로 설정해 보드 1칸(1unit)에 꽉 차도록 합니다.
    /// </summary>
    public static Sprite LoadSprite(string path)
    {
        // 1차: Sprite 타입으로 직접 로드
        Sprite sp = Resources.Load<Sprite>(path);
        if (sp != null) return sp;

        // 2차: Texture2D로 로드 후 스프라이트 생성
        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;

        // 이미지 장변을 pixelsPerUnit으로 설정 → 1 world unit에 꽉 채움
        float ppu = Mathf.Max(tex.width, tex.height);
        return Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            ppu);
    }

    private static Sprite CreateCircle()
    {
        int size = 64;
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
