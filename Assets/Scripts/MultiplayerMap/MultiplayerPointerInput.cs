using UnityEngine;
using UnityEngine.InputSystem;
namespace J2.MultiplayerMap
{
    // Sends UI intent only; does not place, buy, sell or merge units locally.
    public sealed class MultiplayerPointerInput
    {
        readonly MultiplayerSceneController owner;
        int? pressed;
        Vector2 start;
        bool dragging, touchGesture;
        public MultiplayerPointerInput(MultiplayerSceneController controller){owner=controller;}
        public void Cancel(){pressed=null;dragging=false;touchGesture=false;owner.Hud.DragEntityId=null;}
        public bool Tick(float scale,Vector2 offset)
        {
            var touch=Touchscreen.current?.primaryTouch;
            if(touch!=null&&touch.press.wasPressedThisFrame){touchGesture=true;Begin(touch.position.ReadValue(),scale,offset);}
            if(touchGesture)
            {
                if(touch!=null&&touch.press.isPressed)Drag(touch.position.ReadValue());
                if(touch!=null&&touch.press.wasReleasedThisFrame)
                {
                    bool used=pressed.HasValue;
                    if(touch.phase.ReadValue()==UnityEngine.InputSystem.TouchPhase.Canceled)Cancel();
                    else End(touch.position.ReadValue(),scale,offset,true);
                    return used;
                }
                return pressed.HasValue;
            }
            var mouse=Mouse.current;if(mouse==null)return false;var point=mouse.position.ReadValue();
            if(mouse.rightButton.wasPressedThisFrame&&!OverUi(point,scale,offset))
            {
                var id=Pick(point);if(id.HasValue){owner.Hud.RequestDetails(id.Value);return true;}
            }
            if(mouse.leftButton.wasPressedThisFrame)Begin(point,scale,offset);
            if(mouse.leftButton.isPressed)Drag(point);
            bool consumed=pressed.HasValue;
            if(mouse.leftButton.wasReleasedThisFrame)End(point,scale,offset,false);
            return consumed;
        }
        bool OverUi(Vector2 p,float scale,Vector2 offset)=>owner.Hud.OverUi((new Vector2(p.x,Screen.height-p.y)-offset)/scale);
        int? Pick(Vector2 screen)
        {
            // Renderer bounds work for models without gameplay colliders.
            var ray=owner.World.ViewCamera.ScreenPointToRay(screen);float nearest=float.MaxValue;int? found=null;
            foreach(var pair in owner.Spawner.Entities)
            {
                if(pair.Value==null||!pair.Value.activeInHierarchy)continue;
                foreach(var renderer in pair.Value.GetComponentsInChildren<Renderer>())
                    if(renderer.enabled&&renderer.bounds.IntersectRay(ray,out var distance)&&distance<nearest){nearest=distance;found=pair.Key;}
            }
            return found;
        }
        void Begin(Vector2 p,float scale,Vector2 offset){pressed=OverUi(p,scale,offset)?null:Pick(p);start=p;dragging=false;}
        void Drag(Vector2 p){if(pressed.HasValue&&Vector2.Distance(p,start)>18){dragging=true;owner.Hud.DragEntityId=pressed;}}
        void End(Vector2 p,float scale,Vector2 offset,bool touch)
        {
            if(pressed.HasValue)
            {
                var logical=(new Vector2(p.x,Screen.height-p.y)-offset)/scale;
                if(dragging)
                {
                    if(logical.y>=790)owner.Hud.RequestSell(pressed.Value);
                    else if(!owner.Hud.OverUi(logical))
                    {
                        var plane=new Plane(Vector3.up,owner.World.ViewedArena.position);
                        var ray=owner.World.ViewCamera.ScreenPointToRay(p);
                        if(plane.Raycast(ray,out var distance))owner.Hud.RequestPlacement(pressed.Value,ray.GetPoint(distance));
                    }
                }
                else if(touch)owner.Hud.RequestDetails(pressed.Value);
                else if(Mouse.current?.clickCount.ReadValue()>=2)owner.Hud.RequestAutoDeploy(pressed.Value);
            }
            Cancel();
        }
    }
}
