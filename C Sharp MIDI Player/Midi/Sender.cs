using System;
using System.Threading;

namespace C_Sharp_MIDI_Player
{
    class Sender
    {
        public Boolean Playing { get; private set; }

        private Timer timer;

        private double totalTime;

        public Sender(Timer timer) {
            this.timer = timer;

            totalTime = 0;

            Playing = true;
        }

        public void Send(MIDIEvent[] events) {
            foreach (MIDIEvent e in events)
            {
                var offset = e.time * 1000 - timer.getStopWatch().ElapsedMilliseconds;
                if (offset > 0)
                {
                    Thread.Sleep((int) offset);
                }

                KDMAPI.SendDirectData(e.data);
            }
        }
    }
}
