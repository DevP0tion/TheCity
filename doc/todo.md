# TODO / 백로그

## 우선순위 높음

### abnormality-battle — M1 런타임 실행 검증 필요
Q4 훅 존재·시그니처 검증 완료 (verification.md §5.5). M1 은 M1-1~M1-7 + R2/R4 만 남음.

- [ ] M1-1: BaseLib `CustomEncounterModel` 존재·사용 방식
- [ ] M1-2: Slugify 실행 검증 (샘플 EncounterModel.Id.Entry 값 확인)
- [ ] M1-3: `res://scenes/backgrounds/` 자산 복사·import 파이프라인
- [ ] M1-4: `Hook.ShouldStopCombatFromEnding` 시그니처 (다부위 전투 종료 조건 재검증)
- [ ] M1-5: `MinionPower` 적용 API (`CombatCmd.ApplyPower` / `PowerModifier` 어느 쪽)
- [ ] M1-6: `EnterCombatWithoutExitingEvent` reward 빈 배열 시 보상 UI 동작
- [ ] M1-7: 이벤트 ID prefix 규약 (ABNORMALITY_* 충돌 테스트)

### abnormality-battle — 런타임 QA 체크 (Implementer M1 완료 후 runtime-qa)
- [ ] R2: 세이브/로드 시 다부위 partial load 엣지 케이스에서 `Hook.AfterMapGenerated` 발화 타이밍
- [ ] R4: 첫 방 진입 시 `IRunState.CurrentMapCoord` null window 가능성 — `AddVisitedMapCoord` 호출 순서 실행 검증

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
