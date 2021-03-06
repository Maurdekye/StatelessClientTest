using Microsoft.AspNetCore.SignalR;
using StatelessClientTest.Hubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Security.Principal;
using System.Threading.Tasks;

namespace StatelessClientTest.Game
{
    public class GameManager
    {
        public const int TICK_RATE = 1000 / 128;
        public const int REPORT_RATE = 1000 / 60;
        public static Vector2 PLAY_AREA_SIZE = new Vector2(10, 10);

        private IHubContext<ConnectionHub> Hub;

        private Dictionary<string, string> ActiveConnections;
        private Queue<Entity> NewEntityBuffer;
        public Dictionary<string, Player> Players;
        public GameState State { get; private set; }
        public Stopwatch Timer { get; private set; }
        public Random Rng { get; private set; }

        private readonly object ConnectionAccessLock = new object();

        // always acquire PlayerAccessLock > EntityAccessLock
        private readonly object PlayerAccessLock = new object();
        private readonly object EntityAccessLock = new object();


        public GameManager(IHubContext<ConnectionHub> hub)
        {
            Hub = hub;

            State = new GameState();
            ActiveConnections = new Dictionary<string, string>();
            NewEntityBuffer = new Queue<Entity>();
            Players = new Dictionary<string, Player>();
            Timer = new Stopwatch();
            Rng = new Random();

            Timer.Start();
            _ = SimulationThread();
            _ = ReportingThread();
        }

        public void RegisterUserConnection(string userid, string connectionid)
        {
            lock (ConnectionAccessLock)
            {
                ActiveConnections.TryAdd(connectionid, userid);
            }
        }

        public void UnregisterUserConnection(string connectionid)
        {
            lock (ConnectionAccessLock)
            {
                ActiveConnections.Remove(connectionid);
            }
        }

        public void TryRevivePlayer(string userid)
        {
            lock (PlayerAccessLock) lock (EntityAccessLock)
            {
                if (Players.ContainsKey(userid) && Players[userid].Defeated)
                {
                    Players[userid].Revive(GetSpawnPosition());
                }
            }
        }

        public void TryAddNewPlayer(string userid, string name)
        {
            lock (PlayerAccessLock)
            {
                if (!Players.ContainsKey(userid))
                {
                    var new_player = new Player(this, userid, name, GetSpawnPosition());
                    Players.Add(userid, new_player);
                    QueueNewEntity(new_player);
                }
            }
        }

        public void RemovePlayer(string userid)
        {
            lock (PlayerAccessLock)
            {
                if (Players.ContainsKey(userid))
                {
                    Player player = Players[userid];
                    Players.Remove(userid);

                    lock (EntityAccessLock)
                    {
                        State.Entities.Remove(player);
                    }
                }
            }
        }

        public Vector2 GetSpawnPosition()
        {
            return new Vector2((float)Rng.NextDouble(), (float)Rng.NextDouble()) * new Vector2(PLAY_AREA_SIZE.X - 2, PLAY_AREA_SIZE.Y - 2) + new Vector2(1, 1);
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
                    lock (EntityAccessLock)
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
            // state updates
            lock (EntityAccessLock)
            {
                foreach (Entity entity in State.Entities)
                {
                    entity.Update(timeDelta);
                }
                State.Entities.RemoveAll(p => p.ShouldDestroy());
                while (NewEntityBuffer.Count > 0)
                    State.Entities.Add(NewEntityBuffer.Dequeue());
            }

            // collision checks
            lock (EntityAccessLock)
            {
                foreach (Entity entity1 in State.Entities.Where(e => e.CollisionsEnabled))
                {
                    foreach (Entity entity2 in State.Entities.Where(e => e.CollisionsEnabled).SkipWhile(e => e != entity1).Skip(1))
                    {
                        var sq_dist = entity1.Radius + entity2.Radius;
                        sq_dist *= sq_dist;
                        if (Vector2.DistanceSquared(entity1.Position, entity2.Position) < sq_dist)
                        {
                            Vector2 collision_point;
                            if (entity1.Position == entity2.Position)
                                collision_point = entity1.Position;
                            else
                            {
                                var delta = Vector2.Normalize(entity2.Position - entity1.Position);
                                var point_a = entity1.Position + delta * entity1.Radius;
                                var point_b = entity2.Position - delta * entity2.Radius;
                                collision_point = (point_a + point_b) / 2;
                            }
                            entity1.Collide(entity2, collision_point);
                            entity2.Collide(entity1, collision_point);
                        }
                    }
                }
            }
        }

        public void PlayerControlUpdate(string userid, Dictionary<string, bool> controlState)
        {
            lock (PlayerAccessLock)
            {
                if (Players.ContainsKey(userid))
                {
                    lock (EntityAccessLock)
                    {
                        Players[userid].SetInputs(controlState);
                    }
                }
            }
        }
        public void TryFireProjectile(string userid, Vector2 target)
        {
            lock (PlayerAccessLock)
            {
                if (Players.ContainsKey(userid))
                {
                    lock (EntityAccessLock)
                    {
                        Players[userid].TryFireProjectile(target);
                    }
                }
            }
        }

        public void QueueNewEntity(Entity entity)
        {
            lock (EntityAccessLock)
            {
                NewEntityBuffer.Enqueue(entity);
            }
        }

        public void SendProjectile(Player sender, Vector2 target)
        {
            lock (EntityAccessLock)
            {
                var new_projectile = new Projectile(sender, sender.Position, target - sender.Position);
                QueueNewEntity(new_projectile);
            }
        }

        public class GameState
        {
            public List<Entity> Entities;

            public GameState()
            {
                Entities = new List<Entity>();
            }
        }
    }
}
