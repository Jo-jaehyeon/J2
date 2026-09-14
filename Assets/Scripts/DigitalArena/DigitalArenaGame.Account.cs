using UnityEngine;

namespace DigitalArena
{
    public sealed partial class DigitalArenaGame
    {
        [SerializeField] bool externalLoginEnabled = false;
        bool CanEnterGame => !externalLoginEnabled || (account != null && account.Ready && !account.Busy);
        ArenaAccountClient account;
        ArenaNicknamePanel nicknamePanel;
        bool NeedsNickname => externalLoginEnabled && mainMenu && account != null && account.Profile != null && !account.Ready;
        Texture2D googleLogin, naverLogin, kakaoLogin;

        void InitializeAccount()
        {
            account = gameObject.AddComponent<ArenaAccountClient>();
            if (!externalLoginEnabled) return;
            googleLogin = Resources.Load<Texture2D>("UI/Login/google");
            naverLogin = Resources.Load<Texture2D>("UI/Login/naver");
            kakaoLogin = Resources.Load<Texture2D>("UI/Login/kakao");
            nicknamePanel = new ArenaNicknamePanel(transform, account, font, mainBackground);
        }

        void AccountMenu()
        {
            PanelBox(new Rect(360,430,720,368));
            if (account.Profile == null)
            {
                Label(392,448,656,40,"로그인 / 회원가입",27,Ink,true,TextAnchor.MiddleCenter);
                Label(390,500,660,30,"사용할 계정을 선택하세요",17,Muted,false,TextAnchor.MiddleCenter);
                ProviderButton(new Rect(404,552,200,62), googleLogin, "Google 로그인", "google", Color.white);
                ProviderButton(new Rect(620,552,200,62), naverLogin, "네이버", "naver", Hex(0x03A94D));
                ProviderButton(new Rect(836,552,200,62), kakaoLogin, "카카오", "kakao", Hex(0xFEE500));
                if (account.Busy) ui.Button("cancelLogin",new Rect(620,711,200,44),"로그인 취소",Panel,account.CancelLogin);
            }
            Label(390,628,660,66,account.Message,16,account.NicknameAvailable?Mint:Ink,false,TextAnchor.MiddleCenter);
        }

        void ProviderButton(Rect rect, Texture2D image, string label, string provider, Color color)
        {
            if (image != null)
            {
                ui.Button("login-"+provider,rect,"",Color.clear,()=>account.Login(provider),!account.Busy,true,label);
                ui.Picture(rect,image,ScaleMode.ScaleToFit,account.Busy?.45f:1);
            }
            else
            {
                ui.Button("login-"+provider,rect,"",color,()=>account.Login(provider),!account.Busy);
                Label(rect.x,rect.y,rect.width,rect.height,label,20,Hex(0x172A30),true,TextAnchor.MiddleCenter);
            }
        }

        void AccountSummary()
        {
            var profile = account.Profile;
            PanelBox(new Rect(402,572,636,94));
            Label(426,582,410,32,profile.nickname,24,Ink,true);
            Label(426,617,430,28,"MMR "+profile.mmr+"   ·   "+profile.wins+"승 "+profile.losses+"패",18,Mint);
            ui.Button("logout",new Rect(910,590,106,46),"로그아웃",Panel,account.Logout,!account.Busy);
            ui.Button("refreshProfile",new Rect(794,590,106,46),"새로고침",Panel,account.RefreshProfile,!account.Busy);
        }
    }
}
