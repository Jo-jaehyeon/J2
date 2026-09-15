# 서버 TCP 연결 시작점

씬의 GameObject에 `NetworkManager` 컴포넌트를 붙이면 Start에서 `127.0.0.1:9900`으로 연결합니다.
Inspector의 host/port를 변경할 수 있습니다. 오브젝트를 자동 생성하지 않습니다.
OnDestroy에서 연결 중인 소켓과 세션을 정리합니다.

기존 레퍼런스 구조:
- NetworkManager → Connector.Connect(endpoint, sessionFactory)
- Session.cs: SocketAsyncEventArgs 송수신, PacketSession 프레임 조립
- ServerSession.cs: 연결/종료 로그와 수신 처리 진입점
- RecvBuffer.cs, SendBuffer.cs: 기존 버퍼 클래스
- Handler/PacketManager.cs → GamePacketHandler.g.cs → 카테고리별 PacketHandler.cs: Protobuf 분기 및 처리 함수

싱글턴, DontDestroyOnLoad, 자동 생성·재접속, 메인 스레드 큐, 세션 ID, 핸드셰이크·핑·채팅 송수신과 게임 이벤트는 없습니다.
Protocol의 패킷 C# 소스와 공식 Google.Protobuf 런타임 DLL을 사용합니다. 각 핸들러의 처리 함수는 빈 뼈대입니다. Common/proto/GenPacket.bat 실행으로 생성하며 Unity 카테고리 핸들러의 기존 구현은 보존됩니다.
연결 성공은 TCP 연결만 의미하며 로그인/인증 성공을 의미하지 않습니다.

Unity 메뉴 `J2/Network/Check TCP connection`은 실행 중인 서버에 TCP 연결만 확인합니다.
`J2/Network/Check TCP locally`는 임시 로컬 리스너에 연결하고 자동 송신이 없음을 확인합니다.

## Common packet sending
`NetworkManager.SendPacket(PacketId packetId, Google.Protobuf.IMessage packet)` forwards to the session's common sender. Pass an unencoded protobuf object, not pre-serialized bytes. It validates the outgoing C_ type and ID, serializes the 4-byte header and protobuf body through PacketCodec, and queues the frame on the TCP session. A true return value is local queue acceptance, not a server acknowledgement.

Example:
```csharp
var packet = new C_FindMatch { SessionId = network.SessionId, MatchType = 0, MMR = profile.mmr };
bool queued = network.SendPacket(PacketId.CFindmatch, packet);
```
All existing C_ protobuf packets use this same function. No packet-specific sending methods are needed. Matchmaking prerequisites and duplicate-request checks belong to DigitalArenaGame.SelectMultiplayer. S_EnterGame records the session ID and S_FindMatch records the game ID. C_EnterGame initiation, acceptance, cancellation and reconnection remain separate work. Existing map preview opens after local enqueue, not after a server-confirmed match.

Verification: `J2/Network/Check FindMatch locally` tests real loopback TCP sending of FindMatch and Ping, rejects incorrect IDs, null/server-direction packets and disconnected sends. See `Logs/send-packet-test.log`.

## Automatic singleton lifetime
Use `NetworkManager.Instance.SendPacket(packetId, packet)` on Unity's main thread. The manager is created automatically after the first scene loads (or on first access), so manual scene placement is unnecessary. Existing active scene managers are reused to preserve Inspector host/port values. `DontDestroyOnLoad` keeps the object and TCP session across scene changes. Duplicate manager components are disabled and removed before their Start can connect; their other GameObject components are preserved. Static references reset on play entry and access during shutdown does not create a replacement. Automatic instances use 127.0.0.1:9900 by default. Connection retry remains unimplemented.
