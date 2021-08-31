using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StatelessClientTest.Hubs
{
    public class ConnectionHub : Hub
    {
        public readonly GameStateManager _gameStateManager;

        public ConnectionHub(GameStateManager gameStateManager) 
        {
            _gameStateManager = gameStateManager;
        }

        public override Task OnConnectedAsync()
        {
            var userid = Context.GetHttpContext().Request.Query["username"].ToString();
            
            _gameStateManager.RegisterPlayerIfNotRegistered(userid);
            _gameStateManager.RegisterUserConnection(userid, Context.ConnectionId);

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exc)
        {
            _gameStateManager.UnregisterUserConnection(Context.ConnectionId);
            return Task.CompletedTask;
        }
    }
}
