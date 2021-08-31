﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using StatelessClientTest.Hubs;
using System.Diagnostics;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StatelessClientTest
{
    public class ReportingWorker : BackgroundService
    {
        public const int ReportRate = 2000 / 1;

        private readonly IHubContext<ConnectionHub> _hubContext;
        private readonly GameStateManager _gameStateManager;

        public ReportingWorker(IHubContext<ConnectionHub> hubContext, GameStateManager gameStateManager)
        {
            _hubContext = hubContext;
            _gameStateManager = gameStateManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopWatch = Stopwatch.StartNew();

            while (true)
            {
                foreach (string connectionid in _gameStateManager.ActiveConnections.Keys)
                {
                    await _hubContext.Clients.Client(connectionid).SendAsync("GameStateReport", stopWatch.ElapsedTicks, JsonSerializer.Serialize(_gameStateManager.GameState.Players.Select(kvp => new { Player = kvp.Key, ControlState = kvp.Value.ControlState, Position = kvp.Value.Position })));
                }

                await Task.Delay(ReportRate);
            }
        }
    }
}
