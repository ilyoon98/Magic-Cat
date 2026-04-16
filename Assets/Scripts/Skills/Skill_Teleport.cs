using UnityEngine;

/// <summary>
/// 순간이동 — 최대 3칸 내 빈 칸으로 순간이동
/// 충전 기반: 최대 3회, 턴 시작마다 1회 자동 충전
/// </summary>
public class Skill_Teleport : SkillBase
{
    public int maxCharges    = 3;
    public int teleportRange = 3;

    private int charges;

    public int Charges => charges;

    public override SkillPreviewType PreviewType => SkillPreviewType.Teleport;

    private void Awake()
    {
        skillName   = "순간이동";
        description = $"최대 {teleportRange}칸 내 빈 칸으로 순간이동 (최대 {maxCharges}회, 매 턴 자동 충전)";
        maxCooldown = 0; // 충전 방식 — cooldown 미사용
        charges     = maxCharges;
    }

    // 충전 방식이므로 CanUse는 charges 기준
    public override bool CanUse()
    {
        if (CheatManager.Instance != null && CheatManager.Instance.ZeroCooldown) return true;
        return charges > 0;
    }

    protected override void OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        int dist = Mathf.Abs(caster.GridPos.x - targetPos.x)
                 + Mathf.Abs(caster.GridPos.y - targetPos.y);

        if (dist == 0 || dist > teleportRange)
        {
            Debug.Log("[순간이동] 범위 밖이거나 현재 위치입니다.");
            return;
        }

        var tile = BoardManager.Instance.GetTile(targetPos);
        if (tile == null || tile.IsOccupied)
        {
            Debug.Log("[순간이동] 목표 타일 사용 불가");
            return;
        }

        charges--;
        caster.PlaceOnBoard(targetPos);

        Vector3 worldPos = BoardManager.Instance.GridToWorld(targetPos);
        EffectManager.Instance?.PlayExplosion(worldPos);
        GameUI.Instance?.ShowNotify($"⚡ 순간이동! (충전 {charges}/{maxCharges})", 0.8f);
    }

    /// <summary>
    /// 턴 시작 시 ArcanePlayerUnit.StartTurn()에서 호출
    /// </summary>
    public void RechargeOne()
    {
        if (charges < maxCharges)
        {
            charges++;
            Debug.Log($"[순간이동] 충전 {charges}/{maxCharges}");
        }
    }

    // currentCooldown은 항상 0으로 유지 (PortraitPanel 쿨타임 표시 우회)
    public int GetDisplayCharges() => charges;
}
