using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace StatelessClientTest.Game
{
    public class GamePlayer
    {
        public const int MAX_BUFFERED_ACTIONS = 1;

        public string Name;
        public Vector2 Position;
        public Vector2 Direction { get; private set; }
        public long lastProjectile;

        //public bool ShouldSerializeControlState() => false;
        [JsonIgnore]
        internal Dictionary<string, PlayerControl> ControlState;
        //public bool ShouldSerializeManager() => false;
        [JsonIgnore]
        public GameStateManager Manager;
        //public bool ShouldSerializeActionBuffer() => false;
        [JsonIgnore]
        public List<PlayerAction> ProjectileBuffer;

        public GamePlayer(GameStateManager manager, string name, Vector2 position)
        {
            Name = name;
            Position = position;
            ControlState = new Dictionary<string, PlayerControl>();
            lastProjectile = 0;
            Manager = manager;
            ProjectileBuffer = new List<PlayerAction>();
            foreach (var control in new string[] { "up", "down", "left", "right", "sprinting", "sneaking" }) 
            {
                ControlState.Add(control, new PlayerControl(GameStateManager.ACCELERATION));
            }
        }

        public GamePlayer(GameStateManager manager, string name) : this(manager, name, new Vector2(0, 0)) { }

        public void Update(float timeDelta)
        {   
            // control state

            foreach (var control in ControlState.Values)
            {
                control.Update(timeDelta);
            }

            // movement

            float speed = GameStateManager.BASE_SPEED;
            float sprinting = ControlState["sprinting"].Value;
            speed = speed * (1 - sprinting) + GameStateManager.SPRINT_SPEED * sprinting;
            float sneaking = ControlState["sneaking"].Value;
            speed = speed * (1 - sneaking)  + GameStateManager.SNEAK_SPEED * sneaking;

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

            Position.X = Math.Clamp(Position.X, 0, GameStateManager.PLAY_AREA.X);
            Position.Y = Math.Clamp(Position.Y, 0, GameStateManager.PLAY_AREA.Y);

            // projectile buffer

            lock (ProjectileBuffer)
            {
                while (ProjectileBuffer.Count > 0)
                {
                    var projectile_action = (FireProjectileAction)ProjectileBuffer[0];

                    var target = projectile_action.Target;
                    if (target == Position)
                        break;

                    long fire_time = Manager.Timer.ElapsedTicks;

                    if ((fire_time - lastProjectile) / (float)Stopwatch.Frequency < GameStateManager.PROJ_RATE)
                        break;

                    ProjectileBuffer.RemoveAt(0);

                    Manager.SendProjectile(this, target);
                    lastProjectile = fire_time;
                }

            }
        }
        public void TryFireProjectile(Vector2 target)
        {
            lock (ProjectileBuffer)
            {
                if (ProjectileBuffer.Count < MAX_BUFFERED_ACTIONS)
                    ProjectileBuffer.Add(new FireProjectileAction(target));
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
