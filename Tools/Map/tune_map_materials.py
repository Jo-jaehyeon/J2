from pathlib import Path
p=Path('Tools/Map/build_prefabs.cs');s=p.read_text(encoding='utf-8')
s=s.replace('mat.color=color;mat.SetFloat("_Glossiness",.16f);materials[item.Name]=mat;', 'mat.color=color;mat.SetFloat("_Glossiness",item.Name.Contains("Water")?.65f:.16f);UnityEditor.EditorUtility.SetDirty(mat);materials[item.Name]=mat;')
p.write_text(s,encoding='utf-8')
