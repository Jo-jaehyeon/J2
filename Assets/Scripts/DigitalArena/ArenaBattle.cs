using System;
using System.Collections.Generic;
using System.Linq;
namespace DigitalArena
{
    public sealed class ArenaBattle
    {
        public sealed class Fighter
        {
            public int Tier, AttackCount, HitCount;
            public DigimonData Data;
            public float Sp, LastDamage;
            public bool UsedSkill, Enemy;
            public float X,Y,Hp,MaxHp,Damage,Cooldown,Flash;
            public int Cell, NextCell=-1;
            public float MoveProgress;
            public Fighter Target;
            public bool Alive=>Hp>0;
            public bool Moving=>NextCell>=0;
        }
        public readonly List<Fighter> Fighters=new List<Fighter>();
        readonly ArenaTrain train;
        public const float Duration=30;
        public bool Finished {get;private set;}
        public bool Won {get;private set;}
        public float Time {get;private set;}
        public int Survivors=>Fighters.Count(f=>f.Enemy&&f.Alive);
        public static int EnemyCount(int stage,int round)=>Math.Min(8,1+(round-1)/3+(stage-1)/3);
        public static int WorldRow(int cell) {int row=cell/8;return row<4?row:row+1;}
        public static int TileDistance(int a,int b)=>Math.Max(Math.Abs(a%8-b%8),Math.Abs(a/8-b/8));
        public static DigimonData CreateData(ArenaRules rules,int tier,bool enemy)
        {
            var data=rules.Catalog.Get(tier,enemy).Copy();
            float scale=enemy?.58f*(1+(rules.Stage-1)*.1f+(rules.StageRound-1)*.045f):1;
            data.HP*=scale;data.ATK*=scale;data.INT*=scale;return data;
        }
        public ArenaBattle(ArenaRules rules)
        {
            train=rules.Train;
            foreach(var piece in rules.Pieces.Where(p=>p.Cell>=0)) Add(rules,piece.Tier,false,(3-piece.Cell/8)*8+piece.Cell%8);
            for(int i=0;i<EnemyCount(rules.Stage,rules.StageRound);i++) Add(rules,rules.Catalog.EnemyAt(rules.Stage,i),true,32+i);
        }
        void Add(ArenaRules rules,int tier,bool enemy,int cell)
        {
            var d=CreateData(rules,tier,enemy);
            Fighters.Add(new Fighter{Tier=tier,Enemy=enemy,Cell=cell,X=cell%8,Y=WorldRow(cell),Data=d,Hp=d.HP,MaxHp=d.HP,Damage=d.attack==AttackKind.Physical?d.ATK:d.INT,Cooldown=.3f+Fighters.Count*.06f});
        }
        public bool Occupied(int cell,Fighter except=null)=>Fighters.Any(f=>f!=except&&f.Alive&&(f.Cell==cell||f.NextCell==cell));
        bool Clear(int a,int b)=>!train.Blocks(a%8,WorldRow(a),b%8,WorldRow(b));
        public bool InRange(int cell,Fighter target,int range)=>TileDistance(cell,target.Cell)<=range&&Clear(cell,target.Cell);
        bool CanStep(int from,int to,Fighter unit)
        {
            if(Occupied(to,unit)||!Clear(from,to))return false;
            if(from%8!=to%8&&from/8!=to/8)
            {
                int sideA=(from/8)*8+to%8,sideB=(to/8)*8+from%8;
                if(Occupied(sideA,unit)||Occupied(sideB,unit)||!Clear(from,sideA)||!Clear(from,sideB)||!Clear(sideA,to)||!Clear(sideB,to))return false;
            }
            return true;
        }
        int FindStep(Fighter unit,Fighter[] enemies)
        {
            var previous=Enumerable.Repeat(-1,64).ToArray();var queue=new Queue<int>();
            queue.Enqueue(unit.Cell);previous[unit.Cell]=unit.Cell;
            while(queue.Count>0)
            {
                int cell=queue.Dequeue();
                var victim=enemies.FirstOrDefault(e=>InRange(cell,e,unit.Data.range));
                if(cell!=unit.Cell&&victim!=null)
                {
                    unit.Target=victim;while(previous[cell]!=unit.Cell)cell=previous[cell];return cell;
                }
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int x=cell%8+dx,y=cell/8+dy;
                    if(dx==0&&dy==0||x<0||x>=8||y<0||y>=8)continue;
                    int next=y*8+x;if(previous[next]>=0||!CanStep(cell,next,unit))continue;
                    previous[next]=cell;queue.Enqueue(next);
                }
            }
            return -1;
        }
        public void Tick(float dt)
        {
            if(Finished||dt<=0)return;
            dt=Math.Min(dt,Duration-Time);Time+=dt;
            foreach(var f in Fighters)
            {
                f.Flash=Math.Max(0,f.Flash-dt);f.Cooldown-=dt;
                if(!f.Alive){f.NextCell=-1;continue;}
                if(!f.Moving)continue;
                f.MoveProgress=Math.Min(1,f.MoveProgress+dt*f.Data.SPD);
                f.X=f.Cell%8+(f.NextCell%8-f.Cell%8)*f.MoveProgress;
                f.Y=WorldRow(f.Cell)+(WorldRow(f.NextCell)-WorldRow(f.Cell))*f.MoveProgress;
                if(f.MoveProgress>=1){f.Cell=f.NextCell;f.NextCell=-1;}
            }
            foreach(var f in Fighters)
            {
                if(!f.Alive||f.Moving)continue;
                var enemies=Fighters.Where(t=>t.Alive&&t.Enemy!=f.Enemy).OrderBy(t=>TileDistance(f.Cell,t.Cell)).ToArray();
                var target=enemies.FirstOrDefault(t=>!t.Moving&&InRange(f.Cell,t,f.Data.range));
                if(target!=null)
                {
                    f.Target=target;
                    if(f.Cooldown>0)continue;
                    f.UsedSkill=f.Sp>=f.Data.SP;
                    f.LastDamage=DigimonDamage.Calculate(f.Data,target.Data,f.UsedSkill?f.Data.skillPower:1);
                    target.Hp=Math.Max(0,target.Hp-f.LastDamage);f.AttackCount++;target.HitCount++;
                    f.Sp=f.UsedSkill?0:Math.Min(f.Data.SP,f.Sp+f.Data.spPerAttack);
                    f.Cooldown=f.Data.modelTier>=5?.7f:.95f;f.Flash=.22f;
                }
                else if(f.Data.SPD>0)
                {
                    if(enemies.Any(t=>InRange(f.Cell,t,f.Data.range)))continue;
                    int next=FindStep(f,enemies);
                    if(next>=0){f.NextCell=next;f.MoveProgress=0;}
                }
            }
            bool ally=Fighters.Any(f=>f.Alive&&!f.Enemy),enemy=Fighters.Any(f=>f.Alive&&f.Enemy);
            if(!ally||!enemy||Time>=Duration){Finished=true;Won=ally&&!enemy;}
        }
    }
}
