using UnityEngine;
using System.Collections;

/// <summary>
/// 1행동 = 1턴 시스템 (Crypt of the NecroDancer 방식)
/// 플레이어 1개 행동 → 즉시 적 행동 → 플레이어 턴
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
        StopAllCoroutines(); // 진행 중인 적 턴 코루틴 강제 중단
        isProcessing = false;
        CurrentPhase = TurnPhase.PlayerTurn;
        TurnCount    = 0;
    }

    private void TickAllStatusEffects()
    {
        // 플레이어
        player?.GetComponent<StatusEffectHandler>()?.TickEffects();

        // 적 전체 — 복사본으로 순회 (틱 중 적 사망 시 리스트 변경 방지)
        var snapshot = new System.Collections.Generic.List<EnemyUnit>(
            EnemyManager.Instance.GetActiveEnemies());
        foreach (var enemy in snapshot)
            enemy?.GetComponent<StatusEffectHandler>()?.TickEffects();
    }

    // ── 플레이어 턴 시작 ──────────────────────────────────────────────────
    public void StartPlayerTurn()
    {
        if (player == null) return; // 아직 플레이어가 배치되지 않은 경우 방어
        TurnCount++;
        CurrentPhase = TurnPhase.PlayerTurn;
        isProcessing = false;
        player.StartTurn();
        // 바닥 오브젝트 쿨다운 갱신 (함정 재활성화 포함)
        FloorObjectManager.Instance?.OnTurnStart();
        GameUI.Instance?.Refresh();
        // 이동 가능 타일 하이라이트 갱신 (턴 시작 시 즉시 표시)
        player.GetComponent<PlayerInputController>()?.RefreshMoveHighlight();
    }

    // ── 플레이어가 행동을 완료하면 즉시 호출 ──────────────────────────────
    public void OnPlayerActed()
    {
        if (isProcessing) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.PlayerTurn) return;
        isProcessing = true;
        GameUI.Instance?.Refresh();
        StartCoroutine(RunEnemyTurn());
    }

    // ── 턴 스킵 (아무것도 안 하고 적 턴으로) ─────────────────────────────
    public void SkipTurn()
    {
        if (CurrentPhase != TurnPhase.PlayerTurn || isProcessing) return;
        if (GameManager.Instance?.CurrentState != GameManager.GameState.PlayerTurn) return;
        isProcessing = true;
        GameUI.Instance?.Refresh();
        StartCoroutine(RunEnemyTurn());
    }

    // ── 적 턴 처리 ────────────────────────────────────────────────────────
    private IEnumerator RunEnemyTurn()
    {
        CurrentPhase = TurnPhase.EnemyTurn;
        GameUI.Instance?.Refresh();
        // 적 턴 시작 시 이동 하이라이트 제거
        player?.GetComponent<PlayerInputController>()?.RefreshMoveHighlight();

        yield return new WaitForSeconds(0.15f);

        // 상태이상 틱 (플레이어 + 적 모두)
        TickAllStatusEffects();

        // 적 행동 전 이전 경고 클리어 (행동 중에 ClearWarning/RefreshWarning 수행)
        ClearAllEnemyWarnings();

        EnemyManager.Instance.ExecuteAllEnemyTurns(player);
        yield return new WaitForSeconds(0.25f);

        if (!player.IsAlive)
        {
            GameManager.Instance.OnPlayerDead();
            yield break;
        }

        // 맵 클리어·게임오버가 이미 발동됐으면 PlayerTurn으로 되돌리지 않음
        var gs = GameManager.Instance.CurrentState;
        if (gs == GameManager.GameState.MapClear ||
            gs == GameManager.GameState.GameOver  ||
            gs == GameManager.GameState.Idle)
        {
            yield break;
        }

        // 적 행동 후 새 경고 표시 (플레이어 턴 동안 유지됨)
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
