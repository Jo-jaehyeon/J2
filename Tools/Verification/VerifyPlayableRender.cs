var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var gt=AppDomain.CurrentDomain.GetAssemblies().SelectMany(a=>a.GetTypes()).Single(t=>t.FullName=="DigitalArena.DigitalArenaGame");
var g=UnityEngine.Object.FindAnyObjectByType(gt);
var world=(Component)gt.GetField("world",flags).GetValue(g);var wt=world.GetType();
var motor=(Component)gt.GetField("playableCharacter",flags).GetValue(g);var mt=motor.GetType();
gt.GetField("help",flags).SetValue(g,false);
var p=new Vector3(3,.25f,-3);var screen=(Vector2)wt.GetMethod("Project").Invoke(world,new object[]{p});
var offset=(Vector2)gt.GetField("uiOffset",flags).GetValue(g);var scale=(float)gt.GetField("uiScale",flags).GetValue(g);
if(!(bool)gt.GetMethod("HandlePointer",flags).Invoke(g,new object[]{EventType.MouseDown,0,1,(screen-offset)/scale}))throw new Exception("Ground click not consumed");
if(Vector3.Distance(p,(Vector3)mt.GetProperty("Destination").GetValue(motor))>.01f)throw new Exception("Click destination mismatch");
var camera=(Camera)wt.GetProperty("Camera").GetValue(world);var before=camera.transform.position;
screen=(Vector2)wt.GetMethod("Project").Invoke(world,new object[]{new Vector3(8,.25f,-3)});
gt.GetMethod("HandlePointer",flags).Invoke(g,new object[]{EventType.MouseDown,0,1,(screen-offset)/scale});
if(Vector3.Distance(p,(Vector3)mt.GetProperty("Destination").GetValue(motor))>.01f || camera.transform.position!=before)throw new Exception("Outside click changed target/camera");
var render=motor.GetComponentInChildren<SkinnedMeshRenderer>();
var go=new GameObject("Playable render validation");var cam=go.AddComponent<Camera>();
var rt=new RenderTexture(128,128,24);var previous=RenderTexture.active;
var layers=motor.GetComponentsInChildren<Transform>().Select(t=>t.gameObject.layer).ToArray();
var transforms=motor.GetComponentsInChildren<Transform>();
try{
 foreach(var t in transforms)t.gameObject.layer=31;
 cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.magenta;
 cam.transform.position=motor.transform.position+new Vector3(0,1,-4);cam.transform.LookAt(motor.transform.position+Vector3.up*.9f);cam.orthographic=true;cam.orthographicSize=1.1f;cam.targetTexture=rt;cam.Render();
 RenderTexture.active=rt;var tex=new Texture2D(128,128,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,128,128),0,0);tex.Apply();
 var pixels=tex.GetPixels().Count(c=>c.g>.05f && (c.r<.95f || c.b<.95f));UnityEngine.Object.Destroy(tex);if(pixels<300)throw new Exception("Avatar invisible: "+pixels);
 return "PASS: screen click reaches exact coordinate; outside click changes neither destination nor camera; visible rendered avatar pixels="+pixels;
}finally{for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);gt.GetField("help",flags).SetValue(g,true);}
