using System.Numerics;

namespace StatelessClientTest
{
    public class GamePlayer
    {
        public Vector2 Position;
        public PlayerControlState ControlState;

        public GamePlayer()
        {
            Position = new Vector2(0f, 0f);
            ControlState = new PlayerControlState();
        }
    }
}
