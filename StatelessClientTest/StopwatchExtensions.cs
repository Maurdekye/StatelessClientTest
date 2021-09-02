using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace StatelessClientTest
{
    public static class StopwatchExtensions
    {
        public static float ElapsedSeconds(this Stopwatch stopwatch) => stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
    }
}
