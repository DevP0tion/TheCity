# The City / 도시 — 프로젝트 개요

## 모드 정보
- **게임**: Slay the Spire 2 (Godot 4.5.1 / C# / .NET 9.0)
- **모드 ID**: `TheCity`
- **의존성**: BaseLib (Alchyr.Sts2.BaseLib)
- **목표**: 컨텐츠 추가 모드, 멀티플레이어 호환

## 기술 스택
- **엔진**: Godot 4.5.1 (MegaDot)
- **언어**: C# (.NET 9.0)
- **패칭**: HarmonyX (게임 내장)
- **모드 라이브러리**: BaseLib (NuGet: `Alchyr.Sts2.BaseLib`)
- **모드 포맷**: `.dll` + `.pck` + `.json` 매니페스트

## 프로젝트 구조
```
TheCity/
├── assets/                    # Godot 에셋 (이미지, 씬 등)
│   ├── mod_image.png
│   └── localization/
│       ├── eng/
│       │   └── settings_ui.json
│       └── kor/
│           └── settings_ui.json
├── src/                       # C# 소스코드
│   ├── ModStart.cs            # 모드 엔트리포인트 (namespace: TheCity)
│   ├── TheCityConfig.cs       # BaseLib SimpleModConfig 설정
│   ├── Resource/              # 공유 자원 시스템 (namespace: TheCity.Resource)
│   │   ├── SharedResourceManager.cs
│   │   ├── SharedResourceSync.cs
│   │   ├── CardFields.cs
│   │   └── CombatLifecyclePatches.cs
│   └── UI/                    # UI 시스템 (namespace: TheCity.UI)
│       ├── ResourceDisplay.cs
│       ├── ResourcePanel.cs
│       └── ResourcePanelPatch.cs
├── lib/                       # 게임 DLL (gitignore)
├── doc/                       # 설계 문서
├── TheCity.csproj
├── TheCity.json               # 모드 매니페스트
├── project.godot
├── Directory.Build.props      # 로컬 경로 설정 (gitignore)
└── Directory.Build.props.example
```

## 네임스페이스 규칙
- `TheCity` — 루트 (ModStart, Config 등)
- `TheCity.Resource` — 자원 시스템
- `TheCity.UI` — UI 시스템
- `TheCity.Event` — 동적 이벤트 시스템 (미구현)

## 엔트리포인트 패턴
```csharp
[ModInitializer(nameof(ModInit))]
public static class ModStart
{
    public const string ModId = "TheCity";
    public static void ModInit() { ... }
}
```
- `static class` 사용 (Node 상속 X, partial X)
- 로깅: `GD.Print($"[{ModId}] message")`

## 빌드
```bash
# DLL만 빌드
dotnet build -c Release

# DLL + .pck (Godot 경로 필요)
dotnet publish -c Release
```

## MCP 도구
- STS2 Modding MCP 연결 가능 (151개 도구)
- 게임 코드 검색, 엔티티 조회, 훅 탐색, 코드 생성, 디버깅
- MCP 서버는 Windows 로컬에서 실행, TCP 21337
