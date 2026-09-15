using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DigitalArena
{
    // Retains Unity's native TextField and its IME/selection state between frames.
    // No IMGUI TextEditor, manual composition concatenation, or per-frame text assignment.
    public sealed class ArenaNicknamePanel : IDisposable
    {

        readonly ArenaAccountClient	account;

        string	playerId;
        bool	visible;

        readonly GameObject						owner;
        readonly PanelSettings					settings;
        readonly VisualElement					screen;
        readonly TextField						input;
        readonly UnityEngine.UIElements.Button	check, confirm, change;
        readonly UnityEngine.UIElements.Label	message;

        public ArenaNicknamePanel(Transform parent, ArenaAccountClient account, Font font, Texture2D background)
        {
            this.account = account;
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Nickname Input Panel Settings";
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.sortingOrder = 100;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/Login/NicknameTheme");
            owner = new GameObject("Nickname Input (UI Toolkit)");
            owner.transform.SetParent(parent, false);

            var document = owner.AddComponent<UIDocument>();

            document.panelSettings = settings;
            document.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Login/NicknameView");

            var root = document.rootVisualElement;

            root.pickingMode = PickingMode.Ignore;
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            screen = root.Q("nicknameScreen");
            screen.style.backgroundImage = new StyleBackground(background);
            input = root.Q<TextField>("nickname");
            input.isDelayed = false;
            input.multiline = false;
            message = root.Q<UnityEngine.UIElements.Label>("nicknameMessage");
            check = root.Q<UnityEngine.UIElements.Button>("checkNickname");
            confirm = root.Q<UnityEngine.UIElements.Button>("confirmNickname");
            change = root.Q<UnityEngine.UIElements.Button>("changeAccount");
            input.RegisterValueChangedCallback(_ => account.InvalidateNickname());
            check.clicked += () => AfterComposition(() => account.CheckNickname(input.value));
            confirm.clicked += () => AfterComposition(() => account.SetNickname(input.value));
            change.clicked += () =>
            {
                input.Blur();
                account.Logout();
            };
            screen.style.display = DisplayStyle.None;
        }

        void AfterComposition(Action action)
        {
            // Give focus loss a UI tick to commit the final Hangul syllable before
            // freezing the field for an HTTP request. Never submit a stale cached value.
            input.Blur();

            var requestedPlayer = playerId;

            input.schedule.Execute(() =>
            {
                if (visible && !account.Busy && account.Profile?.playerId == requestedPlayer)
                {
                    action();
                }
            });
        }

        public void Sync(bool show, float scale, Vector2 offset)
        {
            if (visible != show)
            {
                if (!show)
                {
                    input.Blur();
                }

                visible = show;
                screen.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!show)
            {
                return;
            }

            if (playerId != account.Profile.playerId)
            {
                playerId = account.Profile.playerId;
                input.SetValueWithoutNotify("");
                account.InvalidateNickname();
            }

            if (!Mathf.Approximately(settings.scale, scale))
            {
                settings.scale = scale;
            }

            screen.style.left = offset.x / scale;
            screen.style.top = offset.y / scale;
            input.isReadOnly = account.Busy;
            check.SetEnabled(!account.Busy);
            change.SetEnabled(!account.Busy);
            confirm.SetEnabled(!account.Busy && account.NicknameAvailable && account.CheckedNickname == input.value);
            confirm.text = account.Busy ? "처리 중…" : "닉네임 확정 · 시작하기";
            message.text = account.Message;
        }

        public void Dispose()
        {
            if (owner != null)
            {
                UnityEngine.Object.Destroy(owner);
            }

            if (settings != null)
            {
                UnityEngine.Object.Destroy(settings);
            }
        }
    }
}
