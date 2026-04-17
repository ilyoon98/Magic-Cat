using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 1행동 = 1턴
///
/// [이동]  WASD / 화살표
/// [공격]  좌클릭 → 마우스 방향으로 경로 미리보기 (호버) → 클릭 확정
/// [스킬]  Q/E 누르면 미리보기 진입
///           · 방향 스킬: 마우스 방향으로 경로 표시 (벽/적에서 막힘)
///           · 순간이동:  마우스 위치 타일 표시
///         같은 키 재입력 또는 좌클릭 → 확정 발사
///         다른 키 / ESC / 우클릭 → 취소
/// [턴 스킵] Space
/// [메뉴]  ESC (스킬 미리보기 중이면 취소 우선)
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    // 다른 시스템이 "스킬 대기 중" 상태를 알 수 있게 공개
    public static bool IsSkillPending => _instance != null && _instance.state == InputState.SkillPending;
    private static PlayerInputController _instance;

    private PlayerUnit  player;
    private TurnManager turnManager;

    // ── 입력 상태 ─────────────────────────────────────────────────────────
    private enum InputState { Normal, SkillPending }
    private InputState state = InputState.Normal;
    private int        pendingSkillIndex = 0;   // 1=Q, 2=E

    // ── 스킬 미리보기용 타일 목록 ─────────────────────────────────────────
    private readonly List<Vector2Int> previewTiles = new List<Vector2Int>();
    private Vector2Int lastPreviewDir = Vector2Int.zero;
    private Vector2Int lastPreviewTarget = new Vector2Int(-999, -999);

    // ── 텔레포트 범위 타일 (파란색 고정 표시) ────────────────────────────
    private readonly List<Vector2Int> teleportRangeTiles = new List<Vector2Int>();

    // ── 공격 미리보기용 타일 목록 ─────────────────────────────────────────
    private readonly List<Vector2Int> attackPreviewTiles = new List<Vector2Int>();
    private Vector2Int lastAttackPreviewDir = Vector2Int.zero;

    // ── 이동 가능 타일 하이라이트 ─────────────────────────────────────────
    private readonly List<Vector2Int> moveTiles = new List<Vector2Int>();

    // ── 일반 호버 ─────────────────────────────────────────────────────────
    private Vector2Int lastHover = new Vector2Int(-1, -1);

    public void Init(PlayerUnit p)
    {
        _instance   = this;
        player      = p;
        turnManager = TurnManager.Instance;
    }

    // ── 이동 가능 타일 하이라이트 공개 API ───────────────────────────────
    public void RefreshMoveHighlight()
    {
        // 공격 미리보기 항상 초기화 (턴 전환·행동 완료 후 잔상 방지)
        ClearAttackPreview();
        lastAttackPreviewDir = Vector2Int.zero;
        ClearMoveHighlight();
        if (player == null || player.HasActedThisTurn) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.PlayerTurn) return;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in dirs)
        {
            Vector2Int pos  = player.GridPos + dir;
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile != null && !tile.IsOccupied)
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
            // 적 경고 타일은 건드리지 않음 (Danger는 EnemyUnit이 관리)
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile != null) tile.SetHighlight(Tile.HighlightType.None);
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

        // ESC는 게임 상태와 무관하게 항상 최우선 처리
        // (메뉴가 열려 Paused 상태여도 ESC로 닫을 수 있어야 함)
        HandleEsc();

        if (GameManager.Instance.CurrentState != GameManager.GameState.PlayerTurn) return;
        if (turnManager.CurrentPhase != TurnManager.TurnPhase.PlayerTurn) return;

        if (state == InputState.SkillPending)
        {
            UpdateSkillPreview();
            HandleSkillConfirmCancel();
            return;
        }

        // ── Normal 상태 ───────────────────────────────────────────────────
        UpdateHoverHighlight();
        RefreshMoveHighlight();   // 이동 가능 타일 항상 갱신 (행동 후/전 자동 반영)

        if (player.HasActedThisTurn)
        {
            ClearAttackPreview(); // 행동 완료 시 공격 미리보기 제거
            return;
        }

        HandleMove();
        HandleClickAttack();
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

        // 타이틀/갤러리/설정/스테이지선택 화면, 또는 씬 전환 중이면 처리 생략
        if (GameManager.Instance.CurrentState == GameManager.GameState.Idle) return;
        if (SpecialSceneController.Instance != null && SpecialSceneController.Instance.IsTransitioning) return;

        if (state == InputState.SkillPending)
        {
            CancelSkill();
        }
        else
        {
            // 인게임 메뉴 토글
            if (GameUI.Instance != null)
            {
                if (GameUI.IsMenuOpen) GameUI.Instance.CloseMenu();
                else                   GameUI.Instance.OpenMenu();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Normal 상태 핸들러
    // ═══════════════════════════════════════════════════════════════════════

    private void UpdateHoverHighlight()
    {
        Vector2Int hover = GetMouseGridPos();

        // ── 공격 미리보기 업데이트 (호버 변화와 무관하게 매 프레임 체크) ──
        // 행동 완료 후 마우스가 움직이지 않아도 미리보기를 지워야 하므로
        bool isMoveTile = moveTiles.Contains(hover);
        bool canShowAtk = !player.HasActedThisTurn && !isMoveTile;

        if (canShowAtk)
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

        if (hover == lastHover) return; // 호버 타일이 바뀌지 않았으면 타일 색 갱신 불필요

        // ── 이전 호버 타일 복원 ───────────────────────────────────────────
        var prevTile = BoardManager.Instance.GetTile(lastHover);
        if (prevTile != null)
        {
            if (attackPreviewTiles.Contains(lastHover))
            {
                // 공격 미리보기 타일: 해당 타일의 원래 미리보기 색 유지
                bool isEnemy = prevTile.IsOccupied && prevTile.OccupiedUnit is EnemyUnit;
                prevTile.SetHighlight(isEnemy ? Tile.HighlightType.Attack : Tile.HighlightType.Skill);
            }
            else
            {
                prevTile.SetHighlight(moveTiles.Contains(lastHover)
                    ? Tile.HighlightType.Move
                    : Tile.HighlightType.None);
            }
        }

        // ── 현재 호버 타일 강조 ───────────────────────────────────────────
        Tile cur = BoardManager.Instance.GetTile(hover);
        if (cur != null)
        {
            bool cheatTp = CheatManager.Instance != null && CheatManager.Instance.TeleportMode;
            if (cheatTp && !cur.IsOccupied)
                cur.SetHighlight(Tile.HighlightType.Selected);
            else if (isMoveTile)
                cur.SetHighlight(Tile.HighlightType.Move);
            else if (attackPreviewTiles.Contains(hover))
            {
                // 공격 미리보기가 이미 이 타일을 설정함 — 호버 강조만 추가 (마지막 타일 강조)
                bool isEnemy = cur.IsOccupied && cur.OccupiedUnit is EnemyUnit;
                cur.SetHighlight(isEnemy ? Tile.HighlightType.Attack : Tile.HighlightType.Skill);
            }
            else
            {
                // 공격 범위 밖 또는 행동 완료 후
                cur.SetHighlight(cur.IsOccupied ? Tile.HighlightType.Attack : Tile.HighlightType.None);
            }
        }

        lastHover = hover;
    }

    // ── 공격 미리보기 경로 생성 ───────────────────────────────────────────
    private void BuildAttackPreview(Vector2Int dir)
    {
        Vector2Int pos = player.GridPos + dir;
        while (BoardManager.Instance.IsInBounds(pos))
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) break;

            if (tile.IsOccupied)
            {
                // 적에서 경로 끝 (적 타일 빨간색)
                if (tile.OccupiedUnit is EnemyUnit)
                {
                    tile.SetHighlight(Tile.HighlightType.Attack);
                    attackPreviewTiles.Add(pos);
                }
                // 벽/플레이어면 표시 없이 종료
                break;
            }

            // 빈 타일: 스킬 색(청록)으로 경로 표시
            tile.SetHighlight(Tile.HighlightType.Skill);
            attackPreviewTiles.Add(pos);
            pos += dir;
        }
    }

    // ── 공격 미리보기 타일 초기화 ─────────────────────────────────────────
    private void ClearAttackPreview()
    {
        foreach (var pos in attackPreviewTiles)
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) continue;
            // moveTiles는 이미 별도로 관리되므로 Move 색으로 복원
            tile.SetHighlight(moveTiles.Contains(pos)
                ? Tile.HighlightType.Move
                : Tile.HighlightType.None);
        }
        attackPreviewTiles.Clear();
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
        if (player.TryMove(dir)) turnManager.OnPlayerActed();
    }

    private void HandleClickAttack()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Vector2Int targetPos = GetMouseGridPos();
        Tile tile = BoardManager.Instance.GetTile(targetPos);
        if (tile == null) return;

        // 치트 순간이동 모드
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

        // 이동 가능 타일 클릭 → 이동 (공격 미리보기 타일이 아닌 빈 이웃 타일)
        if (moveTiles.Contains(targetPos))
        {
            Vector2Int moveDir = targetPos - player.GridPos;
            if (player.TryMove(moveDir)) turnManager.OnPlayerActed();
            return;
        }

        if (player.TryAttackToward(targetPos))
            turnManager.OnPlayerActed();
    }

    private void HandleSkillKeyPress()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        int skillIndex = 0;
        if (kb.qKey.wasPressedThisFrame) skillIndex = 1;
        if (kb.eKey.wasPressedThisFrame) skillIndex = 2;
        if (skillIndex == 0) return;

        var skill = player.GetSkill(skillIndex);
        if (skill == null) return;

        if (!skill.CanUse())
        {
            GameUI.Instance?.ShowNotify("스킬 쿨타임 중", 0.7f);
            return;
        }

        // 미리보기가 필요 없는 즉시 발동 스킬
        if (skill.PreviewType == SkillBase.SkillPreviewType.None)
        {
            if (player.TryUseSkill(skillIndex, GetMouseGridPos()))
                turnManager.OnPlayerActed();
            return;
        }

        // 미리보기 모드 진입
        ClearAttackPreview();
        ClearHoverHighlight();
        ClearMoveHighlight();    // 스킬 대기 중에는 이동 하이라이트 숨김
        state             = InputState.SkillPending;
        pendingSkillIndex = skillIndex;
        lastPreviewDir    = Vector2Int.zero;
        lastPreviewTarget = new Vector2Int(-999, -999);

        // 텔레포트 스킬이면 이동 가능 범위 타일을 미리 파란색으로 표시
        if (skill.PreviewType == SkillBase.SkillPreviewType.Teleport)
            ShowTeleportRange(skill as Skill_Teleport);

        GameUI.Instance?.ShowNotify($"[{(skillIndex == 1 ? "Q" : "E")}] {skill.skillName} — 클릭 or 같은키 확정 / 다른키 취소", 3f);
    }

    private void HandleSkip()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            turnManager.SkipTurn();
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
    }

    // ── 방향 스킬 미리보기 ────────────────────────────────────────────────
    private void UpdateDirectionalPreview()
    {
        Vector2Int mousePos = GetMouseGridPos();
        Vector2Int delta    = mousePos - player.GridPos;
        if (delta == Vector2Int.zero) return;

        Vector2Int dir = GridUtil.SnapToCardinal(delta);
        if (dir == lastPreviewDir) return; // 방향 변화 없으면 재계산 불필요
        lastPreviewDir = dir;

        ClearSkillPreview();

        // 플레이어 위치에서 dir 방향으로 타일을 하나씩 확인
        Vector2Int pos = player.GridPos + dir;
        while (BoardManager.Instance.IsInBounds(pos))
        {
            var tile = BoardManager.Instance.GetTile(pos);
            if (tile == null) break;

            if (tile.IsOccupied)
            {
                // 적이면 경로 끝 타일을 빨간색으로 표시 후 중단
                if (tile.OccupiedUnit is EnemyUnit)
                {
                    tile.SetHighlight(Tile.HighlightType.Attack);
                    previewTiles.Add(pos);
                }
                // 벽(플레이어 or 장애물)이면 표시 없이 중단
                break;
            }

            // 빈 타일: 경로 색으로 표시
            tile.SetHighlight(Tile.HighlightType.Skill);
            previewTiles.Add(pos);
            pos += dir;
        }
    }

    // ── 순간이동 미리보기 ─────────────────────────────────────────────────
    private void UpdateTeleportPreview()
    {
        Vector2Int target = GetMouseGridPos();
        if (target == lastPreviewTarget) return;

        // 이전 호버 타일 복원 (범위 타일이면 파란색, 아니면 None)
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

        var tile = BoardManager.Instance.GetTile(target);
        if (tile == null) return;

        var tp = player.GetSkill(pendingSkillIndex) as Skill_Teleport;
        int dist = Mathf.Abs(target.x - player.GridPos.x) + Mathf.Abs(target.y - player.GridPos.y);
        bool valid = !tile.IsOccupied && tp != null && dist > 0 && dist <= tp.teleportRange;

        // 호버 강조: 유효 타일이면 Selected(노란), 무효면 Attack(빨간)
        tile.SetHighlight(valid ? Tile.HighlightType.Selected : Tile.HighlightType.Attack);
    }

    // ── 텔레포트 범위 파란색 표시 ────────────────────────────────────────
    private void ShowTeleportRange(Skill_Teleport tp)
    {
        ClearTeleportRange();
        if (tp == null) return;

        for (int x = -tp.teleportRange; x <= tp.teleportRange; x++)
        {
            for (int y = -tp.teleportRange; y <= tp.teleportRange; y++)
            {
                int dist = Mathf.Abs(x) + Mathf.Abs(y);
                if (dist == 0 || dist > tp.teleportRange) continue;

                Vector2Int pos  = player.GridPos + new Vector2Int(x, y);
                var tile = BoardManager.Instance.GetTile(pos);
                if (tile == null || tile.IsOccupied) continue;

                tile.SetHighlight(Tile.HighlightType.Move);
                teleportRangeTiles.Add(pos);
            }
        }
    }

    private void ClearTeleportRange()
    {
        foreach (var pos in teleportRangeTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        teleportRangeTiles.Clear();
    }

    // ── 확정 / 취소 ───────────────────────────────────────────────────────
    private void HandleSkillConfirmCancel()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        // 확정: 같은 키 재입력 또는 좌클릭
        bool sameKeyQ   = pendingSkillIndex == 1 && kb.qKey.wasPressedThisFrame;
        bool sameKeyE   = pendingSkillIndex == 2 && kb.eKey.wasPressedThisFrame;
        bool leftClick  = mouse != null && mouse.leftButton.wasPressedThisFrame;

        if (sameKeyQ || sameKeyE || leftClick)
        {
            ConfirmSkill();
            return;
        }

        // 취소: 다른 스킬 키, 이동 키, Space, 우클릭
        bool otherSkill = (pendingSkillIndex == 1 && kb.eKey.wasPressedThisFrame)
                       || (pendingSkillIndex == 2 && kb.qKey.wasPressedThisFrame);
        bool moveKey    = kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame
                       || kb.aKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame
                       || kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame
                       || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        bool spaceKey   = kb.spaceKey.wasPressedThisFrame;
        bool rightClick = mouse != null && mouse.rightButton.wasPressedThisFrame;

        if (otherSkill || moveKey || spaceKey || rightClick)
        {
            CancelSkill();

            // 취소 후 이동 키나 Space는 원래 동작 실행
            if (moveKey)  HandleMove();
            if (spaceKey) turnManager.SkipTurn();
        }
    }

    private void ConfirmSkill()
    {
        Vector2Int targetPos = GetMouseGridPos();
        ClearSkillPreview();
        state = InputState.Normal;

        if (player.TryUseSkill(pendingSkillIndex, targetPos))
        {
            turnManager.OnPlayerActed();
        }
        else
        {
            GameUI.Instance?.ShowNotify("스킬 사용 불가", 0.7f);
        }
        pendingSkillIndex = 0;
    }

    private void CancelSkill()
    {
        ClearSkillPreview();
        state             = InputState.Normal;
        pendingSkillIndex = 0;
        GameUI.Instance?.ShowNotify("스킬 취소", 0.5f);
    }

    // ── 미리보기 타일 초기화 ──────────────────────────────────────────────
    private void ClearSkillPreview()
    {
        foreach (var pos in previewTiles)
            BoardManager.Instance.GetTile(pos)?.SetHighlight(Tile.HighlightType.None);
        previewTiles.Clear();

        // 텔레포트 범위 타일도 함께 초기화
        ClearTeleportRange();

        // 현재 호버 강조도 초기화
        if (lastPreviewTarget.x != -999)
        {
            BoardManager.Instance.GetTile(lastPreviewTarget)?.SetHighlight(Tile.HighlightType.None);
        }

        lastPreviewDir    = Vector2Int.zero;
        lastPreviewTarget = new Vector2Int(-999, -999);
    }

    private void ClearHoverHighlight()
    {
        BoardManager.Instance.GetTile(lastHover)?.SetHighlight(Tile.HighlightType.None);
        lastHover = new Vector2Int(-1, -1);
    }

    // ── 마우스 → 그리드 좌표 ─────────────────────────────────────────────
    private Vector2Int GetMouseGridPos()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        world.z = 0f;
        return BoardManager.Instance.WorldToGrid(world);
    }
}
