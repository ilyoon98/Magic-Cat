using UnityEngine;
using System.Collections;

/// <summary>
/// 턴 구조
///
/// [플레이어 턴]
///   이동 / 평타 / 스킬 중 최대 2행동 선택
///   · 2행동 소진 → 자동으로 적 턴으로 전환
///   · 1행동 후 Space → 수동으로 적 턴으로 전환
///
/// [AI 턴]
///   각 적이 아래 우선순위로 1가지만 수행 후 턴 종료
///     1. 공격 준비 완료 → 공격 실행
///     2. 플레이어가 사거리 안 → 공격 준비 (이동 없음)
///     3. 플레이어가 사거리 밖 → 1칸 이동
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public enum TurnPhase { PlayerTurn, EnemyTurn }
    public TurnPhase CurrentPhase { get; private set; }
    public int TurnCount { get; private set; }

    private PlayerUnit player;
    private bool isProcessing; // 적 처리 중 중복 입력 방지

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetPlayer(PlayerUnit p) { player = p; }
    public PlayerUnit GetPlayer() => player;

    /// <summary>게임 재시작 시 처리 중 상태와 턴 수를 초기화</summary>
    public void Reset()
    {
        StopAllCoroutines();
        isProcessing = false;
        CurrentPhase = TurnPhase.PlayerTurn;
        TurnCount    = 0;
    }

    private void TickAllStatusEffects()
    {
        player?.GetComponent<StatusEffectHandler>()?.TickEffects();

        var snapshot = new System.Collections.Generic.List<EnemyUnit>(
            EnemyManager.Instance.GetActiveEnemies());
        foreach (var enemy in snapshot)
            enemy?.GetComponent<StatusEffectHandler>()?.TickEffects();
    }

    // ── 플레이어 턴 시작 ──────────────────────────────────────────────────
    public void StartPlayerTurn()
    {
        if (player == null) return;
        TurnCount++;
        CurrentPhase = TurnPhase.PlayerTurn;
        isProcessing = false;
        player.StartTurn();
        FloorObjectManager.Instance?.OnTurnStart();
        GameUI.Instance?.Refresh();
        player.GetComponent<PlayerInputController>()?.RefreshMoveHighlight();
    }

    // ── 플레이어 행동 1개 완료 시 호출 ───────────────────────────────────
    /// <summary>
    /// 행동 후 ActionsUsed &gt;= 2이면 자동으로 적 턴 시작.
    /// 1행동만 했으면 대기 (Space로 수동 종료 가능).
    /// </summary>
    public void OnPlayerActed()
    {
        if (isProcessing) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.PlayerTurn) return;

        GameUI.Instance?.Refresh();

        // 2행동 소진 → 자동 턴 종료
        if (player != null && player.ActionsUsed >= 2)
            EndPlayerTurn();
        // else: 1행동만 완료 — 두 번째 행동 대기
    }

    // ── 플레이어 턴 종료 (2행동 소진 or 수동 스킵) ───────────────────────
    private void EndPlayerTurn()
    {
        if (isProcessing) return;
        isProcessing = true;
        player?.GetComponent<PlayerInputController>()?.RefreshMoveHighlight();
        GameUI.Instance?.Refresh();
        StartCoroutine(RunEnemyTurn());
    }

    // ── 수동 턴 종료 (Space) — 행동 수와 무관하게 즉시 적 턴 ─────────────
    public void SkipTurn()
    {
        if (CurrentPhase != TurnPhase.PlayerTurn || isProcessing) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.PlayerTurn) return;
        EndPlayerTurn();
    }

    // ── 적 턴 처리 ────────────────────────────────────────────────────────
    private IEnumerator RunEnemyTurn()
    {
        CurrentPhase = TurnPhase.EnemyTurn;
        GameUI.Instance?.Refresh();
        player?.GetComponent<PlayerInputController>()?.RefreshMoveHighlight();

        yield return new WaitForSeconds(0.15f);

        TickAllStatusEffects();
        ClearAllEnemyWarnings();

        EnemyManager.Instance.ExecuteAllEnemyTurns(player);
        yield return new WaitForSeconds(0.25f);

        if (!player.IsAlive)
        {
            GameManager.Instance.OnPlayerDead();
            yield break;
        }

        var gs = GameManager.Instance.CurrentState;
        if (gs == GameManager.GameState.MapClear ||
            gs == GameManager.GameState.GameOver  ||
            gs == GameManager.GameState.Idle)
        {
            yield break;
        }

        RefreshAllEnemyWarnings();
        GameManager.Instance.ChangeState(GameManager.GameState.PlayerTurn);
    }

    private void ClearAllEnemyWarnings()
    {
        foreach (var enemy in EnemyManager.Instance.GetActiveEnemies())
            enemy?.ClearWarning();
    }

    private void RefreshAllEnemyWarnings()
    {
        foreach (var enemy in EnemyManager.Instance.GetActiveEnemies())
            enemy?.RefreshWarning();
    }
}
