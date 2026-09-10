using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalArena
{
    public sealed class ArenaBattle
    {
        public sealed class Fighter
        {
            public int Tier;
            public int AttackCount, HitCount;
            public DigimonData Data;
            public float Sp, LastDamage;
            public bool UsedSkill;
            public bool Enemy;
            public float X, Y, Hp, MaxHp, Damage, Cooldown, Flash;
            public Fighter Target;
            public bool Alive => Hp > 0;
        }
        public readonly List<Fighter> Fighters = new List<Fighter>();
        readonly ArenaTrain train;
        readonly ArenaRules rules;
        public const float Duration=30;
        public bool Finished { get; private set; }
        public bool Won { get; private set; }
        public float Time { get; private set; }
        public int Survivors => Fighters.Count(f => f.Enemy && f.Alive);
        public static int EnemyTier(int stage) => Math.Min(6, stage - 1);
        public static int EnemyCount(int stage, int round) => Math.Min(8, 1 + (round - 1) / 3 + (stage - 1) / 3);
        public static DigimonData CreateData(ArenaRules rules,int tier,bool enemy)
        {
            var data=rules.Catalog.Get(tier,enemy).Copy();
            float scale=enemy?0.58f*(1+(rules.Stage-1)*0.1f+(rules.StageRound-1)*0.045f):1;
            data.HP*=scale;data.ATK*=scale;data.INT*=scale;
            return data;
        }
        public ArenaBattle(ArenaRules rules)
        {
            train=rules.Train; this.rules=rules;
            foreach (var p in rules.Pieces.Where(p => p.Cell >= 0))
                Add(p.Tier, false, p.Cell % ArenaRules.Columns, 3 - p.Cell / ArenaRules.Columns);

            for (int i = 0; i < EnemyCount(rules.Stage, rules.StageRound); i++)
                Add(rules.Catalog.EnemyAt(rules.Stage,i), true, i % 8, 6 + i / 8);
        }
        void Add(int tier, bool enemy, float x, float y)
        {
            var data=CreateData(rules,tier,enemy);
            Fighters.Add(new Fighter { Tier=tier,Enemy=enemy,X=x,Y=y,Data=data,Hp=data.HP,MaxHp=data.HP,
                Damage=data.attack==AttackKind.Physical?data.ATK:data.INT,Cooldown=0.3f+Fighters.Count*0.06f });
        }
        public void Tick(float dt)
        {
            if (Finished || dt <= 0) return;
            dt=Math.Min(dt,Duration-Time); Time += dt;
            foreach (var f in Fighters)
            {
                f.Flash = Math.Max(0, f.Flash - dt);
                if (!f.Alive) continue;
                f.Cooldown -= dt;
                var target = Fighters.Where(t => t.Alive && t.Enemy != f.Enemy)
                    .OrderBy(t => DistanceSquared(f, t)).FirstOrDefault();
                if (target == null) continue;
                float dx = target.X - f.X, dy = target.Y - f.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                float range = f.Data.range;
                bool blocked=train.Blocks(f.X,f.Y,target.X,target.Y);
                // A small tolerance avoids sub-pixel movement stalling at attack range on Mono.
                if (distance > range + 0.015f || blocked)
                {
                    train.Waypoint(f.X,f.Y,target.X,target.Y,out float wx,out float wy);
                    float mx=wx-f.X,my=wy-f.Y, length=(float)Math.Sqrt(mx*mx+my*my);
                    float step=Math.Min(blocked?length:Math.Max(0,length-range),dt*f.Data.SPD);
                    if(length>0.00001f) { f.X+=mx/length*step; f.Y+=my/length*step; }
                }
                else if (f.Cooldown <= 0)
                {
                    f.UsedSkill=f.Sp>=f.Data.SP;
                    f.LastDamage=DigimonDamage.Calculate(f.Data,target.Data,f.UsedSkill?f.Data.skillPower:1);
                    target.Hp = Math.Max(0, target.Hp - f.LastDamage);
                    f.AttackCount++;target.HitCount++;
                    f.Sp=f.UsedSkill?0:Math.Min(f.Data.SP,f.Sp+f.Data.spPerAttack);
                    f.Cooldown = f.Data.modelTier >= 5 ? 0.7f : 0.95f;
                    f.Flash = 0.22f; f.Target = target;
                }
            }
            bool allyAlive = Fighters.Any(f => !f.Enemy && f.Alive);
            bool enemyAlive = Fighters.Any(f => f.Enemy && f.Alive);
            if (!allyAlive || !enemyAlive || Time >= Duration)
            { Finished = true; Won = allyAlive && !enemyAlive; }
        }
        static float DistanceSquared(Fighter a, Fighter b) => (a.X-b.X)*(a.X-b.X) + (a.Y-b.Y)*(a.Y-b.Y);
    }
}
