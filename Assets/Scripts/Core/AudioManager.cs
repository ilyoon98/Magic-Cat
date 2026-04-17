using UnityEngine;

/// <summary>
/// BGM / SFX 통합 관리
///
/// BGM : Resources/Audio/BGM/{name}.mp3  (루프)
/// SFX : Resources/Audio/SFX/{name}.mp3  (원샷)
///
/// 볼륨은 PlayerPrefs에 저장
/// PlayBGM("BGM_Title") / PlaySFX("SFX_Attack") 식으로 호출
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private const string BGM_VOL_KEY = "BGMVolume";
    private const string SFX_VOL_KEY = "SFXVolume";

    public float BGMVolume => bgmSource != null ? bgmSource.volume : 0.8f;
    public float SFXVolume => sfxSource != null ? sfxSource.volume : 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop        = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume      = PlayerPrefs.GetFloat(BGM_VOL_KEY, 0.8f);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop        = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume      = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1.0f);
    }

    // ── 볼륨 ────────────────────────────────────────────────────────────
    public void SetBGMVolume(float v)
    {
        if (bgmSource == null) return;
        bgmSource.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(BGM_VOL_KEY, bgmSource.volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float v)
    {
        if (sfxSource == null) return;
        sfxSource.volume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(SFX_VOL_KEY, sfxSource.volume);
        PlayerPrefs.Save();
    }

    // ── BGM ─────────────────────────────────────────────────────────────
    /// <summary>Resources/Audio/BGM/{name} 재생 (이미 같은 클립이 재생 중이면 유지)</summary>
    public void PlayBGM(string name)
    {
        var clip = Resources.Load<AudioClip>($"Audio/BGM/{name}");
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] BGM 없음: Audio/BGM/{name}");
            return;
        }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource?.Stop();
    }

    public void FadeBGM(float targetVolume, float duration)
    {
        StartCoroutine(FadeBGMCoroutine(targetVolume, duration));
    }

    private System.Collections.IEnumerator FadeBGMCoroutine(float target, float dur)
    {
        float start = bgmSource.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        bgmSource.volume = target;
        if (target <= 0f) bgmSource.Stop();
    }

    // ── SFX ─────────────────────────────────────────────────────────────
    /// <summary>Resources/Audio/SFX/{name} 원샷 재생</summary>
    public void PlaySFX(string name)
    {
        var clip = Resources.Load<AudioClip>($"Audio/SFX/{name}");
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] SFX 없음: Audio/SFX/{name}");
            return;
        }
        sfxSource.PlayOneShot(clip, sfxSource.volume);
    }
}
