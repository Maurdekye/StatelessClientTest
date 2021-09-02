using System.Numerics;

namespace StatelessClientTest.Game
{
    public class Projectile : GameEntity
    {
        public string EntityType { get => "Projectile"; }

        public GamePlayer Firer { get; }
        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; }

        public Projectile(GamePlayer firer, Vector2 position, Vector2 direction)
        {
            Firer = firer;
            Position = position;
            Direction = Vector2.Normalize(direction);
        }

        public void Update(float timeDelta)
        {
            Position += Direction * timeDelta * GameStateManager.PROJ_SPEED;
        }

        public bool ShouldDestroy()
        {
            return Position.X < 0 
                || Position.Y < 0 
                || Position.X > GameStateManager.PLAY_AREA.X 
                || Position.Y > GameStateManager.PLAY_AREA.Y;
        }
    }
}
