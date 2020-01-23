//using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace C_Sharp_MIDI_Player
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NoteCol
    {
        public uint rgba;
        public uint rgba2;

        public static uint Compress(byte r, byte g, byte b, byte a)
        {
            return (uint)((r << 24) & 0xff000000) |
                       (uint)((g << 16) & 0xff0000) |
                       (uint)((b << 8) & 0xff00) |
                       (uint)(a & 0xff);
        }

        /*
        public static uint Blend(uint from, uint with)
        {
            Vector4 fromv = new Vector4((float)(from >> 24 & 0xff) / 255.0f, (float)(from >> 16 & 0xff) / 255.0f, (float)(from >> 8 & 0xff) / 255.0f, (float)(from & 0xff) / 255.0f);
            Vector4 withv = new Vector4((float)(with >> 24 & 0xff) / 255.0f, (float)(with >> 16 & 0xff) / 255.0f, (float)(with >> 8 & 0xff) / 255.0f, (float)(with & 0xff) / 255.0f);

            float blend = withv.W;
            float revBlend = (1 - withv.W) * fromv.W;

            return Compress(
                    (byte)((fromv.X * revBlend + withv.X * blend) * 255),
                    (byte)((fromv.Y * revBlend + withv.Y * blend) * 255),
                    (byte)((fromv.Z * revBlend + withv.Z * blend) * 255),
                    (byte)((blend + revBlend) * 255)
                );
        }
        */
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Note
    {
        public double start, end;
        public int colorPointer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIDIEvent
    {
        public float time;
        public uint data;
        public byte vel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ColorEvent
    {
        public double time;
        public NoteCol color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TempoEvent
    {
        public long time;
        public int tempo;
    }

    public enum ParsingStage
    {
        Opening,
        FirstPass,
        SecondPass,
        MergingKeys,
        RemovingOverlaps,
        MergingEvents,
    }

    public class MIDIFile
    {
        protected List<long> trackBeginnings = new List<long>();
        protected List<uint> trackLengths = new List<uint>();

        protected TrackParser[] parsers;
        protected TempoEvent[] globalTempos;

        public event Action ParseFinished;
        public event Action ParseCancelled;

        //Persistent values
        public NoteCol[] OriginalMidiNoteColors { get; protected set; } = null;
        public NoteCol[] MidiNoteColors { get; protected set; } = null;
        public int FirstKey { get; protected set; } = 255;
        public int LastKey { get; protected set; } = 0;
        public double MidiLength { get; protected set; } = 0;
        public long MidiNoteCount { get; protected set; } = 0;

        public ParsingStage ParseStage { get; protected set; } = ParsingStage.Opening;
        public double ParseNumber { get; protected set; }
        public string ParseStatusText { get; protected set; }

        public ushort division { get; private set; }
        public int trackcount { get; private set; }
        public ushort format { get; private set; }

        protected Stream MidiFileReader;
        protected MIDILoaderSettings loaderSettings;

        public string filepath;
        protected CancellationToken cancel;

        public MIDIEvent[][] MIDINoteEvents { get; private set; } = null;
        public MIDIEvent[] MIDIControlEvents { get; private set; } = null;
        public Note[][] Notes { get; private set; } = new Note[256][];
        public int[] FirstRenderNote { get; private set; } = new int[256];
        public int[] FirstUnhitNote { get; private set; } = new int[256];
        public double lastRenderTime { get; set; } = 0;
        public int[] LastColorEvent;
        public ColorEvent[][] ColorEvents;

        public MIDIFile(string path, MIDILoaderSettings settings, CancellationToken cancel)
        {
            loaderSettings = settings;
            this.cancel = cancel;
            filepath = path;
        }

        void AssertText(string text)
        {
            foreach (char c in text)
            {
                if (MidiFileReader.ReadByte() != c)
                {
                    throw new Exception("Corrupt chunk headers");
                }
            }
        }

        uint ReadInt32()
        {
            uint length = 0;
            for (int i = 0; i != 4; i++)
                length = (uint)((length << 8) | (byte)MidiFileReader.ReadByte());
            return length;
        }

        ushort ReadInt16()
        {
            ushort length = 0;
            for (int i = 0; i != 2; i++)
                length = (ushort)((length << 8) | (byte)MidiFileReader.ReadByte());
            return length;
        }

        void ParseHeaderChunk()
        {
            AssertText("MThd");
            uint length = ReadInt32();
            if (length != 6) throw new Exception("Header chunk size isn't 6");
            format = ReadInt16();
            ReadInt16();
            division = ReadInt16();
            if (format == 2) throw new Exception("Midi type 2 not supported");
        }

        void ParseTrackChunk()
        {
            AssertText("MTrk");
            uint length = ReadInt32();
            trackBeginnings.Add(MidiFileReader.Position);
            trackLengths.Add(length);
            if (MidiFileReader.Position + length > MidiFileReader.Length)
                MidiFileReader.Position = MidiFileReader.Length;
            else
                MidiFileReader.Position += length;
            trackcount++;
            ParseStatusText = "Checking MIDI\nFound " + trackcount + " tracks";
            Console.WriteLine("Track " + trackcount + ", Size " + length);
        }

        public void Parse()
        {
            try
            {
                Open();
                FirstPassParse();
                foreach (var p in parsers)
                {
                    p.globaTempos = globalTempos;
                    p.PrepareForSecondPass();
                }
                SecondPassParse();
                MidiLength = parsers.Select(p => p.trackSeconds).Max();
                foreach (var p in parsers) p.Dispose();
                parsers = null;
                globalTempos = null;
                trackBeginnings = null;
                trackLengths = null;
                MidiFileReader.Dispose();
                MidiFileReader = null;
                GC.Collect();
                cancel.ThrowIfCancellationRequested();
                LastColorEvent = new int[trackcount * 16];
                SetColors();
                ParseFinishedInvoke();
            }
            catch (OperationCanceledException)
            {
                MidiFileReader.Close();
                MidiFileReader.Dispose();
                ParseCancelledInvoke();
            }
        }

        void SecondPassParse()
        {
            object l = new object();
            int tracksParsed = 0;
            ParseStage = ParsingStage.SecondPass;
            Parallel.For(0, parsers.Length, (i) =>
            {
                parsers[i].SecondPassParse();
                lock (l)
                {
                    tracksParsed++;
                    ParseNumber += 20;
                    ParseStatusText = "Loading MIDI\nTracks " + tracksParsed + " of " + parsers.Length;
                    Console.WriteLine("Pass 2 Parsed track " + tracksParsed + "/" + parsers.Length);
                }
            });
            cancel.ThrowIfCancellationRequested();
            int keysMerged = 0;
            var eventMerger = Task.Run(() =>
            {
                int count = loaderSettings.EventPlayerThreads;
                MIDINoteEvents = new MIDIEvent[count][];
                Parallel.For(0, count, new ParallelOptions() { CancellationToken = cancel }, i =>
                {
                    try
                    {
                        MIDINoteEvents[i] = TimedMerger<MIDIEvent>.MergeMany(parsers.Select(p => new SkipIterator<MIDIEvent>(p.NoteEvents, i, count)).ToArray(), e =>
                        {
                            return e.time;
                        }).ToArray();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            });
            var controlEventMerger = Task.Run(() =>
            {
                int count = loaderSettings.EventPlayerThreads;
                MIDIControlEvents = TimedMerger<MIDIEvent>.MergeMany(parsers.Select(p => p.ControlEvents).ToArray(), e => e.time).ToArray();
            });
            cancel.ThrowIfCancellationRequested();
            ParseStage = ParsingStage.MergingKeys;
            long noteCount = 0;
            Parallel.For(0, 256, new ParallelOptions() { CancellationToken = cancel }, (i) =>
            {
                var en = TimedMerger<Note>.MergeMany(parsers.Select(p => p.Notes[i]).ToArray(), n => n.start);
                if (loaderSettings.RemoveOverlaps) Notes[i] = RemoveOverlaps(en).ToArray();
                else Notes[i] = en.ToArray();
                foreach (var p in parsers) p.Notes[i] = null;
                lock (l)
                {
                    noteCount += Notes[i].Length;
                    keysMerged++;
                    ParseNumber += 10;
                    ParseStatusText = "Merging Notes\nMerged keys " + keysMerged + " of " + 256;
                    Console.WriteLine("Merged key " + keysMerged + "/" + 256);
                }
            });
            MidiNoteCount = noteCount;
            List<ColorEvent[]> ce = new List<ColorEvent[]>();
            foreach (var p in parsers) ce.AddRange(p.ColorEvents);
            ColorEvents = ce.ToArray();
            cancel.ThrowIfCancellationRequested();
            ParseStatusText = "Merging Events...";
            ParseStage = ParsingStage.MergingEvents;
            Console.WriteLine("Merging events...");
            controlEventMerger.GetAwaiter().GetResult();
            eventMerger.GetAwaiter().GetResult();
            ParseStatusText = "Done!";
        }

        IEnumerable<Note> RemoveOverlaps(IEnumerable<Note> input)
        {
            List<Note> tickNotes = new List<Note>();
            double currTick = -1;
            double epsilon = 0.00001;
            foreach (var n in input)
            {
                if (n.start > currTick)
                {
                    foreach (var _n in tickNotes) yield return _n;
                    tickNotes.Clear();
                    currTick = n.start + epsilon;
                    tickNotes.Add(n);
                }
                else
                {
                    var count = tickNotes.Count;
                    var end = n.end + epsilon;
                    if (count != 0 && tickNotes[count - 1].end <= end)
                    {
                        int i = count - 1;
                        for (; i >= 0; i--)
                        {
                            if (tickNotes[i].end > end) break;
                        }
                        i++;
                        if (i == 0)
                            tickNotes.Clear();
                        else if (i != count)
                            tickNotes.RemoveRange(i, count - i);
                        tickNotes.Add(n);
                    }
                    else
                    {
                        tickNotes.Add(n);
                    }
                }
            }
            foreach (var _n in tickNotes) yield return _n;
        }

        public void SetColorEvents(double time)
        {
            Parallel.For(0, MidiNoteColors.Length, i =>
            {
                MidiNoteColors[i] = OriginalMidiNoteColors[i];
                var ce = ColorEvents[i];
                var last = LastColorEvent[i];
                if (ce.Length == 0) return;
                if (ce.First().time > time)
                {
                    LastColorEvent[i] = 0;
                    return;
                }
                if (ce.Last().time <= time)
                {
                    MidiNoteColors[i] = ce.Last().color;
                    return;
                }
                if (ce[last].time < time)
                {
                    for (int j = last; j < ce.Length; j++)
                        if (ce[j + 1].time > time)
                        {
                            LastColorEvent[i] = j;
                            MidiNoteColors[i] = ce[j].color;
                            return;
                        }
                }
                else
                {
                    for (int j = last; j >= 0; j--)
                        if (ce[j].time <= time)
                        {
                            LastColorEvent[i] = j;
                            MidiNoteColors[i] = ce[j].color;
                            return;
                        }
                }
            });
        }

        protected void Open()
        {
            MidiFileReader = File.Open(filepath, FileMode.Open);
            ParseHeaderChunk();
            while (MidiFileReader.Position < MidiFileReader.Length)
            {
                try
                {
                    ParseTrackChunk();
                }
                catch
                {
                    if (trackBeginnings.Count == 0) throw new Exception("Corrupt first track chunk header");
                    else
                    {
                        ParseStatusText = "Chunk " + (trackcount - 1) + " has corrupt length\nCalculating length...";
                        var pos = trackBeginnings.Last();
                        var reader = new BufferByteReader(MidiFileReader, 10000, pos, MidiFileReader.Length - pos);
                        try
                        {
                            var len = TrackParser.GetCorruptChunkLength(reader);
                            trackLengths[trackLengths.Count - 1] = (uint)len;
                            MidiFileReader.Position = pos + len;
                        }
                        catch { throw new Exception("Could not determine length of corrupt chunk"); }
                    }
                }
                ParseNumber += 2;
                cancel.ThrowIfCancellationRequested();
            }
            parsers = new TrackParser[trackcount];
        }

        protected void FirstPassParse()
        {
            object l = new object();
            int tracksParsed = 0;
            ParseStage = ParsingStage.FirstPass;
            Parallel.For(0, parsers.Length, new ParallelOptions() { CancellationToken = cancel }, (i) =>
            {
                var reader = new BufferByteReader(MidiFileReader, 100000, trackBeginnings[i], trackLengths[i]);
                parsers[i] = new TrackParser(reader, division, i, loaderSettings);
                parsers[i].FirstPassParse();
                lock (l)
                {
                    tracksParsed++;
                    ParseNumber += 20;
                    ParseStatusText = "Analyzing MIDI\nTracks " + tracksParsed + " of " + parsers.Length;
                    Console.WriteLine("Pass 1 Parsed track " + tracksParsed + "/" + parsers.Length);

                    if (FirstKey > parsers[i].FirstKey) FirstKey = parsers[i].FirstKey;
                    if (LastKey < parsers[i].LastKey) LastKey = parsers[i].LastKey;
                }
            });
            cancel.ThrowIfCancellationRequested();
            var temposMerge = TimedMerger<TempoEvent>.MergeMany(parsers.Select(p => p.Tempos).ToArray(), t => t.time);
            globalTempos = temposMerge.Cast<TempoEvent>().ToArray();
            MidiNoteCount = parsers.Select(p => p.noteCount).Sum();
        }

        protected void SetColors()
        {
            MidiNoteColors = new NoteCol[trackcount * 16];
            OriginalMidiNoteColors = new NoteCol[trackcount * 16];
            for (int i = 0; i < OriginalMidiNoteColors.Length; i++)
            {
                int r, g, b;
                HsvToRgb((i * 40) % 360, 1, 1, out r, out g, out b);
                OriginalMidiNoteColors[i] = new NoteCol()
                {
                    rgba = NoteCol.Compress((byte)r, (byte)g, (byte)b, 255),
                    rgba2 = NoteCol.Compress((byte)r, (byte)g, (byte)b, 255),
                };
            }
        }

        public void SetColors(Bitmap img, bool randomise)
        {
            Random r = new Random();
            double[] order = new double[trackcount * 16];
            int[] coords = new int[trackcount * 16];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = r.NextDouble();
                coords[i] = i;
            }
            if (randomise)
            {
                Array.Sort(order, coords);
            }
            for (int i = 0; i < trackcount; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    int y = coords[i * 16 + j];
                    int x = y % 16;
                    y = y - x;
                    y /= 16;
                    System.Drawing.Color col;
                    if (img.Width == 16)
                    {
                        col = img.GetPixel(x, y % img.Height);
                        OriginalMidiNoteColors[i * 16 + j].rgba = NoteCol.Compress(col.R, col.G, col.B, col.A);
                        OriginalMidiNoteColors[i * 16 + j].rgba2 = NoteCol.Compress(col.R, col.G, col.B, col.A);
                    }
                    else
                    {
                        col = img.GetPixel(x * 2, y % img.Height);
                        OriginalMidiNoteColors[i * 16 + j].rgba = NoteCol.Compress(col.R, col.G, col.B, col.A);
                        col = img.GetPixel(x * 2 + 1, y % img.Height);
                        OriginalMidiNoteColors[i * 16 + j].rgba2 = NoteCol.Compress(col.R, col.G, col.B, col.A);
                    }
                }
            }
        }

        protected void ParseCancelledInvoke()
        {
            ParseCancelled?.Invoke();
        }

        protected void ParseFinishedInvoke()
        {
            ParseFinished?.Invoke();
        }

        void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}