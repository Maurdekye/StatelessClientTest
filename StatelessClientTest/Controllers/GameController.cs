using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StatelessClientTest.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StatelessClientTest.Controllers
{
    public class GameController : Controller
    {

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }
    }
}
