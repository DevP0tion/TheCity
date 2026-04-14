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

## STS2 Modding MCP

This repo has `.mcp.json` configured for the local STS2 Modding MCP server (~150 tools). When working on game integration — hook signatures, entity lookup, patch scaffolding, game code search — prefer MCP tools over guessing:

- `search_game_code`, `get_entity_source`, `list_hooks`, `get_hook_signature` — investigate game internals before writing patches.
- `generate_harmony_patch`, `generate_card`, `generate_event`, `generate_net_message` — scaffolding generators.
- `bridge_*` tools — live game state inspection and scripted play via a running game instance.

`doc/todo.md` lists specific MCP queries the design docs flagged for later verification (e.g. host-detection API, `AfterCardPlayed` signature).

## Known-pending design

`doc/dynamic-event-design.md` specifies a host-driven event generation system (`src/Event/` — not yet created). Before implementing, use MCP `search_game_code` with patterns like `"AddLocalHostPlayer|GetLocalPlayerId|IsHost"` to confirm the current host-detection API — the doc explicitly marks this as unverified.
