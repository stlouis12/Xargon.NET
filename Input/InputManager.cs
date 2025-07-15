using SDL3;

namespace Xargon.NET.Input;

public class InputManager
{
    private readonly HashSet<SDL.Scancode> _pressedKeys = new();
    private readonly HashSet<SDL.Scancode> _heldKeys = new();
    
    public bool QuitRequested { get; private set; }

    public void Init() { }

    public void PollEvents()
    {
        _pressedKeys.Clear();
        QuitRequested = false;
        
        while (SDL.PollEvent(out SDL.Event e))
        {
            switch (e.Type)
            {
                case (uint)SDL.EventType.Quit:
                    QuitRequested = true;
                    break;
                
                case (uint)SDL.EventType.KeyDown:
                    if (!_heldKeys.Contains(e.Key.Scancode))
                    {
                        _pressedKeys.Add(e.Key.Scancode);
                        _heldKeys.Add(e.Key.Scancode);
                    }
                    break;

                case (uint)SDL.EventType.KeyUp:
                    _heldKeys.Remove(e.Key.Scancode);
                    break;
            }
        }
    }

    public bool IsKeyPressed(SDL.Scancode scancode) => _pressedKeys.Contains(scancode);
    public bool IsKeyHeld(SDL.Scancode scancode) => _heldKeys.Contains(scancode);
}
