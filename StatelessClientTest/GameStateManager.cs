using StatelessClientTest.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace StatelessClientTest
{
    public class GameStateManager
    {
        public Dictionary<string, string> ActiveConnections { get; init; } = new();

        public GameState GameState { get; init; } = new();


        public void RegisterPlayerIfNotRegistered(string userid)
        {
            if (!GameState.Players.ContainsKey(userid))
            {
                GameState.Players.Add(userid, new GamePlayer());
            }
        }

        public void RegisterUserConnection(string userid, string connectionid)
        {
            ActiveConnections.Add(connectionid, userid);
        }

        public void UnregisterUserConnection(string connectionid)
        {
            ActiveConnections.Remove(connectionid);
        }
    }
}
