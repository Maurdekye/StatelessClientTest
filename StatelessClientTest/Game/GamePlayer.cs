using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace StatelessClientTest.Game
{
    public class GamePlayer : GameEntity
    {
        public const int MAX_BUFFERED_ACTIONS = 1;
        public const float BASE_SPEED = 1.5f;
        public const float SNEAK_SPEED = 0.6f;
        public const float SPRINT_SPEED = 3f;
        public const float ACCELERATION = 3f;
        public const float FIRE_RATE = 0.25f;
        public readonly string[] CONTROL_NAMES = new string[] { "up", "down", "left", "right", "sprinting", "sneaking" };
        public float Radius => 0.5f;
        public string EntityType => "Player";

        public string Name;
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; private set; }
        public int Score { get; private set; }
        public long LastProjectile;
        public bool Defeated;

        [JsonIgnore]
        internal Dictionary<string, PlayerControl> ControlState;
        [JsonIgnore]
        public GameStateManager Manager;
        [JsonIgnore]
        public Queue<PlayerAction> ProjectileBuffer;

        public GamePlayer(GameStateManager manager, string name, Vector2 position)
        {
            Name = name;
            Position = position;
            Direction = new Vector2(0, 0);
            Score = 0;
            LastProjectile = 0;
            Defeated = false;

            ControlState = new Dictionary<string, PlayerControl>();
            Manager = manager;
            ProjectileBuffer = new Queue<PlayerAction>();

            foreach (var control in CONTROL_NAMES) 
            {
                ControlState.Add(control, new PlayerControl(ACCELERATION));
            }
        }

        public GamePlayer(GameStateManager manager, string name) : this(manager, name, new Vector2(0, 0)) { }

        public void Update(float timeDelta)
        {   
            foreach (var control in ControlState.Values)
            {
                control.Update(timeDelta);
            }

            UpdateMovement(timeDelta);
            CheckIfShouldFire();
        }

        public void UpdateMovement(float timeDelta)
        {
            float speed = BASE_SPEED;
            float sprinting = ControlState["sprinting"].Value;
            speed = speed * (1 - sprinting) + SPRINT_SPEED * sprinting;
            float sneaking = ControlState["sneaking"].Value;
            speed = speed * (1 - sneaking) + SNEAK_SPEED * sneaking;

            Vector2 movement_direction = new Vector2(0f);
            movement_direction += new Vector2(0f, 1f) * ControlState["up"].Value;
            movement_direction += new Vector2(0f, -1f) * ControlState["down"].Value;
            movement_direction += new Vector2(-1f, 0f) * ControlState["left"].Value;
            movement_direction += new Vector2(1f, 0f) * ControlState["right"].Value;

            Vector2 movement = movement_direction;
            if (movement_direction.Length() > 1)
                movement = Vector2.Normalize(movement_direction);

            Direction = movement;

            movement *= timeDelta * speed;
            Position += movement;

            Position = new Vector2(
                Math.Clamp(Position.X, 0, GameStateManager.PLAY_AREA_SIZE.X),
                Math.Clamp(Position.Y, 0, GameStateManager.PLAY_AREA_SIZE.Y)
            );
        }

        public void CheckIfShouldFire()
        {
            lock (ProjectileBuffer)
            {
                while (ProjectileBuffer.Count > 0)
                {
                    var projectile_action = (FireProjectileAction)ProjectileBuffer.Peek();

                    var target = projectile_action.Target;
                    if (target == Position)
                        break;

                    long fire_time = Manager.Timer.ElapsedTicks;

                    if ((fire_time - LastProjectile) / (float)Stopwatch.Frequency < FIRE_RATE)
                        break;

                    ProjectileBuffer.Dequeue();

                    Manager.SendProjectile(this, target);
                    LastProjectile = fire_time;
                }
            }
        }

        public void TryFireProjectile(Vector2 target)
        {
            if (Defeated)
                return;

            lock (ProjectileBuffer)
            {
                if (ProjectileBuffer.Count < MAX_BUFFERED_ACTIONS)
                    ProjectileBuffer.Enqueue(new FireProjectileAction(target));
            }
        }

        public void SetInputs(Dictionary<string, bool> inputMap)
        {
            if (Defeated)
                return;

            foreach (var control in ControlState.Keys)
            {
                if (inputMap.ContainsKey(control))
                {
                    ControlState[control].Pressed = inputMap[control];
                }
            }
        }

        public void Defeat()
        {
            Defeated = true;
            foreach (var control in CONTROL_NAMES)
            {
                ControlState[control].Pressed = false;
            }
        }

        public void Revive(Vector2 position)
        {
            Defeated = false;
            Position = position;
            foreach (var control in CONTROL_NAMES)
            {
                ControlState[control].Reset();
            }
        }

        public bool ShouldDestroy()
        {
            return false;
        }

        public void Collide(GameEntity other)
        {
            if (other is Projectile && ((Projectile)other).Firer != this)
            {
                Defeat();
                ((Projectile)other).Firer.Score += 1;
            }
        }
    }
    public class PlayerControl
    {
        float ControlAttenuation { get; }
        public bool Pressed { get; set; }
        public float Value { get; private set; }

        public PlayerControl(float controlAttenuation)
        {
            ControlAttenuation = controlAttenuation;
            Pressed = false;
            Value = 0;
        }

        public void Update(float timeDelta)
        {
            if (Pressed)
                Value += ControlAttenuation * timeDelta;
            else
                Value -= ControlAttenuation * timeDelta;
            Value = Math.Clamp(Value, 0, 1);
        }

        public void Reset()
        {
            Pressed = false;
            Value = 0;
        }
    }

    public interface PlayerAction { }

    public struct FireProjectileAction : PlayerAction
    {
        public Vector2 Target;

        public FireProjectileAction(Vector2 target)
        {
            Target = target;
        }
    }
}
