using Xargon.NET.Core;
using Xargon.NET.GameObjects;
using Xargon.NET.Audio;
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
    private ObjectManager _objectManager;

    public bool ShouldQuit { get; private set; } = false;

    public GameStateManager(ConfigManager configManager, SoundManager soundManager, ShapeManager shapeManager, UIManager uiManager)
    {
        _configManager = configManager;
        _objectManager = new ObjectManager(soundManager);
        _soundManager = soundManager;
        _shapeManager = shapeManager;
        _uiManager = uiManager;
    }

    public void InitializeGameData()
    {
        // Load initial game data here (e.g., object lists, level data)
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
        // Update logic based on the current game state
        switch (_currentState)
        {
            case GameFlowState.Initializing:
                SwitchState(GameFlowState.TitleScreen);
                break;

            case GameFlowState.TitleScreen:
                if (input.IsKeyPressed(Scancode.Space))
                {
                    SwitchState(GameFlowState.Playing); // Go to main game for now
                }
                if (input.IsKeyPressed(Scancode.Escape))
                {
                    ShouldQuit = true;
                }
                break;

            case GameFlowState.Playing:
                // Update game logic, player input, etc.
                if (input.IsKeyPressed(Scancode.Escape))
                {
                     SwitchState(GameFlowState.TitleScreen); // Go back to title for now
                }
                break;
        }
    }

    public void Draw(IntPtr renderer)
    {
        // Drawing logic based on the current game state
        switch (_currentState)
        {
            case GameFlowState.TitleScreen:
                _uiManager.ShowTitleScreen();
                break;

            case GameFlowState.Playing:
                 _uiManager.DrawStatusWindow(true);
                // Draw game world, player, enemies, etc. here
                break;
            
            // Other states
        }
    }
}
