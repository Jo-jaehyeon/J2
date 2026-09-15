using System;
using System.Collections;
using System.Collections.Generic;

namespace DigitalArena
{
    public sealed partial class DigitalArenaGame
    {
        void EnterMultiplayerPreview()
        {
            if (mainMenu && CanEnterGame)
            {
                GameSceneFlow.Load(GameSceneFlow.Multiplayer);
            }
        }
    }
}
