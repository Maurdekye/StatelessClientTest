using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace StatelessClientTest.Game
{
    public interface GameEntity
    {
        public float Radius { get; }
        public string EntityType { get; }
        public Vector2 Position { get; }

        public void Update(float timeDelta);
        public bool ShouldDestroy();
        public void Collide(GameEntity other);
    }
}
