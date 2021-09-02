using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StatelessClientTest.Hubs
{
    [Authorize]
    public class ConnectionHub : Hub
    {
        public Game.GameStateManager GameManager;

        public ConnectionHub(Game.GameStateManager game) 
        {
            GameManager = game;
        }

        public override async Task OnDisconnectedAsync(Exception exc)
        {
            GameManager.UnregisterUserConnection(Context.ConnectionId);
        }

        public async Task RegisterUser()
        {
            var userid = Context.UserIdentifier;

            GameManager.TryAddNewPlayer(userid, Context.User.Identity.Name);
            GameManager.RegisterUserConnection(userid, Context.ConnectionId);
        }

        public async Task UpdateControlState(Dictionary<string, bool> newState)
        {
            GameManager.PlayerControlUpdate(Context.UserIdentifier, newState);
        }

        public async Task<Vector2> GetPlayAreaDimensions()
        {
            return Game.GameStateManager.PLAY_AREA_SIZE;
        }

        public async Task SendProjectile(Vector2 target)
        {
            GameManager.TryFireProjectile(Context.UserIdentifier, target);
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
