using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Sharp_MIDI_Player
{
    class Timer
    {
        private Stopwatch stopWatch;

        public Timer() {
            stopWatch = new Stopwatch();
        }

        public void Start() {
            stopWatch.Start();
        }

        public void Stop() {
            stopWatch.Stop();
        }

        public Stopwatch getStopWatch() {
            return stopWatch;
        }
    }
}
