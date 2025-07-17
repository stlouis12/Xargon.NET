using System.Numerics;
using System.Runtime.InteropServices;
using Xargon.NET.GameObjects;
using Xargon.NET.Graphics;
using Xargon.NET.Helpers;
using static SDL3.SDL;

namespace Xargon.NET.Core;

[Flags]
public enum TileFlags : ushort
{
    None = 0,
    PlayerThru = 1 << 0, // Player can pass through
    NotStair = 1 << 1,
    NotVine = 1 << 2,
    NotWater = 1 << 3,
    MsgUpdate = 1 << 4, // Tile has custom update logic
    MsgDraw = 1 << 5, // Tile has custom draw logic
    MsgTouch = 1 << 6 // Tile has custom touch logic
    // Add other flags from the original game here
}

public struct TileInfo
{
    public ushort ShapeId;
    public TileFlags Flags;
    public string Name;
}

/// <summary>
/// Manages loading, drawing, and collision for the tile-based game world.
/// Replaces the `bd` array and functions like `loadboard`, `drawboard`, and `cando`.
/// </summary>
public class Board
{
    public const int BOARD_WIDTH = 256;
    public const int BOARD_HEIGHT = 128;
    private const int TILE_SIZE = 16;

    private readonly ShapeManager _shapeManager;
    private readonly ushort[,] _tileData = new ushort[BOARD_WIDTH, BOARD_HEIGHT];
    private readonly TileInfo[] _tileInfo;

    public Board(ShapeManager shapeManager)
    {
        _shapeManager = shapeManager;
        _tileInfo = new TileInfo[ushort.MaxValue];
        LoadTileInfo("tiles.xr1");
    }

    /// <summary>
    /// Loads tile properties from TILES.XR1, which corresponds to `init_info()`
    /// in the original C code.
    /// </summary>
    private void LoadTileInfo(string filename)
    {
        string path = Path.Combine("Assets", filename);
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Tile info file not found: {path}");
            return;
        }

        // Set default flags for all tiles first
        const TileFlags defaultFlags = TileFlags.NotVine | TileFlags.NotStair | TileFlags.NotWater;
        for (int i = 0; i < _tileInfo.Length; i++)
        {
            _tileInfo[i] = new TileInfo { ShapeId = 0x4500, Flags = defaultFlags, Name = "" };
        }

        try
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ushort index = reader.ReadUInt16();
                ushort shapeId = reader.ReadUInt16();
                ushort flagsToXor = reader.ReadUInt16();
                byte nameLen = reader.ReadByte();
                string name = new(reader.ReadChars(nameLen));

                _tileInfo[index].ShapeId = shapeId;
                _tileInfo[index].Flags ^= (TileFlags)flagsToXor; // XOR with the default flags
                _tileInfo[index].Name = name;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading tile info: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a level map from a .XR1 file. Corresponds to `loadboard()`.
    /// </summary>
    public void LoadBoard(string filename)
    {
        string path = Path.Combine("Assets", filename);
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Board file not found: {path}");
            LoadDefaultBoard();
            return;
        }

        try
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                for (int x = 0; x < BOARD_WIDTH; x++)
                {
                    // The original board data was likely 16-bit integers.
                    _tileData[x, y] = reader.ReadUInt16();
                }
            }
            // The rest of the file contains object data, which is loaded by ObjectManager.
            Console.WriteLine($"Loaded board from {filename}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading board {filename}: {ex.Message}");
            LoadDefaultBoard();
        }
    }

    private void LoadDefaultBoard()
    {
        Console.WriteLine("Board file not found. Using empty default board.");
        // Initialize an empty board if no file is found
        for (int x = 0; x < BOARD_WIDTH; x++)
            for (int y = 0; y < BOARD_HEIGHT; y++)
                _tileData[x, y] = 0;
    }

    /// <summary>
    /// Updates the viewport position to keep a target (the player) in frame.
    /// Replaces `calc_scroll()`.
    /// </summary>
    public void UpdateViewport(Viewport viewport, Vector2 focusPosition)
    {
        int targetX = (int)focusPosition.X - viewport.Width / 2;
        int targetY = (int)focusPosition.Y - viewport.Height / 2;

        // Clamp viewport to board boundaries
        viewport.X = Math.Clamp(targetX, 0, (BOARD_WIDTH * TILE_SIZE) - viewport.Width);
        viewport.Y = Math.Clamp(targetY, 0, (BOARD_HEIGHT * TILE_SIZE) - viewport.Height);
    }

    /// <summary>
    /// Checks if a tile at a specific world coordinate is solid for collision purposes.
    /// This is the core of the `cando()` function.
    /// </summary>
    public bool IsSolid(int worldX, int worldY)
    {
        // Out-of-bounds is always solid
        if (worldX < 0 || worldY < 0 || worldX >= BOARD_WIDTH * TILE_SIZE || worldY >= BOARD_HEIGHT * TILE_SIZE)
            return true;

        int tileX = worldX / TILE_SIZE;
        int tileY = worldY / TILE_SIZE;

        ushort tileId = _tileData[tileX, tileY];

        // A tile is solid if the f_playerthru flag is NOT set.
        return !_tileInfo[tileId].Flags.HasFlag(TileFlags.PlayerThru);
    }

    /// <summary>
    /// Renders the visible portion of the board. Corresponds to `drawboard()`.
    /// </summary>
    public void Draw(IntPtr renderer, Viewport viewport)
    {
        int startX = viewport.X / TILE_SIZE;
        int startY = viewport.Y / TILE_SIZE;
        int endX = (viewport.X + viewport.Width) / TILE_SIZE + 1;
        int endY = (viewport.Y + viewport.Height) / TILE_SIZE + 1;

        // Clamp drawing to the board's dimensions
        startX = Math.Max(0, startX);
        startY = Math.Max(0, startY);
        endX = Math.Min(BOARD_WIDTH, endX);
        endY = Math.Min(BOARD_HEIGHT, endY);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                ushort tileId = _tileData[x, y];
                if (tileId == 0) continue; // Skip empty tiles

                // Get the shape ID for this tile from the info table.
                ushort shapeId = _tileInfo[tileId].ShapeId;
                IntPtr texture = _shapeManager.GetTexture(shapeId);

                if (texture != IntPtr.Zero)
                {
                    FRect destRect = new FRect
                    {
                        X = x * TILE_SIZE - viewport.X,
                        Y = y * TILE_SIZE - viewport.Y,
                        W = TILE_SIZE,
                        H = TILE_SIZE
                    };
                    RenderTexture(renderer, texture, IntPtr.Zero, ref destRect);
                }
            }
        }
    }
}
