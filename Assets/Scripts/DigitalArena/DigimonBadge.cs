using UnityEngine;

namespace DigitalArena
{
    // Procedural circular emblems: silhouette identifies type, fill identifies element.
    public static class DigimonBadge
    {
        public static readonly Color[] Colors={new Color32(234,64,55,255),new Color32(40,114,237,255),new Color32(155,225,59,255),new Color32(250,219,53,255),new Color32(119,213,248,255),new Color32(147,92,50,255),Color.white,new Color32(22,20,29,255),Color.gray};
        public static Texture2D Create(DigimonType type,DigimonElement element)
        {
            const int n=64;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Bilinear;
            Color fill=Colors[(int)element];Color ink=(element==DigimonElement.Dark||element==DigimonElement.Water||element==DigimonElement.Earth)?Color.white:new Color32(28,30,36,255);
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float a=(x-31.5f)/31.5f,b=(y-31.5f)/31.5f,r=Mathf.Sqrt(a*a+b*b);
                bool glyph=false;float ax=Mathf.Abs(a),by=Mathf.Abs(b);
                switch(type)
                {
                    case DigimonType.Vaccine: glyph=b>-.45f&&b<.58f&&ax<(.58f-b)*.45f; if(b<-.04f&&ax<.065f&&b>-.23f) glyph=false; break;
                    case DigimonType.Virus: glyph=r<.46f+.14f*Mathf.Cos(Mathf.Atan2(b,a)*6); if(r<.13f) glyph=false; break;
                    case DigimonType.Data: glyph=ax<.43f&&by<.49f-ax*.4f; if((ax<.025f&&b<0)||Mathf.Abs(b-ax*.42f)<.035f) glyph=false; break;
                    case DigimonType.Unknown: glyph=(r>.27f&&r<.40f&&b>0)||(ax<.07f&&b<.22f&&b>-.18f)||(r<.08f&&b<0)||(ax<.07f&&b<-.30f&&b>-.45f); break;
                    case DigimonType.Free: glyph=Mathf.Abs(ax-.23f)<.055f&&by<.38f||Mathf.Abs(by-.38f)<.055f&&ax<.3f; break;
                    case DigimonType.None: glyph=by<.055f&&ax<.4f; break;
                }
                t.SetPixel(x,y,r>1?Color.clear:r>.89f?new Color32(231,208,146,255):glyph?ink:fill);
            }
            t.Apply();return t;
        }
    }
}
