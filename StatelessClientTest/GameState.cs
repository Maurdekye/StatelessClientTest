using System.Collections.Generic;

namespace StatelessClientTest
{
    public class GameState
    {
        public Dictionary<string, GamePlayer> Players;
        
        public GameState()
        {
            Players = new Dictionary<string, GamePlayer>();
        }
    }
}
