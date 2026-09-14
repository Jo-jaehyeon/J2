using System;
using System.Linq;
using DigitalArena;
#if !ARENA_HEADLESS
using UnityEditor;
using UnityEngine;
#endif

public static class DigitalArenaValidation
{
#if ARENA_HEADLESS
    public static void Main() { Run(); }
#else
    [MenuItem("Digital Arena/Validate Prototype Rules")]
#endif
    public static void Run()
    {
        int checks=0;
        void Check(bool condition,string description) { if(!condition) throw new Exception("Arena validation failed: "+description); checks++; }
        ArenaBalance TierBalance(int tier,int count=60)
        {
            var b=new ArenaBalance();b.poolCounts=Enumerable.Repeat(count,5).ToArray();
            foreach(var row in b.levels) { row.weights=new int[5];row.weights[tier]=100; } return b;
        }
        DigimonCatalog BaseCatalog() { var c=new DigimonCatalog();c.allies=c.allies.Take(5).ToArray();return c; }
        ArenaRules Game(int tier=0,int count=60) { var r=new ArenaRules(100,TierBalance(tier,count),BaseCatalog());r.BeginRound();return r; }
        void Round(ArenaRules r,bool won=true,int survivors=0) { r.StartBattle();r.ResolveBattle(won,survivors);r.BeginRound(); }
        var game=Game();
        var auto=Game();
        auto.Pieces.Add(new ArenaRules.Piece{Id=100,Tier=0,BenchSlot=8});
        auto.Pieces.Add(new ArenaRules.Piece{Id=101,Tier=0,BenchSlot=2});
        auto.StartBattle();
        Check(auto.Pieces[1].Cell==0&&auto.Pieces[0].Cell==-1,"battle fills limit from leftmost bench slot, not purchase order");
        auto.StartBattle();Check(auto.Pieces.Count(p=>p.Cell>=0)==1,"repeated battle start cannot overdeploy");
        var autoMulti=Game();for(int i=0;i<10;i++) Round(autoMulti);
        autoMulti.Pieces.Add(new ArenaRules.Piece{Id=100,Tier=0,Cell=0});
        autoMulti.Pieces.Add(new ArenaRules.Piece{Id=101,Tier=0,BenchSlot=9});
        autoMulti.Pieces.Add(new ArenaRules.Piece{Id=102,Tier=0,BenchSlot=4});
        autoMulti.Pieces.Add(new ArenaRules.Piece{Id=103,Tier=0,BenchSlot=1});
        autoMulti.StartBattle();
        Check(autoMulti.Pieces[0].Cell==0&&autoMulti.Pieces[3].Cell==1&&autoMulti.Pieces[2].Cell==2&&autoMulti.Pieces[1].Cell==-1,"fills remaining slots in bench order without moving deployed units");
        var autoSparse=Game();for(int i=0;i<10;i++) Round(autoSparse);
        autoSparse.Pieces.Add(new ArenaRules.Piece{Id=100,Tier=0,BenchSlot=9});autoSparse.StartBattle();
        Check(autoSparse.Pieces[0].Cell==0,"partial bench deploys available piece only");
        var autoEmpty=Game();autoEmpty.StartBattle();Check(!autoEmpty.Preparing&&autoEmpty.Pieces.Count==0,"empty bench still starts battle");
        Check(game.Health==100 && game.Level==1 && game.Gold==2,"initial health, level and income");
        game.BeginRound();Check(game.Round==1 && game.Gold==2,"duplicate income blocked");
        Round(game);
        for(int i=0;i<5;i++) Check(game.Buy(i),"purchase candidate "+i);
        Check(game.PurchaseCount==5 && game.Gold==0,"all five candidates can be purchased");
        Check(!game.Buy(0) && !game.Buy(5),"sold slot and sixth purchase blocked");
        Check(game.Pieces.Count==3 && game.Pieces.Count(p=>p.Stars==2)==1,"automatic same-unit star upgrade");
        Check(game.Pieces.Select(p=>p.BenchSlot).Distinct().Count()==game.Pieces.Count,"merge retains unique bench slots");
        var p=game.Pieces[0];var q=game.Pieces[1];int oldP=p.BenchSlot,oldQ=q.BenchSlot;
        Check(game.MoveToBench(p.Id,oldQ) && q.BenchSlot==oldP && p.BenchSlot==oldQ,"bench slot swap");
        Check(game.MoveToBench(p.Id,9) && p.BenchSlot==9,"move into empty bench slot");
        Check(game.AutoDeploy(p.Id) && p.Cell==0,"double click targets chosen piece");
        Check(!game.AutoDeploy(q.Id),"deployment level cap");
        Check(game.Place(p.Id,31),"last of 32 field slots is valid");
        Check(!game.Place(p.Id,32),"field bounds");
        Check(game.Place(q.Id,31) && p.Cell<0 && p.BenchSlot>=0,"swap field with bench at cap");
        game.StartBattle();Check(!game.MoveToBench(q.Id,0) && !game.AutoDeploy(p.Id) && !game.BuyXp(),"battle locks preparation");
        game.ResolveBattle(true,0);Check(game.Xp==4,"battle XP");game.ResolveBattle(false,5);
        Check(game.Xp==4 && game.Health==100,"duplicate results ignored");
        for(int i=0;i<6;i++) { game.BeginRound();game.StartBattle(); }
        Check(game.Stage==1 && game.StageRound==8,"eight rounds per stage");game.BeginRound();Check(game.Stage==2 && game.StageRound==1,"stage advance");
        var xp=Game();Round(xp);Round(xp);Check(xp.BuyXp()&&xp.BuyXp() && xp.Level==2 && xp.Xp==2 && xp.Gold==0,"paid XP costs four"); Round(xp); Round(xp); Check(xp.BuyXp() && xp.Level==3 && xp.Xp==0,"paid and passive XP overflow");
        var passive=Game();for(int i=0;i<5;i++) Round(passive);
        var xpTable=TierBalance(0);xpTable.levels[0].requiredXp=3;xpTable.levels[1].requiredXp=7;
        var variableXp=new ArenaRules(1,xpTable);variableXp.BeginRound();variableXp.StartBattle();variableXp.BeginRound();
        Check(variableXp.BuyXp()&&variableXp.Level==2&&variableXp.Xp==1&&variableXp.RequiredXp==7,"table XP purchase and carry");
        Round(variableXp);variableXp.StartBattle();variableXp.BeginRound();Check(variableXp.Xp==3&&variableXp.Level==2,"passive XP uses same table");
        Check(variableXp.BuyXp()&&variableXp.Level==3&&variableXp.Xp==0&&variableXp.RequiredXp==10,"next level has different XP threshold");
        xpTable.levels[0].requiredXp=1;xpTable.levels[1].requiredXp=1;xpTable.levels[2].requiredXp=2;
        var multiXp=new ArenaRules(1,xpTable);multiXp.BeginRound();multiXp.StartBattle();multiXp.BeginRound();multiXp.BuyXp();
        Check(multiXp.Level==4&&multiXp.Xp==0,"one purchase crosses multiple thresholds");
        xpTable.levels[0].requiredXp=0;Check(xpTable.Validate()!=null,"zero required XP rejected");
        xpTable.levels[0].requiredXp=-1;Check(xpTable.Validate()!=null,"negative required XP rejected");
        xpTable.levels[0].requiredXp=1000001;Check(xpTable.Validate()!=null,"excessive required XP rejected");
        Check(variableXp.Balance.levels[0].requiredXp==3,"XP table snapshot isolated from edits");
        Check(passive.Level==2 && passive.Xp==0,"passive XP levels player");
        for(int i=0;i<180;i++) Round(passive);
        Check(passive.Level==32 && passive.Xp==0,"32-level cap");
        Check(passive.RequiredXp==0&&!passive.BuyXp(),"max level has no next threshold");
        var stock=Game(0,3);Check(stock.Offers.Count(t=>t==0)==3 && stock.Offers.Count(t=>t==-1)==2,"limited pool creates empty slots");
        Check(stock.Buy(0),"purchase reserved stock");Round(stock);
        Check(stock.Offers.Count(t=>t==0)==2,"unsold reservations returned, purchased stock consumed");
        stock.Buy(0);stock.Buy(1);Round(stock);
        Check(stock.Offers.All(t=>t==-1),"exhausted pool stays empty");
        var empty=Game(0,0);Check(empty.Offers.All(t=>t==-1) && !empty.Buy(0),"zero inventory safe");
        var reroll=Game(0,5);
        Check(reroll.CanReroll&&reroll.Reroll()&&reroll.Gold==0,"reroll costs exactly two gold");
        Check(reroll.Offers.All(t=>t==0)&&reroll.RemainingPool[0]==0,"reroll returns all reservations before drawing from exhausted pool");
        var oldOffers=(int[])reroll.Offers.Clone();var oldStock=(int[])reroll.RemainingPool.Clone();
        Check(!reroll.CanReroll&&!reroll.Reroll()&&reroll.Gold==0&&reroll.Offers.SequenceEqual(oldOffers)&&reroll.RemainingPool.SequenceEqual(oldStock),"unaffordable reroll is atomic");
        Round(reroll);Check(reroll.Buy(0),"purchase before reroll");
        var keptPiece=reroll.Pieces.Single();int rerollRound=reroll.Round,rerollXp=reroll.Xp;
        Check(reroll.Reroll()&&reroll.Pieces.Single()==keptPiece&&reroll.Round==rerollRound&&reroll.Xp==rerollXp,"reroll preserves owned pieces and progression");
        Check(reroll.PurchaseCount==0&&reroll.Offers.Count(t=>t==0)==4&&reroll.RemainingPool[0]==0,"purchased slot resets without returning owned stock");
        Round(reroll);Check(reroll.Reroll()&&reroll.Buy(0),"replacement offers remain purchasable");
        oldOffers=(int[])reroll.Offers.Clone();oldStock=(int[])reroll.RemainingPool.Clone();int rerollGold=reroll.Gold;
        reroll.StartBattle();Check(!reroll.CanReroll&&!reroll.Reroll()&&reroll.Gold==rerollGold&&reroll.Offers.SequenceEqual(oldOffers)&&reroll.RemainingPool.SequenceEqual(oldStock),"combat blocks reroll without mutation");
        var repeatReroll=Game();while(repeatReroll.Gold<20) Round(repeatReroll);
        int repeatGold=repeatReroll.Gold;oldStock=(int[])repeatReroll.RemainingPool.Clone();
        for(int i=0;i<5;i++) Check(repeatReroll.Reroll(),"repeated preparation reroll");
        Check(repeatReroll.Gold==repeatGold-10&&repeatReroll.RemainingPool.SequenceEqual(oldStock),"repeated rerolls charge each time and conserve pool");
        var allBought=Game();while(allBought.Gold<7) Round(allBought);
        for(int i=0;i<5;i++) Check(allBought.Buy(i),"buy all offers before reroll");
        Check(allBought.Reroll()&&allBought.PurchaseCount==0&&allBought.Offers.All(t=>t>=0)&&allBought.Buy(0),"reroll refills all bought slots and allows more than five purchases per round");
        var emptyReroll=Game(0,0);Check(emptyReroll.Reroll()&&emptyReroll.Gold==0&&emptyReroll.Offers.All(t=>t<0),"empty pool reroll stays safe");
        var newLevelBalance=TierBalance(0);newLevelBalance.levels[0].requiredXp=2;newLevelBalance.levels[1].weights=new[]{0,100,0,0,0};
        var currentLevelReroll=new ArenaRules(7,newLevelBalance,BaseCatalog());currentLevelReroll.BeginRound();
        Round(currentLevelReroll);Check(currentLevelReroll.Level==2&&currentLevelReroll.Reroll()&&currentLevelReroll.Offers.All(t=>t==1),"reroll uses current level probabilities");
        var settings=new ArenaBalance();Check(settings.Validate()==null,"default data table");
        settings.levels[0].weights[0]++;Check(settings.Validate()!=null,"invalid probability sum rejected");
        settings=new ArenaBalance();settings.poolCounts[0]=-1;Check(settings.Validate()!=null,"negative stock rejected");
        var shop=Game(4);while(shop.Gold<5) Round(shop);Check(shop.Buy(0),"expensive purchase");Check(!shop.Buy(1)&&!shop.Bought[1],"insufficient gold does not consume candidate");
        var merge=Game(2);while(merge.Gold<9) Round(merge);merge.Pieces.Add(new ArenaRules.Piece{Id=10,Tier=2,Cell=31});
        merge.Pieces.Add(new ArenaRules.Piece{Id=11,Tier=2,BenchSlot=0});
        for(int tier=3;tier<=3;tier++)for(int i=0;i<2;i++)merge.Pieces.Add(new ArenaRules.Piece{Id=20+tier*2+i,Tier=2,Stars=2,BenchSlot=1+(tier-3)*2+i});
        Check(merge.Buy(0)&&merge.Pieces.Count==1&&merge.Pieces[0].Tier==2&&merge.Pieces[0].Stars==3&&merge.Pieces[0].Cell==31,"chain to three stars preserves species and cell 31");
        var full=Game();for(int i=0;i<10;i++) full.Pieces.Add(new ArenaRules.Piece{Id=100+i,Tier=1+i%4,BenchSlot=i});
        Check(!full.Buy(0)&&full.Gold==2&&!full.Bought[0],"full bench rejects purchase atomically");
        full.Pieces[0].Tier=full.Pieces[1].Tier=0;Check(full.Buy(0),"full bench permits instant merge");
        var damage=Game();Round(damage,false,3);Check(damage.Health==94,"stage-one damage");
        Round(damage,false,6);Check(damage.Health==82,"survivor-scaled damage");
        for(int i=0;i<6;i++) Round(damage);Round(damage,false,3);Check(damage.Health==70,"stage-two damage");
        damage.StartBattle();damage.ResolveBattle(false,100);int before=damage.Round;damage.BeginRound();Check(damage.Health==0&&damage.Round==before,"game over freezes round");
        Check(new ArenaRules(1).Gold==0,"no extra capital before first income");
        var income=Game();Check(income.LastIncome==2&&income.Gold==2,"first round income two");
        Round(income);Check(income.LastIncome==2&&income.Gold==5,"stage one keeps base two plus previous victory");
                income.StartBattle();int beforeReward=income.Gold;income.ResolveBattle(true,0);
        Check(income.WinStreak==2&&income.Gold==beforeReward+1,"second victory pays one");
        income.ResolveBattle(true,0);Check(income.Gold==beforeReward+1&&income.WinStreak==2,"duplicate victory cannot pay twice");
        income.BeginRound();income.StartBattle();beforeReward=income.Gold;income.ResolveBattle(true,0);
        Check(income.WinStreak==3&&income.Gold==beforeReward+2,"third victory pays two");
        income.BeginRound();income.StartBattle();beforeReward=income.Gold;income.ResolveBattle(false,0);
        Check(income.WinStreak==0&&income.Gold==beforeReward&&income.LastStreakGold==0,"loss resets streak and pays nothing");
        income.BeginRound();income.StartBattle();beforeReward=income.Gold;income.ResolveBattle(true,0);
        Check(income.WinStreak==1&&income.Gold==beforeReward+1,"win after loss starts new streak");
        income.BeginRound();while(income.Round<8) Round(income);
        Check(income.Stage==1&&income.LastIncome-income.LastInterest==2,"round eight is stage one");
        Round(income);Check(income.Stage==2&&income.LastIncome-income.LastInterest==5,"round nine starts stage two income");
        while(income.Gold<100) Round(income);
        Check(income.LastInterest==5&&income.LastIncome+income.LastVictoryGold+income.LastStreakGold==12,"income cap twelve with capped interest and streak");
        int[] salePrices={1,2,3,4,5};
        for(int stage=0;stage<5;stage++)
        {
            var sale=Game(stage);while(sale.Gold<ArenaRules.Costs[stage]) Round(sale);
            int gold=sale.Gold;Check(sale.Buy(0)&&sale.Gold==gold-ArenaRules.Costs[stage],"stage purchase price");
            var item=sale.Pieces.Single();if(stage%2==1) sale.AutoDeploy(item.Id);
            Check(sale.Sell(item.Id)&&sale.Pieces.Count==0&&sale.Gold==gold-ArenaRules.Costs[stage]+salePrices[stage],"bench or field sale refund");
            Check(!sale.Sell(item.Id)&&!sale.Sell(-1),"duplicate and invalid sale rejected");
        }
        var lockedSale=Game();lockedSale.Buy(0);int lockedId=lockedSale.Pieces[0].Id;lockedSale.StartBattle();
        Check(!lockedSale.Sell(lockedId)&&lockedSale.Pieces.Count==1,"combat sale rejected");
        for(int cost=1;cost<=5;cost++)
        {
            var starsGame=Game(cost-1,100);
            int purchased=0;
            while(purchased<27)
            {
                for(int slot=0;slot<5&&purchased<27;slot++) if(starsGame.Buy(slot)) purchased++;
                if(purchased<27) Round(starsGame);
            }
            Check(starsGame.Pieces.Count==3&&starsGame.Pieces.All(item=>item.Stars==3&&item.Tier==cost-1),"three stars never merge or evolve");
            Check(starsGame.Pieces.All(item=>item.InvestedGold==9*cost),"chain carries full investment");
            Check(starsGame.RemainingPool[cost-1]+starsGame.Offers.Where((offer,index)=>offer>=0&&!starsGame.Bought[index]).Count()+27==100,"merging conserves reserved and owned stock");
            int beforeStock=starsGame.RemainingPool[cost-1],beforeGold=starsGame.Gold;
            var item=starsGame.Pieces[0];
            Check(starsGame.SalePrice(item)==9*cost-(cost-1),"three star sale formula");
            Check(starsGame.Sell(item.Id)&&starsGame.Gold==beforeGold+9*cost-(cost-1)&&starsGame.RemainingPool[cost-1]==beforeStock+9,"selling three stars returns nine originals and correct gold");
            for(int stars=1;stars<=3;stars++)
            {
                int n=cost*ArenaRules.OriginalCopies(stars);
                Check(ArenaRules.SalePrice(starsGame.Catalog.allies[cost-1],stars)==n-(stars==1?0:cost-1),"all fifteen cost/star sale combinations");
                Check(ArenaRules.SalePrice(starsGame.Catalog.allies[cost-1],stars,n+7)==n+7-(stars==1?0:cost-1),"sale includes additional invested merge cost");
            }
        }
        var mixed=Game();
        for(int i=0;i<10;i++) mixed.Pieces.Add(new ArenaRules.Piece{Id=100+i,Tier=i<2?0:1,Stars=i==0?1:2,BenchSlot=i});
        Check(!mixed.Buy(0)&&mixed.Gold==2&&!mixed.Bought[0],"full bench cannot merge different stars");
        var starBattle=Game();starBattle.Buy(0);starBattle.Pieces[0].Stars=3;starBattle.AutoDeploy(starBattle.Pieces[0].Id);starBattle.StartBattle();
        Check(new ArenaBattle(starBattle).Fighters.Single(f=>!f.Enemy).Stars==3,"battle retains stars for inspection UI");
        var roles=BaseCatalog();
        foreach(DigimonRole role in Enum.GetValues(typeof(DigimonRole)))
        {
            roles.allies[0].role=role;
            Check(roles.Validate()==null&&roles.Copy().allies[0].role==role,"role validates and survives catalog copy");
        }
        roles.allies[0].role=(DigimonRole)99;Check(roles.Validate()!=null,"invalid role rejected");
        Check((int)DigimonType.NoData==5&&DigimonCatalog.TypeNames[5]=="NoData","NoData preserves serialized numeric value");
        var expanded=BaseCatalog();
        var newcomer=expanded.allies[0].Copy();newcomer.id="new_ally";newcomer.name="New ally";newcomer.evolvesTo="ally_1";
        var newEnemy=expanded.enemies[0].Copy();newEnemy.id="new_enemy";newEnemy.name="New enemy";newEnemy.startStage=9;
        expanded.allies=expanded.allies.Concat(new[]{newcomer}).ToArray();expanded.enemies=expanded.enemies.Concat(new[]{newEnemy}).ToArray();
        Check(expanded.Validate()==null,"catalog expands beyond thirteen units");
        Check(expanded.EnemyAt(9)==6&&expanded.EnemyAt(100)==6,"new enemy participates in stage progression");
        var found=new System.Collections.Generic.HashSet<int>();
        for(int seed=0;seed<20;seed++)
        {
            var draw=new ArenaRules(seed,TierBalance(0),expanded);draw.BeginRound();
            foreach(int offer in draw.Offers) { Check(offer==0||offer==5,"same cost candidates only");found.Add(offer); }
        }
        Check(found.SetEquals(new[]{0,5}),"both existing and added units drawn at shared cost");
        var addedGame=new ArenaRules(8,TierBalance(0),expanded);addedGame.BeginRound();Round(addedGame);
        for(int i=0;i<3;i++) { addedGame.Offers[i]=5;Check(addedGame.Buy(i),"purchase added unit"); }
        Check(addedGame.Pieces.Count==1&&addedGame.Pieces[0].Tier==5&&addedGame.Pieces[0].Stars==2,"merge preserves species regardless of evolution target index");
        Round(addedGame);Check(addedGame.RemainingPool[0]==52,"shared cost reservations returned");
        var reordered=expanded.Copy();Array.Reverse(reordered.allies);
        Check(reordered.allies[reordered.Evolution(0)].id=="ally_1","evolution reference survives reorder");
        expanded.allies[0].evolvesTo="new_ally";Check(expanded.Validate()!=null,"cyclic evolution rejected");
        expanded.allies[0].evolvesTo="missing";Check(expanded.Validate()!=null,"missing evolution target rejected");
        expanded.allies[0].evolvesTo="ally_2";newEnemy.id="ally_1";Check(expanded.Validate()!=null,"duplicate IDs rejected across sides");
        var minimal=new DigimonCatalog { allies=new[]{new DigimonData{id="solo",name="Solo"}}, enemies=new[]{new DigimonData{id="creep",name="Creep"}} };
        Check(minimal.Validate()==null,"original thirteen rows not required");
        var absent=new ArenaRules(1,TierBalance(4),minimal);absent.BeginRound();Check(absent.Offers.All(i=>i<0),"cost without any registered unit is unavailable");
        var catalog=new DigimonCatalog();Check(catalog.Validate()==null,"expanded defaults valid");
        Check(catalog.allies.Length==20&&catalog.enemies.Length==6,"20 allies and 6 creeps");
        for(int line=0;line<4;line++)
        {
            var lineage=catalog.Copy();lineage.allies=lineage.allies.Skip(line*5).Take(5).ToArray();
            Check(lineage.Validate()==null,"lineage links valid");
            for(int stage=0;stage<5;stage++)
            {
                Check(lineage.allies[stage].cost==stage+1,"stage cost");
                Check(lineage.Evolution(stage)==(stage<4?stage+1:-1),"lineage evolution target");

                var evolving=new ArenaRules(7,TierBalance(stage),lineage);evolving.BeginRound();while(evolving.Gold<ArenaRules.Costs[stage]) Round(evolving);
                evolving.Pieces.Add(new ArenaRules.Piece{Id=100,Tier=stage,Cell=31});
                evolving.Pieces.Add(new ArenaRules.Piece{Id=101,Tier=stage,BenchSlot=0});
                Check(evolving.Buy(0)&&evolving.Pieces.Count==1&&evolving.Pieces[0].Tier==stage&&evolving.Pieces[0].Stars==2&&evolving.Pieces[0].Cell==31,"every cost upgrades stars on board including final evolution");
            }
        }
        var seenUnits=new System.Collections.Generic.HashSet<int>();
        for(int cost=0;cost<5;cost++) for(int seed=0;seed<40;seed++)
        {
            var rosterShop=new ArenaRules(seed,TierBalance(cost),catalog);rosterShop.BeginRound();
            foreach(int offer in rosterShop.Offers) { Check(catalog.allies[offer].cost==cost+1,"roster shop cost");seenUnits.Add(offer); }
        }
        Check(seenUnits.Count==20,"all roster units appear in shop");
        var copy=catalog.Copy();copy.allies[0].HP=456;Check(catalog.allies[0].HP==189,"catalog snapshot isolation");
        copy.allies[0].SPD=float.NaN;Check(copy.Validate()!=null,"NaN rejected");
        for(int a=0;a<6;a++) for(int b=0;b<6;b++)
        {
            float expected=1;
            if(a<3&&b==(a+1)%3) expected=2;
            if(a==3&&b<3||a<3&&b==5) expected=1.1f;
            Check(Math.Abs(DigimonDamage.TypeMultiplier((DigimonType)a,(DigimonType)b)-expected)<0.001f,"type matrix "+a+":"+b);
        }
        int[,] edges={{1,0},{0,2},{2,1},{3,4},{4,5},{5,3},{6,7},{7,6}};
        for(int a=0;a<9;a++) for(int b=0;b<9;b++)
        {
            bool advantage=false;for(int i=0;i<8;i++) advantage|=edges[i,0]==a&&edges[i,1]==b;
            Check(DigimonDamage.ElementMultiplier((DigimonElement)a,(DigimonElement)b)==(advantage?1.5f:1),"element matrix "+a+":"+b);
        }
        var source=new DigimonData{ATK=100,INT=200,type=DigimonType.Vaccine,element=DigimonElement.Fire};
        var target=new DigimonData{DEF=0,type=DigimonType.Virus,element=DigimonElement.Grass};
        Check(DigimonDamage.Calculate(source,target)==300,"stacked advantage three times");
        target.DEF=100;Check(DigimonDamage.Calculate(source,target)==150,"defense halves damage at 100");
        source.attack=AttackKind.Special;Check(DigimonDamage.Calculate(source,target)==300,"special uses INT");
        Check(DigimonDamage.Calculate(source,target,2)==600,"ability scales attack category");
        var custom=BaseCatalog();custom.allies[0]=new DigimonData{HP=10000,SP=20,ATK=10,INT=70,DEF=0,SPD=0,range=4,attack=AttackKind.Special,type=DigimonType.Free,element=DigimonElement.Neutral};
        custom.enemies[0]=new DigimonData{HP=100000,ATK=0,INT=0,DEF=0,SPD=0,range=4,type=DigimonType.Free,element=DigimonElement.Neutral};
        custom.allies[0].id="ally_1";custom.allies[0].name="test ally";
        custom.enemies[0].id="enemy_1";custom.enemies[0].name="test enemy";
        var combatRules=new ArenaRules(1,TierBalance(0),custom);combatRules.BeginRound();combatRules.Buy(0);combatRules.AutoDeploy(combatRules.Pieces[0].Id);combatRules.StartBattle();
        var measured=new ArenaBattle(combatRules);var attacker=measured.Fighters[0];var victim=measured.Fighters[1];
        attacker.Cell=0;victim.Cell=1;attacker.X=0;victim.X=1;attacker.Y=victim.Y=0;
        measured.Tick(.5f);Check(attacker.Sp==20&&attacker.LastDamage==70,"basic special attack gains SP and uses INT");
        measured.Tick(1);Check(attacker.Sp==0&&attacker.UsedSkill&&attacker.LastDamage==140,"full SP consumes resource for ability");
        attacker.Data.range=1;victim.Data.range=1;victim.Cell=3;victim.X=3;
        measured.Tick(1);Check(attacker.X==0&&!attacker.Moving,"zero SPD does not move");
        attacker.Data.SPD=2;measured.Tick(.1f);Check(attacker.Moving&&attacker.NextCell==1,"movement reserves next tile");
        measured.Tick(.1f);Check(Math.Abs(attacker.X-.2f)<.001f,"SPD controls tile transition duration");
        Check(measured.Occupied(0)&&measured.Occupied(1),"source and destination reserved in transit");
        measured.Tick(.4f);Check(attacker.Cell==1&&attacker.X==1,"move arrives exactly at tile center");
        attacker.Data.SPD=0;
        measured.Tick(100);Check(measured.Finished&&!measured.Won&&measured.Time==30,"timeout exactly thirty seconds");
        Check(ArenaBattle.TileDistance(24,32)==1&&ArenaBattle.TileDistance(24,40)==2,"rail gap excluded from front line range");
        Check(ArenaBattle.TileDistance(24,42)==2&&ArenaBattle.TileDistance(24,43)==3,"diagonally touching tiles count as one");
        for(int range=1;range<=4;range++) Check(ArenaBattle.TileDistance(24,(3+range)*8)==range,"range row boundary "+range);
        var rangeCatalog=new DigimonCatalog();rangeCatalog.allies[0].range=0;Check(rangeCatalog.Validate()!=null,"zero range rejected");
        rangeCatalog.allies[0].range=5;Check(rangeCatalog.Validate()!=null,"range above four rejected");
        var crowdRules=Game();
        for(int i=0;i<32;i++) crowdRules.Pieces.Add(new ArenaRules.Piece{Id=100+i,Tier=0,Cell=i});
        crowdRules.StartBattle();var crowd=new ArenaBattle(crowdRules);
        foreach(var f in crowd.Fighters) {f.Hp=100000;f.Data.ATK=f.Data.INT=0;}
        for(int i=0;i<500;i++)
        {
            crowd.Tick(.025f);
            var owned=new System.Collections.Generic.HashSet<int>();
            foreach(var f in crowd.Fighters.Where(f=>f.Alive))
            {
                Check(owned.Add(f.Cell),"one live fighter per cell");
                if(f.Moving) Check(owned.Add(f.NextCell),"destination reservation unique");
                Check(f.Cell>=0&&f.Cell<64,"fighter remains on grid");
            }
        }
        var train=new ArenaTrain();float prior=train.Center;train.Advance(1);Check(train.Center<prior,"train moves right to left");
        int exit=0;for(int r=2;r<20;r++){train.Advance(r);if(!train.Visible){exit=r;break;}}
        Check(exit>0,"train exits map");train.Advance(exit+1);Check(!train.Visible,"first hidden round");
        train.Advance(exit+2);Check(!train.Visible,"second hidden round");train.Advance(exit+3);Check(train.Visible&&train.Center==7,"returns three rounds after exit on right");
        var route=new ArenaTrain();route.Advance(1);route.Advance(2);route.Advance(3);
        Check(route.Blocks(4,3,4,5),"tram blocks direct crossing");
        route.Waypoint(4,3,4,5,out float wx,out float wy);Check(!route.Blocks(4,3,wx,wy)&&Math.Abs(wx-4)>0.1f,"detour picks clear corner");
        for(int stage=1;stage<=8;stage++)
        {
            var duel=Game(2);while(duel.Gold<9) Round(duel);for(int r=1;r<stage;r++)Round(duel);
            duel.Buy(0);duel.AutoDeploy(duel.Pieces[0].Id);duel.StartBattle();var battle=new ArenaBattle(duel);float frozen=duel.Train.Center;
            Check(battle.Fighters.Where(f=>!f.Enemy).All(f=>f.Y<=3)&&battle.Fighters.Where(f=>f.Enemy).All(f=>f.Y>=5),"vertical deployment");
            for(int tick=0;tick<2000&&!battle.Finished;tick++)
            {
                var positions=battle.Fighters.Select(f=>new[]{f.X,f.Y}).ToArray();battle.Tick(0.025f);
                for(int i=0;i<positions.Length;i++) Check(!duel.Train.Blocks(positions[i][0],positions[i][1],battle.Fighters[i].X,battle.Fighters[i].Y),"movement never penetrates tram");
            }
            Check(battle.Finished&&battle.Won,"combat resolves around train round="+stage+" time="+battle.Time+" fighters="+string.Join(";",battle.Fighters.Select(f=>f.X+","+f.Y+":"+f.Hp)));Check(duel.Train.Center==frozen,"train frozen in combat");
        }
#if ARENA_HEADLESS
        Console.WriteLine("DIGITAL ARENA VALIDATION PASSED: "+checks+" checks");
#else
        Debug.Log("DIGITAL ARENA VALIDATION PASSED: "+checks+" checks");
#endif
    }
}



