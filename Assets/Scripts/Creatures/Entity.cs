using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace J2.Creatures
{
    public enum EntityBattleState
    {
        Preparation,
        Combat
    }

    public class Entity : Creature
    {
        public EntityBattleState BattleState { get; private set; } = EntityBattleState.Preparation;

        public override void Initialize(int entityId, int spawnTypeId)
        {
            BattleState = EntityBattleState.Preparation;
            base.Initialize(entityId, spawnTypeId);
        }

        public void SetBattleState(EntityBattleState state)
        {
            BattleState = state;

            if (!IsDead && state == EntityBattleState.Preparation)
            {
                TeleportToDestination();
            }
        }

        public override void SetDestination(Vector3 worldPosition)
        {
            if (IsDead)
            {
                return;
            }

            base.SetDestination(worldPosition);

            if (BattleState == EntityBattleState.Preparation)
            {
                TeleportToDestination();
            }
        }

        public override void Tick(float deltaTime)
        {
            if (BattleState == EntityBattleState.Combat)
            {
                Move(deltaTime);
            }
        }
    }
}
