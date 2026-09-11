using System;
using System.Linq;

namespace DigitalArena
{
    public enum AttackKind { Physical, Special }
    public enum DigimonType { Vaccine, Virus, Data, Unknown, Free, None }
    public enum DigimonElement { Fire, Water, Grass, Electric, Wind, Earth, Light, Dark, Neutral }
    [Serializable]
    public sealed class DigimonData
    {
        public string id="", name="", evolvesTo="", prefabPath="";
        public int cost=1, modelTier, startStage=1, range=1;
        public float HP=90, SP=100, ATK=15, DEF=10, INT=15, SPD=1.25f;
        public AttackKind attack;
        public DigimonType type;
        public DigimonElement element;
        public float spPerAttack=20, skillPower=2;
        public DigimonData Copy() => (DigimonData)MemberwiseClone();
    }
    [Serializable]
    public sealed class DigimonCatalog
    {
        public DigimonData[] allies=Defaults(false), enemies=Defaults(true);
        public static readonly string[] TypeNames={"백신","바이러스","데이터","언노운","프리","속성없음"};
        public static readonly string[] ElementNames={"불","물","풀","전기","바람","땅","빛","어둠","무"};
        public DigimonData Get(int tier,bool enemy) => (enemy?enemies:allies)[tier];
        public int Evolution(int index) => Array.FindIndex(allies,d=>d.id==allies[index].evolvesTo);
        public int EnemyAt(int stage,int slot=0)
        {
            int best=enemies.Where(d=>d.startStage<=stage).Max(d=>d.startStage);
            var candidates=Enumerable.Range(0,enemies.Length).Where(i=>enemies[i].startStage==best).ToArray();
            return candidates[slot%candidates.Length];
        }
        static DigimonData[] Defaults(bool enemy) => Enumerable.Range(0,enemy?7:6).Select(i=>new DigimonData {
            id=(enemy?"enemy_":"ally_")+i,name=(enemy?ArenaRules.EnemyNames:ArenaRules.Names)[i],
            cost=enemy?0:i<5?i+1:0,modelTier=i,startStage=enemy?i+1:1,evolvesTo=!enemy&&i<5?"ally_"+(i+1):"",
            HP=(float)(90*Math.Pow(2.1,i)), ATK=(float)(15*Math.Pow(2,i)), INT=(float)(15*Math.Pow(2,i)),
            type=enemy?DigimonType.Unknown:i<2?DigimonType.Free:DigimonType.Vaccine,
            element=enemy?DigimonElement.Dark:i<2?DigimonElement.Neutral:DigimonElement.Fire,
            attack=enemy?AttackKind.Special:AttackKind.Physical, range=i>=4?3:1
        }).ToArray();
        public DigimonCatalog Copy() => new DigimonCatalog { allies=allies.Select(d=>d.Copy()).ToArray(),enemies=enemies.Select(d=>d.Copy()).ToArray() };
        public string Validate()
        {
            if(allies==null || enemies==null || allies.Length==0 || enemies.Length==0) return "아군과 크립은 각각 최소 1종이 필요합니다.";
            if(allies.Concat(enemies).Any(d=>d==null)) return "비어 있는 기물 데이터입니다.";
            if(allies.Concat(enemies).Select(d=>d.id).Distinct().Count()!=allies.Length+enemies.Length) return "기물 ID는 중복될 수 없습니다.";
            if(!enemies.Any(d=>d.startStage==1)) return "스테이지 1에서 등장하는 크립이 필요합니다.";
            foreach(var d in allies.Concat(enemies))
            {
                if(d==null) return "비어 있는 기물 데이터입니다.";
                if(string.IsNullOrWhiteSpace(d.id)||string.IsNullOrWhiteSpace(d.name)) return "ID와 이름을 입력하세요.";
                if(d.cost<0||d.cost>5||d.modelTier<0||d.modelTier>6||d.startStage<1) return "코스트 0~5, 기본 외형 0~6, 등장 스테이지 1 이상이어야 합니다.";
                if(d.range<1||d.range>4) return "Attack range must be an integer from 1 to 4.";
                var values=new[]{d.HP,d.SP,d.ATK,d.DEF,d.INT,d.SPD,d.range,d.spPerAttack,d.skillPower};
                if(values.Any(v=>float.IsNaN(v)||float.IsInfinity(v)||v<0||v>1000000) || d.HP<=0 || d.SP<=0 || d.range<=0 || d.skillPower<=0)
                    return "수치는 0~1,000,000, HP·SP·사거리·스킬 배율은 0보다 커야 합니다.";
                if(!Enum.IsDefined(typeof(AttackKind),d.attack)||!Enum.IsDefined(typeof(DigimonType),d.type)||!Enum.IsDefined(typeof(DigimonElement),d.element)) return "유효하지 않은 분류입니다.";
            }
            foreach(var origin in allies)
            {
                var seen=new System.Collections.Generic.HashSet<string>();var current=origin;
                while(!string.IsNullOrEmpty(current.evolvesTo))
                {
                    if(!seen.Add(current.id)) return "진화 대상이 순환합니다: "+origin.name;
                    current=allies.FirstOrDefault(d=>d.id==current.evolvesTo);
                    if(current==null) return "존재하지 않는 진화 대상: "+origin.name;
                }
            }
            return null;
        }
    }
    public static class DigimonDamage
    {
        public static float TypeMultiplier(DigimonType a,DigimonType b)
        {
            if(a==DigimonType.Free || b==DigimonType.Free) return 1;
            if((a==DigimonType.Vaccine&&b==DigimonType.Virus)||(a==DigimonType.Virus&&b==DigimonType.Data)||(a==DigimonType.Data&&b==DigimonType.Vaccine)) return 2;
            if((a==DigimonType.Unknown&&(int)b<3)||((int)a<3&&b==DigimonType.None)) return 1.1f;
            return 1;
        }
        public static float ElementMultiplier(DigimonElement a,DigimonElement b)
        {
            int[] advantage={2,0,1,4,5,3,7,6,-1};
            return advantage[(int)a]==(int)b?1.5f:1;
        }
        public static float Calculate(DigimonData source,DigimonData target,float power=1)
            => (source.attack==AttackKind.Physical?source.ATK:source.INT)*power*(100f/(100f+target.DEF))*TypeMultiplier(source.type,target.type)*ElementMultiplier(source.element,target.element);
    }
}
