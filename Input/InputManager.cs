using static SDL3.SDL;

namespace Xargon.NET.Input;

public class InputManager
{
    private readonly HashSet<Scancode> _pressedKeys = new();
    private readonly HashSet<Scancode> _heldKeys = new();
    
    public bool QuitRequested { get; private set; }

    public void Init() { }

    public void PollEvents()
    {
        _pressedKeys.Clear();
        QuitRequested = false;
        
        while (PollEvent(out Event e))
        {
            switch (e.Type)
            {
                case (uint)EventType.Quit:
                    QuitRequested = true;
                    break;
                
                case (uint)EventType.KeyDown:
                    if (!_heldKeys.Contains(e.Key.Scancode))
                    {
                        _pressedKeys.Add(e.Key.Scancode);
                        _heldKeys.Add(e.Key.Scancode);
                    }
                    break;

                case (uint)EventType.KeyUp:
                    _heldKeys.Remove(e.Key.Scancode);
                    break;
            }
        }
    }

    public bool IsKeyPressed(Scancode scancode) => _pressedKeys.Contains(scancode);
    public bool IsKeyHeld(Scancode scancode) => _heldKeys.Contains(scancode);
}
