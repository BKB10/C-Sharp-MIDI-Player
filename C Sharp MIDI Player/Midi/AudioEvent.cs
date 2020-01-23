using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Sharp_MIDI_Player
{
    class AudioEvent
    {
        public double Time { get; set; }
        public MIDIEvent Event { get; set; }

        public AudioEvent(MIDIEvent inputEvent, double inputTime) {
            Event = inputEvent;
            Time = inputTime;
        }
    }
}
