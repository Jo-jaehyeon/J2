using UnityEngine;
using UnityEngine.InputSystem;
using J2.MultiplayerMap;

namespace DigitalArena
{
    public sealed partial class DigitalArenaGame
    {
        MultiplayerMapPreview multiplayerPreview;
        void EnterMultiplayerPreview()
        {
            if (!mainMenu || !CanEnterGame || multiplayerPreview != null) return;
            var map = Resources.Load<GameObject>("Map/DragonEyeMultiplayer");
            var player = Resources.Load<GameObject>("PlayableCharacter/ShinTaeyil/ShinTaeyil");
            if (map == null || player == null) { multiplayerNotice=true; Debug.LogError("Multiplayer preview assets are missing."); return; }
            var owner = new GameObject("Multiplayer Map Preview");
            owner.transform.SetParent(transform,false);
            var preview=owner.AddComponent<MultiplayerMapPreview>();
            try { preview.Initialize(map,player); }
            catch (System.Exception exception) { preview.Shutdown(); Destroy(owner); multiplayerNotice=true; Debug.LogException(exception); return; }
            multiplayerPreview=preview;
            CancelDrag(); mainMenu=false; multiplayerNotice=false;
        }
        void ExitMultiplayerPreview()
        {
            if (multiplayerPreview == null) return;
            multiplayerPreview.Shutdown(); Destroy(multiplayerPreview.gameObject); multiplayerPreview=null;
            mainMenu=true; multiplayerNotice=false;
        }
        void UpdateMultiplayerPreview()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { ExitMultiplayerPreview(); return; }
            multiplayerPreview.Tick(Time.deltaTime,screen => {
                var point=UiPoint(new Vector2(screen.x,Screen.height-screen.y));
                return point.y<110 || point.y>790;
            });
        }
        void DrawMultiplayerPreview()
        {
            PanelBox(new Rect(24,24,440,78));
            Label(42,33,400,30,"멀티플레이 맵 · 자유 이동",23,Ink,true);
            Label(42,67,400,24,"전투 없이 전장을 둘러보세요",14,Muted);
            ui.Button("exitMultiplayerPreview",new Rect(1216,24,200,48),"메인 화면으로",Panel,ExitMultiplayerPreview);
            PanelBox(new Rect(260,818,920,54));
            Label(278,829,884,31,"바닥 우클릭 / 터치 이동  ·  Esc 돌아가기",17,Ink,false,TextAnchor.MiddleCenter);
        }
    }
}
