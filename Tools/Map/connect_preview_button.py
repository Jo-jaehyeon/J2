from pathlib import Path
p=Path('Assets/Scripts/DigitalArena/DigitalArenaGame.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('void SelectMultiplayer() { if(CanEnterGame) multiplayerNotice=true; } // TODO: multiplayer lobby and network session.', 'void SelectMultiplayer() { EnterMultiplayerPreview(); }')
s=s.replace('            UpdateTouchInput();','            if (multiplayerPreview != null) { UpdateMultiplayerPreview(); return; }\n            UpdateTouchInput();',1)
s=s.replace('            if(mainMenu) { MainMenu(); ui.End(); return; }','            if (multiplayerPreview != null) { DrawMultiplayerPreview(); ui.End(); return; }\n            if(mainMenu) { MainMenu(); ui.End(); return; }',1)
s=s.replace('멀티 플레이  ·  준비 중','멀티 플레이').replace('멀티 플레이는 추후 업데이트 예정입니다. (TODO)','맵을 불러오지 못했습니다. 다시 시도해 주세요.')
s=s.replace('            nicknamePanel?.Dispose();','            if (multiplayerPreview != null) multiplayerPreview.Shutdown();\n            nicknamePanel?.Dispose();',1)
p.write_text(s,encoding='utf-8')
