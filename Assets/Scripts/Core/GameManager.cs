using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Idle = 게임 시작 전 (타이틀/스테이지 선택 화면) — 입력 완전 차단
    public enum GameState { Idle, PlayerTurn, EnemyTurn, MapClear, GameOver, Paused }
    public GameState CurrentState    { get; private set; } = GameState.Idle;
    private GameState stateBeforePause;

    [Header("References")]
    public BoardManager boardManager;
    public TurnManager  turnManager;

    /// <summary>플레이어를 처치한 적 이름 (게임오버 씬 선택에 사용)</summary>
    public static string LastKillerName = "";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentState = GameState.Idle; // 타이틀 화면 중 입력 차단
    }

    /// <summary>startingStage: 1~3, 해당 스테이지부터 시작</summary>
    public void StartGame(int startingStage = 1)
    {
        LastKillerName = "";
        TurnManager.Instance?.Reset();        // 진행 중인 턴 처리 초기화
        StageManager.Instance?.InitStage(startingStage);
        ChangeState(GameState.PlayerTurn);
    }

    // ── 일시정지 / 재개 ───────────────────────────────────────────────────
    public void Pause()
    {
        if (CurrentState == GameState.Paused || CurrentState == GameState.Idle) return;
        stateBeforePause = CurrentState;
        CurrentState = GameState.Paused;
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused) return;
        CurrentState = stateBeforePause;
        GameUI.Instance?.Refresh();
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        if (newState == GameState.PlayerTurn)
            turnManager.StartPlayerTurn();
        else if (newState == GameState.GameOver)
            OnGameOver();
    }

    public void OnPlayerDead() => ChangeState(GameState.GameOver);

    private void OnGameOver()
    {
        int stage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;
        Debug.Log($"[GameManager] 게임오버 — Stage{stage}, Killer={LastKillerName}");

        // 패배 기록 해금 (갤러리용)
        if (!string.IsNullOrEmpty(LastKillerName))
            ProgressManager.UnlockDefeat(stage, LastKillerName);

        SpecialSceneController.Instance?.ShowGameOver(stage, LastKillerName);
    }
}
