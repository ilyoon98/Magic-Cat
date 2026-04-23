using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스탯 데이터 테이블.
///
/// Resources/Table/Enemy.csv 를 TextAsset으로 로드해 파싱한다.
///
/// Enemy.csv 헤더 (1행):
///   INDEX, EnemyName, Health, Damage, ATTackRange, MoveSpeed, Type
///
/// Type 열: "Boss" 이면 IsBoss=true, 그 외("Nomal" 등) false
/// INDEX는 연속이 아닐 수 있음 (예: 1,2,3,4,6 — 5번 없음)
/// </summary>
public static class EnemyDataTable
{
    // ── 데이터 구조체 ─────────────────────────────────────────────────────
    public struct EnemyData
    {
        public int    Index;
        public string Name;
        public int    HP;
        public int    Damage;
        public int    AttackRange;
        public int    MoveSpeed;
        public bool   IsBoss;
    }

    // ── 내부 테이블 (지연 초기화) ─────────────────────────────────────────
    private static Dictionary<int, EnemyData> _table;

    // ── 공개 API ─────────────────────────────────────────────────────────

    /// <summary>INDEX로 적 데이터를 가져온다. 없으면 default 반환.</summary>
    public static EnemyData Get(int index)
    {
        EnsureLoaded();
        if (_table.TryGetValue(index, out var d)) return d;
        Debug.LogWarning($"[EnemyDataTable] INDEX {index} 없음");
        return default;
    }

    /// <summary>테이블에 INDEX가 존재하는지 확인.</summary>
    public static bool Contains(int index)
    {
        EnsureLoaded();
        return _table.ContainsKey(index);
    }

    /// <summary>전체 데이터 열거.</summary>
    public static IEnumerable<EnemyData> All
    {
        get { EnsureLoaded(); return _table.Values; }
    }

    // ── 로드 흐름 ─────────────────────────────────────────────────────────

    private static void EnsureLoaded()
    {
        if (_table != null) return;
        _table = new Dictionary<int, EnemyData>();
        LoadFromCsv();
    }

    private static void LoadFromCsv()
    {
        // Resources/Table/Enemy (확장자 없이)
        var asset = Resources.Load<TextAsset>("Table/Enemy");
        if (asset == null)
        {
            Debug.LogError("[EnemyDataTable] Resources/Table/Enemy.csv 를 찾을 수 없음. " +
                           "Assets/Resources/Table/ 폴더에 Enemy.csv 를 넣어주세요.");
            return;
        }

        var rows = CsvReader.Parse(asset.text);
        if (rows.Count < 2)
        {
            Debug.LogError("[EnemyDataTable] Enemy.csv 에 데이터 행이 없음");
            return;
        }

        // 헤더에서 열 인덱스 검색 (대소문자 무시)
        string[] header = rows[0];
        int cIdx   = FindCol(header, "INDEX");
        int cName  = FindCol(header, "EnemyName");
        int cHP    = FindCol(header, "Health");
        int cDmg   = FindCol(header, "Damage");
        int cRange = FindCol(header, "ATTackRange");   // CSV 오타 그대로
        int cSpeed = FindCol(header, "MoveSpeed");
        int cType  = FindCol(header, "Type");

        if (cIdx < 0 || cName < 0)
        {
            Debug.LogError("[EnemyDataTable] 필수 헤더(INDEX, EnemyName)를 찾을 수 없음. " +
                           "CSV 파일의 1행 헤더를 확인해주세요.");
            return;
        }

        for (int r = 1; r < rows.Count; r++)
        {
            string[] row = rows[r];
            if (row.Length <= cIdx) continue;
            if (!int.TryParse(row[cIdx], out int idx) || idx <= 0) continue;

            var d = new EnemyData
            {
                Index       = idx,
                Name        = Get(row, cName),
                HP          = GetInt(row, cHP,    1),
                Damage      = GetInt(row, cDmg,   1),
                AttackRange = GetInt(row, cRange,  1),
                MoveSpeed   = GetInt(row, cSpeed,  1),
                // "Boss" 외에는 false (CSV 오타 "Nomal" 포함)
                IsBoss      = string.Equals(Get(row, cType), "Boss",
                                  StringComparison.OrdinalIgnoreCase)
            };
            _table[idx] = d;
        }

        Debug.Log($"[EnemyDataTable] 로드 완료 — {_table.Count}건 " +
                  $"(INDEX: {string.Join(", ", _table.Keys)})");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private static int FindCol(string[] header, string name)
    {
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static string Get(string[] row, int col) =>
        col >= 0 && col < row.Length ? row[col] : "";

    private static int GetInt(string[] row, int col, int fallback) =>
        col >= 0 && col < row.Length && int.TryParse(row[col], out int v) ? v : fallback;
}
