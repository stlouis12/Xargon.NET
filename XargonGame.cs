using SDL3;
using System.Diagnostics;
using Xargon.NET.Audio;
using Xargon.NET.Core;
using Xargon.NET.Graphics;
using Xargon.NET.Input;
//using SDL3;

namespace Xargon.NET;

public enum GameFlowState
{
    Initializing,
    TitleScreen,
    MainMenu,
    Playing,
    InGameMenu,
    GameOver
}

/// <summary>
/// This class replaces the logic found in the original xargon.c.
/// It orchestrates the entire game flow.
/// </summary>
public class XargonGame
{
    private readonly IntPtr _renderer;
    private bool _quit = false;

    // Managers to replace global state and systems
    private readonly ConfigManager _configManager;
    private readonly SoundManager _soundManager;
    private readonly InputManager _inputManager;
    private readonly ShapeManager _shapeManager;
    private readonly UIManager _uiManager;
    private readonly GameStateManager _gameStateManager;

    public XargonGame(IntPtr renderer)
    {
        _renderer = renderer;

        // Instantiate all the major systems
        _configManager = new ConfigManager();
        _soundManager = new SoundManager();
        _inputManager = new InputManager();
        _shapeManager = new ShapeManager(_renderer);
        _uiManager = new UIManager(_renderer, _shapeManager);
        _gameStateManager = new GameStateManager(_configManager, _soundManager, _shapeManager, _uiManager);
    }

    /// <summary>
    /// This method mirrors the initialization sequence from the C main() function.
    /// </summary>
    public unsafe void Initialize()
    {
        _configManager.LoadConfig("config.xr1");
        _soundManager.Init("audio.xr1", _configManager);
        _inputManager.Init();

        _shapeManager.Init("graphics.xr1");
        
        _soundManager.PlayTune("song_0.ogg"); // Note: Converted to .ogg

        _gameStateManager.InitializeGameData();
    }

    public void Run()
    {
        Initialize();

        _gameStateManager.SwitchState(GameFlowState.TitleScreen);

        var stopwatch = Stopwatch.StartNew();
        
        while (!_quit)
        {
            float deltaTime = stopwatch.ElapsedMilliseconds / 1000.0f;
            stopwatch.Restart();

            // Handle input
            _inputManager.PollEvents();
            if (_inputManager.QuitRequested)
            {
                _quit = true;
                continue;
            }

            // Update game state
            _gameStateManager.Update(deltaTime, _inputManager);
            if (_gameStateManager.ShouldQuit)
            {
                _quit = true;
                continue;
            }

            // Render
            Render();
        }

        Cleanup();
    }

    private void Render()
    {
        // The original used a black background (color 0)
        SDL.SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        
        SDL.RenderClear(_renderer);

        _gameStateManager.Draw(_renderer);

        SDL.RenderPresent(_renderer);
    }

    private void Cleanup()
    {
        // Corresponds to the exit routines in main()
        _shapeManager.Cleanup();
        _soundManager.Cleanup();
    }
}
