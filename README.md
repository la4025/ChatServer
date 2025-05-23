# 채팅 애플리케이션 프로젝트

이 문서는 AI를 사용하여 제작하였습니다.

이 프로젝트는 C#으로 작성된 다양한 네트워크 애플리케이션의 모음입니다. 
기본적인 에코 서버부터 다중 채팅 서버까지 단계적으로 발전된 버전들을 포함하고 있습니다.
ChatGPT 와 CURSOR AI를 활용하여 제작한 프로젝트입니다.

## 프로젝트 구조

### 1. 에코 서버/클라이언트 시리즈
- **EchoServer**: 기본적인 에코 서버 구현
  - 단일 클라이언트 연결 지원
  - 클라이언트로부터 받은 메시지를 그대로 반환

- **EchoServer_Multi**: 다중 클라이언트 지원 에코 서버
  - 여러 클라이언트의 동시 연결 지원
  - 각 클라이언트의 메시지를 독립적으로 처리

- **EchoServer_Async**: 비동기 처리 에코 서버
  - async/await 패턴을 사용한 비동기 처리
  - 더 효율적인 리소스 관리

### 2. 채팅 서버/클라이언트 시리즈
- **ChatServer/ChatClient**: 기본 채팅 구현
  - 기본적인 1:1 채팅 기능
  - 단순한 메시지 송수신

- **ChatServerV2/ChatClientV2**: 개선된 채팅 구현
  - 다중 클라이언트 지원
  - 기본적인 채팅방 기능

- **ChatServerV3/ChatClientV3**: 고급 채팅 구현
  - 다중 채팅방 지원
  - 닉네임 시스템
  - 입장/퇴장 메시지
  - 비동기 처리

- **ChatServerV3_improve/ChatClientV3_improve**: ChatServerV3/ChatClientV3 개선
  - V3의 모든 기능 포함
  - 추가적인 안정성 및 성능 개선
  - 채팅방 간 채팅 구분 안되는 문제 해결
  - 구분자를 사용하여 바이트 문자열 오류 해결

- **ChatServerV4/ChatClientV4**: 명령어 추가
  - V3_improve의 모든 기능 포함
  - 귓속말 기능 추가 (/w 명령어)
  - 방 유저 목록 확인 기능 (/users 명령어)
  - 자신의 닉네임 확인 기능 (/whoami 명령어)
  - 명령어 시스템 개선 (/exit 명령어)

- **ChatServerV5/ChatClientV5**: 객체지향 설계 적용
  - V4의 모든 기능 포함
  - 객체지향 프로그래밍 원칙 적용
  - 코드 구조 개선 및 유지보수성 향상
  - ClientHandler 클래스를 통한 클라이언트 관리

- **ChatServerV6/ChatClientV6**: JSON 기반 메시지 시스템
  - V5의 모든 기능 포함
  - JSON 기반의 구조화된 메시지 시스템 도입
  - 메시지 타입 구분 (chat, command, join)
  - 더 안정적인 메시지 처리
  - 확장 가능한 메시지 구조

## 기술 스택
- C# (.NET 9.0)
- TCP/IP 소켓 프로그래밍
- 비동기 프로그래밍 (async/await)
- 멀티스레딩

## 주요 기능
1. 다중 클라이언트 지원
2. 채팅방 시스템
3. 닉네임 시스템
4. 실시간 메시지 브로드캐스팅
5. 비동기 통신
6. 입장/퇴장 알림

## 실행 방법
1. 서버 실행:
   ```
   dotnet run --project [ServerProjectName]
   ```

2. 클라이언트 실행:
   ```
   dotnet run --project [ClientProjectName]
   ```

## 프로젝트 발전 과정
1. 기본 에코 서버 구현
2. 다중 클라이언트 지원 추가
3. 비동기 처리 도입
4. 채팅 기능 구현
5. 채팅방 시스템 도입
6. 사용자 인터페이스 개선
7. 안정성 및 성능 최적화

## 프로젝트 타임라인

### 2024년 3월
#### EchoServer/ChatClient 기본 구현
- 단일 클라이언트 연결 지원

#### EchoServer_Multi/ChatClient 구현
- 다중 클라이언트 지원 추가

#### EchoServer_Async 구현
- 비동기 처리 도입

#### ChatServer/ChatClient V1 구현
- 기본 채팅 기능

#### ChatServer/ChatClient V2 구현
- 다중 클라이언트 및 채팅방 기능 추가

### 2024년 4월
#### ChatServer/ChatClient V3 구현
- 다중 채팅방 및 닉네임 시스템 도입

#### ChatServer/ChatClient V3_improve 구현
- 안정성 및 성능 개선
- 채팅방 간 채팅 구분 안되는 문제 해결
- 구분자를 사용하여 바이트 문자열 오류 해결

### 2024년 5월
#### ChatServer/ChatClient V4 구현
- 명령어 시스템 개선 및 새로운 기능 추가
- 귓속말 기능 추가 (/w 명령어)
- 방 유저 목록 확인 기능 (/users 명령어)
- 자신의 닉네임 확인 기능 (/whoami 명령어)

#### ChatServer/ChatClient V5 구현
- 객체지향 설계 적용
- ClientHandler 클래스를 통한 클라이언트 관리

#### ChatServer/ChatClient V6 구현
- JSON 기반 메시지 시스템 도입
- 구조화된 메시지 처리
- 확장 가능한 메시지 구조 설계

## 향후 계획
1. 웹소켓 지원 추가
2. 데이터베이스 연동
3. 사용자 인증 시스템
4. 채팅방 암호화
5. 모바일 클라이언트 개발