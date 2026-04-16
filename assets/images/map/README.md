# 맵 아이콘 에셋

환상체(Abnormality) 맵 노드용 아이콘 리소스를 여기 배치.

## 필요 파일

| 파일 | 용도 |
|------|------|
| `map_abnormality.tres` | 기본 아이콘 텍스처 |
| `map_abnormality_outline.tres` | 아이콘 아웃라인 (선택 상태/하이라이트) |

## 누락 시 동작

`MapPointTypePatches.ImageHelper_GetRoomIconPath_Patch`와 `..._Outline_Patch`는
`ResourceLoader.Exists()`로 파일 존재 여부를 1회 체크 후 캐시.
존재하지 않으면 바닐라 경로로 fallback되어 일반 이벤트 아이콘이 표시됨 (크래시 없음).

## 참고

- `.pck` 패킹을 위해 `dotnet publish -c Release` 필요 (`dotnet build`만으로는 반영 안 됨)
- 로드 경로: `res://assets/images/map/map_abnormality.tres`
- `TheCity.csproj:59`의 `<None Include="assets/**" />`에 의해 자동 포함
