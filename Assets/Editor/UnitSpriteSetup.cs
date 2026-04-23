using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Assets/Resources/Units/ 폴더의 PNG 파일을
/// Sprite 타입으로 자동 변환하는 에디터 유틸리티.
///
/// 메뉴: Tools → Magic Cats → Setup Unit Sprites
/// </summary>
public class UnitSpriteSetup
{
    private const string UnitFolder = "Assets/Resources/Units";

    // ── PNG → Sprite 변환 ─────────────────────────────────────────────────

    [MenuItem("Tools/Magic Cats/① Unit PNG → Sprite 변환")]
    public static void SetupSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UnitFolder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; changed = true; }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }

            if (!importer.alphaIsTransparency)
            { importer.alphaIsTransparency = true; changed = true; }

            if (importer.mipmapEnabled)
            { importer.mipmapEnabled = false; changed = true; }

            if (changed)
            {
                importer.SaveAndReimport();
                count++;
                Debug.Log($"[UnitSpriteSetup] Sprite로 변환: {Path.GetFileName(path)}");
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "PNG → Sprite 변환 완료",
            count > 0
                ? $"{count}개 파일을 Sprite 타입으로 변환했습니다.\n폴더: {UnitFolder}"
                : "변환할 파일이 없습니다 (이미 모두 Sprite 타입).",
            "확인");
    }

    // ── CSV ↔ 파일명 불일치 진단 ─────────────────────────────────────────

    [MenuItem("Tools/Magic Cats/② 스프라이트 이름 불일치 진단")]
    public static void DiagnoseNames()
    {
        // Resources/Units 폴더의 실제 파일명 수집 (확장자 제외)
        string[] guids    = AssetDatabase.FindAssets("t:Texture2D", new[] { UnitFolder });
        var fileNames     = new HashSet<string>(System.StringComparer.Ordinal);
        var fileNamesIC   = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        var fileList      = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            string nameNoExt = Path.GetFileNameWithoutExtension(path);
            fileNames.Add(nameNoExt);
            fileNamesIC.Add(nameNoExt);
            fileList.Add(nameNoExt);
        }

        // Enemy.csv 로드
        var csvAsset = Resources.Load<TextAsset>("Table/Enemy");
        if (csvAsset == null)
        {
            EditorUtility.DisplayDialog("오류", "Resources/Table/Enemy.csv 를 찾을 수 없습니다.", "확인");
            return;
        }

        var rows = CsvReader.Parse(csvAsset.text);
        if (rows.Count < 2) { EditorUtility.DisplayDialog("오류", "데이터 없음", "확인"); return; }

        string[] header = rows[0];
        int cName = -1;
        for (int i = 0; i < header.Length; i++)
            if (string.Equals(header[i].Trim(), "EnemyName", System.StringComparison.OrdinalIgnoreCase))
            { cName = i; break; }

        if (cName < 0) { EditorUtility.DisplayDialog("오류", "EnemyName 헤더 없음", "확인"); return; }

        var report = new System.Text.StringBuilder();
        report.AppendLine($"=== 스프라이트 이름 진단 ===\n");
        report.AppendLine("[Resources/Units 파일]");
        foreach (var f in fileList) report.AppendLine($"  · {f}.png");
        report.AppendLine();
        report.AppendLine("[Enemy.csv EnemyName → 예상 파일명]");

        bool allOk = true;
        for (int r = 1; r < rows.Count; r++)
        {
            string[] row  = rows[r];
            string csvName = cName < row.Length ? row[cName].Trim() : "";
            if (string.IsNullOrEmpty(csvName)) continue;

            // 스프라이트 로드 시 공백 제거 (EnemySpawnManager와 동일 로직)
            string expectedFile = csvName.Replace(" ", "");

            if (fileNames.Contains(expectedFile))
                report.AppendLine($"  ✅ \"{csvName}\" → {expectedFile}.png");
            else if (fileNamesIC.Contains(expectedFile))
                report.AppendLine($"  ⚠ \"{csvName}\" → {expectedFile}.png (대소문자 불일치)");
            else
            {
                report.AppendLine($"  ❌ \"{csvName}\" → {expectedFile}.png  ← 파일 없음!");
                allOk = false;
            }
        }

        report.AppendLine();
        report.AppendLine(allOk ? "✅ 모든 이름이 일치합니다." :
            "❌ 불일치 항목이 있습니다.\n" +
            "파일명 = EnemyName (공백 제거) 형식으로 맞춰주세요.\n" +
            "예) EnemyName=\"킹 슬라임\" → 파일명=킹슬라임.png");

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("스프라이트 이름 진단", report.ToString(), "확인");
    }
}
