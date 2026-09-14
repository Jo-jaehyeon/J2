# 로그인 서버 위치 및 연결

로그인 서버는 Unity 프로젝트에서 분리하여 **J2_Server 솔루션의 LoginServer 하위 프로젝트**로 이동했습니다.

- 소스: `C:\Jerry\CPP_Server\J2_Server\LoginServer\LoginServer.py`
- Visual Studio 프로젝트: `C:\Jerry\CPP_Server\J2_Server\LoginServer\LoginServer.pyproj`
- Unity Editor → 로그인 서버: HTTP `http://127.0.0.1:8787`
- 로그인 서버 → MySQL: `localhost:3306`, DB `J2` (Workbench 표시명은 `j2`)
- 기존 게임 서버 TCP: `127.0.0.1:9900`

실행은 서버 솔루션 루트에서:

```powershell
./LoginServer/Start-LoginServer.ps1
```

J2에 `players`, `identities`, `sessions`, `characters`, `matches` 테이블을 생성했습니다. SQLite는 사용하지 않습니다. Unity에는 `Assets/Resources/AccountSettings.json`의 로그인 서버 주소만 넣고 DB 비밀번호는 넣지 않습니다.

서버의 MySQL 접속 정보는 Git에서 제외된 `LoginServer/settings.local.json`에 설정되어 있습니다. 네이버·카카오·구글 실제 로그인은 제공자 앱 키와 콜백 등록이 별도로 필요합니다.

상세 실행 방법, 테이블 설명, 개발자 콘솔 설정 및 검증 범위는 [LoginServer 설정 안내](../../../CPP_Server/J2_Server/LoginServer/README.md)를 확인하세요.
