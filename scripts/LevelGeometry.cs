using Godot;

namespace WixxTheBard;

/// <summary>
/// Builds the M0 hello-world level on a real <see cref="TileMapLayer"/>: a
/// TileSet with a physics layer and a square collision shape, plus a floor and
/// two side walls. Constructed in code so the scaffold is deterministic and free
/// of hand-encoded binary tile data — but it is a genuine TileMap with genuine
/// per-tile collision, which is what the box collides against.
///
/// These are level-geometry constants (grid layout, tile size), not the
/// gameplay-feel numbers that CLAUDE.md rule 1 reserves for <see cref="Tunables"/>.
/// </summary>
public partial class LevelGeometry : TileMapLayer
{
    private const int TileSizePx = 16;
    private const int ColumnsWide = 40; // 40 * 16 = 640px viewport
    private const int RowsTall = 22;    // 22 * 16 = 352px (just shy of 360)
    private const int FloorRow = 20;

    public override void _Ready()
    {
        var sourceId = BuildTileSet();
        Paint(sourceId);
    }

    private int BuildTileSet()
    {
        var image = Image.CreateEmpty(TileSizePx, TileSizePx, false, Image.Format.Rgba8);
        image.Fill(new Color("3a4a5a"));
        var texture = ImageTexture.CreateFromImage(image);

        var atlas = new TileSetAtlasSource
        {
            Texture = texture,
            TextureRegionSize = new Vector2I(TileSizePx, TileSizePx),
        };
        atlas.CreateTile(Vector2I.Zero);

        var tileSet = new TileSet
        {
            TileShape = TileSet.TileShapeEnum.Square,
            TileSize = new Vector2I(TileSizePx, TileSizePx),
        };
        tileSet.AddPhysicsLayer();
        const int physicsLayer = 0;

        var sourceId = tileSet.AddSource(atlas);

        var tileData = atlas.GetTileData(Vector2I.Zero, 0);
        tileData.AddCollisionPolygon(physicsLayer);
        const float half = TileSizePx / 2.0f;
        tileData.SetCollisionPolygonPoints(physicsLayer, 0, new[]
        {
            new Vector2(-half, -half),
            new Vector2(half, -half),
            new Vector2(half, half),
            new Vector2(-half, half),
        });

        TileSet = tileSet;
        return sourceId;
    }

    private void Paint(int sourceId)
    {
        // Floor across the bottom.
        for (var x = 0; x < ColumnsWide; x++)
        {
            SetCell(new Vector2I(x, FloorRow), sourceId, Vector2I.Zero);
        }

        // Left and right walls up to the floor.
        for (var y = 0; y <= FloorRow; y++)
        {
            SetCell(new Vector2I(0, y), sourceId, Vector2I.Zero);
            SetCell(new Vector2I(ColumnsWide - 1, y), sourceId, Vector2I.Zero);
        }

        // A small ledge to make collision visibly do something.
        for (var x = 24; x < 30; x++)
        {
            SetCell(new Vector2I(x, FloorRow - 4), sourceId, Vector2I.Zero);
        }

        _ = RowsTall; // documents intended grid height
    }
}
