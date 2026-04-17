using UnityEngine;

/// <summary>
/// 치트 상태 관리 싱글톤
/// </summary>
public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }

    public bool Invincible         { get; private set; }
    public bool ZeroCooldown       { get; private set; }
    public bool TeleportMode       { get; private set; }
    /// <summary>
    /// false(기본) = 패배·체력·캐릭터 이미지를 블러 처리로 가림.
    /// true = 이미지 원본 노출 (사무실 외 환경에서 확인 용)
    /// </summary>
    public bool ShowSensitiveImages { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool ToggleInvincible()      { Invincible         = !Invincible;         return Invincible; }
    public bool ToggleZeroCooldown()    { ZeroCooldown       = !ZeroCooldown;       return ZeroCooldown; }
    public bool ToggleTeleportMode()    { TeleportMode       = !TeleportMode;       return TeleportMode; }
    public bool ToggleShowImages()      { ShowSensitiveImages = !ShowSensitiveImages; return ShowSensitiveImages; }

    /// <summary>순간이동 완료 후 PlayerInputController에서 호출</summary>
    public void DisableTeleportMode() { TeleportMode = false; }
}
