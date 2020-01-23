using System;
using System.Threading;
using System.Threading.Tasks;

namespace C_Sharp_MIDI_Player
{

    class CSharpMidiPlayer
    {
        [STAThread]
        public static void Main() {
            new MainWindow().ShowDialog();

            MidiLoader loader = new MidiLoader("F:/Music/Black MIDIs/Evans LOLEX MODE II 4.0.mid");

            KDMAPI.InitializeKDMAPIStream();

            Console.WriteLine("Playing...");
            Timer timer = new Timer();
            for (uint i = 0; i < loader.File.MIDINoteEvents.Length; i++)
            {
                StartThread(loader.File.MIDINoteEvents[i], timer);
            }

            timer.Start();

            Thread.Sleep(-1);

            KDMAPI.TerminateKDMAPIStream();
        }

        private static void StartThread(MIDIEvent[] events, Timer timer) {
            Task.Run(() =>
            {
                Sender sender = new Sender(timer);
                sender.Send(events);
            });
        }
    }
}
