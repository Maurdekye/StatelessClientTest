using System.Numerics;

namespace StatelessClientTest.Game
{
    public class Projectile : GameEntity
    {

        public const float SPEED = 12f;
        public string EntityType => "Projectile";
        public float Radius => 0.2f;

        public GamePlayer Firer { get; }
        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; }
        public bool Impacted;

        public Projectile(GamePlayer firer, Vector2 position, Vector2 direction)
        {
            Firer = firer;
            Position = position;
            Direction = Vector2.Normalize(direction);
            Impacted = false;
        }

        public void Update(float timeDelta)
        {
            Position += Direction * timeDelta * SPEED;
        }

        public bool ShouldDestroy()
        {
            return Impacted 
                || Position.X < 0 
                || Position.Y < 0 
                || Position.X > GameStateManager.PLAY_AREA_SIZE.X 
                || Position.Y > GameStateManager.PLAY_AREA_SIZE.Y;
        }

        public void Collide(GameEntity other)
        {
            if (other is GamePlayer && (GamePlayer)other != Firer)
            {
                Impacted = true;
            }
        }
    }
}
