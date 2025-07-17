using System.Drawing;
using System.Numerics;
using Xargon.NET.Audio;
using Xargon.NET.Core;
using Xargon.NET.Graphics;
using Xargon.NET.Input;

namespace Xargon.NET.GameObjects;

/// <summary>
/// The base class for all dynamic entities in the game.
/// This replaces the C `objtype` struct and `kindmsg` function pointers.
/// </summary>
public abstract class GameObject
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public RectangleF Bounds { get; protected set; }
    public bool IsKilled { get; protected set; }

    public virtual bool IsWeapon => false;
    public virtual bool IsKillable => false;

    protected GameObject(int x, int y)
    {
        Position = new Vector2(x, y);
    }

    /// <summary> Replaces msg_update </summary>
    public virtual void Update(float deltaTime, ObjectManager om, InputManager input) { }

    /// <summary> Replaces msg_draw </summary>
    public virtual void Draw(IntPtr renderer, ShapeManager sm, Viewport vp) { }

    /// <summary> Replaces msg_touch </summary>
    public virtual void OnTouch(GameObject other, ObjectManager om) { }

    /// <summary> Replaces killobj() </summary>
    public void Kill() => IsKilled = true;
}

/// <summary>
/// A placeholder base class for enemy types.
/// </summary>
public class Enemy : GameObject
{
    public Enemy(int x, int y) : base(x, y) { }
}

/// <summary>
/// Replaces the logic from x_objman.c.
/// Manages the collection of all game objects.
/// </summary>
public class ObjectManager
{
    private readonly List<GameObject> _objects = new();
    private readonly List<GameObject> _onScreenObjects = new();

    // System managers can be accessed by any game object through this manager
    public Player PlayerObject { get; }
    public Board Board { get; }
    public SoundManager SoundManager { get; }

    public ObjectManager(SoundManager soundManager, Board board)
    {
        SoundManager = soundManager;
        Board = board;
        PlayerObject = new Player(40, 40);
    }

    /// <summary> Replaces init_objs() </summary>
    public void Initialize()
    {
        _objects.Clear();
        _objects.Add(PlayerObject);
        // In a full implementation, objects from the map file would be added here.
    }

    /// <summary> Replaces the main loop logic from upd_objs() </summary>
    public void Update(float deltaTime, Viewport gameViewport, InputManager input)
    {
        // 1. Determine which objects are on screen to update them
        _onScreenObjects.Clear();
        var screenRect = new RectangleF(
            gameViewport.X - 96, gameViewport.Y - 48,
            gameViewport.Width + 192, gameViewport.Height + 96
        );

        foreach (var obj in _objects)
        {
            if (obj.Bounds.IntersectsWith(screenRect)) // A more robust implementation would check an IsAlwaysActive flag
            {
                _onScreenObjects.Add(obj);
            }
        }

        // 2. Update all on-screen objects
        foreach (var obj in _onScreenObjects)
        {
            obj.Update(deltaTime, this, input);
        }

        // 3. Handle object-vs-object collisions
        for (int i = 0; i < _onScreenObjects.Count; i++)
        {
            for (int j = i + 1; j < _onScreenObjects.Count; j++)
            {
                var objA = _onScreenObjects[i];
                var objB = _onScreenObjects[j];

                if (!objA.IsKilled && !objB.IsKilled && objA.Bounds.IntersectsWith(objB.Bounds))
                {
                    objA.OnTouch(objB, this);
                    objB.OnTouch(objA, this);
                }
            }
        }

        // 4. Remove killed objects
        _objects.RemoveAll(o => o.IsKilled);
    }

    public void Draw(IntPtr renderer, ShapeManager sm, Viewport vp)
    {
        foreach (var obj in _onScreenObjects)
        {
            if (!obj.IsKilled)
            {
                obj.Draw(renderer, sm, vp);
            }
        }
    }
}
