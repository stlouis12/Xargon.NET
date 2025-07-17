using Xargon.NET.Audio;
using Xargon.NET.Core;
using Xargon.NET.GameObjects;
using Xargon.NET.Graphics;
using Xargon.NET.Input;
using static SDL3.SDL;

namespace Xargon.NET;

public class GameStateManager
{
    private GameFlowState _currentState = GameFlowState.Initializing;

    private readonly ConfigManager _configManager;
    private readonly SoundManager _soundManager;
    private readonly ShapeManager _shapeManager;
    private readonly UIManager _uiManager;

    private readonly Board _board;
    private readonly ObjectManager _objectManager;
    private readonly Viewport _gameViewport;

    public bool ShouldQuit { get; private set; }

    public GameStateManager(ConfigManager configManager, SoundManager soundManager, ShapeManager shapeManager, UIManager uiManager)
    {
        _configManager = configManager;
        _soundManager = soundManager;
        _shapeManager = shapeManager;
        _uiManager = uiManager;

        _board = new Board(_shapeManager);
        _objectManager = new ObjectManager(_soundManager, _board);
        _gameViewport = new Viewport { X = 0, Y = 0, Width = 320, Height = 188 };
    }

    public void InitializeGameData()
    {
        _board.LoadBoard("map.xr1");
        _objectManager.Initialize();
    }

    public void SwitchState(GameFlowState state)
    {
        _currentState = state;

        // Perform any state-specific initialization or cleanup here.
        switch (_currentState)
        {
            case GameFlowState.TitleScreen:
                // Logic to show the title screen is now handled in Update/Draw
                break;
            case GameFlowState.MainMenu:
                // Logic to show the main menu
                break;
            case GameFlowState.Playing:
                // StartNewGame();
                break;
        }
    }

    public void Update(float deltaTime, InputManager input)
    {
        switch (_currentState)
        {
            case GameFlowState.Initializing:
                SwitchState(GameFlowState.TitleScreen);
                break;

            case GameFlowState.TitleScreen:
                if (input.IsKeyPressed(Scancode.Space)) SwitchState(GameFlowState.Playing);
                if (input.IsKeyPressed(Scancode.Escape)) ShouldQuit = true;
                break;

            case GameFlowState.Playing:
                _objectManager.Update(deltaTime, _gameViewport, input);
                if (_objectManager.PlayerObject != null)
                {
                    _board.UpdateViewport(_gameViewport, _objectManager.PlayerObject.Position);
                }
                if (input.IsKeyPressed(Scancode.Escape)) SwitchState(GameFlowState.TitleScreen);
                break;
        }
    }

    public void Draw(IntPtr renderer)
    {
        switch (_currentState)
        {
            case GameFlowState.TitleScreen:
                _uiManager.ShowTitleScreen();
                break;

            case GameFlowState.Playing:
                _board.Draw(renderer, _gameViewport);
                _objectManager.Draw(renderer, _shapeManager, _gameViewport);
                _uiManager.DrawStatusWindow(true);
                break;
        }
    }
}
