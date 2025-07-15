using System.Numerics;
using System.Drawing;

namespace Xargon.NET.GameObjects;

// Base class for all dynamic entities in the game.
// Replaces the C `objtype` struct and `kindmsg` function pointers.
public abstract class GameObject
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public RectangleF Bounds { get; protected set; }
    public bool IsKilled { get; protected set; }

    // Flags corresponding to original kindflags
    public virtual bool IsWeapon => false;
    public virtual bool IsKillable => false;

    public GameObject(int x, int y)
    {
        Position = new Vector2(x, y);
    }

    // Replaces msg_update
    public virtual void Update(float deltaTime, ObjectManager om, InputManager input) { }

    // Replaces msg_draw
    public virtual void Draw(IntPtr renderer, ShapeManager sm, Viewport vp) { }

    // Replaces msg_touch
    public virtual void OnTouch(GameObject other, ObjectManager om) { }

    // Replaces killobj()
    public void Kill() => IsKilled = true;
}

/// <summary>
/// Replaces the logic from x_objman.c.
/// Manages the collection of all game objects.
/// </summary>
public class ObjectManager
{
    private List<GameObject> _objects = new();
    private List<GameObject> _onScreenObjects = new();
    public Player? PlayerObject { get; private set; }

    /// <summary>
    /// Replaces init_objs()
    /// </summary>
    public void Initialize()
    {
        _objects.Clear();
        PlayerObject = new Player(40, 40);
        _objects.Add(PlayerObject);
    }

    /// <summary>
    /// Replaces the main loop logic from upd_objs()
    /// </summary>
    public void Update(float deltaTime, Viewport gameViewport, InputManager input)
    {
        // 1. Determine on-screen objects
        _onScreenObjects.Clear();
        var screenRect = new RectangleF(
            gameViewport.X - 96, gameViewport.Y - 48,
            gameViewport.Width + 192, gameViewport.Height + 96
        );

        foreach (var obj in _objects)
        {
            // A more robust implementation would check an IsAlwaysActive flag
            if (obj.Bounds.IntersectsWith(screenRect))
            {
                _onScreenObjects.Add(obj);
            }
        }

        // 2. Update objects
        foreach (var obj in _onScreenObjects)
        {
            obj.Update(deltaTime, this, input);
        }

        // 3. Handle collisions
        for (int i = 0; i < _onScreenObjects.Count; i++)
        {
            for (int j = i + 1; j < _onScreenObjects.Count; j++)
            {
                var objA = _onScreenObjects[i];
                var objB = _onScreenObjects[j];

                if (objA.Bounds.IntersectsWith(objB.Bounds))
                {
                    objA.OnTouch(objB, this);
                    objB.OnTouch(objA, this);
                }
            }
        }
    }
}
