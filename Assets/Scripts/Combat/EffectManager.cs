using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ToonFX 이펙트를 위치에 스폰하는 매니저
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    // 이펙트 프리팹 참조
    public GameObject fxPunchNormal;
    public GameObject fxPunchCritical;
    public GameObject fxExplosion;
    public GameObject fxHeal;
    public GameObject fxFireHit;
    public GameObject fxWaterHit;
    public GameObject fxEarthHit;
    public GameObject fxWoodHit;
    public GameObject fxPlayerHit;   // HeartsPinkExplosion — 플레이어 피격
    public GameObject fxBlood;       // Blood — 적 피격

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadEffects();
    }

    private void LoadEffects()
    {
#if UNITY_EDITOR
        // 경로 prefix
        const string P = "Assets/ToonFX/Prefabs/";
        fxPunchNormal   = Load(P + "Punches/PunchNormal.prefab");
        fxPunchCritical = Load(P + "Punches/PunchCritical.prefab");
        fxExplosion     = Load(P + "Explosions/ExplosionBig.prefab");
        fxHeal          = Load(P + "Hearts/HeartsPink.prefab");
        fxFireHit       = Load(P + "Fire/New/FireRed.prefab");
        fxWaterHit      = Load(P + "Water and Bubbles/BubbleBlast.prefab");
        fxEarthHit      = Load(P + "Explosions/ExplosionSimple.prefab");
        fxWoodHit       = Load(P + "Splat/SplatGreen.prefab");
        fxPlayerHit     = Load(P + "Hearts/HeartsPinkExplosion.prefab");
        fxBlood         = Load(P + "Blood/Blood.prefab");
#endif
    }

#if UNITY_EDITOR
    private static GameObject Load(string path)
    {
        var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) Debug.LogWarning($"[EffectManager] 프리팹 없음: {path}");
        return go;
    }
#endif

    public void PlayAttack(Vector3 pos)     => Spawn(fxPunchNormal, pos);
    public void PlayCritical(Vector3 pos)   => Spawn(fxPunchCritical, pos);
    public void PlayExplosion(Vector3 pos)  => Spawn(fxExplosion, pos);
    public void PlayHeal(Vector3 pos)       => Spawn(fxHeal, pos);
    public void PlayFireHit(Vector3 pos)    => Spawn(fxFireHit, pos);
    public void PlayWaterHit(Vector3 pos)   => Spawn(fxWaterHit, pos);
    public void PlayEarthHit(Vector3 pos)   => Spawn(fxEarthHit, pos);
    public void PlayWoodHit(Vector3 pos)    => Spawn(fxWoodHit, pos);
    public void PlayPlayerHit(Vector3 pos)  => Spawn(fxPlayerHit, pos);
    public void PlayBlood(Vector3 pos)      => Spawn(fxBlood, pos);

    private void Spawn(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, pos, Quaternion.identity);

        // 루트 파티클에만 stopAction 설정 (sub-emitter에는 금지)
        var rootPs = go.GetComponent<ParticleSystem>();
        if (rootPs != null)
        {
            var main = rootPs.main;
            main.loop       = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
        }

        // ── 모든 파티클 렌더러를 타일·유닛 위로 올림 ──────────────────────
        // 타일 Inner sortingOrder=1, 유닛=5, DamagePopup=20
        // 이펙트는 유닛 바로 위(6)에 렌더링
        foreach (var pr in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            pr.sortingLayerName = "Default";
            pr.sortingOrder     = 6;
        }

        // 안전망: 최대 지속 시간 후 강제 제거
        Destroy(go, 3f);
    }
}
