using UnityEngine;

namespace DigitalArena
{
    /// <summary>The tamer moves independently of board pieces and battle simulation.</summary>
    [RequireComponent(typeof(Animation))]
    public sealed class PlayableCharacterMotor : MonoBehaviour
    {
        public const float GroundHeight = .25f;
        [SerializeField] float movementSpeed = 4f;
        Vector3 destination;
        bool moving;
        Animation animationPlayer;
        public Vector3 Destination => destination;
        public bool IsMoving => moving;
        public static bool InBattlefield(Vector3 p)
        {
            float x=p.x/ArenaWorld3D.IslandRadiusX;
            float z=(p.z-ArenaWorld3D.IslandCenterZ)/ArenaWorld3D.IslandRadiusZ;
            return x*x+z*z<=1f;
        }

        void Awake() { animationPlayer = GetComponent<Animation>(); ResetPosition(transform.position); }
        public void ResetPosition(Vector3 p)
        {
            p.y = GroundHeight; transform.position = destination = p; moving = false;
            Play("Idle");
        }
        public bool MoveTo(Vector3 p)
        {
            if (!InBattlefield(p)) return false;
            p.y = GroundHeight; destination = p;
            moving = (p - transform.position).sqrMagnitude > .0001f;
            Play(moving ? "Walk" : "Idle"); return true;
        }
        public void Tick(float dt, bool canMove)
        {
            if (!canMove) { Play("Idle"); return; }
            if (!moving) return;
            Play("Walk");
            Vector3 direction = destination - transform.position;
            if (direction.sqrMagnitude > .0001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 540f * dt);
            transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed * dt);
            if ((destination - transform.position).sqrMagnitude <= .0001f) { moving = false; Play("Idle"); }
        }
        void Play(string clip)
        {
            if (animationPlayer == null) animationPlayer = GetComponent<Animation>();
            if (animationPlayer != null && animationPlayer.GetClip(clip) != null && !animationPlayer.IsPlaying(clip))
                animationPlayer.CrossFade(clip, .12f);
        }
    }
}
