using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 스킬 발사체 - 출발지에서 목표 위치까지 날아간 뒤 이펙트 실행
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    public static void Fire(Vector3 from, Vector3 to, Color color, float speed, Action onHit)
    {
        var go = new GameObject("SkillProjectile");

        // 발사체 시각 — 빛나는 원
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateGlowSprite();
        sr.color = color;
        sr.sortingOrder = 8;
        go.transform.localScale = Vector3.one * 0.35f;

        go.transform.position = from;
        go.AddComponent<SkillProjectile>().StartCoroutine(
            go.GetComponent<SkillProjectile>().Move(to, speed, onHit));
    }

    private IEnumerator Move(Vector3 target, float speed, Action onHit)
    {
        float dist = Vector3.Distance(transform.position, target);
        float duration = dist / speed;
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            // 이동 방향으로 회전
            Vector3 dir = (target - start).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
        onHit?.Invoke();
        Destroy(gameObject);
    }

    private static Sprite CreateGlowSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center, dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                alpha = alpha * alpha; // 가운데가 밝은 glow
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
