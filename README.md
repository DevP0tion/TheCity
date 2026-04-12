# STS2Mod

Slay the Spire 2 mod project (Godot 4.5.1 / C# / .NET 9.0)

## Setup

### 필수 도구
- .NET SDK 9.0+
- IDE: Rider (권장) 또는 VS Code + C# Dev Kit
- Godot 4.5.1 Mono (`.pck` 패키징용, Publish 시 필요)

### 필수 라이브러리
게임 디렉토리에서 `lib/` 폴더로 복사:
- `sts2.dll`
- `0Harmony.dll`

경로: `<Steam>/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64/`

### 빌드 설정
`Directory.Build.props.example`을 `Directory.Build.props`로 복사 후 로컬 경로 수정:
```xml
<GodotPath>C:/megadot/MegaDot_v4.5.1-stable_mono_win64.exe</GodotPath>
```

### BaseLib
게임의 `mods/` 폴더에 BaseLib 설치 필요:
https://github.com/Alchyr/BaseLib-StS2/releases

## Build

```bash
# DLL만 빌드 (코드 변경 시)
dotnet build -c Release

# Publish (DLL + .pck 생성, Godot 경로 필요)
dotnet publish -c Release
```

## Project Structure

```
STS2Mod/
├── STS2ModCode/       # C# 소스코드
│   └── MainFile.cs    # 모드 엔트리포인트
├── STS2Mod/           # Godot 에셋 (이미지, 씬 등)
├── lib/               # 게임 DLL (gitignore)
├── STS2Mod.csproj     # 프로젝트 설정
├── STS2Mod.json       # 모드 매니페스트
├── project.godot      # Godot 프로젝트
└── Directory.Build.props  # 로컬 경로 설정 (gitignore)
```

## References
- [BaseLib Mod Template Wiki](https://github.com/Alchyr/ModTemplate-StS2/wiki)
- [STS2 Modding Guide (CN)](https://github.com/freude916/sts2-quickRestart)
- [Harmony Documentation](https://harmony.pardeike.net/articles/intro.html)
