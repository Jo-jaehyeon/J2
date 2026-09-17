using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DigitalArena
{
    public static class GameSceneFlow
    {

        public const string	MainMenu = "MainMenu";
        public const string	Multiplayer = "DragonEyeLake";
        public static bool IsLoading { get; private set; }
        // Until the server protocol includes assignments, retain the existing preview arena.
        public static int LocalArenaIndex { get; private set; } = J2.MultiplayerMap.MultiplayerMapPreview.StartArena;

        public static void SetLocalArenaAssignment(int zeroBasedArenaIndex)
        {
            if (zeroBasedArenaIndex < 0 || zeroBasedArenaIndex >= 8)
            {
                throw new System.ArgumentOutOfRangeException(nameof(zeroBasedArenaIndex));
            }

            LocalArenaIndex = zeroBasedArenaIndex;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            IsLoading = false;
            LocalArenaIndex = J2.MultiplayerMap.MultiplayerMapPreview.StartArena;
        }

        public static void Load(string scene)
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;

            try
            {
                var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);

                if (operation == null)
                {
                    IsLoading = false;

                    return;
                }

                operation.completed += _ => IsLoading = false;
            }
            catch
            {
                IsLoading = false;

                throw;
            }
        }
    }
}
