using UnityEngine;

/// <summary>
/// E — 백마법
///   백모드 전환 + 마우스 위치를 중심으로 +자 5칸에 신성 지역 생성 (거리 무관)
///   강화 (백 게이지 100%) : +자 각 방향 2칸 확장 — 최대 9칸
///   쿨타임: 3턴  /  PreviewType: None (마우스 위치에서 즉시 발동)
///   게이지 충전: 백 +50
///
/// 신성 지역(HolyGround):
///   플레이어가 밟으면 HP +1 회복 후 소멸
///   적이 밟으면 데미지 1 후 소멸
/// </summary>
public class Skill_WhiteMagic : SkillBase
{
    // Point → 마우스로 위치를 선택한 뒤 클릭으로 확정 (십자 미리보기)
    public override SkillPreviewType PreviewType => SkillPreviewType.Point;

    private static readonly Vector2Int[] CardinalDirs =
    {
        Vector2Int.zero,
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right,
    };

    private void Awake()
    {
        skillName   = "백마법";
        description = "마우스 위치 중심 +자 5칸 신성 지역 생성 (강화 시 9칸)";
        maxCooldown = 3;
    }

    protected override bool OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (!(caster is BlackWhitePlayerUnit bw)) return false;

        // 1. 백마법 모드 전환
        bw.SetMode(BlackWhitePlayerUnit.Mode.White);

        // 2. 강화 여부 확인
        bool empowered = bw.WhiteEmpowered;
        int  armLen    = empowered ? 2 : 1; // 일반: 1칸(총 5), 강화: 2칸(총 9)

        // 3. targetPos 중심 +자 설치 (벽·범위 밖 칸은 건너뜀)
        int placed = 0;

        // 중심
        PlaceIfValid(targetPos, ref placed);

        // 4방향 × armLen칸
        foreach (var dir in new[] {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right })
        {
            for (int step = 1; step <= armLen; step++)
                PlaceIfValid(targetPos + dir * step, ref placed);
        }

        if (placed == 0)
        {
            GameUI.Instance?.ShowNotify("설치할 공간이 없습니다.", 0.8f);
            return false;
        }

        EffectManager.Instance?.PlayWoodHit(caster.transform.position);

        if (empowered)
        {
            GameUI.Instance?.ShowNotify($"⬜ 백마법 강화 — 신성 지역 {placed}칸!", 1.5f);
            bw.OnEmpoweredSkillUsed(BlackWhitePlayerUnit.Mode.White);
        }
        else
        {
            GameUI.Instance?.ShowNotify($"⬜ 백마법 — 신성 지역 {placed}칸!", 1.0f);
            bw.MarkSkillUsed(BlackWhitePlayerUnit.Mode.White);
            bw.AddGauge(BlackWhitePlayerUnit.Mode.White, 50f);
        }

        return true;
    }

    private static void PlaceIfValid(Vector2Int pos, ref int placed)
    {
        Tile tile = BoardManager.Instance.GetTile(pos);
        if (tile == null || tile.IsWall) return;

        // 기존 바닥 오브젝트가 있으면 덮어쓰지 않음 (함정 등 보존)
        if (FloorObjectManager.Instance?.GetAt(pos) != null) return;

        FloorObjectManager.Instance?.Spawn(FloorObject.ObjectType.HolyGround, pos);
        placed++;
    }
}
