using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 1행동 = 1턴
/// 이동(WASD/화살표) / 공격(좌클릭) / 스킬(Q·E) / 턴 스킵(Space)
///
/// [공격]
///  좌클릭 → 클릭 방향을 4방향으로 스냅 → 직선 투사체 발사
///  적이 없어도 발사하고 턴 종료
///
/// [스킬 E — 공격형]
///  마우스 방향을 4방향으로 스냅 → 직선 발사 (Skill 내부에서 GridUtil 사용)
///
/// [스킬 Q/E — 순간이동]
///  빈 칸 클릭 필요
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    private PlayerUnit  player;
    private TurnManager turnManager;

    public void Init(PlayerUnit p)
    {
        player      = p;
        turnManager = TurnManager.Instance;
    }

    private void Update()
    {
        if (GameManager.Instance == null || player == null) return;
        if (turnManager.CurrentPhase  != TurnManager.TurnPhase.PlayerTurn) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.PlayerTurn) return;

        UpdateHoverHighlight();

        if (player.HasActedThisTurn) return;

        HandleMove();
        HandleClickAction();
        HandleSkillInput();
        HandleSkip();
    }

    // ── 호버 하이라이트 ───────────────────────────────────────────────────
    private Vector2Int lastHover = new Vector2Int(-1, -1);
    private void UpdateHoverHighlight()
    {
        Vector2Int hover = GetMouseGridPos();
        if (hover == lastHover) return;

        BoardManager.Instance.GetTile(lastHover)?.SetHighlight(Tile.HighlightType.None);

        Tile cur = BoardManager.Instance.GetTile(hover);
        if (cur != null)
        {
            bool cheatTp = CheatManager.Instance != null && CheatManager.Instance.TeleportMode;
            if (cheatTp && !cur.IsOccupied)
                cur.SetHighlight(Tile.HighlightType.Selected);
            else
                cur.SetHighlight(cur.IsOccupied ? Tile.HighlightType.Attack : Tile.HighlightType.Move);
        }

        lastHover = hover;
    }

    // ── 이동 ──────────────────────────────────────────────────────────────
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

    // ── 좌클릭 — 공격 OR 치트 순간이동 ───────────────────────────────────
    private void HandleClickAction()
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

        // 일반 공격 — 방향 스냅 후 직선 발사 (빈 칸 클릭도 OK)
        if (player.TryAttackToward(targetPos))
            turnManager.OnPlayerActed();
    }

    // ── 스킬 ──────────────────────────────────────────────────────────────
    private void HandleSkillInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        int skillIndex = 0;
        if (kb.qKey.wasPressedThisFrame) skillIndex = 1;
        if (kb.eKey.wasPressedThisFrame) skillIndex = 2;
        if (skillIndex == 0) return;

        Vector2Int targetPos = GetMouseGridPos();
        var skill = player.GetSkill(skillIndex);

        // 순간이동은 빈 칸 필수
        if (skill is Skill_Teleport)
        {
            var tile = BoardManager.Instance.GetTile(targetPos);
            if (tile == null || tile.IsOccupied)
            {
                GameUI.Instance?.ShowNotify("빈 칸을 선택하세요", 0.7f);
                return;
            }
        }
        // 공격형 스킬은 방향 스냅을 Skill 내부에서 처리 — 별도 검증 불필요

        if (player.TryUseSkill(skillIndex, targetPos))
            turnManager.OnPlayerActed();
    }

    // ── 턴 스킵 ───────────────────────────────────────────────────────────
    private void HandleSkip()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
            turnManager.SkipTurn();
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
