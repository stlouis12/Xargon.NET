using SDL3;
using Xargon.NET;

public static class Program
{
    private const int SCREEN_WIDTH = 320;
    private const int SCREEN_HEIGHT = 200;
    private const int SCREEN_SCALE = 3;

    public static void Main(string[] args)
    {
        if (!SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Audio | SDL.InitFlags.Gamepad))
        {
            Console.WriteLine($"SDL could not initialize! SDL.Error: {SDL.GetError()}");
            return;
        }

        var window = SDL.CreateWindow(
            "Xargon.NET",
            SCREEN_WIDTH * SCREEN_SCALE,
            SCREEN_HEIGHT * SCREEN_SCALE,
            SDL.WindowFlags.Resizable
        );

        if (window == IntPtr.Zero)
        {
            Console.WriteLine($"Window could not be created! SDL.Error: {SDL.GetError()}");
            SDL.Quit();
            return;
        }

        var renderer = SDL.CreateRenderer(window, null);
        if (renderer == IntPtr.Zero)
        {
            Console.WriteLine($"Renderer could not be created! SDL.Error: {SDL.GetError()}");
            SDL.DestroyWindow(window);
            SDL.Quit();
            return;
        }
        
        SDL.SetRenderLogicalPresentation(renderer, SCREEN_WIDTH, SCREEN_HEIGHT, SDL.RendererLogicalPresentation.Letterbox);
        // SDL.SetHint(SDL.HINT_RENDER_SCALE_QUALITY, "0");  // HINT_RENDER_SCALE_QUALITY appears to be removed in sdl3

        try
        {
            var game = new XargonGame(renderer);
            game.Run();
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
            SDL.DestroyWindow(window);
            SDL.Quit();
        }
    }
}
