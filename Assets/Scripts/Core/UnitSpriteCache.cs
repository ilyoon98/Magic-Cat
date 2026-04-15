using UnityEngine;

/// <summary>
/// 런타임 생성 스프라이트를 캐시해서 재사용
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
