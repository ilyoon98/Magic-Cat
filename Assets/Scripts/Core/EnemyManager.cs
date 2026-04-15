using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private List<EnemyUnit> activeEnemies = new List<EnemyUnit>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterEnemy(EnemyUnit enemy)
    {
        if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }

    public void OnEnemyDefeated(EnemyUnit enemy)
    {
        activeEnemies.Remove(enemy);
        Debug.Log($"[EnemyManager] 적 처치. 남은: {activeEnemies.Count}");

        if (activeEnemies.Count == 0)
            StageManager.Instance?.OnMapCleared();
    }

    public void SpawnBoss(EnemyUnit boss)
    {
        boss.SetAsBoss();
        RegisterEnemy(boss);
    }

    public void ExecuteAllEnemyTurns(PlayerUnit player)
    {
        foreach (var enemy in new List<EnemyUnit>(activeEnemies))
            if (enemy != null && enemy.IsAlive)
                enemy.ExecuteTurn(player);
    }

    public void Reset()
    {
        foreach (var e in activeEnemies)
            if (e != null) Destroy(e.gameObject);
        activeEnemies.Clear();
    }

    public List<EnemyUnit> GetActiveEnemies() => activeEnemies;
}
