using System.Numerics;

namespace StatelessClientTest.Game
{
    public class Projectile : Entity
    {

        public readonly float SPEED = 12f;
        public string EntityType => "Projectile";
        public float Radius => 0.01f;

        public Player Firer { get; }
        public Vector2 Position { get; private set; }
        public Vector2 Direction { get; }
        public bool Impacted;

        public Projectile(Player firer, Vector2 position, Vector2 direction)
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
                || Position.X > GameManager.PLAY_AREA_SIZE.X 
                || Position.Y > GameManager.PLAY_AREA_SIZE.Y;
        }

        public void Collide(Entity other, Vector2 point)
        {
            if (other is Player && ((Player)other) != Firer)
                Impacted = true;
        }
    }
}
