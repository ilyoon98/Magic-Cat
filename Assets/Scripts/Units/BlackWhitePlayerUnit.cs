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

    /// <summary>0 = 완전 흑 (BlackEmpowered), 100 = 완전 백 (WhiteEmpowered), 초기 50</summary>
    public float Gauge { get; private set; } = 50f;

    public bool BlackEmpowered => Gauge <= 0f;
    public bool WhiteEmpowered => Gauge >= 100f;

    // ── 이번 턴 정규 스킬 사용 여부 (강화 후 쿨타임 결정에 사용) ────────────
    private bool usedBlackSkillThisTurn = false;
    private bool usedWhiteSkillThisTurn = false;

    /// <summary>투사체 비행 중 — true 동안 PlayerInputController가 행동 입력을 차단</summary>
    public bool IsProjectilePending { get; private set; } = false;

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

    public override void StartTurn()
    {
        base.StartTurn();
        usedBlackSkillThisTurn = false;
        usedWhiteSkillThisTurn = false;
    }

    /// <summary>정규(비강화) 스킬 사용 시 호출 — 강화 후 쿨타임 초기화 여부 결정용</summary>
    public void MarkSkillUsed(Mode mode)
    {
        if (mode == Mode.Black) usedBlackSkillThisTurn = true;
        else if (mode == Mode.White) usedWhiteSkillThisTurn = true;
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
        BlackWhitePlayerUnit self = this;
        IsProjectilePending = true;
        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            self.IsProjectilePending = false;
            if (captured == null || !captured.IsAlive) return;
            EffectManager.Instance?.PlayAttack(captured.transform.position);
            // Unit.Attack() 의 거리 체크(attackRange=1)를 우회, 직접 데미지
            captured.TakeDamage(self.attackDamage);
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

        IsProjectilePending = true;
        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            self.IsProjectilePending = false;
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

    // ── 백마법 평타: 지점 타격 (장애물·적 무관, 거리 무관) ────────────────
    private bool AttackWhiteMode(Vector2Int targetPos)
    {
        var tile = BoardManager.Instance.GetTile(targetPos);
        if (tile == null) return false;

        // targetPos 위치 그대로 발사 (적이 없어도 허용)
        EnemyUnit target = tile.OccupiedUnit as EnemyUnit;

        Vector3 from = BoardManager.Instance.GridToWorld(GridPos);
        Vector3 to   = BoardManager.Instance.GridToWorld(targetPos);

        EnemyUnit captured = target;
        BlackWhitePlayerUnit self = this;

        IsProjectilePending = true;
        SkillProjectile.Fire(from, to, AttackColor, AttackSpeed, onHit: () =>
        {
            self.IsProjectilePending = false;
            if (captured == null || !captured.IsAlive) return;
            EffectManager.Instance?.PlayWoodHit(captured.transform.position);
            // Unit.Attack() 의 거리 체크(attackRange=1)를 우회, 직접 데미지 (백마법은 거리 무제한)
            captured.TakeDamage(self.attackDamage);
        });

        // 게이지 충전 (백 +50)
        AddGauge(Mode.White, 50f);
        return true;
    }

    // ── 모드 전환 ────────────────────────────────────────────────────────
    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        GameUI.Instance?.Refresh();
        Debug.Log($"[흑백마법사] 모드 → {mode}");
    }

    // ── 게이지 관리 ──────────────────────────────────────────────────────
    /// <summary>
    /// 흑 모드 → Gauge 감소 (0 방향), 백 모드 → Gauge 증가 (100 방향).
    /// 0 이하: 흑 강화 준비, 100 이상: 백 강화 준비.
    /// </summary>
    public void AddGauge(Mode mode, float amount)
    {
        if (mode == Mode.Black)
        {
            Gauge = Mathf.Max(0f, Gauge - amount);
            if (Gauge <= 0f)
                GameUI.Instance?.ShowNotify("⬛ 흑 게이지 MAX — 강화 준비!", 1.2f);
        }
        else if (mode == Mode.White)
        {
            Gauge = Mathf.Min(100f, Gauge + amount);
            if (Gauge >= 100f)
                GameUI.Instance?.ShowNotify("⬜ 백 게이지 MAX — 강화 준비!", 1.2f);
        }
        GameUI.Instance?.Refresh();
    }

    /// <summary>
    /// 강화 스킬 사용 후 게이지 리셋 + 쿨타임 조건부 초기화.
    ///   · 이번 턴에 해당 속성 정규 스킬을 사용했으면 → 해당 스킬 쿨타임 초기화 안 함
    ///   · 사용하지 않았으면 → 해당 스킬 쿨타임 초기화 (보너스)
    ///   · 반대 속성 스킬 쿨타임은 항상 초기화
    /// </summary>
    public void OnEmpoweredSkillUsed(Mode empoweredMode)
    {
        Gauge = 50f;

        bool usedRegular = empoweredMode == Mode.Black
            ? usedBlackSkillThisTurn
            : usedWhiteSkillThisTurn;

        if (empoweredMode == Mode.Black)
        {
            // 반대(백) 쿨타임 항상 초기화
            skill2?.ReduceCooldown(99);
            // 흑 쿨타임: 정규 흑 스킬을 이미 썼으면 초기화 안 함
            if (!usedRegular) skill1?.ReduceCooldown(99);
        }
        else
        {
            // 반대(흑) 쿨타임 항상 초기화
            skill1?.ReduceCooldown(99);
            // 백 쿨타임: 정규 백 스킬을 이미 썼으면 초기화 안 함
            if (!usedRegular) skill2?.ReduceCooldown(99);
        }

        GameUI.Instance?.Refresh();
        Debug.Log($"[흑백마법사] 강화 발동 ({empoweredMode}) — 정규 사용: {usedRegular}, 쿨 초기화: {!usedRegular}");
    }

}
