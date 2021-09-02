using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace StatelessClientTest.Game
{
    public class Player : Entity
    {
        public const int MAX_BUFFERED_ACTIONS = 1;
        public const float BASE_SPEED = 1.5f;
        public const float SNEAK_SPEED = 0.6f;
        public const float SPRINT_SPEED = 3f;
        public const float ACCELERATION = 3f;
        public const float FIRE_RATE = 0.25f;
        public readonly string[] CONTROL_NAMES = new string[] { "up", "down", "left", "right", "sprinting", "sneaking" };
        public float Radius => 0.08f;
        public string EntityType => "Player";

        public string Id;
        public string Name;
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; private set; }
        public int Score { get; private set; }
        public int Deaths { get; private set; }
        public long LastProjectile;
        public bool Defeated;
        public bool CollisionsEnabled { get; private set; }

        [JsonIgnore]
        internal Dictionary<string, PlayerControl> ControlState;
        [JsonIgnore]
        public GameManager Manager;
        [JsonIgnore]
        public Queue<PlayerAction> ProjectileBuffer;

        public Player(GameManager manager, string id, string name, Vector2 position)
        {
            Id = id;
            Name = name;
            Position = position;
            Direction = new Vector2(0, 0);
            Score = 0;
            LastProjectile = 0;
            Defeated = false;
            CollisionsEnabled = true;

            ControlState = new Dictionary<string, PlayerControl>();
            Manager = manager;
            ProjectileBuffer = new Queue<PlayerAction>();

            foreach (var control in CONTROL_NAMES) 
            {
                ControlState.Add(control, new PlayerControl(ACCELERATION));
            }
        }

        public Player(GameManager manager, string id, string name) : this(manager, id, name, new Vector2(0, 0)) { }

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
                Math.Clamp(Position.X, Radius, GameManager.PLAY_AREA_SIZE.X - Radius),
                Math.Clamp(Position.Y, Radius, GameManager.PLAY_AREA_SIZE.Y - Radius)
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
            CollisionsEnabled = false;
            Deaths += 1;
            foreach (var control in CONTROL_NAMES)
            {
                ControlState[control].Pressed = false;
            }
        }

        public void Revive(Vector2 position)
        {
            Defeated = false;
            CollisionsEnabled = true;
            Position = position;
            foreach (var control in CONTROL_NAMES)
            {
                ControlState[control].Reset();
            }
        }

        public bool ShouldDestroy() => false;

        public void Collide(Entity other, Vector2 point)
        {
            if (other is Projectile)
            {
                var projectile = (Projectile)other;
                if (projectile.Firer != this)
                {
                    Defeat();
                    projectile.Firer.Score += 1;
                }
            }
            else if (other is Player)
            {
                var player = (Player)other;
                var dist = Vector2.Distance(Position, player.Position);
                if (dist < Radius + player.Radius && player.Position != point)
                {
                    Position = point + Vector2.Normalize(point - player.Position) * Radius;
                }
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
