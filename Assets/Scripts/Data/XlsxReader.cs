using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

/// <summary>
/// 외부 라이브러리 없이 xlsx(Office Open XML)를 파싱하는 유틸리티.
/// xlsx = ZIP 아카이브 안의 XML 파일 묶음.
///
/// 반환값: List&lt;string[]&gt;
///   - [0] = 헤더 행
///   - [1..] = 데이터 행
///   - 각 string[]의 인덱스는 0부터 시작하는 열 번호(A=0, B=1 …)
/// </summary>
public static class XlsxReader
{
    private static readonly XNamespace NS =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    // ── 공개 API ─────────────────────────────────────────────────────────

    /// <summary>xlsx 파일 바이트 배열을 파싱해 행 목록을 반환한다.</summary>
    public static List<string[]> Parse(byte[] xlsxData)
    {
        if (xlsxData == null || xlsxData.Length == 0)
        {
            Debug.LogWarning("[XlsxReader] 빈 데이터");
            return new List<string[]>();
        }

        try
        {
            using (var ms = new MemoryStream(xlsxData))
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                var sharedStrings = ParseSharedStrings(archive);
                return ParseSheet(archive, "xl/worksheets/sheet1.xml", sharedStrings);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[XlsxReader] 파싱 실패: {e.Message}\n{e.StackTrace}");
            return new List<string[]>();
        }
    }

    // ── 공유 문자열 테이블 파싱 ──────────────────────────────────────────

    private static List<string> ParseSharedStrings(ZipArchive archive)
    {
        var result = new List<string>();
        var entry  = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return result;

        using (var stream = entry.Open())
        {
            var doc = XDocument.Load(stream);
            foreach (var si in doc.Descendants(NS + "si"))
            {
                // <t> 요소가 여러 개일 수 있으므로 전부 이어붙임
                var sb = new StringBuilder();
                foreach (var t in si.Descendants(NS + "t"))
                    sb.Append(t.Value);
                result.Add(sb.ToString());
            }
        }
        return result;
    }

    // ── 시트 파싱 ────────────────────────────────────────────────────────

    private static List<string[]> ParseSheet(
        ZipArchive archive, string entryPath, List<string> sharedStrings)
    {
        // rowIndex(1-based) → colIndex(0-based) → 값
        var rows   = new Dictionary<int, Dictionary<int, string>>();
        int maxRow = 0;
        int maxCol = 0;

        var entry = archive.GetEntry(entryPath);
        if (entry == null)
        {
            Debug.LogWarning($"[XlsxReader] 시트를 찾을 수 없음: {entryPath}");
            return new List<string[]>();
        }

        using (var stream = entry.Open())
        {
            var doc = XDocument.Load(stream);
            foreach (var c in doc.Descendants(NS + "c"))
            {
                string cellRef = c.Attribute("r")?.Value;
                if (string.IsNullOrEmpty(cellRef)) continue;

                int colIdx = CellRefToColIndex(cellRef); // 0-based
                int rowIdx = CellRefToRowIndex(cellRef); // 1-based

                string type  = c.Attribute("t")?.Value ?? "";
                string value = "";

                if (type == "inlineStr")
                {
                    value = c.Element(NS + "is")?.Element(NS + "t")?.Value ?? "";
                }
                else
                {
                    var vElem = c.Element(NS + "v");
                    value = vElem?.Value ?? "";

                    if (type == "s") // shared string 인덱스
                    {
                        if (int.TryParse(value, out int ssIdx) && ssIdx < sharedStrings.Count)
                            value = sharedStrings[ssIdx];
                        else
                            value = "";
                    }
                    // type == "" 또는 "n" → 숫자 → value 그대로 사용
                }

                if (!rows.ContainsKey(rowIdx))
                    rows[rowIdx] = new Dictionary<int, string>();

                rows[rowIdx][colIdx] = value;

                if (rowIdx > maxRow) maxRow = rowIdx;
                if (colIdx > maxCol) maxCol = colIdx;
            }
        }

        // Dictionary → List<string[]> 변환 (행/열 순서 보장)
        var result = new List<string[]>(maxRow);
        int colCount = maxCol + 1;

        for (int r = 1; r <= maxRow; r++)
        {
            var row = new string[colCount];
            if (rows.TryGetValue(r, out var cols))
            {
                for (int col = 0; col < colCount; col++)
                    row[col] = cols.TryGetValue(col, out var v) ? v : "";
            }
            result.Add(row);
        }
        return result;
    }

    // ── 셀 참조 파싱 헬퍼 ───────────────────────────────────────────────

    /// <summary>"AB12" → 열 인덱스(0-based). A=0, B=1, Z=25, AA=26 …</summary>
    private static int CellRefToColIndex(string cellRef)
    {
        int col = 0;
        int i   = 0;
        while (i < cellRef.Length && char.IsLetter(cellRef[i]))
        {
            col = col * 26 + (char.ToUpper(cellRef[i]) - 'A' + 1);
            i++;
        }
        return col - 1; // 0-based
    }

    /// <summary>"AB12" → 행 인덱스(1-based).</summary>
    private static int CellRefToRowIndex(string cellRef)
    {
        int i = 0;
        while (i < cellRef.Length && char.IsLetter(cellRef[i])) i++;
        return int.TryParse(cellRef.Substring(i), out int row) ? row : 1;
    }
}
