using System.Collections.Generic;

/// <summary>
/// 단순 CSV 텍스트 파서.
///
/// 반환값: List&lt;string[]&gt;
///   - [0] = 헤더 행
///   - [1..] = 데이터 행 (빈 행 제외)
///   - 각 string[]의 인덱스는 0부터 시작하는 열 번호
///
/// 지원:
///   - \r\n / \n 줄바꿈 자동 처리
///   - 각 필드 앞뒤 공백 트리밍
/// </summary>
public static class CsvReader
{
    /// <summary>CSV 텍스트를 파싱해 행 목록을 반환한다.</summary>
    public static List<string[]> Parse(string csvText)
    {
        var result = new List<string[]>();
        if (string.IsNullOrEmpty(csvText)) return result;

        // 줄바꿈 정규화 (\r\n → \n)
        csvText = csvText.Replace("\r\n", "\n").Replace('\r', '\n');

        string[] lines = csvText.Split('\n');
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue; // 빈 행 제외

            string[] fields = trimmed.Split(',');
            // 각 필드 앞뒤 공백 제거
            for (int i = 0; i < fields.Length; i++)
                fields[i] = fields[i].Trim();

            result.Add(fields);
        }
        return result;
    }
}
