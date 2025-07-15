using System.Numerics;
using System.Drawing;
using Xargon.NET.Graphics;
using Xargon.NET.Input;
using Xargon.NET.Audio;

namespace Xargon.NET.GameObjects;

// Base class for all dynamic entities in the game.
public abstract class GameObject
{
    public int Id { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public RectangleF Bounds { get; protected set; }
    public bool IsKilled { get; protected set; }

    public virtual bool IsWeapon => false;
    public virtual bool IsKillable => false;

    public GameObject(int x, int y)
    {
        Position = new Vector2(x, y);
    }

    public virtual void Update(float deltaTime, ObjectManager om, InputManager input) { }
    public virtual void Draw(IntPtr renderer, ShapeManager sm, Viewport vp) { }
    public virtual void OnTouch(GameObject other, ObjectManager om) { }
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
    public SoundManager SoundManager { get; }

    public ObjectManager(SoundManager soundManager)
    {
        SoundManager = soundManager;
        PlayerObject = new Player(40, 40);
    }

    /// <summary>
    /// Replaces init_objs()
    /// </summary>
    public void Initialize()
    {
        _objects.Clear();
        _objects.Add(PlayerObject);
    }

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
