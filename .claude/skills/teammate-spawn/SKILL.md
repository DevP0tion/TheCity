---
name: teammate-spawn
description: |
  Claude Code Agent Team 시스템으로 `.claude/agents/`에 정의된 에이전트들을 영구 Teammate로 스폰하는 검증된 레시피.
  서브에이전트(단발 Agent/Task 호출)가 아닌, team_name이 부여된 mailbox-addressable Teammate로 만든다.
  SendMessage로 지시·공유 TaskList·idle 대기까지 가능해진다.
  Use when: 여러 에이전트를 동시에 운영해야 하는 프로젝트, peer DM/공유 작업 목록이 필요한 워크플로우, 이전에 Agent 도구로 스폰했는데 서브에이전트로만 동작했던 경우.
  Triggers: teammate spawn, 팀원 스폰, agent team, 에이전트 팀 구성, TeamCreate, 팀원 만들기,
  spawn teammates, setup team, 팀 세팅, not sub-agent, 서브에이전트 아닌, persistent agents.
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - Bash
  - TeamCreate
  - Agent
  - SendMessage
  - TaskCreate
  - TaskList
---

# teammate-spawn — Agent Team spawning recipe

`.claude/agents/`에 정의된 에이전트들을 영구 Teammate로 스폰하는 3-step 절차.

## 왜 필요한가

Agent 도구를 평범하게 호출하면 **서브에이전트**가 된다 — 작업 하나 실행 후 사라짐.
팀 기능(peer DM, 공유 TaskList, idle 대기, SendMessage 주소 지정)을 쓰려면 두 가지가
동시에 충족돼야 한다:

1. **TeamCreate**로 팀 생성 (공유 설정 + TaskList)
2. **Agent 호출 시 `team_name` + `name` + 극도로 최소화된 `prompt`**

특히 **prompt가 길거나 작업 지시를 담으면 서브에이전트 모드로 동작**한다. 이게 반복 실패의
핵심 원인. prompt는 한 문장 이하 "팀 합류 확인"만.

## 사전 조건

각 에이전트 파일(`.claude/agents/{name}.md`) frontmatter의 `tools:` 목록에
**`SendMessage` 포함 필수**. 없으면 먼저 Edit으로 추가:

```yaml
tools: ..., SendMessage
```

없으면 팀원 간 통신/리포트 불가 → 실질적으로 서브에이전트와 구분 안 됨.

## 레시피

### Step 1 — 팀 생성

```
TeamCreate(
  team_name:   "<team-name>",
  description: "<purpose 한 줄>",
  agent_type:  "orchestrator"    # 선택 — 팀 리드의 역할 태그
)
```

성공 반환:
- `team_file_path`: `~/.claude/teams/<team-name>/config.json`
- `lead_agent_id`: `team-lead@<team-name>` (메인 세션의 SendMessage 주소)

### Step 2 — 전원 병렬 스폰 (단일 응답 내)

**모든 Agent 호출을 하나의 assistant 응답에서 병렬로** 낸다. 순차/분할 호출하면 UI에서
각자 서브에이전트로 보이고 사용자 경험이 어긋난다.

각 Agent 호출 파라미터:

| 파라미터 | 값 |
|---|---|
| `subagent_type` | `.claude/agents/` 에이전트 파일 이름 (예: `cto-lead`) |
| `name` | 같은 값 권장 — SendMessage 주소가 된다 |
| `team_name` | Step 1에서 만든 이름 |
| `prompt` | **"Joined <team> team. Ready."** — 이게 전부. 작업 지시 금지 |
| `description` | 5~10자 UI 라벨 (예: "Spawn cto-lead teammate") |

**prompt에 절대 넣지 말 것**:
- 파일 읽기 지시 (예: "CLAUDE.md 읽어라")
- MCP 호출 지시
- readiness 리포트 양식
- "확인하고 보고하라" 유형 요청

이걸 넣는 순간 서브에이전트로 떨어진다. readiness 리포트가 필요하면 **spawn 이후 별도
SendMessage로** 요청한다.

### Step 3 — 확인

각 Agent 호출 반환이 아래 형태면 성공:
```
Spawned successfully.
agent_id: <name>@<team-name>
name: <name>
team_name: <team-name>
The agent is now running and will receive instructions via mailbox.
```

이어서 각 teammate로부터 `idle_notification`이 도착하면 완전 대기.

## 스폰 후 운영

### 지시는 SendMessage로만

```
SendMessage(
  to:      "<name>",
  summary: "짧은 UI 미리보기 (5~10자)",
  message: "실제 작업 내용"
)
```

### 전체 브로드캐스트
```
SendMessage(to: "*", message: "...")   # 팀 규모 linear 비용 — 남용 금지
```

### 공유 TaskList 활용
복잡 워크플로우는 `TaskCreate`로 작업을 만들고 `TaskUpdate`의 `owner`로 배정. 팀원이
`TaskList`로 자기 일을 찾게 한다.

### 종료
```
SendMessage(to: "*", message: {type: "shutdown_request"})
# 각 teammate가 shutdown_response 반환 후 프로세스 종료
TeamDelete()
```

## 함정 체크리스트

- [ ] `prompt`가 2문장 이상이면 줄여라 — 서브에이전트 떨어짐의 90% 원인
- [ ] 모든 Agent 호출이 **한 응답 내 병렬**인지 확인
- [ ] `team_name` 파라미터가 TeamCreate한 이름과 **정확히** 일치 (오타 주의)
- [ ] 에이전트 파일 `tools:`에 `SendMessage` 포함됐는지
- [ ] `subagent_type`이 `.claude/agents/<name>.md` 파일명과 정확히 일치 (대소문자 포함)
- [ ] 같은 이름의 teammate가 팀에 이미 있으면 중복 스폰 실패 → 다른 이름 또는 TeamDelete

## 실패 징후 → 원인

| 징후 | 원인 |
|------|------|
| Teammate가 파일 읽기/MCP 호출을 시작함 | `prompt`가 작업 지시 포함 — 최소화 |
| `TeamCreate` 없이 바로 `Agent` 호출 | 팀 context 없음 → 서브에이전트 |
| 팀원끼리 SendMessage 실패 | 에이전트 `tools:`에 `SendMessage` 누락 |
| spawn은 됐는데 idle 알림이 안 옴 | `prompt`가 길어 아직 작업 중. 줄이고 재시도 |
| UI에서 "subagent" 라벨로 표시됨 | 병렬이 아니라 순차 호출 — 한 응답에 병렬로 |

## 전형적 팀 구성 예시

```
team_name: <project>
members (subagent_type = name):
  - cto-lead / orchestrator / 워크플로우 조율·품질 게이트
  - analyst-* (여러 명)      / 병렬 조사 (코드/웹/문서)
  - implementer              / 코드 작성 + 빌드 검증
  - qa                       / 런타임/테스트 검증
```

각 역할은 `.claude/agents/<name>.md`로 정의되어 있어야 한다. 이 스킬은 정의된 에이전트들을
팀원으로 활성화하는 방법만 다룬다 — 에이전트 자체 설계는 별도.
