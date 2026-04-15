using UnityEngine;

/// <summary>
/// PlayerPrefs 기반 진행 상황 저장
/// - 스테이지 잠금/해금
/// - 갤러리 이미지 잠금/해금
/// </summary>
public static class ProgressManager
{
    // ── 스테이지 잠금 ──────────────────────────────────────────────────────
    public static bool IsStageUnlocked(int stage)
    {
        if (stage <= 1) return true;
        return PlayerPrefs.GetInt($"Stage{stage}Unlocked", 0) == 1;
    }

    public static void UnlockStage(int stage)
    {
        if (stage <= 1 || stage > 3) return;
        PlayerPrefs.SetInt($"Stage{stage}Unlocked", 1);
        PlayerPrefs.Save();
        Debug.Log($"[Progress] Stage {stage} 해금!");
    }

    // ── 갤러리 이미지 잠금 ─────────────────────────────────────────────────
    /// <summary>
    /// hp=3 (풀피) 는 항상 해금.
    /// hp=2,1,0 은 플레이 중 해당 체력에 도달해야 해금.
    /// </summary>
    public static bool IsGalleryUnlocked(int stage, int hp)
    {
        if (hp >= 3) return true;
        return PlayerPrefs.GetInt($"Gallery_S{stage}_HP{hp}", 0) == 1;
    }

    public static void UnlockGallery(int stage, int hp)
    {
        if (hp >= 3) return; // HP3은 항상 해금이므로 저장 불필요
        string key = $"Gallery_S{stage}_HP{hp}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return; // 이미 해금
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Gallery] Stage{stage}_HP{hp} 이미지 해금!");
    }

    // ── 치트 / 디버그 ─────────────────────────────────────────────────────
    public static void UnlockAll()
    {
        for (int s = 2; s <= 3; s++) UnlockStage(s);
        for (int s = 1; s <= 3; s++)
            for (int h = 0; h <= 2; h++) UnlockGallery(s, h);
        Debug.Log("[Progress] 전체 해금 완료");
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Progress] 진행 데이터 초기화");
    }
}
