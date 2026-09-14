from pathlib import Path
import shutil
r=Path('C:/Jerry/UnityProject/J2');src=r/'Assets/Resources/Map/Source~';backup=src/'Iterations/BeforeUniformBattlefields';backup.mkdir(parents=True,exist_ok=True)
for name in ['DragonEyeMultiplayer.blend','rail-route.json','LakeScenery.png','Overview.png']:
    if (src/name).exists() and not (backup/name).exists():shutil.copy2(src/name,backup/name)
p=r/'Tools/Map/build_multiplayer_map.py';s=p.read_text(encoding='utf-8')
s=s.replace('# Shared square loop crosses arena centers.', '# Shared rectangular loop crosses identical arenas; all turns are outside the arenas.')
s=s.replace('radius=.65','radius=4.0\nhalf_width=62.0\nhalf_depth=40.0')
s=s.replace('[(40-radius,40-radius,0),(-40+radius,40-radius,90),(-40+radius,-40+radius,180),(40-radius,-40+radius,270)]','[(half_width-radius,half_depth-radius,0),(-half_width+radius,half_depth-radius,90),(-half_width+radius,-half_depth+radius,180),(half_width-radius,-half_depth+radius,270)]')
s=s.replace("'routing':'square through all eight battlefields; arenas 4 and 5 rotate 90 degrees','extent':40,'cornerRadius':radius,'cornerCellHalfOffset':3.25", "'routing':'rectangle through eight identical battlefields; arenas 4 and 5 rotate 90 degrees; turns outside all battlefields','halfWidth':half_width,'halfDepth':half_depth,'cornerRadius':radius,'arenaVariant':'shared identical Battlefield'")
a=s.index('# Corner variant retains');b=s.index('for rt,filename in',a)
s=s[:a]+s[b:]
s=s.replace("(corner,'CornerBattlefield')", "(stage,'CornerBattlefield')")
s=s.replace("(rail,'SquareRailway')", "(rail,'RectangularRailway')")
s=s.replace('[(-40,40),(0,40),(40,40),(-40,0),(40,0),(-40,-40),(0,-40),(40,-40)]','[(-40,40),(0,40),(40,40),(-62,0),(62,0),(-40,-40),(0,-40),(40,-40)]')
s=s.replace('for rt in [corner if idx in [0,2,5,7] else stage]:','for rt in [stage]:')
s=s.replace('DragonEyeMultiplayerLake.blend','DragonEyeMultiplayerRectangle.blend')
s=s.replace("'grid':'3x3 center empty','spacing':40", "'grid':'3 / 2 / 3, center empty; side arenas widened to +/-62','rowSpacing':40")
s=s.replace('[stage,corner,tram,lake,rail]','[stage,tram,lake,rail]')
p.write_text(s,encoding='utf-8')
p=r/'Tools/Map/build_prefabs.cs';s=p.read_text(encoding='utf-8')
a=s.index('var cornerArena=');b=s.index('var root=new UnityEngine.GameObject',a)
s=s[:a]+'''// Preserve the old corner prefab GUID while restoring its geometry to the shared arena.
var compatibleArena=UnityEngine.Object.Instantiate(arenaPrefab);compatibleArena.name="CornerBattlefield";
UnityEditor.PrefabUtility.SaveAsPrefabAsset(compatibleArena,basePath+"/CornerBattlefield.prefab");UnityEngine.Object.DestroyImmediate(compatibleArena);
'''+s[b:]
s=s.replace('InstantiatePrefab((slot==0||slot==2||slot==5||slot==7)?cornerPrefab:arenaPrefab)','InstantiatePrefab(arenaPrefab)')
s=s.replace('new UnityEngine.Vector3(col*40,0,row*40)','new UnityEngine.Vector3(col*(row==0?62:40),0,row*40)')
s=s.replace('visual("SquareRailway",root.transform)','visual("RectangularRailway",root.transform)')
p.write_text(s,encoding='utf-8')
p=r/'Tools/Map/validate_map.cs';s=p.read_text(encoding='utf-8-sig')
a=s.index('            // Corner slots turn');b=s.index('\n        }',a)
s=s[:a]+'''            if(best>.01f)throw new System.Exception("Rail misses complete straight arena corridor "+i+" x="+x+" gap="+best);'''+s[b:]
s=s.replace('for(float x=-6;x<=6;x+=.5f)','for(float x=-12;x<=12;x+=.5f)')
s=s.replace('UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(p.x),UnityEngine.Mathf.Abs(p.z))>40.01f','(UnityEngine.Mathf.Abs(p.x)>62.01f||UnityEngine.Mathf.Abs(p.z)>40.01f)')
s=s.replace('square route crosses all eight arenas','rectangular route crosses all eight identical arenas')
s=s.replace('PASS: eight unique outer 3x3 arena slots; center empty.', 'PASS: eight unique outer 3/2/3 arena slots; center empty.')
needle='    if(root.GetComponentsInChildren<J2.MultiplayerMap.MultiplayerMapTrain>()'
where=s.index(needle)
s=s[:where]+'''    var sharedArena=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Map/Battlefield.prefab");
    for(int i=0;i<8;i++) {
        var arena=layout.Arenas[i];
        if(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(arena.gameObject)!=sharedArena)throw new System.Exception("Arena uses a different prefab: "+i);
        if(arena.localScale!=UnityEngine.Vector3.one)throw new System.Exception("Arena scale differs: "+i);
        foreach(var group in new[]{"AllyCells","EnemyCells","AllyBench","EnemyBench"}) {
            var actual=arena.Find(group);var reference=sharedArena.transform.Find(group);
            for(int j=0;j<actual.childCount;j++)if(UnityEngine.Vector3.Distance(actual.GetChild(j).localPosition,reference.GetChild(j).localPosition)>.0001f)throw new System.Exception("Arena cell shape differs: "+i);
        }
    }
    report.AppendLine("PASS: all eight arenas reference the SAME Battlefield prefab with identical scale and all cell/bench positions.");
'''+s[where:]
p.write_text(s,encoding='utf-8')
