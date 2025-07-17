using System.Drawing;
using static SDL3.SDL;

namespace Xargon.NET.Graphics;

public class UIManager
{
    private readonly IntPtr _renderer;
    private readonly ShapeManager _shapeManager;
    private readonly Viewport _statusViewport;

    public UIManager(IntPtr renderer, ShapeManager shapeManager)
    {
        _renderer = renderer;
        _shapeManager = shapeManager;

        _statusViewport = new Viewport
        {
            X = 0,
            Y = 188,
            Width = 320,
            Height = 12
        };
    }

    public void ShowTitleScreen()
    {
        // Placeholder for drawing the title screen
        DrawText("XARGON.NET", 100, 20);
        DrawText("PRESS SPACE TO START", 80, 160);
    }

    public void DrawStatusWindow(bool inGame)
    {
        var rect = new FRect { X = _statusViewport.X, Y = _statusViewport.Y, W = _statusViewport.Width, H = _statusViewport.Height };
        SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        RenderFillRect(_renderer, ref rect);
        DrawText($"Status: {(inGame ? "Playing" : "Title")}", 10, _statusViewport.Y + 2);
    }

    private void DrawText(string text, int x, int y)
    {
        // This is a placeholder text renderer. A full implementation would
        // need a font map to translate characters to shape IDs.
        float currentX = x;
        foreach (char c in text.ToUpper())
        {
            // This mapping is a guess and needs to be verified.
            int shapeId = (c >= 'A' && c <= 'Z') ? 0x0100 + c - 'A' : 0;
            if (shapeId == 0) { currentX += 8; continue; } // Skip unknown chars

            IntPtr charTexture = _shapeManager.GetTexture(shapeId);
            if (charTexture != IntPtr.Zero)
            {
                GetTextureSize(charTexture, out float w, out float h);
                var destRect = new FRect { X = currentX, Y = y, W = w, H = h };
                RenderTexture(_renderer, charTexture, IntPtr.Zero, ref destRect);
                currentX += w;
            }
            else
            {
                currentX += 8;
            }
        }
    }
}

public class Viewport
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
