using UnityEngine;

/// <summary>
/// E — 백마법
///   백모드 전환 + 지정 방향 1칸에 신성 지역 생성
///   강화 (백 게이지 100%) : 방향 직선 3칸에 신성 지역 3개 생성
///   쿨타임: 3턴
///   게이지 충전: 백 +50
///
/// 신성 지역(HolyGround):
///   플레이어가 밟으면 HP +1 회복 후 소멸
///   적이 밟으면 데미지 1 후 소멸
/// </summary>
public class Skill_WhiteMagic : SkillBase
{
    public override SkillPreviewType PreviewType => SkillPreviewType.Directional;

    private void Awake()
    {
        skillName   = "백마법";
        description = "백모드 전환 + 신성 지역 생성 (강화 시 3칸 생성)";
        maxCooldown = 3;
    }

    protected override bool OnUse(PlayerUnit caster, Vector2Int targetPos)
    {
        if (!(caster is BlackWhitePlayerUnit bw)) return false;

        // 1. 백마법 모드 전환
        bw.SetMode(BlackWhitePlayerUnit.Mode.White);

        // 2. 방향 결정
        Vector2Int dir = GridUtil.SnapToCardinal(targetPos - caster.GridPos);

        // 3. 강화 여부에 따라 신성 지역 수 결정
        bool empowered = bw.WhiteEmpowered;
        int  count     = empowered ? 3 : 1;

        // 4. 신성 지역 생성 (방향 직선으로 count칸)
        int placed = 0;
        Vector2Int cur = caster.GridPos + dir;

        for (int i = 0; i < count && placed < count; i++, cur += dir)
        {
            Tile tile = BoardManager.Instance.GetTile(cur);
            if (tile == null || tile.IsWall) break;
            // 적이 있는 칸도 설치 가능 (밟으면 즉시 발동)
            FloorObjectManager.Instance?.Spawn(FloorObject.ObjectType.HolyGround, cur);
            placed++;
        }

        if (placed == 0)
        {
            // 유효 타일 없음 — 스킬 실패
            GameUI.Instance?.ShowNotify("설치할 공간이 없습니다.", 0.8f);
            return false;
        }

        EffectManager.Instance?.PlayWoodHit(caster.transform.position);

        if (empowered)
        {
            GameUI.Instance?.ShowNotify($"⬜ 백마법 강화 — 신성 지역 {placed}개!", 1.5f);
            bw.OnEmpoweredSkillUsed();
        }
        else
        {
            GameUI.Instance?.ShowNotify("⬜ 백마법 — 신성 지역 생성!", 1.0f);
            // 5. 게이지 충전
            bw.AddGauge(BlackWhitePlayerUnit.Mode.White, 50f);
        }

        return true;
    }
}
