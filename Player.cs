using System.Numerics;
using SDL3;
using Xargon.NET.Graphics;
using Xargon.NET.Input;
using Xargon.NET.Audio;

namespace Xargon.NET.GameObjects;

/// <summary>
/// An enum representing the player's current action state.
/// This replaces the `st_` defines from the original C code.
/// </summary>
public enum PlayerState
{
    Stand,
    Begin,
    Die,
    Transport,
    Platform,
    Jumping,
    Climbing,
    Still
}

/// <summary>
/// The Player class, which encapsulates all logic from the original `msg_player` function.
/// </summary>
public class Player : GameObject
{
    // Player-specific fields, replacing parts of the C `objtype` struct
    public PlayerState State { get; private set; }
    private int _subState;      // Animation frame or minor state
    private float _stateCount;  // Timer for current state

    private float _facingDirection = 1f; // 1 for right, -1 for left

    // Physics constants, tuned to feel like the original game
    private const float WalkSpeed = 128.0f; // 8 pixels/frame * 16-ish frames/sec
    private const float Gravity = 800.0f;
    private const float MaxFallSpeed = 256.0f;
    private const float JumpStrength = 280.0f;

    public Player(int x, int y) : base(x, y)
    {
        // Initial state from init_objs() -> objs[0].state = st_stand
        State = PlayerState.Stand;
        // Set initial size from kindxl/kindyl
        Bounds = new System.Drawing.RectangleF(x, y, 24, 42);
    }

    /// <summary>
    /// Translates the `case msg_update:` block from `msg_player`.
    /// </summary>
    public override void Update(float deltaTime, ObjectManager om, InputManager input)
    {
        // Get input direction
        float moveInput = 0;
        if (input.IsKeyHeld(SDL.Scancode.Left)) moveInput = -1;
        if (input.IsKeyHeld(SDL.Scancode.Right)) moveInput = 1;

        // Update state machine
        _stateCount += deltaTime;

        switch (State)
        {
            case PlayerState.Stand:
                UpdateStanding(deltaTime, input, moveInput, om.SoundManager);
                break;

            case PlayerState.Jumping:
                UpdateJumping(deltaTime, input, moveInput, om.SoundManager);
                break;

            // Other states like Climbing, Dying, etc. would go here
        }

        // Update position based on velocity
        // This replaces manual `pobj->x += ...` calls
        Position += Velocity * deltaTime;
        UpdateBounds();
    }

    private void UpdateStanding(float deltaTime, InputManager input, float moveInput, SoundManager sound)
    {
        // Handle starting a walk
        if (moveInput != 0)
        {
            Velocity = new Vector2(moveInput * WalkSpeed, Velocity.Y);
            _facingDirection = moveInput;
            _stateCount = 0; // Reset animation timer
            _subState = (_subState + 1) % 8; // Cycle through walk animation frames
        }
        else
        {
            Velocity = new Vector2(0, Velocity.Y); // Stop moving
        }

        // Handle jumping
        if (input.IsKeyPressed(SDL.Scancode.LCtrl) || input.IsKeyPressed(SDL.Scancode.Space))
        {
            State = PlayerState.Jumping;
            Velocity = new Vector2(Velocity.X, -JumpStrength);
            _stateCount = 0;
            sound.PlaySound("jump.wav"); // Corresponds to snd_play(2, snd_jump)
        }

        // Handle falling off a ledge
        // This replaces `if (cando(...) == (f_playerthru|f_notstair))`
        // if (!IsOnGround())
        // {
        //     State = PlayerState.Jumping;
        // }
    }

    private void UpdateJumping(float deltaTime, InputManager input, float moveInput, SoundManager sound)
    {
        // Apply air control
        Velocity = new Vector2(moveInput * WalkSpeed, Velocity.Y);
        if (moveInput != 0) _facingDirection = moveInput;

        // Apply gravity
        Velocity += new Vector2(0, Gravity * deltaTime);
        if (Velocity.Y > MaxFallSpeed)
        {
            Velocity = new Vector2(Velocity.X, MaxFallSpeed);
        }

        // Check for landing
        // This replaces `if (!trymovey ...)`
        // if (IsOnGround() && Velocity.Y >= 0)
        // {
        //     State = PlayerState.Stand;
        //     Velocity = Vector2.Zero;
        //     _stateCount = -0.2f; // Special value to trigger landing animation
        //     sound.PlaySound("land.wav"); // Corresponds to snd_play(2, snd_land)
        // }
    }

    /// <summary>
    /// Translates the `case msg_draw:` block from `msg_player`.
    /// </summary>
    public override void Draw(IntPtr renderer, ShapeManager sm, Viewport vp)
    {
        int shapeId = 0;
        const int playerShapeBase = 0x0A00; // Base ID for player sprites in graphics.xr1

        switch (State)
        {
            case PlayerState.Stand:
                // This logic determines which sprite to show based on player's action
                if (_stateCount < 0) // Landing animation
                {
                    shapeId = playerShapeBase + 0x1A;
                }
                else if (Velocity.X != 0) // Walking/Running
                {
                    // Select running frame, facing left or right
                    int frame = (_subState / 2) % 4; // 4 animation frames
                    shapeId = playerShapeBase + frame + (_facingDirection > 0 ? 4 : 0);
                }
                else // Standing still
                {
                    // Add logic for looking up/down or fidgeting
                    if (_stateCount > 10) // Fidget animation after 10 seconds
                        shapeId = playerShapeBase + 0x09;
                    else // Idle stance
                        shapeId = playerShapeBase + 0x08;
                }
                break;

            case PlayerState.Jumping:
                if (_facingDirection > 0) // Facing right
                {
                    shapeId = playerShapeBase + (Velocity.Y <= 0 ? 0x11 : 0x12); // Jumping up vs falling down
                }
                else // Facing left
                {
                    shapeId = playerShapeBase + (Velocity.Y <= 0 ? 0x0F : 0x10);
                }
                break;
            
            case PlayerState.Die:
                shapeId = playerShapeBase + 0x13 + (int)(_stateCount * 4); // Ash animation
                break;
        }

        // Draw the final calculated shape, flipping it if necessary
        DrawPlayerShape(renderer, sm, shapeId);
    }

    private void DrawPlayerShape(IntPtr renderer, ShapeManager sm, int shapeId)
    {
        IntPtr texture = sm.GetTexture(shapeId);
        if (texture == IntPtr.Zero) return;

        SDL.GetTextureSize(texture, out float w, out float h);
        var destRect = new SDL.FRect { X = (int)Position.X, Y = (int)Position.Y, W = w, H = h };
        
        // The original game used separate sprites for left/right. A modern approach
        // can use one set and flip them, which saves texture memory.
        // However, to be true to the original, we load the specific left/right sprites.
        // If we were to use flipping:
        // var flip = _facingDirection < 0 ? SDL_RendererFlip.SDL_FLIP_HORIZONTAL : SDL_RendererFlip.SDL_FLIP_NONE;
        // SDL_RenderTextureRotated(renderer, texture, IntPtr.Zero, ref destRect, 0, IntPtr.Zero, flip);

        SDL.RenderTexture(renderer, texture, IntPtr.Zero, destRect); 

        if (IsInvincible())
        {
            // Draw a shield or flash the player to show invincibility
        }
    }
    
    private void UpdateBounds()
    {
        // Update the collision rectangle to match the player's position.
        var bounds = Bounds;
        bounds.Location = new System.Drawing.PointF(Position.X, Position.Y);
        Bounds = bounds;
    }

    /// <summary>
    /// Corresponds to `p_ouch` from the C code.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsInvincible()) return;

        // pl.health -= amount;
        // pl.ouched = 4; // Timer for flashing effect
        // if (pl.health <= 0) {
        //     State = PlayerState.Die;
        //     _stateCount = 0;
        // }
    }

    private bool IsInvincible()
    {
        // Check for invincibility powerup or post-damage grace period
        // return invcount(inv_invin) > 0 || pl.ouched > 0;
        return false;
    }

    public override void OnTouch(GameObject other, ObjectManager om)
    {
        // Handle collisions with other objects (enemies, items, etc.)
        // Example:
        // if (other is Enemy)
        // {
        //     TakeDamage(1);
        // }
        // else if (other is HealthPickup)
        // {
        //     pl.health++;
        //     other.Kill();
        // }
    }
}

