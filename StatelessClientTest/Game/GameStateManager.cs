using Microsoft.AspNetCore.SignalR;
using StatelessClientTest.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace StatelessClientTest.Game
{
    public class GameStateManager
    {
        public const int TICK_RATE = 1000 / 128;
        public const int REPORT_RATE = 1000 / 60;
        public const float ACCELERATION = 3f;
        public const float SNEAK_SPEED = 0.6f;
        public const float BASE_SPEED = 1.5f;
        public const float SPRINT_SPEED = 3f;
        public const float PROJ_SPEED = 12f;
        public const float PROJ_RATE = 0.25f;
        public static Vector2 PLAY_AREA = new Vector2(10, 10);

        private IHubContext<ConnectionHub> Hub;

        private Dictionary<string, string> ActiveConnections;
        public GameState State { get; private set; }
        public Stopwatch Timer { get; private set; }

        private readonly object PlayerLock = new object();
        private readonly object ProjectileLock = new object();


        public GameStateManager(IHubContext<ConnectionHub> hub)
        {
            Hub = hub;

            State = new GameState();
            ActiveConnections = new Dictionary<string, string>();
            Timer = new Stopwatch();

            Timer.Start();
            _ = SimulationThread();
            _ = ReportingThread();
        }

        public void RegisterPlayerIfNotRegistered(string userid, string name)
        {
            lock (PlayerLock)
            {
                if (!State.Players.ContainsKey(userid))
                {
                    State.Players.Add(userid, new GamePlayer(this, name, PLAY_AREA / 2));
                }
            }
        }

        public void RegisterUserConnection(string userid, string connectionid)
        {
            lock (ActiveConnections)
            {
                ActiveConnections.TryAdd(connectionid, userid);
            }
        }

        public void UnregisterUserConnection(string connectionid)
        {
            lock (ActiveConnections)
            {
                ActiveConnections.Remove(connectionid);
            }
        }

        private async Task SimulationThread()
        {
            try
            {
                var last_tick = Timer.ElapsedTicks;
                while (true)
                {
                    var current_tick = Timer.ElapsedTicks;
                    float time_delta = (current_tick - last_tick) / (float)Stopwatch.Frequency;
                    last_tick = current_tick;
                    SimulationStep(time_delta);
                    await Task.Delay(TICK_RATE);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Exception in simulation thread: {e.Message}");
            }
        }

        private async Task ReportingThread()
        {
            try
            {
                while (true)
                {
                    lock (PlayerLock) lock (ProjectileLock)
                    {
                        _ = Hub.Clients.All.SendAsync("GameStateReport", new { Timestamp = Timer.ElapsedTicks, State });
                    }
                    await Task.Delay(REPORT_RATE);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Exception in reporting thread: {e.Message}");
            }
        }

        private void SimulationStep(float timeDelta)
        {
            lock (PlayerLock)
            {
                foreach (GamePlayer player in State.Players.Values)
                {
                    player.Update(timeDelta);
                }
            }

            lock (ProjectileLock)
            {
                foreach (Projectile ent in State.Projectiles)
                {
                    ent.Update(timeDelta);
                }
                State.Projectiles.RemoveAll(p => p.ShouldDestroy());
            }
        }

        public void PlayerControlUpdate(string userid, Dictionary<string, bool> controlState)
        {
            if (State.Players.ContainsKey(userid))
            {
                foreach (var control in State.Players[userid].ControlState.Keys)
                {
                    if (controlState.ContainsKey(control))
                    {
                        lock (PlayerLock)
                        {
                            State.Players[userid].ControlState[control].Pressed = controlState[control];
                        }
                    }
                }
            }
        }
        public void TryFireProjectile(string userid, Vector2 target)
        {
            lock (PlayerLock)
            {
                if (State.Players.ContainsKey(userid))
                {
                    State.Players[userid].TryFireProjectile(target);
                }
            }
        }

        public void SendProjectile(GamePlayer sender, Vector2 target)
        {
            lock (ProjectileLock)
            {
                var new_projectile = new Projectile(sender, sender.Position, target - sender.Position);
                State.Projectiles.Add(new_projectile);
            }
        }

        public class GameState
        {
            public Dictionary<string, GamePlayer> Players;
            public List<Projectile> Projectiles;

            public GameState()
            {
                Players = new Dictionary<string, GamePlayer>();
                Projectiles = new List<Projectile>();
            }
        }
    }
}
