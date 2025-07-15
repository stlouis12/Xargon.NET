using static SDL3.SDL;
using System.Diagnostics;

namespace Xargon.NET;

// In a real project, these would be in their own files.
public class ConfigManager { public void LoadConfig(string s) { } }
public class SoundManager { public void Init(string s) { } public void Start() { } public void PlayTune(string s) { } public void Cleanup() { } }
public class InputManager { public bool QuitRequested { get; private set; } public void Init() { } public void PollEvents() { QuitRequested = SDL_QuitRequested(); } }
public class ShapeManager { public ShapeManager(IntPtr r) { } public void Init(string s) { } public void LoadInitialAssets() { } public void LoadGameAssets() { } public void Cleanup() { } }
public class UIManager { public UIManager(IntPtr r, ShapeManager sm) { } public void ShowTitleScreen() { /* Port of wait() */ } public void DrawStatusWindow(bool b) { } }
public class GameStateManager { public GameStateManager(ConfigManager cm, SoundManager sm, ShapeManager shm, UIManager um) { } public void InitializeGameData() { } public void SwitchState(GameFlowState state) { } public void Update(float dt, InputManager i) { } public void Draw(IntPtr r) { } }

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

    // Game state variables from xargon.c
    private int _gameCount = 0;

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
    public void Initialize()
    {
        _configManager.LoadConfig("config.xr1");
        _soundManager.Init("audio.xr1");
        _inputManager.Init();

        // The original game had a text-mode config screen here (doconfig).
        // In a modern port, this would be an in-game settings menu.
        // For now, we'll assume default settings.

        _shapeManager.Init("graphics.xr1");
        
        // shm_want [1, 2, 53] = 1; shm_do();
        _shapeManager.LoadInitialAssets(); 

        _soundManager.Start();
        _soundManager.PlayTune("song_0.xr1");

        // This replaces the wait() function from x_vol1/2/3.c
        _uiManager.ShowTitleScreen();
        SDL_Delay(2000); // Placeholder for user input

        // Fadeout would be handled here

        // shm_want [5]=1; shm_want [53]=0; shm_do();
        _shapeManager.LoadGameAssets();

        // init_info(), init_objinfo(), etc.
        _gameStateManager.InitializeGameData();

        // Fadein and transition to main menu
    }

    public void Run()
    {
        Initialize();

        _gameStateManager.SwitchState(GameFlowState.MainMenu);

        var stopwatch = Stopwatch.StartNew();

        while (!_quit)
        {
            float deltaTime = stopwatch.ElapsedMilliseconds / 1000.0f;
            stopwatch.Restart();

            // Handle input
            _inputManager.PollEvents();
            if (_inputManager.QuitRequested)
            {
                // In a real game, you would confirm with the user.
                // E.g., _gameStateManager.SwitchState(GameFlowState.ConfirmQuit);
                _quit = true;
                continue;
            }

            // Update game state
            _gameCount++;
            _gameStateManager.Update(deltaTime, _inputManager);

            // Render
            // In the original, this was handled by refresh() inside play()
            Render();
        }

        Cleanup();
    }

    private void Render()
    {
        // The original used a black background (color 0) or a sky color (color 248)
        SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255);
        SDL_RenderClear(_renderer);

        _gameStateManager.Draw(_renderer);

        SDL_RenderPresent(_renderer);
    }

    private void Cleanup()
    {
        // Corresponds to the exit routines in main()
        _shapeManager.Cleanup();
        _soundManager.Cleanup();
    }
}
