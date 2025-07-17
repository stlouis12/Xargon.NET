using System.Drawing;
using System.Numerics;
using Xargon.NET.Audio;
using Xargon.NET.Core;
using Xargon.NET.Graphics;
using Xargon.NET.Input;
using static SDL3.SDL;

namespace Xargon.NET.GameObjects;

/// <summary>
/// An enum representing the player's current action state.
/// This replaces the `st_` defines from the original C code.
/// </summary>
public enum PlayerState
{
    Begin,
    Stand,
    Jumping,
    Die,
    Transport,
    Platform,
    Climbing,
    Still
}

/// <summary>
/// The Player class, which encapsulates all logic from the original `msg_player` function.
/// </summary>
public class Player : GameObject
{
    public PlayerState State { get; private set; }
    public int Health { get; private set; } = 5;
    private float _ouchTimer; // Timer for flashing/invincibility
    private float _facingDirection = 1f;

    private const float WalkSpeed = 128.0f;
    private const float Gravity = 800.0f;
    private const float MaxFallSpeed = 256.0f;
    private const float JumpStrength = 280.0f;

    public Player(int x, int y) : base(x, y)
    {
        State = PlayerState.Begin;
        _ouchTimer = 1.5f; // Start with a brief period of invincibility
        UpdateBounds();
    }

    public override void Update(float deltaTime, ObjectManager om, InputManager input)
    {
        if (State == PlayerState.Die) return; // Don't process input or physics when dead

        // Handle invincibility timer
        if (_ouchTimer > 0)
        {
            _ouchTimer -= deltaTime;
        }

        // Get directional input
        float moveInput = 0;
        if (input.IsKeyHeld(Scancode.Left)) moveInput = -1;
        if (input.IsKeyHeld(Scancode.Right)) moveInput = 1;
        if (moveInput != 0) _facingDirection = moveInput;

        // State-based logic
        switch (State)
        {
            case PlayerState.Begin:
                if (_ouchTimer <= 0) State = PlayerState.Stand;
                break;
            case PlayerState.Stand:
                UpdateStanding(input, moveInput, om.SoundManager);
                break;
            case PlayerState.Jumping:
                UpdateJumping(deltaTime, moveInput);
                break;
        }

        // --- Physics and Collision Resolution ---
        // This is a standard way to handle tile-based physics to avoid tunneling.

        // 1. Apply horizontal movement
        Position += new Vector2(Velocity.X * deltaTime, 0);
        UpdateBounds();
        HandleHorizontalCollision(om.Board);

        // 2. Apply vertical movement
        Position += new Vector2(0, Velocity.Y * deltaTime);
        UpdateBounds();
        HandleVerticalCollision(om.Board, om.SoundManager);
    }

    private void UpdateStanding(InputManager input, float moveInput, SoundManager sound)
    {
        Velocity = new Vector2(moveInput * WalkSpeed, Velocity.Y); // Walk

        // Transition to jumping if on the ground and jump is pressed
        if (!IsFalling())
        {
            if (input.IsKeyPressed(Scancode.Space))
            {
                State = PlayerState.Jumping;
                Velocity = new Vector2(Velocity.X, -JumpStrength);
                sound.PlaySound("jump");
            }
        }
        else // If not on ground, we should be in the jumping/falling state
        {
            State = PlayerState.Jumping;
        }
    }

    private void UpdateJumping(float deltaTime, float moveInput)
    {
        // Allow air control
        Velocity = new Vector2(moveInput * WalkSpeed, Velocity.Y + Gravity * deltaTime);
    }

    private void HandleHorizontalCollision(Board board)
    {
        var rect = GetIntBounds();
        // Check left/right sides
        for (int y = rect.Top; y < rect.Bottom; y++)
        {
            if (Velocity.X > 0 && board.IsSolid(rect.Right, y)) // Moving right, hit wall
            {
                Position = new Vector2(rect.Right - Bounds.Width - 1, Position.Y);
                Velocity = new Vector2(0, Velocity.Y);
                break;
            }
            if (Velocity.X < 0 && board.IsSolid(rect.Left, y)) // Moving left, hit wall
            {
                Position = new Vector2(rect.Left + 1, Position.Y);
                Velocity = new Vector2(0, Velocity.Y);
                break;
            }
        }
        UpdateBounds();
    }

    private void HandleVerticalCollision(Board board, SoundManager sound)
    {
        var rect = GetIntBounds();
        // Check top/bottom sides
        for (int x = rect.Left; x < rect.Right; x++)
        {
            if (Velocity.Y > 0 && board.IsSolid(x, rect.Bottom)) // Falling, hit ground
            {
                Position = new Vector2(Position.X, rect.Bottom - Bounds.Height - 1);
                Velocity = new Vector2(Velocity.X, 0);
                State = PlayerState.Stand;
                // sound.PlaySound("land"); // TODO: Add landing sound
                break;
            }
            if (Velocity.Y < 0 && board.IsSolid(x, rect.Top)) // Jumping, hit ceiling
            {
                Position = new Vector2(Position.X, rect.Top + 1);
                Velocity = new Vector2(0, Velocity.Y); // Zero out Y velocity
                break;
            }
        }
        UpdateBounds();
    }

    // A helper to determine if the player is currently on solid ground
    private bool IsFalling() => Velocity.Y > 0;

    public override void Draw(IntPtr renderer, ShapeManager sm, Viewport vp)
    {
        if (IsInvincible() && (int)(_ouchTimer * 10) % 2 == 0) return; // Flash effect

        // Placeholder for a full animation system
        int shapeId = 0x0A08; // Default idle sprite
        if (State == PlayerState.Jumping) shapeId = 0x0A0F;
        if (State == PlayerState.Die) shapeId = 0x0A13;

        IntPtr texture = sm.GetTexture(shapeId);
        if (texture == IntPtr.Zero) return;

        GetTextureSize(texture, out float w, out float h);
        var destRect = new FRect
        {
            X = Position.X - vp.X,
            Y = Position.Y - vp.Y,
            W = w,
            H = h
        };
        RenderTexture(renderer, texture, IntPtr.Zero, ref destRect);
    }

    public void TakeDamage(int amount, SoundManager sound)
    {
        if (IsInvincible()) return;

        Health -= amount;
        _ouchTimer = 1.0f; // Become invincible and flash for 1 second
        // sound.PlaySound("ouch");

        if (Health <= 0)
        {
            Health = 0;
            State = PlayerState.Die;
            // sound.PlaySound("herodie");
        }
    }

    private bool IsInvincible() => _ouchTimer > 0f;

    public override void OnTouch(GameObject other, ObjectManager om)
    {
        if (other is Enemy)
        {
            TakeDamage(1, om.SoundManager);
        }
    }

    private void UpdateBounds() => Bounds = new RectangleF(Position.X, Position.Y, 24, 42);
    private Rectangle GetIntBounds() => new((int)Bounds.X, (int)Bounds.Y, (int)Bounds.Width, (int)Bounds.Height);
}
