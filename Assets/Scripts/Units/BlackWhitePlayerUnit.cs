using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 2스테이지 — 흑백마법사 (토끼 수인)
///
/// [모드 시스템]
///   Q → 흑마법 모드 전환 + 직선 관통 데미지 (강화: 흑 게이지 100%)
///   E → 백마법 모드 전환 + 신성 지역 생성  (강화: 백 게이지 100%)
///
/// [평타 모드별 효과]
///   기본 : 무제한 사거리 단일 타격 (PlayerUnit 기본값, AttackReach=0)
///   흑모드: 처치 시 인접한 적에게 데미지 튕김 (ChainKill)
///   백모드: 클릭한 적 단일 타격 (방향/장애물 무시)
///
/// [흑백 게이지]
///   초기 : 흑 50 / 백 50
///   충전 : Q 사용 or 흑모드 평타 → 흑 +50   /   E 사용 or 백모드 평타 → 백 +50
///   100% : 해당 스킬 강화 발동 가능 상태
///   강화 후: 양쪽 쿨타임 초기화 + 게이지 50/50 리셋
/// </summary>
public class BlackWhitePlayerUnit : PlayerUnit
{
    public enum Mode { None, Black, White }

    // ── 모드 & 게이지 ─────────────────────────────────────────────────────
    public Mode CurrentMode { get; private set; } = Mode.None;

    public float BlackGauge { get; private set; } = 50f;
    public float WhiteGauge { get; private set; } = 50f;

    public bool BlackEmpowered => BlackGauge >= 100f;
    public bool WhiteEmpowered => WhiteGauge >= 100f;

    // ── 공격 색상 ─────────────────────────────────────────────────────────
    protected override Color AttackColor => CurrentMode switch
    {
        Mode.Black => new Color(0.5f, 0.2f, 0.9f),  // 보라
        Mode.White => new Color(0.9f, 0.9f, 1.0f),  // 흰빛
        _          => new Color(0.7f, 0.7f, 0.8f),  // 회색(기본)
    };

    protected override void Awake()
    {
        base.Awake();
        skill1 = gameObject.AddComponent<Skill_BlackMagic>();
        skill2 = gameObject.AddComponent<Skill_WhiteMagic>();
    }

    // ── 평타 오버라이드 ──────────────────────────────────────────────────
    public override bool TryAttackToward(Vector2Int targetPos)
    {
        if (!CanAttack) return false;

        bool success = CurrentMode switch
        {
            Mode.Black => AttackBlackMode(targetPos),
            Mode.White => AttackWhiteMode(targetPos),
            _          => AttackDefaultMode(targetPos),
        };

        if (!success) return false;

        hasAttackedThisTurn = true;
        ActionsUsed++;
        skill1?.ReduceCooldown(1);
        skill2?.ReduceCooldown(1);
        GameUI.Instance?.Refresh();
        return true;
    }

    // ── 기본 평타: 무제한 사거리 단일 타격 ──────────────────────────────
    private bool AttackDefaultMode(Vector2Int targetPos)
    {
        Vector2Int dir   = GridUtil.SnapToCardinal(targetPos - GridPos);
        EnemyUnit  enemy = GridUtil.FindFirstEnemyInDir(GridPos, dir);

        Vector3 from = BoardManager.Instance.GridToWorld(GridPos);
        Vector3 to   = enemy != null
            ? BoardManager.Instance.GridToWorld(enemy.GridPos)
            : BoardManager.Instance.GridToWorld(GridUtil.GetFarEdge(GridPos, dir));

        EnemyUnit captured = enemy;
        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            if (captured != null && captured.IsAlive)
            {
                EffectManager.Instance?.PlayAttack(captured.transform.position);
                base.Attack(captured);
            }
        });
        return true;
    }

    // ── 흑마법 평타: 처치 시 인접 적에게 튕김 ───────────────────────────
    private bool AttackBlackMode(Vector2Int targetPos)
    {
        Vector2Int dir   = GridUtil.SnapToCardinal(targetPos - GridPos);
        EnemyUnit  enemy = GridUtil.FindFirstEnemyInDir(GridPos, dir);

        Vector3 from = BoardManager.Instance.GridToWorld(GridPos);
        Vector3 to   = enemy != null
            ? BoardManager.Instance.GridToWorld(enemy.GridPos)
            : BoardManager.Instance.GridToWorld(GridUtil.GetFarEdge(GridPos, dir));

        EnemyUnit captured = enemy;
        BlackWhitePlayerUnit self = this;

        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            if (captured == null || !captured.IsAlive) return;

            EffectManager.Instance?.PlayFireHit(captured.transform.position);
            captured.TakeDamage(self.attackDamage);

            // 처치 시 인접 적에게 튕김
            if (!captured.IsAlive)
                self.ChainKillSplash(captured.GridPos);
        });

        // 게이지 충전 (흑 +50)
        AddGauge(Mode.Black, 50f);
        return true;
    }

    /// <summary>처치한 위치 주변 4칸의 살아있는 적에게 데미지 1</summary>
    private void ChainKillSplash(Vector2Int origin)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        bool splashed = false;
        foreach (var dir in dirs)
        {
            var tile = BoardManager.Instance.GetTile(origin + dir);
            if (tile == null) continue;
            if (tile.OccupiedUnit is EnemyUnit adj && adj.IsAlive)
            {
                adj.TakeDamage(1);
                EffectManager.Instance?.PlayBlood(adj.transform.position);
                splashed = true;
            }
        }
        if (splashed)
            GameUI.Instance?.ShowNotify("⛓ 흑마법 처치 — 연쇄!", 1.0f);
    }

    // ── 백마법 평타: 클릭한 적 단일 타격 (방향·장애물 무시) ────────────
    private bool AttackWhiteMode(Vector2Int targetPos)
    {
        // 맵 전체에서 targetPos에 있는 적 직접 탐색
        var tile = BoardManager.Instance.GetTile(targetPos);
        EnemyUnit target = tile?.OccupiedUnit as EnemyUnit;

        // 타겟 칸에 적이 없으면 방향 직선 기준 첫 번째 적으로 폴백
        if (target == null)
        {
            Vector2Int dir = GridUtil.SnapToCardinal(targetPos - GridPos);
            target = GridUtil.FindFirstEnemyInDir(GridPos, dir);
        }

        if (target == null) return false;

        Vector3 from = BoardManager.Instance.GridToWorld(GridPos);
        Vector3 to   = BoardManager.Instance.GridToWorld(target.GridPos);

        EnemyUnit captured = target;
        BlackWhitePlayerUnit self = this;

        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            if (captured == null || !captured.IsAlive) return;
            EffectManager.Instance?.PlayWoodHit(captured.transform.position);
            self.BaseAttack(captured);
        });

        // 게이지 충전 (백 +50)
        AddGauge(Mode.White, 50f);
        return true;
    }

    // base.Attack 래퍼 — C# 람다 내부에서 base.X() 직접 호출 불가하므로 래퍼 사용
    public void BaseAttack(Unit target) => base.Attack(target);

    // ── 모드 전환 ────────────────────────────────────────────────────────
    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        GameUI.Instance?.Refresh();
        Debug.Log($"[흑백마법사] 모드 → {mode}");
    }

    // ── 게이지 관리 ──────────────────────────────────────────────────────
    /// <summary>지정 모드 게이지 추가. 100 초과는 100으로 클램프.</summary>
    public void AddGauge(Mode mode, float amount)
    {
        if (mode == Mode.Black)
        {
            BlackGauge = Mathf.Min(100f, BlackGauge + amount);
            if (BlackGauge >= 100f)
                GameUI.Instance?.ShowNotify("⬛ 흑 게이지 MAX — 강화 준비!", 1.2f);
        }
        else if (mode == Mode.White)
        {
            WhiteGauge = Mathf.Min(100f, WhiteGauge + amount);
            if (WhiteGauge >= 100f)
                GameUI.Instance?.ShowNotify("⬜ 백 게이지 MAX — 강화 준비!", 1.2f);
        }
        GameUI.Instance?.Refresh();
    }

    /// <summary>강화 스킬 사용 후 양쪽 쿨타임 초기화 + 게이지 리셋</summary>
    public void OnEmpoweredSkillUsed()
    {
        BlackGauge = 50f;
        WhiteGauge = 50f;
        skill1?.ReduceCooldown(99); // 강제 0으로
        skill2?.ReduceCooldown(99);
        GameUI.Instance?.Refresh();
        Debug.Log("[흑백마법사] 강화 발동 — 게이지·쿨타임 리셋");
    }

}
