using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace J2.Creatures
{
    [DisallowMultipleComponent]
    public abstract class Creature : MonoBehaviour
    {

        [SerializeField]
        protected float		moveSpeed = 4f;
        [SerializeField]
        protected float		turnSpeed = 540f;
        [SerializeField]
        protected string	idleAnimation = "Idle";
        [SerializeField]
        protected string	moveAnimation = "Walk";
        [SerializeField]
        protected string	attackAnimation = "Attack";
        [SerializeField]
        protected string	deathAnimation = "Death";
        private bool		movementAnimation;

        [SerializeField]
        protected Animation	animationPlayer;
        [SerializeField]
        private Vector3		destination;
        public int EntityId { get; private set; }
        public int SpawnTypeId { get; private set; }
        public Vector3 Destination => destination;
        public bool IsDead { get; private set; }
        public bool IsMoving => !IsDead && (destination - transform.position).sqrMagnitude > .0001f;

        protected virtual void Awake()
        {
            destination = transform.position;
            animationPlayer = animationPlayer != null ? animationPlayer : GetComponentInChildren<Animation>(true);
        }

        protected virtual void Update()
        {
            Tick(Time.deltaTime);
        }

        public virtual void Initialize(int entityId, int spawnTypeId)
        {
            EntityId = entityId;
            SpawnTypeId = spawnTypeId;
            destination = transform.position;
            IsDead = false;
            movementAnimation = false;
            PlayIdleAnimation();
        }

        // World coordinates. Packet handlers call this on the Unity main thread.
        public virtual void SetDestination(Vector3 worldPosition)
        {
            if (!IsDead)
            {
                destination = worldPosition;
            }
        }

        public abstract void Tick(float deltaTime);

        protected virtual void Move(float deltaTime)
        {
            if (IsDead || deltaTime <= 0f)
            {
                return;
            }

            if (!IsMoving)
            {
                FinishMovementAnimation();

                return;
            }

            Vector3 direction = destination - transform.position;

            Vector3 facing = new Vector3(direction.x, 0f, direction.z);

            if (facing.sqrMagnitude > .0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), Mathf.Max(0f, turnSpeed) * deltaTime);
            }

            if (!movementAnimation)
            {
                PlayMoveAnimation();
                movementAnimation = true;
            }

            transform.position = Vector3.MoveTowards(transform.position, destination, Mathf.Max(0f, moveSpeed) * deltaTime);

            if (!IsMoving)
            {
                transform.position = destination;
                FinishMovementAnimation();
            }
        }

        protected void TeleportToDestination()
        {
            transform.position = destination;
            FinishMovementAnimation();
        }

        private void FinishMovementAnimation()
        {
            if (movementAnimation)
            {
                movementAnimation = false;
                PlayIdleAnimation();
            }
        }

        public virtual void StopMovement()
        {
            destination = transform.position;
            FinishMovementAnimation();
        }

        public virtual void PlayIdleAnimation()
        {
            PlayAnimation(idleAnimation);
        }

        public virtual void PlayMoveAnimation()
        {
            PlayAnimation(moveAnimation);
        }

        public virtual void PlayAttackAnimation()
        {
            if (IsDead)
            {
                return;
            }

            StopMovement();
            PlayAnimation(attackAnimation);
        }

        public virtual void PlayDeathAnimation()
        {
            StopMovement();
            IsDead = true;
            PlayAnimation(deathAnimation);
        }

        protected virtual void PlayAnimation(string clipName)
        {
            if (animationPlayer == null)
            {
                animationPlayer = GetComponentInChildren<Animation>(true);
            }

            if (animationPlayer != null && !string.IsNullOrEmpty(clipName) && animationPlayer.GetClip(clipName) != null && !animationPlayer.IsPlaying(clipName))
            {
                animationPlayer.CrossFade(clipName, .12f);
            }
        }
    }
}
