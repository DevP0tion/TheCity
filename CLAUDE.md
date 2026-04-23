# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**TheCity** — a Slay the Spire 2 mod built on Godot 4.5.1 (MegaDot), C# / .NET 9.0, HarmonyX patching, and the `Alchyr.Sts2.BaseLib` NuGet package. Mod format is `.dll` + `.pck` + `TheCity.json` manifest. Targets multiplayer compatibility.

Full design docs live in `doc/` — see `doc/project-overview.md` for the index.

## Build & Publish

```bash
# Code-only change — produces DLL, auto-copies to $(ModsPath)/TheCity/
dotnet build -c Release

# Full mod — produces DLL + .pck, requires Directory.Build.props with GodotPath set
dotnet publish -c Release
```

- `.pck` packaging requires `GodotPath` pointing to a **MegaDot 4.5.1 mono** binary (not vanilla Godot). Set in `Directory.Build.props` (gitignored, copy from `.example`).
- `Sts2PathDiscovery.props` auto-detects the Steam install of Slay the Spire 2 via registry on Windows. If the game isn't installed, falls back to `lib/` which must contain `sts2.dll` + `0Harmony.dll` copied from the game's `data_sts2_windows_x86_64/` directory.
- `$(ModsPath)` resolves to the game's `mods/` folder when the game is installed, or `output/mods/` otherwise. Build's `PostBuildEvent` automatically copies `.dll` and `TheCity.json` there.
- BaseLib itself must be installed separately into the game's `mods/` folder (download from Alchyr/BaseLib-StS2 releases) — it's a runtime dependency, not just a NuGet reference.

## Architecture — Big Picture

### Entry point
`src/ModStart.cs` uses BaseLib's `[ModInitializer(nameof(ModInit))]` attribute on a **static class** (never `partial`, never a `Node` subclass). Order inside `ModInit` is load-bearing: `ModConfigRegistry.Register` must run **before** `harmony.PatchAll()` so config values are available when patches apply.

### Three subsystems with strict layering
```
ModStart ──registers──▶ TheCityConfig (BaseLib SimpleModConfig)
   │
   ├──Harmony patches──▶ CombatManager (lifecycle)
   │                     └─▶ SharedResourceManager.Initialize/Cleanup
   │
   └──Harmony patches──▶ NCombatRoom._Ready (UI injection)
                         └─▶ ResourcePanel (subscribes to SharedResourceManager events)
                             └─▶ ResourceDisplay (one per registered resource)
```

**Key invariant: UI → Resource is a one-way dependency.** `SharedResourceManager` publishes four events (`ValueChanged`, `ResourceRegistered`, `Initialized`, `CleanedUp`) and knows nothing about UI. `ResourcePanel` subscribes on `_Ready` and unsubscribes on `_ExitTree`. Never import `TheCity.UI` from `TheCity.Resource`.

### Shared resource system (`src/Resource/`)
- `SharedResourceManager` is a **static class with `Dictionary<string, int>`** — no instance, no DI. Values are party-shared integers keyed by string ID.
- Register resources in `ModInit` **before** `CombatManager.SetUpCombat` runs. Registration after initialize is silently ignored (the initialize loop only resets already-known keys).
- `Modify(id, delta, sync: true)` / `Set(id, val, sync: true)` fire `ValueChanged` AND send a `SharedResourceSyncMessage` over the network. The receive handler calls `Set(..., sync: false)` to avoid rebroadcast loops. **Always respect the `sync` parameter** when adding new code paths that mutate resources.
- `CardFields` attaches custom int properties to `CardModel` instances via a module-level `Dictionary<CardModel, int>`. It's extension-method based — no game code modification, no publicizer needed. `CardFields.ClearAll()` runs on combat end to prevent stale references.

### UI injection pattern (`src/UI/`)
- `ResourcePanelPatch` Harmony-postfixes `NCombatRoom._Ready` to inject `ResourcePanel` as a child. The same pattern (`<NRoom>._Ready` Postfix) is how to inject UI into `NMapRoom`, `NShopRoom`, `NRestSiteRoom`, `NEventRoom`.
- Panels are built entirely from code (`StyleBoxFlat`, `VBoxContainer`, etc.) — no `.tscn` scenes for runtime UI.
- `ResourcePanel.Instance` singleton check prevents double-injection if `_Ready` fires multiple times.

### Multiplayer networking (`INetMessage` + `IPacketSerializable`)
Patterns that are easy to get wrong (see `doc/multiplayer-networking.md` for full list):
- Use `NetTransferMode.Reliable` (property is `Mode`, **not** `TransferMode`).
- Use `PacketWriter` / `PacketReader` (**not** `StreamPeerBuffer`).
- Message handler signature is `(T msg, ulong senderId)` — **`ulong`**, not `int`.
- `UnregisterMessageHandler<T>` requires the exact same delegate reference passed to `Register` — store the handler in a field if needed.
- Register on `CombatManager.SetUpCombat` Postfix, unregister on `CombatManager.EndCombatInternal` Postfix — anywhere else risks null `NetService` or leaked handlers.
- Singleplayer: `RunManager.Instance.NetService == null` — all send calls become no-ops gracefully if you null-check first.

### Namespace rules
- `TheCity` — root only (ModStart, TheCityConfig)
- `TheCity.Resource` — shared resources, card fields, combat lifecycle patches
- `TheCity.UI` — combat UI panels and their patches
- `TheCity.Event` — dynamic event system (designed in `doc/dynamic-event-design.md`, not yet implemented)

### Logging
Always prefix with `[{ModStart.ModId}]`: `GD.Print($"[{ModStart.ModId}] message")`. Uses Godot's `GD.Print`, not `Console.WriteLine`.

## Agent Team Spawning — MANDATORY

**이 프로젝트는 Agent Team 워크플로를 전제로 설계됐다. `.claude/agents/` 에 6개 에이전트 (cto-lead, sts-game-analyst, mod-code-analyst, mod-implementer, runtime-qa, web-researcher) 가 정의되어 있다. 이 에이전트들을 호출할 때 반드시 아래 규칙을 따른다.**

### RULE-1: Teammate 우선, sub-agent 금지
`.claude/agents/` 정의 에이전트를 호출할 때는 `TeamCreate` 로 팀을 먼저 만들고 `Agent(team_name=..., name=..., prompt="팀 합류 확인")` 로 Teammate 스폰. 그냥 `Agent(subagent_type=..., prompt="<긴 지시>")` 형태 단발 호출은 **금지** — sub-agent로 동작해 SendMessage/공유 TaskList/idle 재사용이 불가능해진다.

**예외:** 100% 단발 read-only 조사 1회성 (후속 작업 없음 확정) 인 경우에 한해 sub-agent 허용. 조금이라도 멀티스텝/후속 구현 가능성이 있으면 Teammate.

### RULE-2: Agent prompt 최소화
Teammate 스폰 시 `Agent` 의 `prompt` 필드는 한 문장 이내 ("팀 합류 확인" 수준). 실제 작업 지시는 스폰 직후 `SendMessage(to: name, ...)` 로 별도 전달. prompt 에 지시가 들어가면 Claude Code 가 sub-agent 모드로 전환해 팀 기능을 못 쓴다 (`teammate-spawn` SKILL.md 확인).

### RULE-3: cto-lead 위임 원칙
복합 기능 (조사 + 구현 + QA) 는 cto-lead 에 위임. 직접 하위 에이전트에 작업을 찔러넣지 말고 cto-lead 가 오케스트레이션하도록 둔다.

### RULE-4: 기존 팀 재사용
같은 세션에서 동일 에이전트가 필요하면 새로 `Agent` 호출하지 말고 `SendMessage(to: <기존이름>, ...)` 로 재지시. 새 Agent 호출은 컨텍스트 없는 신규 팀원을 만들어 토큰/시간 낭비.

---

## 용어 규칙

문서·코드 주석·에이전트 본문·대화 응답에는 **쉬운 한국어**를 쓴다. 영어 음차나 모호한 은어를 발견하면 아래 표에 따라 교체한다. 새 음차어가 등장하면 이 표에 행을 추가하고 같은 커밋에서 기존 사용처도 치환한다.

| 지양 (음차·모호) | 권장 (쉬운 한국어) |
|-----------------|-------------------|
| 피처 | 기능 |
| 로드 베어링 | 순서가 바뀌면 동작 안 함 |
| 어댑테이션 | 조정 / 수정 |
| 라운드트립 / 세이브 라운드트립 | 저장·복원 왕복 |
| (검증) 매트릭스 | (검증) 표 |
| 원자적 사고 | 한 번에 끝내기 |
| 실측 | 실행 검증 |
| 단독 "엣지" | "엣지 케이스" |
| 리전 관행 | 주석 구획(region) 관행 |
| 입력 요약 복창 | 입력 요약 재확인 |
| 블랙박스 Task 호출 | 목적 설명 없이 Task를 던지지 않기 |
| promising | 유력해 보이는 |
| CTD, stale, healthy 등 영단어 단독 사용 | 한국어 + `영단어` 괄호 병기 (예: 강제 종료(CTD), 이미 해제된(stale) 참조) |

**예외 (음차어 유지 허용):**
- `.claude/agents/*.md` 의 `Triggers:` 줄 — 사용자가 "피처 추가해줘"라고 말해도 에이전트가 매칭되어야 하므로 호출 어휘는 음차어까지 포함해 그대로 둔다.
- 공식 기술 고유명사: Godot, MegaDot, Harmony, HarmonyX, BaseLib, Postfix / Prefix / Transpiler, Publicizer 등.
- 영문 코드 식별자 (변수·메서드·타입·네임스페이스명).
- 본문에서 영단어를 **처음 정의할 때** 괄호로 병기하는 경우 (예: "주석 구획(region) 관행"). 이후 반복 언급은 한국어만.

---

## 사용자 질문 규칙

사용자에게 명확화·선택·의사결정을 요청할 때는 가능하면 **`AskUserQuestion` 도구**를 사용한다. 자유 텍스트로 "X일까 Y일까?"라고 묻는 대신 구조화된 선택지로 제시해 사용자가 옵션 키 하나로 답할 수 있게 한다. (Claude Code 환경에서 `AskUserQuestion`은 `ToolSearch`로 먼저 스키마를 로드해야 하는 deferred 도구다 — `ToolSearch(query: "select:AskUserQuestion")` 후 호출.)

**사용할 때:**
- 요청이 모호해 둘 이상의 해석이 가능할 때 (세션 훅의 모호성 점수 경고 포함).
- 범위·배치·네이밍 결정의 기본값이 분명하지 않을 때 (예: 새 규칙을 `CLAUDE.md` vs 별도 문서 어디에 둘지).
- 파괴적·되돌리기 어려운·외부에 영향 주는 작업 직전 승인이 필요할 때.
- 대안을 2–5개로 간추릴 수 있을 때.

**작성 팁:**
- 각 옵션은 짧은 이름 + 한 줄 트레이드오프.
- 기본값(권장안)이 있으면 맨 앞에. 필요하면 "기타 (직접 입력)" 옵션 추가.
- 한 응답에 한 질문. 여러 결정이 엮여 있으면 순차로 나눠서 첫 답이 이후 질문을 바꾸게 한다.

**예외 (자유 텍스트 허용):**
- 한 번의 짧은 진행 확인 (작업 완료 후 "이대로 갈까요?" 수준의 1회성 문장).
- 선택지가 너무 세분화돼 구조화하면 오히려 답변이 느려지는 경우.
- 에이전트 내부 호출 / cto-lead의 오케스트레이션 경로 — 사용자에게 직접 묻는 상황이 아닐 때.

---

## STS2 Modding MCP

This repo has `.mcp.json` configured for the local STS2 Modding MCP server (~150 tools). When working on game integration — hook signatures, entity lookup, patch scaffolding, game code search — prefer MCP tools over guessing:

- `search_game_code`, `get_entity_source`, `list_hooks`, `get_hook_signature` — investigate game internals before writing patches.
- `generate_harmony_patch`, `generate_card`, `generate_event`, `generate_net_message` — scaffolding generators.
- `bridge_*` tools — live game state inspection and scripted play via a running game instance.

`doc/todo.md` lists specific MCP queries the design docs flagged for later verification (e.g. host-detection API, `AfterCardPlayed` signature).

## Known-pending design

`doc/dynamic-event-design.md` specifies a host-driven event generation system (`src/Event/` — not yet created). Before implementing, use MCP `search_game_code` with patterns like `"AddLocalHostPlayer|GetLocalPlayerId|IsHost"` to confirm the current host-detection API — the doc explicitly marks this as unverified.
