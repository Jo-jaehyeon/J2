using System;
using System.Collections;
using System.Collections.Generic;
using DigitalArena;
using UnityEngine;

namespace J2.Spawning
{
    public enum SpawnKind
    {
        Player,
        Ally,
        Creep
    }

    [Serializable]
    public sealed class SpawnTypeEntry
    {

        public SpawnKind	kind;

        [Min(0)]
        public int		id;
        public string	key;
        public string	displayName;
        public string	catalogId;

        public GameObject	prefab;
    }

    [CreateAssetMenu(menuName = "J2/Spawn ID Table")]
    public sealed class SpawnTypeTable : ScriptableObject
    {

        public List<SpawnTypeEntry>	entries = new List<SpawnTypeEntry>();

        public bool TryGet(int id, out SpawnTypeEntry entry)
        {
            entry = null;

            foreach (var candidate in entries)
            {
                if (candidate == null || candidate.id != id)
                {
                    continue;
                }

                if (entry != null)
                {
                    entry = null;

                    return false;
                }

                entry = candidate;
            }

            return entry != null;
        }

        public string Validate(DigimonCatalog catalog)
        {
            var ids = new HashSet<int>();

            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in entries)
            {
                if (row == null || row.id < 0 || !ids.Add(row.id))
                {
                    return "스폰 ID는 0 이상의 고유한 값이어야 합니다.";
                }

                if (string.IsNullOrWhiteSpace(row.key) || !System.Text.RegularExpressions.Regex.IsMatch(row.key, "^[A-Za-z0-9_]+$") || !keys.Add(row.key) || string.IsNullOrWhiteSpace(row.displayName))
                {
                    return "등록 키는 고유해야 하며 표시 이름이 필요합니다.";
                }

                if (!Enum.IsDefined(typeof(SpawnKind), row.kind))
                {
                    return "스폰 분류가 올바르지 않습니다.";
                }

                if (row.kind == SpawnKind.Player)
                {
                    if (row.prefab == null)
                    {
                        return row.displayName + ": 플레이어 프리팹이 필요합니다.";
                    }
                }
                else if (catalog == null || Array.Find(row.kind == SpawnKind.Creep ? catalog.enemies : catalog.allies, d => d.id == row.catalogId) == null)
                {
                    return row.displayName + ": 기물 데이터 ID를 찾지 못했습니다.";
                }
            }

            return null;
        }

        // Unity main thread only. The caller owns parenting, position, layer and network instance IDs.
        public GameObject Spawn(int typeId, Transform parent, Vector3 localPosition, DigimonCatalog catalog)
        {
            if (!TryGet(typeId, out var entry))
            {
                throw new ArgumentException("등록되지 않았거나 중복된 스폰 ID: " + typeId);
            }

            GameObject result;

            if (entry.prefab != null)
            {
                result = Instantiate(entry.prefab, parent, false);
            }
            else
            {
                if (entry.kind == SpawnKind.Player || catalog == null)
                {
                    throw new InvalidOperationException("스폰 데이터가 없습니다: " + entry.key);
                }

                var data = Array.Find(entry.kind == SpawnKind.Creep ? catalog.enemies : catalog.allies, d => d.id == entry.catalogId);

                if (data == null)
                {
                    throw new InvalidOperationException("기물 데이터가 없습니다: " + entry.catalogId);
                }

                result = new GameObject(entry.displayName);
                result.transform.SetParent(parent, false);
                result.AddComponent<UnitModelView>().BuildSpawnModel(data, entry.kind == SpawnKind.Creep);
            }

            result.name = entry.displayName;
            result.transform.localPosition = localPosition;

            return result;
        }
    }
}
