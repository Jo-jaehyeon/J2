from pathlib import Path
p=Path('Tools/Map/validate_map.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('cam.orthographicSize=88','cam.orthographicSize=122')
s=s.replace('new UnityEngine.Vector3(65,145,-110);cam.transform.LookAt(UnityEngine.Vector3.zero);','new UnityEngine.Vector3(65,205,-180);cam.transform.LookAt(new UnityEngine.Vector3(0,8,16));')
old='UnityEngine.Object.DestroyImmediate(tex);'
new='''UnityEngine.Object.DestroyImmediate(tex);
        cam.orthographic=false;cam.fieldOfView=60;cam.transform.position=new UnityEngine.Vector3(28,40,-117);cam.transform.LookAt(new UnityEngine.Vector3(0,13,27));
        cam.Render();UnityEngine.RenderTexture.active=rt;tex=new UnityEngine.Texture2D(1400,1400,UnityEngine.TextureFormat.RGB24,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1400,1400),0,0);tex.Apply();System.IO.File.WriteAllBytes("Assets/Resources/Map/Source~/UnityLakeScenery.png",UnityEngine.ImageConversion.EncodeToPNG(tex));UnityEngine.Object.DestroyImmediate(tex);'''
s=s.replace(old,new);p.write_text(s,encoding='utf-8')
p=Path('Tools/Map/build_multiplayer_map.py');s=p.read_text(encoding='utf-8');s=s.replace("bs.inputs['Base Color'].default_value=(*c,1)","bs.inputs['Base Color'].default_value=(*(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c),1)")
p.write_text(s,encoding='utf-8')
