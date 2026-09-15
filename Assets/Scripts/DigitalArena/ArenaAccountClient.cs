using System.Collections.Generic;
using J2.Networking;
using J2.Protocol;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DigitalArena
{
    [Serializable]
    public sealed class ArenaAccountProfile
    {

        public string	playerId, nickname;
        public int		mmr, wins, losses;
        // TODO: purchase fulfillment is server-only; display inventory when the shop is implemented.
        public string[]	ownedCharacterIds;
    }

    public sealed class ArenaAccountClient : MonoBehaviour
    {
        [Serializable]
        sealed class Settings
        {

            public string	baseUrl = "";
        }

        [Serializable]
        sealed class BeginRequest
        {

            public string	provider;
        }

        [Serializable]
        sealed class BeginResponse
        {

            public string	attemptId, pollToken, authorizeUrl;
        }

        [Serializable]
        sealed class PollRequest
        {

            public string	attemptId;
        }

        [Serializable]
        sealed class PollResponse
        {

            public ArenaAccountProfile	profile;

            public string	status, token;
        }

        [Serializable]
        sealed class NicknameRequest
        {

            public string	nickname;
        }

        [Serializable]
        sealed class Availability
        {

            public string	nickname, message;
            public bool		available;
        }

        [Serializable]
        sealed class ErrorResponse
        {

            public string	message;
        }

        public ArenaAccountProfile Profile { get; private set; }
        public bool Ready => Profile != null && !string.IsNullOrEmpty(Profile.nickname);
        public bool Busy { get; private set; }
        public string Message { get; private set; } = "계정을 선택해 로그인하거나 가입하세요.";
        public string CheckedNickname { get; private set; }
        public bool NicknameAvailable { get; private set; }

        UnityWebRequest	activeRequest;

        string	baseUrl, token;
        // Session token is intentionally memory-only. Never store credentials in PlayerPrefs.

        void Awake()
        {
            var asset = Resources.Load<TextAsset>("AccountSettings");

            var settings = asset == null ? new Settings() : JsonUtility.FromJson<Settings>(asset.text);

            baseUrl = (settings.baseUrl ?? "").TrimEnd('/');
#if UNITY_EDITOR

            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "http://127.0.0.1:8787";
            }
#endif
        }

        bool Configured()
        {
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && (uri.Scheme == "https"
#if UNITY_EDITOR
            || (uri.Scheme == "http" && uri.IsLoopback)
#endif
            ))
            {
                return true;
            }

            Message = "로그인 서버 주소가 아직 설정되지 않았습니다.";

            return false;
        }

        public void Login(string provider)
        {
            if (Busy || !Configured())
            {
                return;
            }

            Busy = true;
            Message = "로그인 페이지를 준비하고 있습니다…";
            StartCoroutine(LoginFlow(provider));
        }

        IEnumerator LoginFlow(string provider)
        {
            BeginResponse start = null;

            yield return Request("/auth/begin", "POST", JsonUtility.ToJson(new BeginRequest { provider = provider }), null, value => start = JsonUtility.FromJson<BeginResponse>(value));

            if (start == null)
            {
                Busy = false;

                yield break;
            }

            // Only allow the configured service's launch endpoint to open a browser.
            if (start.authorizeUrl != baseUrl + "/auth/launch?state=" + start.attemptId)
            {
                Message = "로그인 주소를 검증하지 못했습니다.";
                Busy = false;

                yield break;
            }

            Application.OpenURL(start.authorizeUrl);
            Message = "브라우저에서 로그인한 뒤 게임으로 돌아와 주세요.";

            double deadline = Time.realtimeSinceStartupAsDouble + 300;

            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return new WaitForSecondsRealtime(2);

                PollResponse result = null;

                yield return Request("/auth/poll", "POST", JsonUtility.ToJson(new PollRequest { attemptId = start.attemptId }), start.pollToken, value => result = JsonUtility.FromJson<PollResponse>(value));

                if (result == null)
                {
                    Busy = false;

                    yield break;
                }

                if (result.status != "complete")
                {
                    continue;
                }

                if (result.profile == null || string.IsNullOrEmpty(result.token) || string.IsNullOrEmpty(result.profile.playerId))
                {
                    Message = "계정 정보를 확인하지 못했습니다.";
                    Busy = false;

                    yield break;
                }

                token = result.token;
                Profile = result.profile;
                Message = Ready ? "로그인되었습니다." : "처음 오셨네요! 사용할 닉네임을 설정해 주세요.";
                Busy = false;

                yield break;
            }

            Message = "로그인 시간이 초과되었습니다. 다시 시도해 주세요.";
            Busy = false;
        }

        public void InvalidateNickname()
        {
            CheckedNickname = null;
            NicknameAvailable = false;
            Message = "닉네임 중복 확인을 진행해 주세요.";
        }

        public void CheckNickname(string value)
        {
            if (Busy || Profile == null)
            {
                return;
            }

            Busy = true;
            CheckedNickname = null;
            NicknameAvailable = false;
            StartCoroutine(CheckFlow(value));
        }

        IEnumerator CheckFlow(string value)
        {
            yield return Request("/nicknames/availability?nickname=" + UnityWebRequest.EscapeURL(value), "GET", null, token, json =>
            {
                var result = JsonUtility.FromJson<Availability>(json);

                CheckedNickname = value;
                NicknameAvailable = result.available;
                Message = result.message;
            });

            Busy = false;
        }

        public void SetNickname(string value)
        {
            if (Busy || !NicknameAvailable || CheckedNickname != value || Profile == null)
            {
                return;
            }

            Busy = true;
            StartCoroutine(SetNicknameFlow(value));
        }

        IEnumerator SetNicknameFlow(string value)
        {
            yield return Request("/me/nickname", "POST", JsonUtility.ToJson(new NicknameRequest { nickname = value }), token, json =>
            {
                Profile = JsonUtility.FromJson<ArenaAccountProfile>(json);
                Message = "가입이 완료되었습니다.";
            });

            NicknameAvailable = false;
            CheckedNickname = null;
            Busy = false;
        }

        public void RefreshProfile()
        {
            if (Busy || Profile == null)
            {
                return;
            }

            Busy = true;
            StartCoroutine(RefreshFlow());
        }

        IEnumerator RefreshFlow()
        {
            yield return Request("/me", "GET", null, token, json => Profile = JsonUtility.FromJson<ArenaAccountProfile>(json));

            Busy = false;
        }

        public void Logout()
        {
            if (Busy)
            {
                return;
            }

            Busy = true;
            StartCoroutine(LogoutFlow());
        }

        IEnumerator LogoutFlow()
        {
            yield return Request("/auth/logout", "POST", "{}", token, _ =>
            {
            });

            Profile = null;
            token = null;
            CheckedNickname = null;
            NicknameAvailable = false;
            Busy = false;
            Message = "계정을 선택해 로그인하거나 가입하세요.";
        }

        public void CancelLogin()
        {
            if (Profile != null)
            {
                return;
            }

            activeRequest?.Abort();
            StopAllCoroutines();
            activeRequest = null;
            Busy = false;
            Message = "로그인을 취소했습니다. 계정을 다시 선택할 수 있습니다.";
        }

        void OnDestroy()
        {
            activeRequest?.Abort();
        }

        IEnumerator Request(string path, string method, string body, string bearer, Action<string> success)
        {
            using (var request = new UnityWebRequest(baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 20;
                request.redirectLimit = 0;

                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (!string.IsNullOrEmpty(bearer))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + bearer);
                }

                activeRequest = request;

                yield return request.SendWebRequest();

                activeRequest = null;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Message = "로그인 서버에 연결하지 못했습니다. 잠시 후 다시 시도해 주세요.";

                    try
                    {
                        var error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);

                        if (!string.IsNullOrEmpty(error?.message))
                        {
                            Message = error.message;
                        }
                    }
                    catch (Exception)
                    {
                    }

                    if (request.responseCode == 401 && bearer == token)
                    {
                        Profile = null;
                        token = null;
                        CheckedNickname = null;
                        NicknameAvailable = false;
                    }

                    yield break;
                }

                try
                {
                    success(request.downloadHandler.text);
                }
                catch (Exception)
                {
                    Message = "서버 응답을 확인하지 못했습니다. 다시 시도해 주세요.";
                }
            }
        }
    }
}
