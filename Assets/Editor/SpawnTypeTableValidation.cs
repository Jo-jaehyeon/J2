using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using J2.Spawning;
using UnityEditor;
using UnityEngine;

public static class SpawnTypeTableValidation
{
    public static void Run()
    {
        SpawnTypeTableEditorData.EnsureTable();

        var table = AssetDatabase.LoadAssetAtPath<SpawnTypeTable>(SpawnTypeTableEditorData.AssetPath);

        var catalog = SpawnTypeTableEditorData.Catalog();

        int checks = 0;

        void Check(bool ok, string description)
        {
            if (!ok)
            {
                throw new Exception(description);
            }

            checks++;
        }

        Check(table.Validate(catalog) == null, "Table references valid");
        Check(table.entries.Count >= catalog.allies.Length + catalog.enemies.Length + 1, "All current units registered");

        foreach (var row in table.entries)
        {
            Check(table.TryGet(row.id, out var resolved) && resolved == row, "Stable ID lookup: " + row.key);
        }

        Check(!table.TryGet(int.MaxValue, out _), "Unknown ID rejected");

        var copy = UnityEngine.Object.Instantiate(table);

        try
        {
            copy.entries.Reverse();
            Check(copy.TryGet(table.entries.Find(e => e.key == "player_shintaeyil").id, out var player) && player.kind == SpawnKind.Player, "Row order does not change IDs");
            copy.entries[0].id = copy.entries[1].id;
            Check(copy.Validate(catalog) != null, "Duplicate IDs rejected");
            Check(!copy.TryGet(copy.entries[0].id, out _), "Ambiguous ID lookup rejected");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(copy);
        }

        Check(SpawnTypeTableEditorData.Export(table), "Server export succeeds");

        string server = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../CPP_Server/J2_Server/Common/Generated"));

        var json = JsonUtility.FromJson<SpawnTypeTableEditorData.ExportFile>(File.ReadAllText(Path.Combine(server, "SpawnTypes.json")));

        Check(json.entries.Select(e => e.id).SequenceEqual(table.entries.Select(e => e.id).OrderBy(id => id)), "Server IDs match Unity");
        Check(File.ReadAllText(Path.Combine(server, "SpawnTypes.json")) == File.ReadAllText("Docs/Generated/SpawnTypes.json"), "Client/server manifests match");
        Check(File.ReadAllText(Path.Combine(server, "SpawnTypes.h")).Contains("constexpr const Entry* Find"), "C++ lookup emitted");
        Debug.Log("SPAWN_TABLE_OK: " + checks + " checks; " + table.entries.Count + " types exported.");
    }
}
