using SDL3;
using System.Drawing;

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
        // This method will draw the title screen elements.
        // It's called from the GameStateManager's Draw method when in the TitleScreen state.
        
        // Example: Draw a background image (assuming shape 0 is the title background)
        IntPtr titleBg = _shapeManager.GetTexture(0); // You will need to find the correct shape ID
        if(titleBg != IntPtr.Zero)
        {
            SDL.RenderTexture(_renderer, titleBg, IntPtr.Zero, IntPtr.Zero);
        }

        // Example: Draw text
        DrawText("XARGON.NET", 100, 20);
        DrawText("PRESS SPACE", 110, 160);
    }

    public void DrawStatusWindow(bool inGame)
    {
        var rect = new SDL.FRect { X = _statusViewport.X, Y = _statusViewport.Y, W = _statusViewport.Width, H = _statusViewport.Height };
        SDL.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        SDL.RenderFillRect(_renderer, ref rect);

        DrawText("Status: " + (inGame ? "In Game" : "Title"), 10, _statusViewport.Y + 2);
    }
    
    // A more advanced text renderer would be needed. This is a simple placeholder.
    public void DrawText(string text, int x, int y)
    {
        float currentX = x;
        foreach (char c in text)
        {
            // Placeholder: This assumes your font characters are mapped to shape IDs.
            // You will need to determine the correct mapping from your game's data.
            int shapeId = c; // This will not work correctly without a real font map.
            IntPtr charTexture = _shapeManager.GetTexture(shapeId);
            if(charTexture != IntPtr.Zero)
            {
                SDL.GetTextureSize(charTexture, out float w, out float h);
                var destRect = new SDL.FRect { X = currentX, Y = y, W = w, H = h };
                SDL.RenderTexture(_renderer, charTexture, IntPtr.Zero, ref destRect);
                currentX += w;
            }
            else
            {
                currentX += 8; // Advance even if char not found
            }
        }
    }
}

public struct Viewport
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
