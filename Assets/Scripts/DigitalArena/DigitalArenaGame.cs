using UnityEngine;
using J2.Networking;
using J2.Protocol;
namespace DigitalArena
{
    // Main menu and login only; in-game UI belongs to MultiplayerGameUi.
    public sealed partial class DigitalArenaGame : MonoBehaviour
    {
        ArenaGameUi ui;
        Font font;
        Texture2D mainBackground;
        bool mainMenu=true, multiplayerNotice, testEnterGameSent;
        string matchmakingMessage;
        float uiScale=1;
        Vector2 uiOffset;
        static readonly Color Panel=Hex(0x16292E), Edge=Hex(0x556A62), Ink=Hex(0xF4EEDB), Muted=Hex(0xA7B8AF), Mint=Hex(0x7BE5BC);
        static Color Hex(uint v)=>new Color32((byte)(v>>16),(byte)(v>>8),(byte)v,255);
        void Awake()
        {
            font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},18);
            Application.targetFrameRate=60;
            mainBackground=Resources.Load<Texture2D>("UI/DigiTacticsMainBackground");
            InitializeAccount();ui=new ArenaGameUi(transform,font);
            var camera=new GameObject("Main Menu Camera").AddComponent<Camera>();
            camera.transform.SetParent(transform,false);camera.cullingMask=0;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Hex(0x112A3A);
        }
        void Update(){SendTestEnterGameFromLobby();UpdateUiMetrics();nicknamePanel?.Sync(NeedsNickname,uiScale,uiOffset);}
        void LateUpdate(){UpdateUiMetrics();ui.Begin(uiScale,uiOffset);MainMenu();ui.End();}
        void SelectMultiplayer()=>EnterMultiplayerPreview();
        void OnDestroy(){nicknamePanel?.Dispose();ui?.Dispose();if(font!=null)Destroy(font);}
        void Fill(Rect r,Color c)=>ui.Fill(r,c);
        void SendTestEnterGameFromLobby()
        {
            // Temporary login-disabled test flow: wait for the lobby and TCP readiness.
            if (externalLoginEnabled || !mainMenu)
            {
                return;
            }

            var network = NetworkManager.Instance;

            if (network == null || !network.IsConnected)
            {
                testEnterGameSent = false;

                return;
            }

            if (testEnterGameSent || network.SessionId > 0)
            {
                return;
            }

            var packet = new C_EnterGame
            {
                ClientName = "Unity"
            };

            testEnterGameSent = network.SendPacket(PacketId.CEntergame, packet);
        }
        void MainMenu()
        {
            if (NeedsNickname)
            {
                return;
            }

            if (mainBackground != null)
            {
                ui.Picture(new Rect(0, 0, 1440, 900), mainBackground, ScaleMode.ScaleAndCrop);
            }

            Label(200, 165, 1040, 110, "DigiTactics", 82, new Color(0, 0, 0, .55f), true, TextAnchor.MiddleCenter);
            Label(200, 161, 1040, 110, "DigiTactics", 82, Ink, true, TextAnchor.MiddleCenter);
            Label(390, 277, 660, 34, "디지털 월드에서 시작되는 나만의 전술", 20, Ink, false, TextAnchor.MiddleCenter);

            if (externalLoginEnabled && !account.Ready)
            {
                AccountMenu();

                return;
            }

            if (externalLoginEnabled)
            {
                AccountSummary();
            }

            ui.Button("multi", new Rect(565, 686, 310, 62), "멀티 플레이", Hex(0x24394F), SelectMultiplayer, CanEnterGame);

            if (multiplayerNotice)
            {
                Label(350, 762, 740, 48, matchmakingMessage ?? "맵을 불러오지 못했습니다. 다시 시도해 주세요.", 18, Ink, false, TextAnchor.MiddleCenter);
            }
            else if (externalLoginEnabled)
            {
                Label(350, 762, 740, 48, account.Message, 16, Ink, false, TextAnchor.MiddleCenter);
            }

            Label(390, 831, 660, 24, "DIGITAL WORLD · MULTIPLAYER", 12, Ink, false, TextAnchor.MiddleCenter);
        }
        void Label(float x, float y, float w, float h, string value, int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            ui.Text(new Rect(x, y, w, h), value, size, color, bold, alignment);
        }

        void Frame(Rect r, Color c, float w)
        {
            Fill(new Rect(r.x, r.y, r.width, w), c);
            Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
            Fill(new Rect(r.x, r.y, w, r.height), c);
            Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        void PanelBox(Rect rect)
        {
            Fill(rect, Panel);
            Frame(rect, Edge, 1);
        }
    }
}
