using static SDL3.SDL;

namespace Xargon.NET;

public static class Program
{
    private const int SCREEN_WIDTH = 320;
    private const int SCREEN_HEIGHT = 200;
    private const int SCREEN_SCALE = 3;

    public static void Main(string[] args)
    {
        if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO | SDL_INIT_GAMEPAD) < 0)
        {
            Console.WriteLine($"SDL could not initialize! SDL_Error: {SDL_GetError()}");
            return;
        }

        var window = SDL_CreateWindow(
            "Xargon.NET",
            SCREEN_WIDTH * SCREEN_SCALE,
            SCREEN_HEIGHT * SCREEN_SCALE,
            SDL_WindowFlags.SDL_WINDOW_RESIZABLE
        );

        if (window == IntPtr.Zero)
        {
            Console.WriteLine($"Window could not be created! SDL_Error: {SDL_GetError()}");
            SDL_Quit();
            return;
        }

        var renderer = SDL_CreateRenderer(window, null, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (renderer == IntPtr.Zero)
        {
            Console.WriteLine($"Renderer could not be created! SDL_Error: {SDL_GetError()}");
            SDL_DestroyWindow(window);
            SDL_Quit();
            return;
        }
        
        SDL_SetRenderLogicalPresentation(renderer, SCREEN_WIDTH, SCREEN_HEIGHT, SDL_LogicalPresentation.SDL_LOGICAL_PRESENTATION_LETTERBOX, SDL_ScaleMode.SDL_SCALEMODE_NEAREST);
        SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "0");

        try
        {
            var game = new XargonGame(renderer);
            game.Run();
        }
        finally
        {
            SDL_DestroyRenderer(renderer);
            SDL_DestroyWindow(window);
            SDL_Quit();
        }
    }
}
