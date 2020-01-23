using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Sharp_MIDI_Player.Midi
{
    class TrackMerger
    {
        private double endTick;
        private double startTick;
        private List<List<MIDIEvent>> tracks;
        private List<MIDIEvent> sortedEvents;

        private int[] lastIndex;

        private double progress;

        private bool done;

        public TrackMerger(List<List<MIDIEvent>> tracks)
        {
            this.tracks = tracks;

            done = false;

            progress = 0;

            lastIndex = new int[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
            {
                lastIndex[i] = 0;
            }
        }

        public List<MIDIEvent> Merge()
        {
            sortedEvents = new List<MIDIEvent>();

            double nextTick = -1;

            double i = 0;
            while(!done)
            {
                progress = (double)i / endTick * 100;

                nextTick = MergeTick(i);

                if (i == nextTick) {
                    done = true;

                    break;
                }
                
                i = nextTick;
            }

            progress = 100;

            //done = true;

            return sortedEvents;
        }

        private double MergeTick(double time)
        {
            double nextTick = uint.MaxValue;

            //look for the event that has the least tick that is more than the current tick and set nextTick
            done = true;
            int trackNumber = 0;
            foreach (List<MIDIEvent> track in tracks) //ii is track
            {
                int eventIndex = 0;
                foreach (MIDIEvent e in track)
                {
                    //MIDIEvent event = track[ii];

                    if (e.time == time)
                    {
                        //e.setTrack(i);
                        sortedEvents.Add(e);

                        done = false;
                    }
                    else if (e.time > time)
                    {
                        if (e.time < nextTick)
                        {
                            nextTick = e.time;
                        }

                        //lastIndex.Remove(trackNumber);
                        lastIndex.SetValue(eventIndex, trackNumber);

                        done = false;

                        break;
                    }

                    eventIndex++;
                }

                trackNumber++;
            }

            return nextTick;
        }

        public void Dispose()
        {
            tracks = null;
            sortedEvents = null;
            lastIndex = null;
        }
    }
}