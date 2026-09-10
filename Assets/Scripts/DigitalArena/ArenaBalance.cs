using System;
using System.Linq;

namespace DigitalArena
{
    [Serializable]
    public sealed class ArenaBalance
    {
        [Serializable] public sealed class LevelRow { public int level; public int[] weights; public int requiredXp = 10; }
        public int[] poolCounts = { 60, 45, 35, 25, 18 };
        public LevelRow[] levels;
        public ArenaBalance()
        {
            int[][] odds={new[]{65,25,10,0,0},new[]{50,30,18,2,0},new[]{35,35,23,7,0},new[]{25,30,30,13,2},
                new[]{18,25,32,20,5},new[]{12,20,30,28,10},new[]{8,15,27,32,18},new[]{5,10,20,35,30}};
            levels=Enumerable.Range(1,32).Select(i=>new LevelRow{level=i,weights=(int[])odds[Math.Min(i-1,7)].Clone()}).ToArray();
        }
        public string Validate()
        {
            if(poolCounts==null || poolCounts.Length!=5 || poolCounts.Any(n=>n<0 || n>100000)) return "코스트별 수량은 0~100000의 정수 5개여야 합니다.";
            if(levels==null || levels.Length!=32) return "레벨 1~32의 행이 필요합니다.";
            for(int i=0;i<32;i++)
            {
                if(levels[i]==null || levels[i].level!=i+1 || levels[i].weights==null || levels[i].weights.Length!=5
                    || levels[i].weights.Any(w=>w<0 || w>100) || levels[i].weights.Sum()!=100)
                    return "레벨 "+(i+1)+"의 확률은 0~100이며 합계 100%여야 합니다.";
                if(levels[i].requiredXp<1 || levels[i].requiredXp>1000000)
                    return "레벨 "+(i+1)+"의 필요 XP는 1~1000000이어야 합니다.";
            }
            return null;
        }
        public ArenaBalance Copy()
        {
            return new ArenaBalance{poolCounts=(int[])poolCounts.Clone(),levels=levels.Select(r=>new LevelRow{level=r.level,weights=(int[])r.weights.Clone(),requiredXp=r.requiredXp}).ToArray()};
        }
    }
}
