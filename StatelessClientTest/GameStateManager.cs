using Microsoft.AspNetCore.SignalR;
using StatelessClientTest.Hubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace StatelessClientTest
{
    public class GameStateManager
    {
        public const int TickRate = 1000 / 64;
        public const int ReportRate = 1000 / 1;

        private IHubContext<ConnectionHub> _hubContext;

        private Dictionary<string, string> _activeConnections;
        private GameState _gameState;
        private Stopwatch _stopWatch;

        public GameStateManager(IHubContext<ConnectionHub> hub)
        {
            _hubContext = hub;

            _gameState = new GameState();
            _activeConnections = new Dictionary<string, string>();
            _stopWatch = new Stopwatch();

            _stopWatch.Start();
            _ = SimulationThread();
            _ = ReportingThread();
        }

        public void RegisterPlayerIfNotRegistered(string userid)
        {
            if (!_gameState.Players.ContainsKey(userid))
            {
                _gameState.Players.Add(userid, new GamePlayer());
            }
        }

        public void RegisterUserConnection(string userid, string connectionid)
        {
            _activeConnections.Add(connectionid, userid);
        }

        public void UnregisterUserConnection(string connectionid)
        {
            _activeConnections.Remove(connectionid);
        }

        private async Task SimulationThread()
        {
            var last_tick = _stopWatch.ElapsedTicks;
            while (true)
            {
                var current_tick = _stopWatch.ElapsedTicks;
                var time_delta = (current_tick - last_tick) / Stopwatch.Frequency;
                SimulationStep(time_delta);
                await Task.Delay(TickRate);
            }
        }

        private async Task ReportingThread()
        {
            while (true)
            {
                foreach (string connectionid in _activeConnections.Keys)
                {
                    _ = _hubContext.Clients.Client(connectionid).SendAsync("GameStateReport", new { Timestamp = _stopWatch.ElapsedTicks, _gameState });
                }
                await Task.Delay(ReportRate);
            }
        }

        private void SimulationStep(float timeDelta)
        {
            foreach (GamePlayer player in _gameState.Players.Values)
            {
                var speed = 1.0f;
                if (player.ControlState.Sprinting)
                    speed = 2.5f;

                Vector2 movement = new Vector2(0f);
                if (player.ControlState.Up)
                    movement += new Vector2(0f, 1f);
                if (player.ControlState.Right)
                    movement += new Vector2(1f, 0f);
                if (player.ControlState.Down)
                    movement += new Vector2(0f, -1f);
                if (player.ControlState.Left)
                    movement += new Vector2(-1f, 0f);

                movement = movement / movement.Length();
                movement = movement * speed * timeDelta;

                player.Position += movement;
            }
        }

        public void PlayerControlUpdate(string userid, PlayerControlState controlState)
        {
            if (_gameState.Players.ContainsKey(userid))
            {
                _gameState.Players[userid].ControlState = controlState;
            }
        }
    }

    public class GameState
    {
        public Dictionary<string, GamePlayer> Players;
        
        public GameState()
        {
            Players = new Dictionary<string, GamePlayer>();
        }
    }

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

    public class PlayerControlState
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
        public bool Sprinting;

        public PlayerControlState()
        {
            Up = false;
            Down = false;
            Left = false;
            Right = false;
            Sprinting = false;
        }
    }
}
