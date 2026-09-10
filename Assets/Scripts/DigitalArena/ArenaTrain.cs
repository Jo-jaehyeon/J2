using System;
using System.Collections.Generic;

namespace DigitalArena
{
    public sealed class ArenaTrain
    {
        public float Center { get; private set; } = 8.5f;
        public float Previous { get; private set; } = 8.5f;
        public bool Visible { get; private set; } = true;
        public int ReturnRound { get; private set; }
        public void Advance(int round)
        {
            Previous=Center;
            if(!Visible)
            {
                if(round<ReturnRound) return;
                Visible=true; Previous=9; Center=7; return;
            }
            Center-=1.5f;
            if(Center+1.5f< -0.5f) { Visible=false; ReturnRound=round+3; }
        }
        // Padded footprint in board coordinates: blocks movement and attacks through the tram.
        public bool Blocks(float ax,float ay,float bx,float by)
        {
            if(!Visible) return false;
            float lo=0,hi=1;
            return Slab(ax,bx-ax,Center-1.65f,Center+1.65f,ref lo,ref hi)
                && Slab(ay,by-ay,3.48f,4.52f,ref lo,ref hi);
        }
        static bool Slab(float origin,float delta,float min,float max,ref float lo,ref float hi)
        {
            if(Math.Abs(delta)<0.00001f) return origin>=min && origin<=max;
            float a=(min-origin)/delta,b=(max-origin)/delta;
            if(a>b) { float swap=a;a=b;b=swap; }
            lo=Math.Max(lo,a); hi=Math.Min(hi,b); return lo<=hi;
        }
        public void Waypoint(float ax,float ay,float bx,float by,out float x,out float y)
        {
            x=bx;y=by;
            if(!Blocks(ax,ay,bx,by)) return;
            var nodes=new List<float[]> { new[]{ax,ay},new[]{bx,by} };
            foreach(float cx in new[]{Center-1.8f,Center+1.8f})
                if(cx>=-0.4f && cx<=7.4f) { nodes.Add(new[]{cx,3.3f});nodes.Add(new[]{cx,4.7f}); }
            int n=nodes.Count; var distance=new float[n];var previous=new int[n];var visited=new bool[n];
            for(int i=0;i<n;i++) { distance[i]=float.PositiveInfinity; previous[i]=-1; } distance[0]=0;
            for(int iteration=0;iteration<n;iteration++)
            {
                int current=-1;
                for(int i=0;i<n;i++) if(!visited[i] && (current<0 || distance[i]<distance[current])) current=i;
                if(current<0 || float.IsPositiveInfinity(distance[current])) break;
                visited[current]=true;
                for(int next=0;next<n;next++)
                {
                    if(visited[next] || Blocks(nodes[current][0],nodes[current][1],nodes[next][0],nodes[next][1])) continue;
                    float dx=nodes[current][0]-nodes[next][0],dy=nodes[current][1]-nodes[next][1];
                    float candidate=distance[current]+(float)Math.Sqrt(dx*dx+dy*dy);
                    if(candidate<distance[next]) { distance[next]=candidate; previous[next]=current; }
                }
            }
            if(previous[1]<0) { x=ax;y=ay;return; }
            int hop=1;while(previous[hop]>0) hop=previous[hop];
            x=nodes[hop][0];y=nodes[hop][1];
        }
    }
}
