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
        public const int TICKRATE = 1000 / 64;
        public const int REPORT_RATE = 1000 / 1;

        private IHubContext<ConnectionHub> _hubContext;

        private Dictionary<string, string> ActiveConnections;
        private GameState State;
        private Stopwatch Timer;

        public GameStateManager(IHubContext<ConnectionHub> hub)
        {
            _hubContext = hub;

            State = new GameState();
            ActiveConnections = new Dictionary<string, string>();
            Timer = new Stopwatch();

            Timer.Start();
            _ = SimulationThread();
            _ = ReportingThread();
        }

        public void RegisterPlayerIfNotRegistered(string userid)
        {
            if (!State.Players.ContainsKey(userid))
            {
                State.Players.Add(userid, new GamePlayer());
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

        private async Task SimulationThread()
        {
            var last_tick = Timer.ElapsedTicks;
            while (true)
            {
                var current_tick = Timer.ElapsedTicks;
                var time_delta = (current_tick - last_tick) / Stopwatch.Frequency;
                SimulationStep(time_delta);
                await Task.Delay(TICKRATE);
            }
        }

        private async Task ReportingThread()
        {
            while (true)
            {
                foreach (string connectionid in ActiveConnections.Keys)
                {
                    _ = _hubContext.Clients.Client(connectionid).SendAsync("GameStateReport", new { Timestamp = Timer.ElapsedTicks, State });
                }
                await Task.Delay(REPORT_RATE);
            }
        }

        private void SimulationStep(float timeDelta)
        {
            foreach (GamePlayer player in State.Players.Values)
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
            if (State.Players.ContainsKey(userid))
            {
                State.Players[userid].ControlState = controlState;
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
