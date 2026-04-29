using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 플레이어 입력 처리
///
/// [턴당 최대 2행동] 이동 / 평타 / 스킬 각 1회씩
///   WASD / 화살표   → 이동
///   좌클릭 1회      → 평타 미리보기 진입 (방향 하이라이트)
///   좌클릭 2회      → 평타 실행 (미리보기 상태에서 재클릭)
///   Q / E           → 스킬 (미리보기 진입 또는 즉시 발동)
///   Space           → 턴 조기 종료
///   ESC / 우클릭    → 미리보기 취소 / 인게임 메뉴 토글
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    public static bool IsSkillPending => _instance != null && _instance.state == InputState.SkillPending;
    private static PlayerInputController _instance;

    private PlayerUnit  player;
    private TurnManager turnManager;

    // ── 입력 상태 ─────────────────────────────────────────────────────────
    private enum InputState { Normal, AttackPending, SkillPending }
    private InputState state = InputState.Normal;

    private int        pendingSkillIndex  = 0;
    private Vector2Int pendingAttackDir   = Vector2Int.zero; // AttackPending 상태의 현재 방향

    // ── 미리보기 타일 목록 ────────────────────────────────────────────────
    private readonly List<Vector2Int> previewTiles       = new List<Vector2Int>();
    private readonly List<Vector2Int> teleportRangeTiles = new List<Vector2Int>();
    private readonly List<Vector2Int> attackPreviewTiles = new List<Vector2Int>();
    private readonly List<Vector2Int> moveTiles          = new List<Vector2Int>();

    private Vector2Int lastPreviewDir       = Vector2Int.zero;
    private Vector2Int lastPreviewTarget    = new Vector2Int(-999, -999);
    private Vector2Int lastAttackPreviewDir = Vector2Int.zero;
    private Vector2Int lastHover            = new Vector2Int(-1, -1);

    public void Init(PlayerUnit p)
    {
        _instance   = this;
        player      = p;
        turnManager = TurnManager.Instance;
    }

    // ── 이동 가능 타일 하이라이트 ─────────────────────────────────────────
    public void RefreshMoveHighlight()
    {
        // 미리보기 중에는 이동 하이라이트 갱신 안 함
        if (state != InputState.Normal) return;

        ClearAttackPreview();
        lastAttackPreviewDir = Vector2Int.zero;
        ClearMoveHighlight();

        if (player == null || !player.CanMove) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.PlayerTurn) return;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int pos = player.GridPos + dir;
            var tile = BoardManager.Instance.GetTile(pos);
            // Danger(적 공격 예고) 타일은 이동 하이라이트로 덮지 않음
            if (tile != null && !tile.IsOccupied
                && tile.CurrentHighlight != Tile.HighlightType.Danger)
            {
                tile.SetHighlight(Tile.HighlightType.Move);
                moveTiles.Add(pos);
            }
        }
    }

    private void ClearMoveHighlight()
    {
        foreach (var pos in moveTiles)
        {
            var t = BoardManager.Instance.GetTile(pos);
            // Danger 타일은 이동 하이라이트 해제 시에도 건드리지 않음
            if (t != null && t.CurrentHighlight != Tile.HighlightType.Danger)
                t.SetHighlight(Tile.HighlightType.None);
        }
        moveTiles.Clear();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        if (GameManager.Instance == null || player == null) return;

        HandleEsc();

        if (GameManager.Instance.CurrentState != GameManager.GameState.PlayerTurn) return;
        if (turnManager.CurrentPhase != TurnManager.TurnPhase.PlayerTurn) return;

        // ── SkillPending ─────────────────────────────────────────────────
        if (state == InputState.SkillPending)
        {
            UpdateSkillPreview();
            HandleSkillConfirmCancel();
            return;
        }

        // ── AttackPending ────────────────────────────────────────────────
        if (state == InputState.AttackPending)
        {
            UpdateAttackPendingPreview();
            HandleAttackConfirmCancel();
            return;
        }

        // ── Normal ───────────────────────────────────────────────────────
        UpdateHoverHighlight();
        RefreshMoveHighlight();

        // 투사체 비행 중 — 착탄 전까지 행동 입력 차단
        if (player is BlackWhitePlayerUnit bwFlight && bwFlight.IsProjectilePending)
            return;

        if (player.HasActedThisTurn)
        {
            ClearAttackPreview();
            return;
        }

        HandleMove();
        HandleFirstClickAttack();
        HandleSkillKeyPress();
        HandleSkip();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ESC
    // ═══════════════════════════════════════════════════════════════════════
    private void HandleEsc()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Idle) return;
        if (SpecialSceneController.Instance != null && SpecialSceneController.Instance.IsTransitioning) return;

        if (state == InputState.SkillPending)
        {
            CancelSkill();
            return;
        }
        if (state == InputState.AttackPending)
        {
            CancelAttackPending();
            return;
        }

        if (GameUI.Instance != null)
        {
            if (GameUI.IsMenuOpen) GameUI.Instance.CloseMenu();
            else                   GameUI.Instance.OpenMenu();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Normal 상태 핸들러
    // ═══════════════════════════════════════════════════════════════════════

    private void UpdateHoverHighlight()
    {
        Vector2Int hover = GetMouseGridPos();

        bool isMoveTile = moveTiles.Contains(hover);
        bool canShowAtk = player.CanAttack && !isMoveTile;

        // 백마법 모드: 맵 전체 타일 하이라이트 (사거리 무제한)
        bool isWhiteMode = player is BlackWhitePlayerUnit bwHov
            && bwHov.CurrentMode == BlackWhitePlayerUnit.Mode.White;

        // 전체 맵 하이라이트 완료 여부 추적용 센티넬
        var whiteMapSentinel = new Vector2Int(-998, -998);

        if (canShowAtk && isWhiteMode)
        {
            // 아직 전체 맵 하이라이트를 안 그렸으면 한 번만 그림
            if (lastAttackPreviewDir != whiteMapSentinel)
            {
                ClearAttackPreview();
                BuildWhiteModeFullMapPreview();
                lastAttackPreviewDir = whiteMapSentinel;
            }
            // 마우스 이하 hover 처리는 하단 lastHover 로직에서 담당
        }
        else if (canShowAtk && !isWhiteMode)
        {
            Vector2Int delta = hover - player.GridPos;
            Vector2Int dir   = delta != Vector2Int.zero ? GridUtil.SnapToCardinal(delta) : Vector2Int.zero;

            if (dir != lastAttackPreviewDir)
            {
                ClearAttackPreview();
                if (dir != Vector2Int.zero)
                    BuildAttackPreview(dir);
                lastAttackPreviewDir = dir;
            }
        }
        else if (attackPreviewTiles.Count > 0)
        {
            ClearAttackPreview();
            lastAttackPreviewDir = Vector2Int.zero;
        }

        if (hover == lastHover) return;

        var prevTile = BoardManager.Instance.GetTile(lastHover);
        if (prevTile != null)
        {
            if (attackPreviewTiles.Contains(lastHover))
            {
                bool isEnemy = prevTile.IsOccupied && prevTile.OccupiedUnit is EnemyUnit;
                prevTile.SetHighlight(isEnemy ? Tile.HighlightType.Attack : Tile.HighlightType.Skill);
            }
            else if (prevTile.CurrentHighlight == Tile.HighlightType.Danger)
            {
                // Danger(적 공격 예고) 하이라이트는 마우스 이탈 시 건드리지 않음
            }
            else
            {
                prevTile.SetHighlight(moveTiles.Contains(lastHover)
                    ? Tile.HighlightType.Move
                    : Tile.HighlightType.None);
            }
        }

        Tile cur = BoardManager.Instance.GetTile(hover);
        if (cur != null)
        {
            bool cheatTp = CheatManager.Instance != null && CheatManager.Instance.TeleportMode;
            if (cheatTp && !cur.IsOccupied)
                cur.SetHighlight(Tile.HighlightType.Selected);
            else if (cur.CurrentHighlight == Tile.HighlightType.Danger)
            {
                // Danger(적 공격 예고) 타일은 마우스 진입 시에도 건드리지 않음
            }
            else if (isMoveTile)
                cur.SetHighlight(Tile.HighlightType.Move);
            else if (attackPreviewTiles.Contains(hover))
            {
                bool isEnemy = cur.IsOccupied && cur.OccupiedUnit is EnemyUnit;
                cur.SetHighlight(isEnemy ? Tile.HighlightType.Attack : Tile.HighlightType.Skill);
            }
            else
                cur.SetHighlight(cur.IsOccupied ? Tile.HighlightType.Attack : Tile.HighlightType.None);
        }

        lastHover = hover;
    }

    // 1차 클릭 — 평타 미리보기 진입 (이동은 WASD 전용)
    private void HandleFirstClickAttack()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2Int targetPos = GetMouseGridPos();
        Tile tile = BoardManager.Instance.GetTile(targetPos);
        if (tile == null) return;

        // 치트 순간이동
        if (CheatManager.Instance != null && CheatManager.Instance.TeleportMode)
        {
            if (!tile.IsOccupied)
            {
                player.PlaceOnBoard(targetPos);
                CheatManager.Instance.DisableTeleportMode();
                CheatPanel.Instance?.RefreshUI();
                GameUI.Instance?.ShowNotify("✅ 이동 완료", 0.7f);
            }
            return;
        }

        // 평타 행동이 남아있지 않으면 무시
        if (!player.CanAttack) return;

        // 백마법 모드: 적 클릭 → 선택 하이라이트 (재클릭으로 확정)
        if (player is BlackWhitePlayerUnit bwWhite
            && bwWhite.CurrentMode == BlackWhitePlayerUnit.Mode.White)
        {
            // 적이 있는 타일만 선택 가능. 빈 칸 클릭은 무시
            if (tile.OccupiedUnit is EnemyUnit)
                EnterWhiteAttackPending(targetPos);
            return;
        }

        // 클릭 위치 기준으로 방향 산출 → 평타 미리보기 진입
        Vector2Int delta = targetPos - player.GridPos;
        if (delta == Vector2Int.zero) return;

        Vector2Int attackDir = GridUtil.SnapToCardinal(delta);
        EnterAttackPending(attackDir);
    }

    private void HandleMove()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2Int dir = Vector2Int.zero;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)    dir = Vector2Int.up;
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)  dir = Vector2Int.down;
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  dir = Vector2Int.left;
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) dir = Vector2Int.right;
        if (dir == Vector2Int.zero) return;

        if (player.TryMove(dir))
            turnManager.OnPlayerActed();
    }

    private void HandleSkip()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            turnManager.SkipTurn();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AttackPending 상태
    // ═══════════════════════════════════════════════════════════════════════

    private void EnterAttackPending(Vector2Int dir)
    {
        ClearAttackPreview();
        ClearMoveHighlight();
        ClearHoverHighlight();
        lastAttackPreviewDir = Vector2Int.zero;

        state            = InputState.AttackPending;
        pendingAttackDir = dir;

        BuildAttackPreview(dir);
        lastAttackPreviewDir = dir;

        GameUI.Instance?.ShowNotify("⚔ 같은 방향 재클릭으로 공격 / 다른 행동 or ESC 취소", 2.5f);
    }

    /// <summary>백마법 모드 전용: 적 선택 → AttackPending 진입 (Selected 하이라이트)</summary>
    private void EnterWhiteAttackPending(Vector2Int enemyPos)
    {
        ClearAttackPreview();
        ClearMoveHighlight();
        ClearHoverHighlight();

        state            = InputState.AttackPending;
        pendingAttackDir = enemyPos - player.GridPos; // 선택된 적의 상대 위치 저장

        // 선택된 적 타일을 노란색(Selected)으로 표시
        var tile = BoardManager.Instance.GetTile(enemyPos);
        if (tile != null)
        {
            tile.SetHighlight(Tile.HighlightType.Selected);
            attackPreviewTiles.Add(enemyPos);
        }

        GameUI.Instance?.ShowNotify("⬜ 같은 적 재클릭으로 공격 확정 / 다른 적 클릭으로 교체 / ESC 취소", 2.5f);
    }

    // 마우스 방향이 바뀌면 미리보기도 갱신
    private void UpdateAttackPendingPreview()
    {
        // 백마법 모드: 선택된 적 하이라이트만 유지, 방향 갱신 하지 않음
        if (player is BlackWhitePlayerUnit bwUpd
            && bwUpd.CurrentMode == BlackWhitePlayerUnit.Mode.White)
            return;

        Vector2Int hover = GetMouseGridPos();
        Vector2Int delta = hover - player.GridPos;
        if (delta == Vector2Int.zero) return;

        Vector2Int dir = GridUtil.SnapToCardinal(delta);
        if (dir == pendingAttackDir) return;

        // 방향이 바뀌면 미리보기 갱신
        ClearAttackPreview();
        pendingAttackDir     = dir;
        lastAttackPreviewDir = dir;
        BuildAttackPreview(dir);
    }

    private void HandleAttackConfirmCancel()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        // ── 좌클릭 ───────────────────────────────────────────────────────
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            // 백마법 모드: 재클릭=확정, 다른 적=교체, 빈 칸=취소
            if (player is BlackWhitePlayerUnit bwCancel
                && bwCancel.CurrentMode == BlackWhitePlayerUnit.Mode.White)
            {
                Vector2Int clickedPos     = GetMouseGridPos();
                Vector2Int selectedEnemy  = player.GridPos + pendingAttackDir;

                if (clickedPos == selectedEnemy)
                {
                    // 같은 적 재클릭 → 확정
                    ConfirmAttack();
                }
                else
                {
                    var clickedTile = BoardManager.Instance.GetTile(clickedPos);
                    if (clickedTile != null && clickedTile.OccupiedUnit is EnemyUnit)
                        EnterWhiteAttackPending(clickedPos); // 다른 적 → 선택 교체
                    else
                        CancelAttackPending();               // 빈 칸 → 취소
                }
                return;
            }

            // 일반 모드: 즉시 공격 확정
            ConfirmAttack();
            return;
        }

        // ── 우클릭 → 취소 ───────────────────────────────────────────────
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            CancelAttackPending();
            return;
        }

        if (kb == null) return;

        // ── Q / E → 평타 취소 후 스킬로 전환 ────────────────────────────
        if (kb.qKey.wasPressedThisFrame)
        {
            CancelAttackPending(silent: true);
            TryEnterSkillMode(1);
            return;
        }
        if (kb.eKey.wasPressedThisFrame)
        {
            CancelAttackPending(silent: true);
            TryEnterSkillMode(2);
            return;
        }

        // ── 이동키 → 평타 취소 후 이동 ──────────────────────────────────
        bool moveKey = kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame
                    || kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame
                    || kb.upArrowKey.wasPressedThisFrame  || kb.downArrowKey.wasPressedThisFrame
                    || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        if (moveKey)
        {
            CancelAttackPending(silent: true);
            HandleMove();
            return;
        }

        // ── Space → 평타 취소 후 턴 종료 ────────────────────────────────
        if (kb.spaceKey.wasPressedThisFrame)
        {
            CancelAttackPending(silent: true);
            turnManager.SkipTurn();
        }
    }

    private void ConfirmAttack()
    {
        Vector2Int dir = pendingAttackDir;
        ClearAttackPreview();
        state            = InputState.Normal;
        pendingAttackDir = Vector2Int.zero;

        // 백마법 모드: 저장된 선택 적 위치 사용 (player.GridPos + dir = 선택한 적 GridPos)
        // 일반 모드: 플레이어 위치 + 방향 = 공격 목표
        Vector2Int targetPos = player.GridPos + dir;

        if (player.TryAttackToward(targetPos))
            turnManager.OnPlayerActed();
        else
            GameUI.Instance?.ShowNotify("공격 불가", 0.6f);
    }

    private void CancelAttackPending(bool silent = false)
    {
        ClearAttackPreview();
        state            = InputState.Normal;
        pendingAttackDir = Vector2Int.zero;
        lastAttackPreviewDir = Vector2Int.zero;

        if (!silent)
            GameUI.Instance?.ShowNotify("평타 취소", 0.5f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Skill 공통 진입
    // ═══════════════════════════════════════════════════════════════════════

    private void TryEnterSkillMode(int skillIndex)
    {
        var skill = player.GetSkill(skillIndex);
        if (skill == null) return;

        if (!skill.IsFree && !player.CanUseSkill)
        {
            GameUI.Instance?.ShowNotify("이번 턴 스킬 행동을 이미 사용했습니다", 0.8f);
            return;
        }

        if (!skill.CanUse())
        {
            GameUI.Instance?.ShowNotify("스킬 쿨타임 중", 0.7f);
            return;
        }

        // 즉시 발동 스킬
        if (skill.PreviewType == SkillBase.SkillPreviewType.None)
        {
            if (player.TryUseSkill(skillIndex, GetMouseGridPos()))
            {
                if (!skill.IsFree) turnManager.OnPlayerActed();
                else               GameUI.Instance?.Refresh();
            }
            return;
        }

        // 미리보기 모드
        ClearHoverHighlight();
        ClearMoveHighlight();
        state             = InputState.SkillPending;
        pendingSkillIndex = skillIndex;
        lastPreviewDir    = Vector2Int.zero;
        lastPreviewTarget = new Vector2Int(-999, -999);

        if (skill.PreviewType == SkillBase.SkillPreviewType.Teleport)
            ShowTeleportRange(skill);

        GameUI.Instance?.ShowNotify(
            $"[{(skillIndex == 1 ? "Q" : "E")}] {skill.skillName} — 클릭 or 같은키 확정 / 다른키 취소", 3f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Normal 상태 스킬 키 처리
    // ═══════════════════════════════════════════════════════════════════════

    private void HandleSkillKeyPress()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        int skillIndex = 0;
        if (kb.qKey.wasPressedThisFrame) skillIndex = 1;
        if (kb.eKey.wasPressedThisFrame) skillIndex = 2;
        if (skillIndex == 0) return;

        TryEnterSkillMode(skillIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SkillPending 상태
    // ═══════════════════════════════════════════════════════════════════════

    private void UpdateSkillPreview()
    {
        var skill = player.GetSkill(pendingSkillIndex);
        if (skill == null) { CancelSkill(); return; }

        if (skill.PreviewType == SkillBase.SkillPreviewType.Directional)
            UpdateDirectionalPreview();
        else if (skill.PreviewType == SkillBase.SkillPreviewType.Teleport)
            UpdateTeleportPreview();
        else if (skill.PreviewType == SkillBase.SkillPreviewType.Point)
            UpdatePointPreview();
    }

    private void UpdateDirectionalPreview()
    {
        Vector2Int mousePos = GetMouseGridPos();
        Vector2Int delta    = mousePos - player.GridPos;
        if (delta == Vector2Int.zero) return;

        Vector2Int dir = GridUtil.SnapToCardinal(delta);
        if (dir == lastPreviewDir) return;
        lastPreviewDir = dir;

        ClearSkillPreview();

        // 흑마법 강화 모드: 3줄 평행 미리보기
        if (player is BlackWhitePlayerUnit bwUnit && bwUnit.BlackEmpowered
            && pendingSkillIndex == 1) // Q = 흑마법
        {
            Vector2Int perpA = new Vector2Int(-dir.y,  dir.x);
            Vector2Int perpB = new Vector2Int( dir.y, -dir.x);
            ShowSkillLine(player.GridPos,        dir);
            ShowSkillLine(player.GridPos + perpA, dir);
            ShowSkillLine(player.GridPos + perpB, dir);
        }
        else
        {
            ShowSkillLine(player.GridPos, dir);
        }
    }

    /// <summary>지정 시작점에서 dir 방향으로 스킬 미리보기 타일을 추가한다.</summary>
    private void ShowSkillLine(Vector2Int start, Vector2Int dir)
    {
        Vector2Int pos = start + dir;
        while (BoardManager.Instance.IsInBounds(pos))
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) break;
            if (tile.IsWall) break;

            if (tile.IsOccupied)
            {
                if (tile.OccupiedUnit is EnemyUnit)
                {
                    tile.SetHighlight(Tile.HighlightType.Attack);
                    previewTiles.Add(pos);
                }
                break;
            }

            tile.SetHighlight(Tile.HighlightType.Skill);
            previewTiles.Add(pos);
            pos += dir;
        }
    }

    /// <summary>Point 타입 스킬 미리보기 — 마우스 위치의 십자(+) 타일을 강조.</summary>
    private void UpdatePointPreview()
    {
        Vector2Int target = GetMouseGridPos();
        if (target == lastPreviewTarget) return;

        ClearSkillPreview();
        lastPreviewTarget = target;

        // 중심
        HighlightPointTile(target);

        // 4방향 인접 (강화 시 2칸 확장 미리보기)
        bool empowered = player is BlackWhitePlayerUnit bwP && bwP.WhiteEmpowered;
        int  armLen    = empowered ? 2 : 1;

        foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            for (int step = 1; step <= armLen; step++)
                HighlightPointTile(target + dir * step);
    }

    private void HighlightPointTile(Vector2Int pos)
    {
        var tile = BoardManager.Instance.GetTile(pos);
        if (tile == null || tile.IsWall) return;
        tile.SetHighlight(Tile.HighlightType.Skill);
        previewTiles.Add(pos);
    }

    private void UpdateTeleportPreview()
    {
        Vector2Int target = GetMouseGridPos();
        if (target == lastPreviewTarget) return;

        if (lastPreviewTarget.x != -999)
        {
            var prevTile = BoardManager.Instance.GetTile(lastPreviewTarget);
            if (prevTile != null)
            {
                prevTile.SetHighlight(teleportRangeTiles.Contains(lastPreviewTarget)
                    ? Tile.HighlightType.Move
                    : Tile.HighlightType.None);
            }
        }
        lastPreviewTarget = target;

        var tile2 = BoardManager.Instance.GetTile(target);
        if (tile2 == null) return;

        var skill = player.GetSkill(pendingSkillIndex);
        int dist  = Mathf.Abs(target.x - player.GridPos.x) + Mathf.Abs(target.y - player.GridPos.y);
        bool valid = IsValidTeleportTarget(skill, tile2, dist);

        tile2.SetHighlight(valid ? Tile.HighlightType.Selected : Tile.HighlightType.Attack);
    }

    private bool IsValidTeleportTarget(SkillBase skill, Tile tile, int dist)
    {
        if (tile == null) return false;
        if (skill is Skill_Teleport tp)
            return !tile.IsOccupied && dist > 0 && dist <= tp.teleportRange;
        if (skill is Skill_ElementPlace)
            return !tile.IsOccupied && dist > 0;
        return !tile.IsOccupied && dist > 0;
    }

    private void ShowTeleportRange(SkillBase skill)
    {
        ClearTeleportRange();

        int range = skill is Skill_Teleport tp ? tp.teleportRange : int.MaxValue;
        int bw    = BoardManager.Instance.Width;
        int bh    = BoardManager.Instance.Height;

        for (int x = -bw; x <= bw; x++)
        for (int y = -bh; y <= bh; y++)
        {
            int dist = Mathf.Abs(x) + Mathf.Abs(y);
            if (dist == 0 || dist > range) continue;

            Vector2Int pos = player.GridPos + new Vector2Int(x, y);
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null || tile.IsOccupied || tile.IsWall) continue;

            // 함정·하트 등 바닥 오브젝트가 있는 칸은 설치 불가 → 파란 강조 제외
            if (FloorObjectManager.Instance?.GetAt(pos) != null) continue;

            tile.SetHighlight(Tile.HighlightType.Move);
            teleportRangeTiles.Add(pos);
        }
    }

    private void ClearTeleportRange()
    {
        foreach (var pos in teleportRangeTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        teleportRangeTiles.Clear();
    }

    private void HandleSkillConfirmCancel()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        var skill = player.GetSkill(pendingSkillIndex);

        bool sameKeyQ  = pendingSkillIndex == 1 && kb.qKey.wasPressedThisFrame;
        bool sameKeyE  = pendingSkillIndex == 2 && kb.eKey.wasPressedThisFrame;
        bool leftClick = mouse != null && mouse.leftButton.wasPressedThisFrame;

        // ── 같은 키 재입력 ────────────────────────────────────────────────
        if (sameKeyQ || sameKeyE)
        {
            // SameKeyConfirms=false (원소포설 등 토글 방식) → 재입력 시 취소
            if (skill != null && !skill.SameKeyConfirms)
                CancelSkill();
            else
                ConfirmSkill();
            return;
        }

        // ── 좌클릭 확정 ──────────────────────────────────────────────────
        if (leftClick)
        {
            ConfirmSkill();
            return;
        }

        // ── 다른 스킬 키 → 취소 후 해당 스킬 진입 ────────────────────────
        bool ePressed = kb.eKey.wasPressedThisFrame;
        bool qPressed = kb.qKey.wasPressedThisFrame;
        bool otherSkillKey = (pendingSkillIndex == 1 && ePressed)
                          || (pendingSkillIndex == 2 && qPressed);
        if (otherSkillKey)
        {
            int otherIndex = pendingSkillIndex == 1 ? 2 : 1;
            CancelSkill(silent: true);
            TryEnterSkillMode(otherIndex);
            return;
        }

        // ── 이동 / 스킵 / 우클릭 → 취소 ─────────────────────────────────
        bool moveKey    = kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame
                       || kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame
                       || kb.upArrowKey.wasPressedThisFrame  || kb.downArrowKey.wasPressedThisFrame
                       || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        bool spaceKey   = kb.spaceKey.wasPressedThisFrame;
        bool rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame;

        if (moveKey || spaceKey || rightClick)
        {
            CancelSkill(silent: moveKey || spaceKey); // 행동 전환 시 "취소" 메시지 생략
            if (moveKey)  HandleMove();
            if (spaceKey) turnManager.SkipTurn();
        }
    }

    private void ConfirmSkill()
    {
        Vector2Int targetPos = GetMouseGridPos();
        int savedIndex = pendingSkillIndex;

        ClearSkillPreview();
        state             = InputState.Normal;
        pendingSkillIndex = 0;

        var skill = player.GetSkill(savedIndex);
        if (player.TryUseSkill(savedIndex, targetPos))
        {
            if (skill == null || !skill.IsFree)
                turnManager.OnPlayerActed();
            else
                GameUI.Instance?.Refresh();
        }
        else
        {
            GameUI.Instance?.ShowNotify("스킬 사용 불가", 0.7f);
        }
    }

    private void CancelSkill(bool silent = false)
    {
        ClearSkillPreview();
        state             = InputState.Normal;
        pendingSkillIndex = 0;
        if (!silent)
            GameUI.Instance?.ShowNotify("스킬 취소", 0.5f);
    }

    private void ClearSkillPreview()
    {
        foreach (var pos in previewTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        previewTiles.Clear();

        ClearTeleportRange();

        if (lastPreviewTarget.x != -999)
            BoardManager.Instance.GetTile(lastPreviewTarget)?.SetHighlight(Tile.HighlightType.None);

        lastPreviewDir    = Vector2Int.zero;
        lastPreviewTarget = new Vector2Int(-999, -999);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 공통 유틸
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>백마법 모드: 플레이어 타일 제외 맵 전체 비벽 타일 하이라이트.</summary>
    private void BuildWhiteModeFullMapPreview()
    {
        int w = BoardManager.Instance.Width;
        int h = BoardManager.Instance.Height;
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var pos = new Vector2Int(x, y);
            if (pos == player.GridPos) continue;
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null || tile.IsWall) continue;
            bool isEnemy = tile.IsOccupied && tile.OccupiedUnit is EnemyUnit;
            tile.SetHighlight(isEnemy ? Tile.HighlightType.Attack : Tile.HighlightType.Skill);
            attackPreviewTiles.Add(pos);
        }
    }

    private void BuildAttackPreview(Vector2Int dir)
    {
        int maxSteps = player.AttackReach > 0 ? player.AttackReach : int.MaxValue;
        int steps    = 0;
        Vector2Int pos = player.GridPos + dir;

        while (BoardManager.Instance.IsInBounds(pos) && steps < maxSteps)
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) break;

            // 벽 타일 — 공격 불가, 그 너머 하이라이트 없음
            if (tile.IsWall) break;

            if (tile.IsOccupied)
            {
                if (tile.OccupiedUnit is EnemyUnit)
                {
                    tile.SetHighlight(Tile.HighlightType.Attack);
                    attackPreviewTiles.Add(pos);
                }
                break;
            }

            tile.SetHighlight(Tile.HighlightType.Skill);
            attackPreviewTiles.Add(pos);
            steps++;
            pos += dir;
        }
    }

    private void ClearAttackPreview()
    {
        foreach (var pos in attackPreviewTiles)
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) continue;
            // Danger 타일은 공격 미리보기 해제 시에도 건드리지 않음
            if (tile.CurrentHighlight == Tile.HighlightType.Danger) continue;
            tile.SetHighlight(moveTiles.Contains(pos)
                ? Tile.HighlightType.Move
                : Tile.HighlightType.None);
        }
        attackPreviewTiles.Clear();
        // 전체맵 sentinel도 초기화 → 다음 프레임에 백모드 미리보기 재빌드 허용
        lastAttackPreviewDir = Vector2Int.zero;
    }

    private void ClearHoverHighlight()
    {
        BoardManager.Instance.GetTile(lastHover)?.SetHighlight(Tile.HighlightType.None);
        lastHover = new Vector2Int(-1, -1);
    }

    private Vector2Int GetMouseGridPos()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        world.z = 0f;
        return BoardManager.Instance.WorldToGrid(world);
    }
}
