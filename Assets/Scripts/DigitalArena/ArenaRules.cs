using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalArena
{
    // Engine-independent rules: all random draws use a seed for reproducible runs.
    public sealed class ArenaRules
    {
        public static readonly string[] Names = { "코로몬", "아구몬", "그레이몬", "메탈그레이몬", "워그레이몬" };
        public static readonly string[] EnemyNames = { "츠메몬", "케라몬", "크리사리몬", "인펠몬", "디아블로몬", "아마게몬" };
        public static readonly string[] EvolutionNames = { "유년기", "성장기", "성숙기", "완전체", "궁극체" };
        public static readonly int[] Costs = { 1, 3, 9, 27, 81 };
        public static int EvolutionRank(DigimonData data) => data.cost>0?data.cost:Math.Max(1,Math.Min(5,data.modelTier));
        public static int PurchasePrice(DigimonData data) => Costs[EvolutionRank(data)-1];
        public static int SalePrice(DigimonData data) => PurchasePrice(data)-(EvolutionRank(data)==1?0:Costs[EvolutionRank(data)-2]);
        public readonly ArenaBalance Balance;
        public readonly DigimonCatalog Catalog;
        public readonly ArenaTrain Train = new ArenaTrain();
        public readonly int[] RemainingPool;
        public sealed class Piece
        {
            public int Id, Tier, Cell = -1, BenchSlot = -1;
        }
        public readonly List<Piece> Pieces = new List<Piece>();
        public readonly int[] Offers = { -1,-1,-1,-1,-1 };
        public readonly bool[] Bought = new bool[5];
        public int Gold { get; private set; } = 0;
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int RequiredXp => Level >= MaxLevel ? 0 : Balance.levels[Level-1].requiredXp;
        public int Health { get; private set; } = 100;
        public int Round { get; private set; }
        public int Stage => (Round - 1) / 8 + 1;
        public int StageRound => (Round - 1) % 8 + 1;
        public int LastIncome { get; private set; }
        public int LastInterest { get; private set; }
        public int WinStreak { get; private set; }
        public int LastVictoryGold { get; private set; }
        public int LastStreakGold { get; private set; }
        public int Interest => Math.Min(5,Gold/10);
        public int PurchaseCount => Bought.Count(b=>b);
        public bool Preparing { get; private set; }
        public int LastBattleXp { get; private set; }
        public string Notice { get; private set; } = "기물을 선택한 뒤 전장에 배치하세요.";
        public const int MaxLevel = 32;
        public const int Columns = 8;
        public const int BoardSize = 32;
        public const int BenchSize = 10;
        readonly Random random;
        int nextId;
        bool battleResolved;
        public ArenaRules(int seed, ArenaBalance balance = null, DigimonCatalog catalog = null)
        {
            string catalogError=(catalog??new DigimonCatalog()).Validate(); if(catalogError!=null) throw new ArgumentException(catalogError);
            Catalog=(catalog??new DigimonCatalog()).Copy();
            random = new Random(seed); Balance=(balance??new ArenaBalance()).Copy();
            string error=Balance.Validate(); if(error!=null) throw new ArgumentException(error);
            RemainingPool=(int[])Balance.poolCounts.Clone();
        }
        public void BeginRound()
        {
            if (Health <= 0 || Preparing) return;
            for(int i=0;i<5;i++) if(Offers[i]>=0 && !Bought[i]) RemainingPool[Catalog.allies[Offers[i]].cost-1]++;
            Round++; Train.Advance(Round);
            LastInterest = Interest;
            LastIncome = (Stage==1?2:5) + LastInterest;
            Gold = (int)Math.Min(int.MaxValue,(long)Gold+LastIncome);
            Array.Clear(Bought,0,5);
            LastBattleXp = 0;
            Preparing = true;
            for (int i = 0; i < Offers.Length; i++) Offers[i] = DrawTier();
            Notice = "라운드 수입 +" + LastIncome + "pt · 포인트 내에서 후보 5개까지 구매 가능";
        }
        int DrawTier()
        {
            var candidates=Enumerable.Range(0,5).Select(c=>Enumerable.Range(0,Catalog.allies.Length).Where(i=>Catalog.allies[i].cost==c+1).ToArray()).ToArray();
            int total=Enumerable.Range(0,5).Where(c=>RemainingPool[c]>0&&candidates[c].Length>0).Sum(c=>Balance.levels[Level-1].weights[c]);
            if(total==0) return -1;
            int roll=random.Next(total);
            for(int c=0;c<5;c++)
            {
                if(RemainingPool[c]<=0||candidates[c].Length==0) continue;
                roll-=Balance.levels[Level-1].weights[c];
                if(roll<0) { RemainingPool[c]--;return candidates[c][candidates[c].Length==1?0:random.Next(candidates[c].Length)]; }
            }
            return -1;
        }
        public bool Buy(int index)
        {
            if (!Preparing || index < 0 || index >= 5 || Bought[index]) return false;
            int tier = Offers[index];
            if(tier<0) return false;
            if (Gold < PurchasePrice(Catalog.allies[tier])) { Notice = "골드가 부족합니다."; return false; }
            if (Pieces.Count(p => p.Cell < 0) >= BenchSize && (Catalog.Evolution(tier)<0 || Pieces.Count(p => p.Tier == tier) < 2))
            { Notice = "대기석이 가득 찼습니다. 기물을 전장에 배치하세요."; return false; }
            Gold -= PurchasePrice(Catalog.allies[tier]);
            Bought[index] = true;
            Pieces.Add(new Piece { Id = ++nextId, Tier = tier, BenchSlot=FirstBenchSlot() });
            Notice = Catalog.allies[tier].name + " 합류 · " + PurchaseCount + " / 5 구매";
            Merge();
            return true;
        }
        public bool Sell(int id)
        {
            if(!Preparing || Health<=0) return false;
            var piece=Pieces.Find(p=>p.Id==id);
            if(piece==null) return false;
            var data=Catalog.allies[piece.Tier];
            int price=SalePrice(data);
            Pieces.Remove(piece);
            Gold=(int)Math.Min(int.MaxValue,(long)Gold+price);
            Notice=data.name+" 판매 · +"+price+" 골드";
            return true;
        }
        void Merge()
        {
            bool changed;
            do { changed=false;
            for (int tier = 0; tier < Catalog.allies.Length; tier++)
            {
                int next=Catalog.Evolution(tier);
                if(next<0) continue;
                while (Pieces.Count(p => p.Tier == tier) >= 3)
                {
                    var group = Pieces.Where(p => p.Tier == tier).OrderByDescending(p => p.Cell >= 0).Take(3).ToArray();
                    changed=true;
                    int cell = group[0].Cell;
                    foreach (var piece in group) Pieces.Remove(piece);
                    Pieces.Add(new Piece { Id = ++nextId, Tier = next, Cell = cell, BenchSlot=cell<0?FirstBenchSlot():-1 });
                    Notice = "진화! " + Catalog.allies[tier].name + " ×3 → " + Catalog.allies[next].name;
                }
            }
            } while(changed);
        }
        public bool BuyXp(int amount = 4)
        {
            if (!Preparing || Gold < 4 || Level >= MaxLevel || amount <= 0) return false;
            Gold -= 4;
            GrantXp(amount);
            Notice = "경험치 +" + amount + " · 배치 가능 " + Level + "기";
            return true;
        }
        void GrantXp(int amount)
        {
            if (Level >= MaxLevel) return;
            long total=(long)Xp+amount;
            while (Level < MaxLevel && total >= RequiredXp) { total -= RequiredXp; Level++; }
            Xp=(int)total;
            if (Level == MaxLevel) Xp = 0;
        }
        public bool AutoDeploy(int id)
        {
            if (!Preparing) return false;
            var piece = Pieces.Find(p => p.Id == id);
            if (piece == null || piece.Cell >= 0) return false;
            for (int c = 0; c < BoardSize; c++)
                if (Pieces.All(p => p.Cell != c)) return Place(id, c);
            return false;
        }
        public bool Place(int id, int cell)
        {
            if (!Preparing || cell < -1 || cell >= BoardSize) return false;
            if(cell==-1) return MoveToBench(id,FirstBenchSlot());
            var piece = Pieces.Find(p => p.Id == id);
            if (piece == null) return false;
            var occupant = Pieces.Find(p => p.Cell == cell && cell >= 0);
            if (occupant == piece) return true;
            if (cell >= 0 && piece.Cell < 0 && occupant == null && Pieces.Count(p => p.Cell >= 0) >= Level)
            { Notice = "배치 한도입니다. XP를 구매해 레벨을 올리세요."; return false; }
            if (occupant != null) { occupant.Cell = piece.Cell; occupant.BenchSlot=piece.BenchSlot; }
            piece.Cell = cell; piece.BenchSlot=-1;
            return true;
        }
        int FirstBenchSlot()
        {
            for(int i=0;i<BenchSize;i++) if(Pieces.All(p=>p.Cell>=0 || p.BenchSlot!=i)) return i;
            return -1;
        }
        public bool MoveToBench(int id,int slot)
        {
            if(!Preparing || slot<0 || slot>=BenchSize) return false;
            var piece=Pieces.Find(p=>p.Id==id); if(piece==null) return false;
            var occupant=Pieces.Find(p=>p.Cell<0 && p.BenchSlot==slot);
            if(occupant==piece) return true;
            if(occupant!=null) { occupant.Cell=piece.Cell; occupant.BenchSlot=piece.BenchSlot; }
            piece.Cell=-1;piece.BenchSlot=slot;return true;
        }
        public void StartBattle()
        {
            if (!Preparing || Health <= 0) return;
            Preparing = false;
            battleResolved = false;
        }
        public void ResolveBattle(bool won, int survivors)
        {
            if (Round <= 0 || Preparing || battleResolved || Health <= 0) return;
            battleResolved = true;
            WinStreak=won?WinStreak+1:0;
            LastVictoryGold=won?1:0;
            LastStreakGold=won&&WinStreak>=3?1:0;
            Gold=(int)Math.Min(int.MaxValue,(long)Gold+LastVictoryGold+LastStreakGold);
            LastBattleXp = Level < MaxLevel ? 2 : 0;
            GrantXp(LastBattleXp);
            if (won) Notice = "전투 승리! 다음 라운드를 준비하세요.";
            else
            {
                int damage = 2 * Stage * Math.Max(0, survivors);
                Health = Math.Max(0, Health - damage);
                Notice = "전투 패배 · 남은 크립 " + Math.Max(0, survivors) + "기 × 스테이지 " + Stage + " × 2 = 체력 -" + damage;
            }
            Notice += LastBattleXp > 0 ? " · 기본 경험치 +2 XP" : " · 최대 레벨";
            if(won) Notice+=" · 승리 +1 골드"+(LastStreakGold>0?" · "+WinStreak+"연승 +1 골드":"");
        }
    }
}

