using UnityEngine;

/// <summary>
/// 유닛 스프라이트 로드 및 캐시 유틸리티.
///
/// LoadSprite():
///   - Resources/{path} 에서 Sprite 또는 Texture2D를 로드한다.
///   - ppu(pixels per unit)를 "이미지 높이 = 1 world unit(= 1 타일)"이 되도록 재계산한다.
///   - pivot = center(0.5, 0.5) → 유닛 transform이 타일 중심에 있을 때
///     스프라이트 하단이 자동으로 타일 하단에 정렬된다.
///     (sprite bottom = tileCenter.y − height/2 = tileCenter.y − 0.5 = tile bottom)
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
    /// Resources/{path} 에서 스프라이트를 로드하고
    /// ppu = 이미지 높이로 재설정하여 반환한다.
    ///
    /// - Sprite 타입 텍스처 → Resources.Load&lt;Sprite&gt; 후 텍스처 추출 → 재생성
    /// - Default 타입 텍스처 → Resources.Load&lt;Texture2D&gt; 후 생성
    /// - 둘 다 없으면 null 반환
    /// </summary>
    public static Sprite LoadSprite(string path)
    {
        // 1차: Sprite 타입으로 로드 (meta에서 Sprite로 설정된 경우)
        Sprite baseSp = Resources.Load<Sprite>(path);
        if (baseSp != null)
        {
            Texture2D tex = baseSp.texture;
            // ppu = 이미지 높이 → sprite height = 1.0 world unit = 1 타일
            float ppu = tex.height;
            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), // center pivot
                ppu);
        }

        // 2차: Texture2D로 로드 (Default 타입인 경우)
        Texture2D tex2d = Resources.Load<Texture2D>(path);
        if (tex2d == null) return null;

        float ppu2 = tex2d.height;
        return Sprite.Create(
            tex2d,
            new Rect(0, 0, tex2d.width, tex2d.height),
            new Vector2(0.5f, 0.5f),
            ppu2);
    }

    // ── 런타임 생성 원형 스프라이트 ─────────────────────────────────────────

    private static Sprite CreateCircle()
    {
        int size = 64;
        var tex    = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
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
        // 원형은 ppu = size → 1 world unit 크기
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
