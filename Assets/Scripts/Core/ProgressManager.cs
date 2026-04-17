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

    // ── 맵별 클리어 ───────────────────────────────────────────────────────
    /// <summary>
    /// map=1,2 → 일반 맵. map=3 → 보스(=IsClearUnlocked 재사용).
    /// </summary>
    public static bool IsMapCleared(int stage, int map)
    {
        if (map >= 3) return IsClearUnlocked(stage);
        return PlayerPrefs.GetInt($"MapClear_S{stage}_M{map}", 0) == 1;
    }

    public static void UnlockMapClear(int stage, int map)
    {
        if (map >= 3) { UnlockClear(stage); return; } // 보스는 IsClearUnlocked로 통합
        string key = $"MapClear_S{stage}_M{map}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Progress] Stage{stage} Map{map} 클리어 기록!");
    }

    // ── 치트 / 디버그 ─────────────────────────────────────────────────────
    public static void UnlockAll()
    {
        for (int s = 2; s <= 3; s++) UnlockStage(s);
        for (int s = 1; s <= 3; s++)
        {
            for (int h = 0; h <= 2; h++) UnlockGallery(s, h);
            for (int m = 1; m <= 3; m++) UnlockMapClear(s, m);
            UnlockClear(s);
            UnlockBackground(s);
            UnlockDefeat(s, "Slime");
            UnlockDefeat(s, "BigSlime");
            UnlockDefeat(s, "Trap");
        }
        Debug.Log("[Progress] 전체 해금 완료");
    }

    // ── 몬스터별 패배 이미지 ──────────────────────────────────────────────
    /// <summary>해당 몬스터에게 패배 시 자동 해금</summary>
    public static bool IsDefeatUnlocked(int stage, string killerKey)
        => PlayerPrefs.GetInt($"Defeat_S{stage}_{killerKey}", 0) == 1;

    public static void UnlockDefeat(int stage, string killerKey)
    {
        if (string.IsNullOrEmpty(killerKey)) return;
        string key = $"Defeat_S{stage}_{killerKey}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Gallery] Defeat S{stage} by {killerKey} 해금!");
    }

    // ── 스테이지 클리어 이미지 ────────────────────────────────────────────
    /// <summary>해당 스테이지 보스 클리어 시 해금</summary>
    public static bool IsClearUnlocked(int stage)
        => PlayerPrefs.GetInt($"Clear_S{stage}", 0) == 1;

    public static void UnlockClear(int stage)
    {
        string key = $"Clear_S{stage}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Gallery] Clear S{stage} 해금!");
    }

    // ── 스테이지 배경 이미지 ──────────────────────────────────────────────
    /// <summary>해당 스테이지에 처음 진입 시 해금</summary>
    public static bool IsBackgroundUnlocked(int stage)
        => PlayerPrefs.GetInt($"BG_S{stage}", 0) == 1;

    public static void UnlockBackground(int stage)
    {
        string key = $"BG_S{stage}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Gallery] Background S{stage} 해금!");
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Progress] 진행 데이터 초기화");
    }
}
