using SDL3;
using System.Runtime.InteropServices;
using Xargon.NET.Helpers;

namespace Xargon.NET.Graphics;

public class ShapeManager
{
    private readonly IntPtr _renderer;
    private readonly Dictionary<int, IntPtr> _textures = new();
    private byte[] _palette = new byte[256 * 3]; // RGB palette

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ShapeHeader
    {
        public short Width;
        public short Height;
        public int Offset;
    }

    public ShapeManager(IntPtr renderer)
    {
        _renderer = renderer;
    }

    public void Init(string filename)
    {
        string path = Path.Combine("Assets", filename);
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Graphics file not found: {path}");
            return;
        }

        LoadPalette(path);
        LoadShapes(path);
    }

    private void LoadPalette(string filename)
    {
        try
        {
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            fs.Read(_palette, 0, 768);

            for (int i = 0; i < 256; i++)
            {
                byte temp = _palette[i * 3];
                _palette[i * 3] = _palette[i * 3 + 2];
                _palette[i * 3 + 2] = temp;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading palette: {ex.Message}");
        }
    }

    private unsafe void LoadShapes(string filename)
    {
        try
        {
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            fs.Seek(768, SeekOrigin.Begin);

            int shapeIndex = 0;
            while (fs.Position + Marshal.SizeOf<ShapeHeader>() <= fs.Length)
            {
                ShapeHeader header = reader.ReadStruct<ShapeHeader>();

                if (header.Width <= 0 || header.Height <= 0 || header.Width > 512 || header.Height > 512)
                {
                    break;
                }

                long currentPos = fs.Position;
                fs.Seek(header.Offset, SeekOrigin.Begin);
                byte[] pixels = reader.ReadBytes(header.Width * header.Height);
                fs.Seek(currentPos, SeekOrigin.Begin);

                IntPtr surface = CreateRgbaSurfaceFromPalette(pixels, header.Width, header.Height);

                if (surface != IntPtr.Zero)
                {
                    IntPtr texture = SDL.CreateTextureFromSurface(_renderer, surface);
                    SDL.DestroySurface(surface);
                    if (texture != IntPtr.Zero)
                    {
                        SDL.SetTextureBlendMode(texture, SDL.BlendMode.Blend);
                        _textures[shapeIndex] = texture;
                    }
                }
                shapeIndex++;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading shapes: {ex.Message}");
        }
    }

    private unsafe IntPtr CreateRgbaSurfaceFromPalette(byte[] pixels, int width, int height)
    {
        IntPtr surface = SDL.CreateSurface(width, height, SDL.PixelFormat.RGBA8888);
        if (surface == IntPtr.Zero) return IntPtr.Zero;

        if (SDL.LockSurface(surface))
        {
            SDL.DestroySurface(surface);
            return IntPtr.Zero;
        }

        byte* surfacePixels = (byte*)((SDL.Surface*)surface)->Pixels;
        for (int i = 0; i < pixels.Length; i++)
        {
            byte paletteIndex = pixels[i];
            byte* pixel = surfacePixels + (i * 4);
            if (paletteIndex == 0) // Assuming color 0 is transparent
            {
                pixel[0] = 0; pixel[1] = 0; pixel[2] = 0; pixel[3] = 0;
            }
            else
            {
                pixel[0] = _palette[paletteIndex * 3 + 0];
                pixel[1] = _palette[paletteIndex * 3 + 1];
                pixel[2] = _palette[paletteIndex * 3 + 2];
                pixel[3] = 255;
            }
        }

        SDL.UnlockSurface(surface);
        return surface;
    }

    public IntPtr GetTexture(int id)
    {
        _textures.TryGetValue(id, out var texture);
        return texture;
    }

    public void Cleanup()
    {
        foreach (var texture in _textures.Values)
        {
            SDL.DestroyTexture(texture);
        }
        _textures.Clear();
    }
}
