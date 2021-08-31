using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StatelessClientTest.Hubs
{
    [Authorize]
    public class ConnectionHub : Hub
    {
        public GameStateManager Game;

        public ConnectionHub(GameStateManager game) 
        {
            Game = game;
        }

        public override async Task OnConnectedAsync()
        {
            var userid = Context.UserIdentifier;
            
            Game.RegisterPlayerIfNotRegistered(userid);
            Game.RegisterUserConnection(userid, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exc)
        {
            Game.UnregisterUserConnection(Context.ConnectionId);
        }

        public async Task UpdateControlState(PlayerControlState newState)
        {
            Game.PlayerControlUpdate(Context.UserIdentifier, newState);
        }

        public async Task<string> GetName()
        {
            return Context.User.Identity.Name;
        }

        public async Task<string> GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
