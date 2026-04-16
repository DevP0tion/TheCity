using Godot;

namespace TheCity.Map;

/// <summary>
/// 5각 별 아이콘을 런타임에 생성하는 팩토리.
/// 게임의 asset atlas(atlases/ui_atlas.sprites/map/icons/*.tres)에 모드의 .tres를 주입할 수 없어
/// <see cref="ImageTexture"/>를 픽셀 단위로 생성하여 사용.
///
/// 캐시된 단일 인스턴스를 모든 Abnormality 노드가 공유.
/// </summary>
internal static class StarTextureFactory
{
    private const int Size = 64;

    private static ImageTexture? _star;
    public static ImageTexture Star => _star ??= CreateStar();

    private static ImageTexture CreateStar()
    {
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        img.Fill(new Color(0f, 0f, 0f, 0f));

        // 5각 별 꼭짓점: 외경/내경 번갈아
        var center = new Vector2(Size / 2f, Size / 2f);
        float outerR = Size * 0.47f;
        float innerR = Size * 0.22f;
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = -Mathf.Pi / 2f + i * Mathf.Pi / 5f;
            float r = (i % 2 == 0) ? outerR : innerR;
            pts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }

        // 1 패스: 각 픽셀이 폴리곤 내부인지 판정
        var inside = new bool[Size, Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            inside[x, y] = PointInPolygon(new Vector2(x + 0.5f, y + 0.5f), pts);
        }

        // 2 패스: 내부 채움 + 경계 픽셀은 외곽선 색
        var fill = new Color(1f, 0.88f, 0.3f, 1f);       // gold
        var outline = new Color(0.15f, 0.12f, 0.05f, 1f); // dark outline

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            if (!inside[x, y]) continue;

            bool isEdge = x == 0 || x == Size - 1 || y == 0 || y == Size - 1 ||
                          !inside[x - 1, y] || !inside[x + 1, y] ||
                          !inside[x, y - 1] || !inside[x, y + 1];
            img.SetPixel(x, y, isEdge ? outline : fill);
        }

        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>Ray-casting 기반 point-in-polygon 판정.</summary>
    private static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if ((poly[i].Y > p.Y) != (poly[j].Y > p.Y) &&
                p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
