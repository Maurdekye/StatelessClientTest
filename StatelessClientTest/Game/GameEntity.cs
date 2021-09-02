using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StatelessClientTest.Game
{
    public interface GameEntity
    {
        public string EntityType { get; }
        public void Update(float timeDelta);
        public bool ShouldDestroy();
    }
}
