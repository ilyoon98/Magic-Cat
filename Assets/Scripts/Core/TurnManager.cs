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
        TurnCount++;
        CurrentPhase = TurnPhase.PlayerTurn;
        isProcessing = false;
        player.StartTurn();
        GameUI.Instance?.Refresh();
    }

    // ── 플레이어가 행동을 완료하면 즉시 호출 ──────────────────────────────
    public void OnPlayerActed()
    {
        if (isProcessing) return;
        isProcessing = true;
        GameUI.Instance?.Refresh();
        StartCoroutine(RunEnemyTurn());
    }

    // ── 턴 스킵 (아무것도 안 하고 적 턴으로) ─────────────────────────────
    public void SkipTurn()
    {
        if (CurrentPhase != TurnPhase.PlayerTurn || isProcessing) return;
        isProcessing = true;
        GameUI.Instance?.Refresh();
        StartCoroutine(RunEnemyTurn());
    }

    // ── 적 턴 처리 ────────────────────────────────────────────────────────
    private IEnumerator RunEnemyTurn()
    {
        CurrentPhase = TurnPhase.EnemyTurn;
        GameUI.Instance?.Refresh();

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
