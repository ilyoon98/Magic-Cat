using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 스테이지/맵(라운드)별 적 배치 데이터 테이블.
///
/// 우선순위:
///   1) StreamingAssets/Table/EnemySpawn.xlsx 로드 성공 시 해당 데이터 사용
///   2) 실패 시 하드코딩 폴백 데이터 사용
///
/// EnemySpawn.xlsx 헤더 (1행):
///   Stage | Round | EnemyIndex | Count
///
/// 예시:
///   1 | 1 | 1 | 2    → Stage1 Map1에 Goblin(인덱스1) 2마리
///   1 | 2 | 1 | 2    → Stage1 Map2에 Goblin 2마리
///   1 | 2 | 2 | 1    → Stage1 Map2에 Orc(인덱스2) 1마리
///   1 | 3 | 3 | 1    → Stage1 Map3(보스)에 Centaurus(인덱스3) 1마리
///
/// GetEnemyIndices(stage, round) 반환값:
///   Count만큼 EnemyIndex를 반복한 확장 리스트.
///   예: Stage1 Map2 → [1, 1, 2]
/// </summary>
public static class EnemySpawnTable
{
    // key: (stage, round) / value: (EnemyIndex, Count) 쌍 목록
    private static Dictionary<(int stage, int round), List<(int index, int count)>> _table;

    // ── 공개 API ─────────────────────────────────────────────────────────

    /// <summary>
    /// 해당 스테이지·맵의 적 인덱스 목록 반환.
    /// Count만큼 인덱스를 펼쳐서 반환한다.
    /// 데이터가 없으면 빈 리스트 반환 (StageManager 폴백 처리).
    /// </summary>
    public static List<int> GetEnemyIndices(int stage, int round)
    {
        EnsureLoaded();

        var key = (stage, round);
        if (!_table.TryGetValue(key, out var entries))
            return new List<int>();

        var result = new List<int>();
        foreach (var (idx, cnt) in entries)
            for (int i = 0; i < cnt; i++)
                result.Add(idx);
        return result;
    }

    // ── 로드 흐름 ─────────────────────────────────────────────────────────

    private static void EnsureLoaded()
    {
        if (_table != null) return;
        _table = new Dictionary<(int, int), List<(int, int)>>();
        TryLoadFromXlsx();
        if (_table.Count == 0)
        {
            Debug.Log("[EnemySpawnTable] xlsx 로드 실패 또는 비어 있음 → 하드코딩 폴백 사용");
            LoadFallback();
        }
        else
        {
            Debug.Log($"[EnemySpawnTable] xlsx 로드 완료 ({_table.Count}맵)");
        }
    }

    private static void TryLoadFromXlsx()
    {
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Table", "EnemySpawn.xlsx");
            if (!File.Exists(path))
            {
                Debug.Log($"[EnemySpawnTable] 파일 없음: {path}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var rows = XlsxReader.Parse(bytes);

            if (rows.Count < 2) return;

            string[] header = rows[0];
            int cStage = FindCol(header, "Stage");
            int cRound = FindCol(header, "Round");
            int cIndex = FindCol(header, "EnemyIndex");
            int cCount = FindCol(header, "Count");

            if (cStage < 0 || cRound < 0 || cIndex < 0)
            {
                Debug.LogWarning("[EnemySpawnTable] 필수 헤더(Stage, Round, EnemyIndex)를 찾을 수 없음");
                return;
            }

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (!int.TryParse(SafeGet(row, cStage), out int stage) || stage <= 0) continue;
                if (!int.TryParse(SafeGet(row, cRound), out int round) || round <= 0) continue;
                if (!int.TryParse(SafeGet(row, cIndex), out int enemyIdx) || enemyIdx <= 0) continue;
                int count = cCount >= 0 && int.TryParse(SafeGet(row, cCount), out int c) ? c : 1;
                if (count <= 0) count = 1;

                var key = (stage, round);
                if (!_table.ContainsKey(key))
                    _table[key] = new List<(int, int)>();
                _table[key].Add((enemyIdx, count));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EnemySpawnTable] xlsx 로드 오류: {e.Message}");
        }
    }

    // ── 하드코딩 폴백 ────────────────────────────────────────────────────
    // EnemySpawn.xlsx 기반:
    //   Stage | Round | EnemyIndex | Count
    //     1   |   1   |     1      |   2     (Goblin ×2)
    //     1   |   2   |     1      |   2     (Goblin ×2)
    //     1   |   2   |     2      |   1     (Orc ×1)
    //     1   |   3   |     3      |   1     (Centaurus ×1)
    //     2   |   1   |     4      |   3     (Slime ×3)
    //     2   |   2   |     4      |   4     (Slime ×4)
    //     2   |   3   |     5      |   1     (BigSlime ×1)
    //     2   |   3   |     4      |   1     (Slime ×1)
    //     3   |   1   |     4      |   4     (Slime ×4)
    //     3   |   2   |     4      |   5     (Slime ×5)
    //     3   |   3   |     5      |   1     (BigSlime ×1)
    //     3   |   3   |     4      |   2     (Slime ×2)

    private static void LoadFallback()
    {
        Add(1, 1, 1, 2);
        Add(1, 2, 1, 2); Add(1, 2, 2, 1);
        Add(1, 3, 3, 1);
        Add(2, 1, 4, 3);
        Add(2, 2, 4, 4);
        Add(2, 3, 5, 1); Add(2, 3, 4, 1);
        Add(3, 1, 4, 4);
        Add(3, 2, 4, 5);
        Add(3, 3, 5, 1); Add(3, 3, 4, 2);
    }

    private static void Add(int stage, int round, int enemyIndex, int count)
    {
        var key = (stage, round);
        if (!_table.ContainsKey(key))
            _table[key] = new List<(int, int)>();
        _table[key].Add((enemyIndex, count));
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private static int FindCol(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string SafeGet(string[] row, int col) =>
        col >= 0 && col < row.Length ? row[col].Trim() : "";
}
