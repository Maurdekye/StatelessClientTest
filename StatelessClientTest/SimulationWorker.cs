using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace StatelessClientTest
{
    public class SimulationWorker : BackgroundService
    {
        public const int TickRate = 1000 / 64;
        private readonly GameStateManager _gameStateManager;

        public SimulationWorker(GameStateManager gameStateManager)
        {
            _gameStateManager = gameStateManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopWatch = Stopwatch.StartNew();
            var lastTick = stopWatch.ElapsedTicks;
            while (true)
            {
                var current_tick = stopWatch.ElapsedTicks;
                var time_delta = (current_tick - lastTick) / Stopwatch.Frequency;
                SimulationStep(time_delta);
                await Task.Delay(TickRate);
            }
        }

        private void SimulationStep(float timeDelta)
        {
            foreach (GamePlayer player in _gameStateManager.GameState.Players.Values)
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
    }
}
