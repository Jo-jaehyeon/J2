using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DigitalArena;
using J2.Spawning;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SpawnTypeTableEditorData
{

    public const string	AssetPath = "Assets/Resources/SpawnTypeTable.asset";
    public static string LastStatus { get; private set; } = "";

    private static bool	pending;

    static SpawnTypeTableEditorData()
    {
        Undo.undoRedoPerformed += ScheduleExport;
    }

    public static DigimonCatalog Catalog() => JsonUtility.FromJson<DigimonCatalog>(File.ReadAllText("Assets/Resources/DigimonCatalog.json"));

    [MenuItem("Digital Arena/Spawn ID Table")]
    public static void Open()
    {
        EnsureTable();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SpawnTypeTable>(AssetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public static void EnsureTable()
    {
        var table = AssetDatabase.LoadAssetAtPath<SpawnTypeTable>(AssetPath);

        if (table == null)
        {
            table = ScriptableObject.CreateInstance<SpawnTypeTable>();
            AssetDatabase.CreateAsset(table, AssetPath);
            ImportMissing(table);
        }

        Export(table);
    }

    public static void ImportMissing(SpawnTypeTable table)
    {
        Undo.RecordObject(table, "Import spawn IDs");

        var catalog = Catalog();

        var used = new HashSet<int>(table.entries.Select(e => e.id));

        void Add(string key, string name, SpawnKind kind, string catalogId, string resource, int start)
        {
            if (table.entries.Any(e => e.key == key))
            {
                return;
            }

            while (used.Contains(start))
            {
                start++;
            }

            used.Add(start);
            table.entries.Add(new SpawnTypeEntry { id = start, key = key, displayName = name, kind = kind, catalogId = catalogId, prefab = string.IsNullOrEmpty(resource) ? null : Resources.Load<GameObject>(resource) });
        }

        Add("player_shintaeyil", "신태일", SpawnKind.Player, "", "PlayableCharacter/ShinTaeyil/ShinTaeyil", 0);

        foreach (var row in catalog.allies)
        {
            Add("ally_" + row.id, row.name, SpawnKind.Ally, row.id, row.prefabPath, 1001);
        }

        foreach (var row in catalog.enemies)
        {
            Add("creep_" + row.id, row.name, SpawnKind.Creep, row.id, row.prefabPath, 2001);
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssetIfDirty(table);
    }

    public static void ScheduleExport()
    {
        if (pending)
        {
            return;
        }

        pending = true;
        EditorApplication.delayCall += () =>
        {
            pending = false;

            var table = AssetDatabase.LoadAssetAtPath<SpawnTypeTable>(AssetPath);

            if (table != null)
            {
                Export(table);
            }
        };
    }

    [Serializable]
    public sealed class ExportRow
    {

        public int		id;
        public string	key, name, kind, catalogId, prefabPath;
    }

    [Serializable]
    public sealed class ExportFile
    {

        public ExportRow[]	entries;

        public int	schemaVersion = 1;
    }

    public static bool Export(SpawnTypeTable table)
    {
        string error = table.Validate(Catalog());

        if (error != null)
        {
            LastStatus = "내보내기 실패: " + error;
            Debug.LogError(LastStatus);

            return false;
        }

        var rows = table.entries.OrderBy(e => e.id).Select(e => new ExportRow { id = e.id, key = e.key, name = e.displayName, kind = e.kind.ToString(), catalogId = e.catalogId, prefabPath = e.prefab == null ? "" : AssetDatabase.GetAssetPath(e.prefab) }).ToArray();

        string json = JsonUtility.ToJson(new ExportFile { entries = rows }, true) + "\n";

        var header = new StringBuilder("// Generated from Unity SpawnTypeTable.asset. Do not edit.\n#pragma once\n#include <cstdint>\n\nnamespace J2SpawnTypes\n{\nstruct Entry { std::int32_t id; const char* key; const char* kind; };\ninline constexpr Entry Entries[] = {\n");

        foreach (var row in rows)
        {
            string key = row.key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");

            header.Append("    { ").Append(row.id).Append(", \"").Append(key).Append("\", \"").Append(row.kind).Append("\" },\n");
        }

        header.Append("};\nnamespace Id\n{\n");

        foreach (var row in rows)
        {
            header.Append("inline constexpr std::int32_t Type_").Append(row.key).Append(" = ").Append(row.id).Append(";\n");
        }

        header.Append("}\nconstexpr const Entry* Find(std::int32_t id) { for (const auto& entry : Entries) { if (entry.id == id) { return &entry; } } return nullptr; }\n}\n");

        string server = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../CPP_Server/J2_Server"));

        if (!Directory.Exists(Path.Combine(server, "Common")))
        {
            LastStatus = "서버 Common 폴더를 찾지 못했습니다: " + server;
            Debug.LogError(LastStatus);

            return false;
        }

        string output = Path.Combine(server, "Common/Generated");

        Directory.CreateDirectory(output);
        Directory.CreateDirectory("Docs/Generated");
        WriteChanged(Path.Combine(output, "SpawnTypes.json"), json);
        WriteChanged(Path.Combine(output, "SpawnTypes.h"), header.ToString());
        WriteChanged("Docs/Generated/SpawnTypes.json", json);
        AssetDatabase.SaveAssetIfDirty(table);
        LastStatus = rows.Length + "종 서버 동기화 완료: " + output;

        return true;
    }

    static void WriteChanged(string path, string content)
    {
        if (!File.Exists(path) || File.ReadAllText(path) != content)
        {
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }
}
