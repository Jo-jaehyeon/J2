if(!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play mode required");
var catalog=UnityEngine.JsonUtility.FromJson<DigitalArena.DigimonCatalog>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("DigimonCatalog").text);
var reports=new System.Collections.Generic.List<string>();
foreach(var path in DigimonBlenderAssetBuilder.Manifests())
{
    var m=UnityEngine.JsonUtility.FromJson<DigimonBlenderAssetBuilder.Manifest>(System.IO.File.ReadAllText(path));
    var data=catalog.allies.Concat(catalog.enemies).Single(d=>d.name==m.korean);
    string resource="Digimon/"+m.grade+"/"+m.name+"/"+m.name;
    if(data.prefabPath!=resource) throw new System.Exception("Catalog mismatch: "+m.name);
    var prefab=UnityEngine.Resources.Load<UnityEngine.GameObject>(data.prefabPath);
    var root=UnityEngine.Object.Instantiate(prefab);
    root.transform.position=new UnityEngine.Vector3(100,0,100);
    try
    {
        var anim=root.GetComponent<UnityEngine.Animation>();
        var driver=root.GetComponent<DigitalArena.ArenaUnitAnimation>();
        if(!anim.IsPlaying("Idle"))throw new System.Exception("Idle startup: "+m.name);
        var f=new DigitalArena.ArenaBattle.Fighter { Hp=10,Data=data };
        driver.Pose(true,f,.01f);
        if(!anim.IsPlaying("Walk"))throw new System.Exception("Walk: "+m.name);
        f.AttackCount=1;driver.Pose(false,f,.01f);
        if(!anim.IsPlaying("Attack"))throw new System.Exception("Attack: "+m.name);
        f.AttackCount=2;f.UsedSkill=true;driver.Pose(false,f,.01f);
        if(!anim.IsPlaying("Attack"))throw new System.Exception("Special fallback: "+m.name);
        f.HitCount=1;driver.Pose(false,f,.01f);
        if(!anim.IsPlaying("Attack"))throw new System.Exception("Missing Hit fallback: "+m.name);
        f.Hp=0;driver.Pose(false,f,.5f);
        if(!anim.IsPlaying("Death")||driver.DeathComplete)throw new System.Exception("Premature death completion: "+m.name);
        driver.Pose(false,f,.51f);
        if(!driver.DeathComplete)throw new System.Exception("Death completion: "+m.name);
        string report=m.name+": PASS; resource/catalog, Idle startup, Walk, Attack, Special/Hit fallbacks, Death duration";
        System.IO.File.WriteAllText(System.IO.Path.GetDirectoryName(path)+"/runtime-validation.txt",report);
        reports.Add(report);
    }
    finally { UnityEngine.Object.Destroy(root); }
}
return string.Join("\n",reports);
