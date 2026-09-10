using UnityEngine;
namespace DigitalArena
{
    [RequireComponent(typeof(Animation))]
    public sealed class ArenaUnitAnimation : MonoBehaviour
    {
        Animation player;
        int attackCount, hitCount;
        float lockedUntil, deathTime;
        bool dying;
        public bool DeathComplete => dying && deathTime>=.85f;
        void Awake() { player=GetComponent<Animation>();player.Play("Idle"); }
        public void Pose(bool moving,ArenaBattle.Fighter fighter,float dt)
        {
            if(player==null) player=GetComponent<Animation>();
            if(fighter!=null&&!fighter.Alive)
            {
                if(!dying) { dying=true;player.Play("Death"); }
                deathTime+=dt;return;
            }
            if(dying) return;
            if(fighter!=null&&fighter.AttackCount!=attackCount)
            {
                attackCount=fighter.AttackCount;player.CrossFade(fighter.UsedSkill?"Special":"Attack",.06f);lockedUntil=Time.time+.45f;
            }
            else if(fighter!=null&&fighter.HitCount!=hitCount)
            {
                hitCount=fighter.HitCount;player.CrossFade("Hit",.04f);lockedUntil=Time.time+.22f;
            }
            else if(Time.time>=lockedUntil) player.CrossFade(moving?"Walk":"Idle",.14f);
        }
    }
}
