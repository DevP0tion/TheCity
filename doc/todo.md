# TODO / 백로그

## 우선순위 높음

### 동적 이벤트 시스템 구현
- [ ] `src/Event/` 폴더 구조 생성
- [ ] `EventData`, `EventDataRegistry` 구현
- [ ] `EventDataSyncMessage` + `EventDataSync` (INetMessage)
- [ ] `HostUtil.IsHost` — MCP `search_game_code`로 정확한 API 확인
- [ ] `IEventDataGenerator` 인터페이스 + 기본 구현
- [ ] `EventManager` 오케스트레이터
- [ ] `CustomEventModel` 기반 동적 이벤트 클래스

### 호스트 판별 API 확인
- [ ] MCP `search_game_code` pattern: `"AddLocalHostPlayer|GetLocalPlayerId|IsHost"`
- [ ] MCP `get_entity_source` class: `"LocalContext"`
- [ ] 멀티플레이어 테스트

## 우선순위 중간

### 카드 구현
- [ ] 카드 사용 시 공유 자원 변경하는 카드 작성
- [ ] AfterCardPlayed 훅 시그니처 MCP로 확인
- [ ] 커스텀 카드 풀 결정 (전 캐릭터 / 특정 캐릭터)

### UI 개선
- [ ] TheCityConfig.EnableResourceUI 연동
- [ ] 자원 아이콘 추가
- [ ] 유물 바 정확한 위치 게임 실행 후 조정
- [ ] 증가/감소 시 색상 구분

### 설정 확장
- [ ] 필요 시 TheCityConfig에 설정 항목 추가

## 우선순위 낮음

### 로컬라이제이션
- [ ] 카드/유물/이벤트 로컬라이제이션 (eng, kor)
- [ ] `assets/localization/eng/cards.json` 등

### 리소스
- [ ] mod_image.png 교체
- [ ] 카드 이미지 (250x190 + 1000x760)
- [ ] .pck 패키징 테스트 (`dotnet publish`)

### 테스트
- [ ] MCP Bridge로 자동 테스트 시나리오 구성
- [ ] 멀티플레이어 동기화 테스트

## MCP 세션에서 확인할 사항
```
# 호스트 판별
search_game_code: "AddLocalHostPlayer|GetLocalPlayerId|IsHost"
get_entity_source: "LocalContext"

# 카드 플레이 훅
search_game_code: "AfterCardPlayed"
list_hooks (AfterCardPlayed 시그니처)

# 이벤트 풀
search_game_code: "EventPool|SharedEvents|AddEvent"
```
