using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { PlayerTurn, EnemyTurn, MapClear, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("References")]
    public BoardManager boardManager;
    public TurnManager  turnManager;

    private bool isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// startingStage: 1~3, 해당 스테이지부터 시작
    /// </summary>
    public void StartGame(int startingStage = 1)
    {
        isGameOver = false;
        StageManager.Instance?.InitStage(startingStage);
        ChangeState(GameState.PlayerTurn);
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
        isGameOver = true;
        Debug.Log("[GameManager] 게임오버");
        ResultScreen.Instance?.Show(ResultScreen.ResultType.GameOver);
    }
}
