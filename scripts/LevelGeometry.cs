using Godot;

namespace WixxTheBard;

/// <summary>
/// Builds the M6 vertical-slice biome on a real <see cref="TileMapLayer"/>: one
/// continuous Exploration level (SPEC §4.1, §12) strung together from the verbs
/// M1-M5 already proved — a rolling-terrain warm-up, the tar struggle hazard
/// (§4.4), a sprint stretch, a high ledge only the tilt super-jump can reach (the
/// "secret" §4.1 calls out for the verb), an enemy gauntlet, and the Rock Off
/// arena at the end. Built in code so the layout is deterministic and free of
/// hand-encoded binary tile data — but it is a genuine TileMap with genuine
/// per-tile collision, which is what <see cref="Player"/> collides against.
///
/// Terrain only ever changes by a single tile at a time (no blind drops outside
/// the tar gap, which is the one hazard with its own catch — <see cref="TarPit"/>)
/// — design pillar 1 is flow over precision, so missing a hop never costs a fall
/// into the void.
///
/// These are level-layout constants (grid layout, tile size), not the
/// gameplay-feel numbers that CLAUDE.md rule 1 reserves for <see cref="Tunables"/>.
/// </summary>
public partial class LevelGeometry : TileMapLayer
{
    private const int TileSizePx = 16;
    private const int ColumnsWide = 101; // 101 * 16 = 1616px world width
    private const int RowsTall = 24;     // 24 * 16 = 384px world height
    private const int BaseFloorRow = 22; // 22 * 16 = 352px — the biome's ground line

    // A rolling-terrain warm-up (cols 11-20): each step is exactly one tile, so a
    // tap-jump always clears it — varied silhouette, zero precision risk.
    private const int RollStartCol = 11;
    private static readonly int[] RollProfile = { 0, -1, -1, -2, -2, -3, -3, -2, -1, 0 };

    // The tar struggle (SPEC §4.4): a floor gap the TarPit Area2D in the scene
    // catches on entry — these are level-layout indices, not gameplay-feel
    // numbers (Tunables, rule 1).
    private const int TarStartCol = 21;
    private const int TarEndCol = 27; // exclusive

    // The super-jump secret (SPEC §4.1 — "great use for the tilt super-jump —
    // hidden high ledges"): a floating island far above anything a normal jump
    // reaches, sitting over a continuous floor so missing it never costs a fall.
    private static readonly int[] SecretIslandCols = { 53, 54, 55 };
    private const int SecretIslandRow = 4;

    private static bool IsTarGap(int col) => col >= TarStartCol && col < TarEndCol;

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
        // The ground: rolling terrain, broken by the tar-pit gap (SPEC §4.4).
        for (var x = 0; x < ColumnsWide; x++)
        {
            if (IsTarGap(x))
            {
                continue;
            }

            SetCell(new Vector2I(x, FloorRowFor(x)), sourceId, Vector2I.Zero);
        }

        // Left and right walls — the right wall doubles as the Rock Off arena's
        // back wall (SPEC §6), same boundary-painting approach as the M0 scaffold.
        for (var y = 0; y <= BaseFloorRow; y++)
        {
            SetCell(new Vector2I(0, y), sourceId, Vector2I.Zero);
            SetCell(new Vector2I(ColumnsWide - 1, y), sourceId, Vector2I.Zero);
        }

        // The super-jump secret: an isolated island, unreachable by a normal jump.
        foreach (var x in SecretIslandCols)
        {
            SetCell(new Vector2I(x, SecretIslandRow), sourceId, Vector2I.Zero);
        }

        _ = RowsTall; // documents intended grid height
    }

    private static int FloorRowFor(int col)
    {
        if (col >= RollStartCol && col < RollStartCol + RollProfile.Length)
        {
            return BaseFloorRow + RollProfile[col - RollStartCol];
        }

        return BaseFloorRow;
    }
}
