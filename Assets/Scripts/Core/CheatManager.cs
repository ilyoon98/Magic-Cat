using UnityEngine;

/// <summary>
/// 치트 상태 관리 싱글톤
/// </summary>
public class CheatManager : MonoBehaviour
{
    public static CheatManager Instance { get; private set; }

    public bool Invincible    { get; private set; }
    public bool ZeroCooldown  { get; private set; }
    public bool TeleportMode  { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool ToggleInvincible()   { Invincible   = !Invincible;   return Invincible; }
    public bool ToggleZeroCooldown() { ZeroCooldown = !ZeroCooldown; return ZeroCooldown; }
    public bool ToggleTeleportMode() { TeleportMode = !TeleportMode; return TeleportMode; }

    /// <summary>순간이동 완료 후 PlayerInputController에서 호출</summary>
    public void DisableTeleportMode() { TeleportMode = false; }
}
