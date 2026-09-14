from pathlib import Path
p=Path('Tools/Map/validate_map.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('UnityEngine.Mathf.Abs(bench.bounds.center.z-(a.position.z-9))>.01f', 'UnityEngine.Vector3.Distance(bench.bounds.center,a.TransformPoint(new UnityEngine.Vector3(0,.08f,-9)))>.01f')
s=s.replace('p.y!=0','UnityEngine.Mathf.Abs(p.y-.325f)>.001f').replace('>24.01f','>40.01f')
s=s.replace('var train=layout.SharedTrain;float length=train.RouteLength;', '''var train=layout.SharedTrain;float length=train.RouteLength;
    for(int i=0;i<8;i++) {
        float expected=(i==3||i==4)?90:0;
        if(UnityEngine.Quaternion.Angle(layout.Arenas[i].localRotation,UnityEngine.Quaternion.Euler(0,expected,0))>.01f)throw new System.Exception("Wrong arena orientation "+i);
        var a=layout.Arenas[i];
        // Inspect the central straight corridor end-to-end, not just the nearest route point.
        for(float x=-6;x<=6;x+=.5f) {
            var wanted=a.TransformPoint(new UnityEngine.Vector3(x,0,0));float best=float.MaxValue;
            for(int j=0;j<train.Route.Length;j++) {
                var p0=train.transform.TransformPoint(train.Route[j]);var p1=train.transform.TransformPoint(train.Route[(j+1)%train.Route.Length]);p0.y=0;p1.y=0;
                var delta=p1-p0;float t=UnityEngine.Mathf.Clamp01(UnityEngine.Vector3.Dot(wanted-p0,delta)/delta.sqrMagnitude);best=UnityEngine.Mathf.Min(best,UnityEngine.Vector3.Distance(wanted,p0+delta*t));
            }
            // Corner slots turn through the center rather than continuing out the far edge.
            bool corner=(i==0||i==2||i==5||i==7);
            bool outgoingOutside=corner && ((a.position.x>0&&x>0)||(a.position.x<0&&x<0));
            if(!outgoingOutside && best>.3f)throw new System.Exception("Rail misses arena corridor "+i+" x="+x+" gap="+best);
        }
    }
''')
s=s.replace('report.AppendLine("Route length: "+length);','report.AppendLine("PASS: square route crosses all eight arenas; side arenas 4 and 5 rotate 90 degrees.");report.AppendLine("Route length: "+length);')
p.write_text(s,encoding='utf-8')
