using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using C_Sharp_MIDI_Player.Midi;

namespace C_Sharp_MIDI_Player
{
    class MidiLoader
    {
        public MIDIFile File { get; private set; }

        public MidiLoader(String filePath)
        {
            CancellationToken cancel = new CancellationToken(false);

            Console.WriteLine("Loading...");

            File = new MIDIFile(filePath, new MIDILoaderSettings(), cancel);
            File.Parse();

            Console.WriteLine("Done loading.");
        }
    }
}
