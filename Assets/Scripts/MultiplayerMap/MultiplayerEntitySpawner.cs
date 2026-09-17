using System;
using System.Collections;
using J2.Creatures;
using System.Collections.Generic;
using DigitalArena;
using J2.Protocol;
using J2.Spawning;
using UnityEngine;

namespace J2.MultiplayerMap
{
    public sealed class MultiplayerEntitySpawner : MonoBehaviour
    {

        readonly Dictionary<int, GameObject>	entities = new Dictionary<int, GameObject>();

        readonly Dictionary<int, int>	types = new Dictionary<int, int>();

        SpawnTypeTable		table;
        DigimonCatalog		catalog;

        Transform								arena;

        EntityBattleState	battleState = EntityBattleState.Preparation;
        public int Count => entities.Count;
        public IReadOnlyDictionary<int, GameObject> Entities => entities;

        public void Initialize(Transform arenaTransform)
        {
            arena = arenaTransform;
            table = Resources.Load<SpawnTypeTable>("SpawnTypeTable");

            var data = Resources.Load<TextAsset>("DigimonCatalog");

            catalog = data != null ? JsonUtility.FromJson<DigimonCatalog>(data.text) : null;
        }

        public bool TryGetEntity(int entityId, out GameObject entity) => entities.TryGetValue(entityId, out entity);

        public bool TryGetCreature(int entityId, out Creature creature)
        {
            creature = null;

            if (entities.TryGetValue(entityId, out var entity) && entity != null)
            {
                creature = entity.GetComponent<Creature>();
            }

            return creature != null;
        }

        public bool SetDestination(int entityId, Vector3 worldPosition)
        {
            if (!TryGetCreature(entityId, out var creature))
            {
                return false;
            }

            creature.SetDestination(worldPosition);

            return true;
        }

        public void SetBattleState(EntityBattleState state)
        {
            battleState = state;

            foreach (var instance in entities.Values)
            {
                if (instance != null && instance.TryGetComponent<Entity>(out var unit))
                {
                    unit.SetBattleState(state);
                }
            }
        }

        public bool Spawn(S_Spawn packet)
        {
            if (packet == null || arena == null || table == null)
            {
                return false;
            }

            if (!table.TryGet(packet.SpawnTypeId, out var entry))
            {
                Debug.LogWarning("Unknown or duplicate SpawnTypeId: " + packet.SpawnTypeId);

                return false;
            }

            // Spawn X/Y are offsets on the viewed arena floor (local X/Z).
            var position = new Vector3(packet.X, entry.kind == SpawnKind.Player ? .25f : 0f, packet.Y);

            if (entities.TryGetValue(packet.EntityId, out var existing) && existing != null)
            {
                if (types[packet.EntityId] != packet.SpawnTypeId)
                {
                    Debug.LogWarning("EntityId already belongs to a different spawn type: " + packet.EntityId);

                    return false;
                }

                return true; // Duplicate spawn notifications must not reset a moving entity.
            }

            GameObject created;

            try
            {
                created = table.Spawn(packet.SpawnTypeId, arena, position, catalog);
            }
            catch (System.Exception error)
            {
                Debug.LogError("Spawn failed: " + error.Message);

                return false;
            }

            foreach (var node in created.GetComponentsInChildren<Transform>(true))
            {
                node.gameObject.layer = arena.gameObject.layer;
            }

            Creature creature;

            if (entry.kind == SpawnKind.Player)
            {
                creature = created.GetComponent<J2.Creatures.Player>();

                if (creature == null)
                {
                    creature = created.AddComponent<J2.Creatures.Player>();
                }
            }
            else
            {
                var unit = created.GetComponent<Entity>();

                if (unit == null)
                {
                    unit = created.AddComponent<Entity>();
                }

                creature = unit;
            }

            creature.Initialize(packet.EntityId, packet.SpawnTypeId);

            if (creature is Entity spawnedUnit)
            {
                spawnedUnit.SetBattleState(battleState);
            }

            entities[packet.EntityId] = created;
            types[packet.EntityId] = packet.SpawnTypeId;

            return true;
        }
    }
}
