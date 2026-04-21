// Pedal Chord – ReBuzz managed generator (control) machine
// A chord and arpeggio trigger that fires notes on other generator machines.
//
// This machine has NO audio I/O – it is a pure controller, like PeerCtrl.
// Add it to the machine view, right-click → Target Settings to aim each
// voice track at a generator, then write root notes into the pattern editor.
//
// Build:   dotnet build PedalChord.csproj -c Release
// Output:  C:\Program Files\ReBuzz\Gear\Generators\Pedal Chord.NET.dll

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Buzz.MachineInterface;
using BuzzGUI.Interfaces;

namespace WDE.PedalChord
{
    // =========================================================================
    // Chord library
    // =========================================================================

    static class ChordLib
    {
        /// <summary>Semitone offsets from the root note for each chord type.</summary>
        public static readonly int[][] Intervals =
        {
            // ── Triads ────────────────────────────────────────────────────
            new[] { 0, 4, 7 },                    //  0  Major
            new[] { 0, 3, 7 },                    //  1  Minor
            // ── Seventh chords ────────────────────────────────────────────
            new[] { 0, 4, 7, 10 },                //  2  Dom 7
            new[] { 0, 4, 7, 11 },                //  3  Maj 7
            new[] { 0, 3, 7, 10 },                //  4  Min 7
            // ── Suspended ─────────────────────────────────────────────────
            new[] { 0, 2, 7 },                    //  5  Sus 2
            new[] { 0, 5, 7 },                    //  6  Sus 4
            // ── Altered triads ────────────────────────────────────────────
            new[] { 0, 4, 8 },                    //  7  Aug
            new[] { 0, 3, 6 },                    //  8  Dim
            new[] { 0, 3, 6, 9 },                 //  9  Dim 7
            // ── Sixth chords ──────────────────────────────────────────────
            new[] { 0, 4, 7, 9 },                 // 10  Maj 6
            new[] { 0, 3, 7, 9 },                 // 11  Min 6
            // ── Ninth (add/extended) ──────────────────────────────────────
            new[] { 0, 4, 7, 14 },                // 12  Add 9
            new[] { 0, 4, 7, 11, 14 },            // 13  Maj 9
            new[] { 0, 3, 7, 10, 14 },            // 14  Min 9
            // ── Bare fifth ────────────────────────────────────────────────
            new[] { 0, 7 },                       // 15  Power
            // == NEW from 16 onwards — indices 0-15 preserved for compatibility ==
            // ── More seventh chords ───────────────────────────────────────
            new[] { 0, 3, 6, 10 },                // 16  Half Dim 7  (m7b5)
            new[] { 0, 3, 7, 11 },                // 17  Min Maj 7
            new[] { 0, 4, 8, 11 },                // 18  Aug Maj 7
            new[] { 0, 4, 8, 10 },                // 19  Aug 7       (7#5)
            new[] { 0, 3, 6, 11 },                // 20  Dim Maj 7
            new[] { 0, 5, 7, 10 },                // 21  7 Sus4
            new[] { 0, 2, 7, 10 },                // 22  7 Sus2
            // ── Ninth chords ──────────────────────────────────────────────
            new[] { 0, 4, 7, 10, 14 },            // 23  Dom 9
            new[] { 0, 3, 7, 14 },                // 24  Min Add 9
            new[] { 0, 4, 7, 9, 14 },             // 25  6/9
            new[] { 0, 3, 7, 9, 14 },             // 26  Min 6/9
            new[] { 0, 4, 7, 10, 13 },            // 27  7 b9
            new[] { 0, 4, 7, 10, 15 },            // 28  7 #9
            new[] { 0, 5, 7, 10, 14 },            // 29  9 Sus4
            // ── Altered / substitution ────────────────────────────────────
            new[] { 0, 4, 6, 10 },                // 30  7 b5
            new[] { 0, 4, 6, 10, 13 },            // 31  7 b5 b9
            new[] { 0, 4, 6, 10, 15 },            // 32  7 b5 #9
            new[] { 0, 4, 6 },                    // 33  b5 Triad
            // ── Eleventh chords ───────────────────────────────────────────
            new[] { 0, 4, 7, 10, 17 },            // 34  Dom 11
            new[] { 0, 4, 7, 11, 17 },            // 35  Maj 11
            new[] { 0, 3, 7, 10, 17 },            // 36  Min 11
            new[] { 0, 4, 7, 10, 18 },            // 37  7 #11       (Lydian Dom)
            new[] { 0, 4, 7, 11, 18 },            // 38  Maj 7 #11   (Lydian)
            new[] { 0, 4, 7, 17 },                // 39  Add 11
            // ── Thirteenth chords ─────────────────────────────────────────
            new[] { 0, 4, 7, 10, 14, 21 },        // 40  Dom 13
            new[] { 0, 4, 7, 11, 14, 21 },        // 41  Maj 13
            new[] { 0, 3, 7, 10, 14, 21 },        // 42  Min 13
            new[] { 0, 4, 7, 10, 14, 17, 21 },    // 43  Dom 13 Full
            // ── Shell / open voicings ─────────────────────────────────────
            new[] { 0, 4, 10 },                   // 44  Dom Shell    (1 3 b7)
            new[] { 0, 4, 11 },                   // 45  Maj 7 Shell  (1 3 7)
            new[] { 0, 3, 10 },                   // 46  Min 7 Shell  (1 b3 b7)
            // ── Exotic / modal ────────────────────────────────────────────
            new[] { 0, 5, 10, 15 },               // 47  Quartal
            new[] { 0, 7, 14, 21 },               // 48  Quintal
            new[] { 0, 2, 4, 6, 8, 10 },          // 49  Whole Tone
            new[] { 0, 1, 2 },                    // 50  Cluster
        };
    }

    // =========================================================================
    // Buzz note encoding helpers
    // =========================================================================

    static class BN
    {
        public const int NoValue = 0;
        public const int Off     = 0xFF;  // 255

        // Buzz note value -> zero-based MIDI note number (C-0 = 0)
        // Format: high nibble = octave (0-9), low nibble = semitone (1=C ... 12=B)
        public static int ToMidi(int b)
        {
            if (b <= 0 || b == Off) return -1;
            int oct  = (b >> 4) & 0xF;
            int semi = (b & 0xF) - 1;
            return oct * 12 + semi;
        }

        // Zero-based MIDI note -> Buzz note value (clamped to C-0 ... B-9)
        public static int FromMidi(int m)
        {
            if (m < 0)   return Off;
            if (m > 119) m = 119;
            return ((m / 12) << 4) | (m % 12 + 1);
        }
    }

    // =========================================================================
    // Per-voice runtime state  (NOT serialised)
    // =========================================================================

    class VoiceState
    {
        // Persistent parameter values (survive empty rows in the pattern)
        public int ChordType    = 0;
        public int Mode         = 0;    // 0=Chord  1=Arp-Up  2=Arp-Down  3=Arp-UpDown  4=Arp-DownUp  5=Random
        public int Speed        = 2;
        public int Length       = 4;
        public int OctaveSpread = 1;
        public int Step         = 1;   // chord tones advanced per arp tick (1-8)
        public int OctWalk      = 0;   // 0=Off 1=Up 2=Ping-pong
        public int OctOffset    = 0;   // runtime: current octave offset
        public int OctDir       = 1;   // runtime: oct walk direction (+1/-1)
        public int Swing        = 0;
        public int SwingPhaseVal = 0;  // set by SetSwingPhase parameter
        public int Velocity      = 100; // 1-127 MIDI velocity
        public int Humanize      = 0;   // 0-100 timing randomisation
        public int HumanizeVel   = 0;   // 0-100 velocity randomisation

        // Note trigger: set by SetNote, consumed by Work()
        public int  PendingNote  = 0;
        public bool HasNewNote   = false;
        public bool PendingReset = false; // set by Arp Reset param; consumed in Work()

        // Playback
        public bool  Active   = false;
        public int[] Notes    = new int[0]; // expanded MIDI note numbers
        public int   ArpIdx   = 0;
        public int   ArpDir   = 1;          // +1 or -1 (ping-pong direction)
        public int ArpTicks       = 0;   // countdown to next arp step
        public int ArpStepParity  = 0;   // kept in range [0,1] — even=long, odd=short

        // Active note slots (chord uses up to 5; arpeggio uses slot 0 only)
        public int[] SlotOff   = new int[16]; // ticks until note-off (0 = idle)
        public int[] SlotTrack = new int[16]; // target track index for each slot
    }

    // =========================================================================
    // Serialisable machine state
    // =========================================================================

    [Serializable]
    public class TrackTarget
    {
        public string MachineName { get; set; } = "";
        public int    BaseTrack   { get; set; } = 0;
    }

    [Serializable]
    public class PedalChordState
    {
        // Null by default: XmlSerializer then replaces rather than appends on reload
        public List<TrackTarget> Targets { get; set; } = null;

        public TrackTarget Get(int idx)
        {
            if (Targets == null) Targets = new List<TrackTarget>();
            while (Targets.Count <= idx) Targets.Add(new TrackTarget());
            return Targets[idx];
        }
    }

    // =========================================================================
    // Machine declaration
    //
    // No InputCount / OutputCount -> pure control machine, like PeerCtrl.
    // Appears in the Generators list; has no audio wires.
    // Notes are fired programmatically via IParameter.SetValue on target machines.
    // =========================================================================

    [MachineDecl(
        Name      = "Pedal Chord",
        ShortName = "PdlChrd",
        Author    = "WDE",
        MaxTracks = 16)]
    public class PedalChordMachine : IBuzzMachine
    {
        const int MaxVoices = 16;
        const int MaxSlots  = 16;  // max simultaneous notes (Dom13 Full = 7; 16 covers 2 octaves of any chord)

        IBuzzMachineHost     host;
        PedalChordState      _state = new PedalChordState();
        VoiceState[]         _vs    = new VoiceState[MaxVoices];
        int                  _prevPit      = int.MaxValue; // tick-boundary detection (like v1.0)
        Random               _rng   = new Random();
        TargetSettingsWindow _settingsWin = null;
        IParameter           _ownNoteParam = null;  // our own Note param, for multi-track workaround

        // ── Constructor ───────────────────────────────────────────────────────
        public PedalChordMachine(IBuzzMachineHost host)
        {
            this.host = host;
            for (int i = 0; i < MaxVoices; i++)
                _vs[i] = new VoiceState();
        }

        // Cache of the pvalues Dictionary<int,int> from our own Note ParameterCore.
        // Populated lazily on first SetNote call (after ParameterGroups exist).
        // Dictionary.TryGetValue is allocation-free — safe on the audio thread.
        System.Collections.Concurrent.ConcurrentDictionary<int,int> _ownNotePValues = null;

        void TryInitPValues()
        {
            try
            {
                if (_ownNoteParam == null)
                {
                    var pg = host?.Machine?.ParameterGroups;
                    if (pg == null || pg.Count < 3) return;
                    _ownNoteParam = pg[2].Parameters.FirstOrDefault(
                        p => p?.Type == ParameterType.Note);
                }
                if (_ownNoteParam == null || _ownNotePValues != null) return;
                var fi = _ownNoteParam.GetType().GetField("pvalues",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fi != null)
                    _ownNotePValues = fi.GetValue(_ownNoteParam)
                        as System.Collections.Concurrent.ConcurrentDictionary<int,int>;
            }
            catch { }
        }

        // ── IBuzzMachine host property ────────────────────────────────────────
        public IBuzzMachineHost Host
        {
            get => host;
            set => host = value;
        }

        IBuzz Buzz => host?.Machine?.Graph?.Buzz;

        // =====================================================================
        // Track parameters
        // =====================================================================

        // ── Note ─────────────────────────────────────────────────────────────
        // ── Note parameter ───────────────────────────────────────────────────
        [ParameterDecl(
            Name        = "Note",
            IsStateless = true,
            Description = "Root note. Piano keyboard input: z=C-4, s=C#-4 … 255 = note-off.")]
        public void SetNote(Note value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].PendingNote = value.Value;  // Note.Value is byte
            _vs[track].HasNewNote  = true;

            // ReBuzz workaround: machine.parametersChanged is Dictionary<IParameter,int>
            // so simultaneous notes on multiple tracks overwrite each other — only the
            // last track's SetNote is called. Poll other tracks' pvalues here while
            // they are still valid (before the post-Tick pvalue reset).
            // Poll other tracks' fresh note values to work around the ReBuzz
            // parametersChanged dict only storing one track per IParameter.
            if (_ownNotePValues == null) TryInitPValues();
            if (_ownNotePValues != null)
            {
                int noVal = _ownNoteParam.NoValue;
                for (int t = 0; t < MaxVoices; t++)
                {
                    if (t == track) continue;
                    int pv;
                    if (_ownNotePValues.TryGetValue(t, out pv) && pv != noVal)
                    {
                        _vs[t].PendingNote = (byte)pv;
                        _vs[t].HasNewNote  = true;
                    }
                }
            }
        }

        [ParameterDecl(Name = "Velocity", MinValue = 1, MaxValue = 127, DefValue = 100,
                       Description = "Note velocity sent to target (1-127)")]
        public void SetVelocity(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Velocity = Math.Max(1, Math.Min(127, value));
        }

        [ParameterDecl(Name = "Humanize", MinValue = 0, MaxValue = 100, DefValue = 0,
                       Description = "Random ±timing drift per arp step (scales with Speed)")]
        public void SetHumanize(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Humanize = Math.Max(0, Math.Min(100, value));
        }

        [ParameterDecl(Name = "Hum. Vel", MinValue = 0, MaxValue = 100, DefValue = 0,
                       Description = "Random ±velocity variation per arp step (scales with Velocity)")]
        public void SetHumanizeVel(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].HumanizeVel = Math.Max(0, Math.Min(100, value));
        }

        [ParameterDecl(Name = "Arp Reset", MinValue = 0, MaxValue = 1, DefValue = 0,
                       IsStateless = true,
                       Description = "1 = restart arp from first note on this step")]
        public void SetArpReset(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            if (value != 0) _vs[track].PendingReset = true;
        }

        [ParameterDecl(
            Name              = "Chord",
            DefValue          = 0,
            ValueDescriptions = new[]
            {
                // Triads
                "Major",     "Minor",
                // 7th chords
                "Dom 7",     "Maj 7",     "Min 7",
                // Suspended
                "Sus 2",     "Sus 4",
                // Altered triads
                "Aug",       "Dim",       "Dim 7",
                // 6th chords
                "Maj 6",     "Min 6",
                // 9th (add/extended)
                "Add 9",     "Maj 9",     "Min 9",
                // Bare 5th
                "Power",
                // More 7th chords
                "Half Dim 7","Min Maj 7", "Aug Maj 7","Aug 7",    "Dim Maj 7",
                "7 Sus4",    "7 Sus2",
                // 9th chords
                "Dom 9",     "Min Add 9", "6/9",      "Min 6/9",
                "7 b9",      "7 #9",      "9 Sus4",
                // Altered / substitution
                "7 b5",      "7 b5 b9",  "7 b5 #9",  "b5 Triad",
                // 11th chords
                "Dom 11",    "Maj 11",    "Min 11",
                "7 #11",     "Maj7 #11",  "Add 11",
                // 13th chords
                "Dom 13",    "Maj 13",    "Min 13",   "Dom 13 Full",
                // Shell voicings
                "Dom Shell", "Maj7 Shell","Min7 Shell",
                // Exotic / modal
                "Quartal",   "Quintal",   "Whole Tone","Cluster",
            },
            Description = "Chord type")]
        public void SetChord(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].ChordType = Math.Max(0, Math.Min(ChordLib.Intervals.Length - 1, value));
        }

        [ParameterDecl(
            Name              = "Mode",
            DefValue          = 0,
            ValueDescriptions = new[]
            {
                "Chord", "Arp Up", "Arp Down", "Arp Up+Down", "Arp Down+Up", "Arp Random"
            },
            Description = "Chord or arpeggio mode")]
        public void SetMode(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Mode = Math.Max(0, Math.Min(5, value));
        }

        [ParameterDecl(
            Name        = "Speed",
            MinValue    = 1,
            MaxValue    = 1024,
            DefValue    = 2,
            Description = "Arpeggio: ticks between successive notes (1-1024)")]
        public void SetSpeed(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Speed = Math.Max(1, value);
        }

        [ParameterDecl(
            Name        = "Length",
            MinValue    = 0,
            MaxValue    = 16384,
            DefValue    = 4,
            Description = "Note duration in ticks (0 = no auto note-off, let target decide)")]
        public void SetLength(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Length = Math.Max(0, value);
        }

        [ParameterDecl(
            Name        = "Octaves",
            MaxValue    = 4,
            DefValue    = 1,
            Description = "Octave spread: arp note list duplicated over 1-4 octaves")]
        public void SetOctaves(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].OctaveSpread = Math.Max(1, Math.Min(4, value));
        }

        [ParameterDecl(Name = "Step", MinValue = 1, MaxValue = 8, DefValue = 1,
                       Description = "Chord tones advanced per arp step (1=every note, 2=skip one, etc.)")]
        public void SetStep(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Step = Math.Max(1, Math.Min(8, value));
        }

        [ParameterDecl(Name = "Oct Walk", MinValue = 0, MaxValue = 2, DefValue = 0,
                       ValueDescriptions = new[] { "Off", "Up", "Ping-pong" },
                       Description = "How the octave shifts after each full chord cycle")]
        public void SetOctWalk(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].OctWalk = Math.Max(0, Math.Min(2, value));
        }

        [ParameterDecl(Name = "Swing", MinValue = 0, MaxValue = 100, DefValue = 0,
                       Description = "0 = straight  |  50 = medium shuffle  |  100 = 2:1 triplet swing")]
        public void SetSwing(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].Swing = Math.Max(0, Math.Min(100, value));
        }

        [ParameterDecl(Name = "Swing On", MinValue = 0, MaxValue = 1, DefValue = 0,
                       Description = "0 = swing on 1st beat  |  1 = swing on 2nd beat")]
        public void SetSwingPhase(int value, int track)
        {
            if ((uint)track >= MaxVoices) return;
            _vs[track].SwingPhaseVal = value & 1;
        }

        // =====================================================================
        // Chord building
        // =====================================================================

        int[] BuildNotes(int buzzRoot, int chordType, int octaves)
        {
            int root = BN.ToMidi(buzzRoot);
            if (root < 0) return new int[0];

            var ivals = ChordLib.Intervals[Math.Max(0, Math.Min(ChordLib.Intervals.Length - 1, chordType))];
            var list  = new List<int>();
            for (int o = 0; o < octaves; o++)
                foreach (int s in ivals)
                    list.Add(root + s + o * 12);

            return list
                .Select(n => Math.Max(0, Math.Min(119, n)))
                .Distinct()
                .ToArray();
        }

        // =====================================================================
        // Note-triggering helpers
        // =====================================================================

        // Find the Note parameter on a target generator.
        //
        // Four-pass search so we handle native machines (C++ DLLs), managed
        // machines, and any unusual parameter-group layouts.
        //
        // Standard Buzz group layout:
        //   index 0 → internal / hidden parameters
        //   index 1 → global parameters
        //   index 2 → track parameters  (Note is always the first one)
        //
        // ReBuzz's native-machine wrapper does not always set
        // pg.Type == ParameterGroupType.Track, so we MUST try index 2
        // directly before falling back to the type-based search.
        IParameter FindNoteParam(IMachine m)
        {
            if (m == null || m.ParameterGroups == null) return null;

            // Pass 1 – explicit ParameterType.Note anywhere (most reliable for managed machines)
            foreach (var pg in m.ParameterGroups)
            {
                if (pg?.Parameters == null) continue;
                foreach (var p in pg.Parameters)
                    if (p?.Type == ParameterType.Note) return p;
            }

            // Pass 2 – track group = index 2 (3-group machines, the norm)
            //           OR index 1 (2-group machines).
            //           Always the LAST group; try it directly.
            {
                int tgi = (m.ParameterGroups.Count > 2) ? 2 : m.ParameterGroups.Count - 1;
                if (tgi >= 0)
                {
                    var pg2 = m.ParameterGroups[tgi];
                    if (pg2?.Parameters != null)
                    {
                        var p = pg2.Parameters.FirstOrDefault(x => x != null);
                        if (p != null) return p;
                    }
                }
            }

            // Pass 3 – any group typed Track, first parameter
            foreach (var pg in m.ParameterGroups)
            {
                if (pg?.Type != ParameterGroupType.Track || pg.Parameters == null) continue;
                var p = pg.Parameters.FirstOrDefault(x => x != null);
                if (p != null) return p;
            }

            // Pass 4 – last-resort: first parameter of the last non-empty group
            //          (some unconventional machines put track params here)
            for (int gi = m.ParameterGroups.Count - 1; gi >= 0; gi--)
            {
                var pg = m.ParameterGroups[gi];
                if (pg?.Parameters == null || pg.Parameters.Count == 0) continue;
                var p = pg.Parameters.FirstOrDefault(x => x != null);
                if (p != null) return p;
            }

            return null;
        }

        // Find a velocity/volume parameter on the target machine by name.
        // Looks for "Volume", "Velocity", "Vol", or "Vel" (case-insensitive).
        // Returns null if none found — velocity delivery is best-effort.
        IParameter FindVelocityParam(IMachine m, IParameter noteParam)
        {
            if (m == null || m.ParameterGroups == null) return null;
            var velNames = new[] { "volume", "velocity", "vol", "vel" };
            foreach (var pg in m.ParameterGroups)
            {
                if (pg?.Parameters == null) continue;
                foreach (var p in pg.Parameters)
                {
                    if (p == null || p == noteParam) continue;
                    if (velNames.Contains(p.Name?.ToLowerInvariant() ?? "")) return p;
                }
            }
            return null; // no named velocity param found — skip velocity delivery
        }

        void EnsureTrackCount(IMachine m, int needed)
        {
            // In ReBuzz IMachine.TrackCount may or may not have a setter on the
            // interface.  We try the direct setter first; if the interface only
            // exposes a getter we fall back to reflection on the concrete type,
            // which always has a writable backing field.  Either way we swallow
            // exceptions – the worst outcome is the note is ignored by the machine.
            if (m == null || m.TrackCount >= needed) return;
            try
            {
                // Attempt 1: direct (works if interface declares set)
                m.TrackCount = needed;
            }
            catch
            {
                // Attempt 2: reflection on the concrete implementation
                try
                {
                    var prop = m.GetType().GetProperty("TrackCount");
                    if (prop?.CanWrite == true) prop.SetValue(m, needed);
                }
                catch { }
            }
        }

        void FireNote(IParameter np, IMachine m, int track, int midiNote,
                      IParameter vp = null, int velocity = 100)
        {
            if (np == null || m == null || track < 0) return;
            EnsureTrackCount(m, track + 1);
            if (vp != null)
                try { vp.SetValue(track, Math.Max(vp.MinValue,
                                             Math.Min(vp.MaxValue, velocity))); } catch { }
            try { np.SetValue(track, BN.FromMidi(midiNote)); } catch { return; }
            try { m.SendControlChanges(); } catch { }
        }

        void FireOff(IParameter np, IMachine m, int track)
        {
            if (np == null || m == null || track < 0) return;
            try { np.SetValue(track, BN.Off); } catch { return; }
            try { m.SendControlChanges(); } catch { }
        }

        IMachine ResolveTarget(int v)
        {
            try
            {
                string name = _state.Get(v).MachineName;
                if (string.IsNullOrEmpty(name)) return null;
                return Buzz?.Song?.Machines?.FirstOrDefault(m => m.Name == name);
            }
            catch { return null; }
        }

        // =====================================================================
        // Voice control
        // =====================================================================

        void Kill(VoiceState vs, IParameter np, IMachine m)
        {
            for (int s = 0; s < MaxSlots; s++)
                if (vs.SlotOff[s] > 0) { FireOff(np, m, vs.SlotTrack[s]); vs.SlotOff[s] = 0; }
            vs.Active = false;
        }

        void Start(VoiceState vs, IParameter np, IParameter vp, IMachine m, int baseTrack)
        {
            Kill(vs, np, m);
            if (vs.Notes.Length == 0) return;

            vs.Active   = true;
            vs.ArpIdx   = (vs.Mode == 2 || vs.Mode == 5) ? vs.Notes.Length - 1 : 0;
            vs.ArpDir   = (vs.Mode == 5) ? -1 : 1;
            vs.ArpTicks      = 0;
            vs.ArpStepParity = 0;
            vs.OctOffset     = 0;
            vs.OctDir        = 1;

            if (vs.Mode == 0)   // Chord: all notes simultaneously
            {
                for (int i = 0; i < vs.Notes.Length && i < MaxSlots; i++)
                {
                    int t = baseTrack + i;
                    FireNote(np, m, t, vs.Notes[i], vp, vs.Velocity);
                    vs.SlotTrack[i] = t;
                    vs.SlotOff[i]   = vs.Length > 0 ? vs.Length : 0;
                }
            }
            else
            {
                StepArp(vs, np, vp, m, baseTrack);
            }
        }

        void StepArp(VoiceState vs, IParameter np, IParameter vp, IMachine m, int baseTrack)
        {
            if (vs.Notes.Length == 0) return;
            int idx = (vs.Mode == 5) ? _rng.Next(vs.Notes.Length) : vs.ArpIdx;
            int _velDrift = vs.HumanizeVel > 0
                ? (int)Math.Round(vs.Velocity * vs.HumanizeVel / 200.0)
                : 0;
            int _velJitter  = _velDrift > 0 ? _rng.Next(-_velDrift, _velDrift + 1) : 0;
            int _firedVel   = Math.Max(1, Math.Min(127, vs.Velocity + _velJitter));
            int _midiNote = vs.Notes[idx];
            if (vs.OctWalk != 0)
                _midiNote = Math.Min(119, _midiNote + vs.OctOffset * 12);
            FireNote(np, m, baseTrack, _midiNote, vp, _firedVel);
            vs.SlotTrack[0] = baseTrack;
            vs.SlotOff[0]   = vs.Length > 0 ? vs.Length : 0;
            if (vs.Mode != 5) AdvArp(vs);
            // Swing: compute integer long/short tick counts using Math.Round so
            // long + short = 2×Speed exactly — tempo is always locked.
            float _ratio    = 1f + vs.Swing / 100f;   // [1.0 .. 2.0]
            int   _longT    = (int)Math.Round(2.0 * vs.Speed * _ratio / (_ratio + 1.0));
            int   _shortT   = Math.Max(1, 2 * vs.Speed - _longT);
            _longT          = Math.Max(1, 2 * vs.Speed - _shortT);
            bool  _isLong   = ((vs.ArpStepParity + vs.SwingPhaseVal) % 2 == 0);
            int   _base     = _isLong ? _longT : _shortT;
            vs.ArpStepParity = 1 - vs.ArpStepParity;   // toggle 0↔1 — never grows
            // Humanize: random ±drift proportional to Speed, non-cumulative.
            // Drift range = ±(Speed × Humanize / 200), minimum ±0.
            int _drift = vs.Humanize > 0
                ? (int)Math.Round(vs.Speed * vs.Humanize / 200.0)
                : 0;
            int _jitter = _drift > 0 ? _rng.Next(-_drift, _drift + 1) : 0;
            vs.ArpTicks = Math.Max(1, _base + _jitter);
        }

        void AdvArp(VoiceState vs)
        {
            int len = vs.Notes.Length;
            if (len <= 1) { if (vs.OctWalk != 0) AdvOct(vs); return; }
            int s = vs.Step;
            switch (vs.Mode)
            {
                case 1: // Up — advance by Step, wrap and advance oct on each wrap
                {
                    int next = vs.ArpIdx + s;
                    if (next >= len) AdvOct(vs);
                    vs.ArpIdx = next % len;
                    break;
                }
                case 2: // Down — retreat by Step
                {
                    int next = vs.ArpIdx - s;
                    if (next < 0) AdvOct(vs);
                    vs.ArpIdx = ((next % len) + len) % len;
                    break;
                }
                case 3:  // Up+Down ping-pong
                case 4:  // Down+Up ping-pong
                {
                    vs.ArpIdx += vs.ArpDir * s;
                    // Reflect until in range — handles step > 1 correctly.
                    while (vs.ArpIdx >= len || vs.ArpIdx < 0)
                    {
                        if (vs.ArpIdx >= len)
                        {
                            vs.ArpIdx = 2 * (len - 1) - vs.ArpIdx;
                            vs.ArpDir = -1;
                            AdvOct(vs);  // completed an upward pass
                        }
                        if (vs.ArpIdx < 0)
                        {
                            vs.ArpIdx = -vs.ArpIdx;
                            vs.ArpDir = +1;
                        }
                    }
                    break;
                }
            }
        }

        void AdvOct(VoiceState vs)
        {
            if (vs.OctWalk == 0 || vs.OctaveSpread <= 1) return;
            switch (vs.OctWalk)
            {
                case 1: // Up — cycle through octaves, wrap at top
                    vs.OctOffset = (vs.OctOffset + 1) % vs.OctaveSpread;
                    break;
                case 2: // Ping-pong — bounce between 0 and OctaveSpread-1
                    vs.OctOffset += vs.OctDir;
                    if (vs.OctOffset >= vs.OctaveSpread)
                    {
                        vs.OctOffset = Math.Max(0, vs.OctaveSpread - 2);
                        vs.OctDir    = -1;
                    }
                    else if (vs.OctOffset < 0)
                    {
                        vs.OctOffset = Math.Min(1, vs.OctaveSpread - 1);
                        vs.OctDir    = +1;
                    }
                    break;
            }
        }

        // =====================================================================
        // IBuzzMachine - Work (control machine)
        // =====================================================================

        // void Work() with no parameters = "control machine" in ReBuzz.
        // This is called by manageMachineHost AFTER it has already delivered
        // parameter changes (SetNote etc.) via its own Tick() path.
        // IBuzzMachine.Tick() is NEVER called for managed machines — all
        // per-tick logic must live here in Work().
        //
        // After writing pvalues on the target machine we call
        // tgt.SendControlChanges() which sets a flag that makes the target's
        // TickAndWork() run an extra AudioTick(), picking up our freshly-written
        // pvalues and delivering the note to the native machine in time.
        public void Work()
        {
            if (Buzz == null) return;

            // Work() is called once per audio buffer (many times per pattern tick).
            // PosInTick resets to 0 at each tick boundary; we use that to fire timing
            // logic exactly once per pattern tick (safe for SendControlChanges).
            int  pit     = host?.MasterInfo?.PosInTick ?? 0;
            bool newTick = pit < _prevPit;
            _prevPit     = pit;

            for (int v = 0; v < MaxVoices; v++)
            {
                VoiceState  vs      = _vs[v];
                TrackTarget cfg     = _state.Get(v);
                IMachine    tgt     = ResolveTarget(v);
                IParameter  np      = (tgt != null) ? FindNoteParam(tgt) : null;
                IParameter  vp      = (np  != null) ? FindVelocityParam(tgt, np) : null;
                int         baseTrk = cfg.BaseTrack;

                // Note delivery from pattern — only arrives on the first Work()
                // call of a tick (set by managedMachineHost.Tick() beforehand).
                if (vs.HasNewNote)
                {
                    vs.HasNewNote = false;
                    if (vs.PendingNote == BN.Off)
                    {
                        if (tgt != null && np != null) Kill(vs, np, tgt);
                        else vs.Active = false;
                    }
                    else if (vs.PendingNote > 0)
                    {
                        int _octs = vs.OctWalk != 0 ? 1 : vs.OctaveSpread;
                        vs.Notes = BuildNotes(vs.PendingNote, vs.ChordType, _octs);
                        if (tgt != null && np != null)
                        Start(vs, np, vp, tgt, baseTrk);
                    }
                    continue;
                }

                // Arp reset — rewind note sequence to start, fires on next tick.
                if (vs.PendingReset && newTick && vs.Active)
                {
                    vs.PendingReset = false;
                    vs.ArpIdx = (vs.Mode == 2 || vs.Mode == 4) ? vs.Notes.Length - 1 : 0;
                    vs.ArpDir = (vs.Mode == 4) ? -1 : 1;
                    vs.ArpTicks = 1;
                }
                else vs.PendingReset = false;

                if (!vs.Active || tgt == null || np == null) continue;
                if (!newTick) continue;

                // Note-off countdowns
                for (int s = 0; s < MaxSlots; s++)
                {
                    if (vs.SlotOff[s] <= 0) continue;
                    if (--vs.SlotOff[s] == 0)
                    FireOff(np, tgt, vs.SlotTrack[s]);
                }

                // Arpeggio step
                if (vs.Mode != 0 && vs.ArpTicks > 0 && --vs.ArpTicks == 0)
                StepArp(vs, np, vp, tgt, baseTrk);
            }
        }

        // =====================================================================
        // IBuzzMachine - Stop
        // =====================================================================

        // Called on the UI thread when the user presses Stop.
        // Send note-offs for every active slot on every voice, then reset all
        // voice state so Work() fires nothing until the next Play.
        public void Stop()
        {
for (int v = 0; v < MaxVoices; v++)
            {
                VoiceState vs  = _vs[v];
                IMachine   tgt = ResolveTarget(v);
                IParameter np  = (tgt != null) ? FindNoteParam(tgt) : null;

                if (vs.Active && np != null && tgt != null)
                {
                    for (int s = 0; s < MaxSlots; s++)
                        if (vs.SlotOff[s] > 0)
                            try { np.SetValue(vs.SlotTrack[s], BN.Off); } catch { }
                    try { tgt.SendControlChanges(); } catch { }
                }

                vs.Active        = false;
                vs.HasNewNote    = false;
                vs.PendingNote   = 0;
                vs.PendingReset  = false;
                vs.ArpTicks      = 0;
                vs.ArpStepParity = 0;
                vs.OctOffset     = 0;
                vs.OctDir        = 1;
                for (int s = 0; s < MaxSlots; s++) vs.SlotOff[s] = 0;
            }
        }


        // =====================================================================
        // Machine state persistence
        // =====================================================================

        public PedalChordState MachineState
        {
            get => _state;
            set { if (value != null) _state = value; }
        }

        // =====================================================================
        // Import rename fix-up
        // =====================================================================

        public void ImportFinished(IDictionary<string, string> nameMap)
        {
            if (_state?.Targets == null) return;
            foreach (TrackTarget t in _state.Targets)
                if (!string.IsNullOrEmpty(t.MachineName) &&
                    nameMap.TryGetValue(t.MachineName, out string n))
                    t.MachineName = n;
        }

        // =====================================================================
        // Context-menu commands
        // =====================================================================

        // ReBuzz calls Command(id) for right-click items and sometimes at init.
        public void Command(int id)
        {
            switch (id)
            {
                case 0: OpenSettings();  break;
                case 1: ShowAbout();     break;
                case 2: ShowChordRef();  break;
                case 3: ShowDiagnostics(); break;
            }
        }

        public IEnumerable<IMenuItem> Commands => new IMenuItem[]
        {
            new MenuEntry(0, "Target Settings\u2026", OpenSettings),
            new MenuEntry(1, "About\u2026",           ShowAbout),
            new MenuEntry(2, "Chord Reference\u2026", ShowChordRef),
            new MenuEntry(3, "Diagnostics\u2026",     ShowDiagnostics),
        };

        void OpenSettings()
        {
            // Guard: not ready yet (ReBuzz sometimes calls Command(0) at placement)
            if (host?.Machine == null) return;

            // Already open – bring to front on the UI thread
            if (_settingsWin != null)
            {
                try
                {
                    Application.Current?.Dispatcher?.BeginInvoke(
                        new Action(() => _settingsWin?.Activate()));
                }
                catch { }
                return;
            }

            // ── Pre-capture Buzz data on the CALLING thread ────────────────────
            // We must read IBuzz / IMachine here, before handing off to the UI
            // thread, because those objects may only be safely touched on the
            // thread that owns them.
            int          trackCount   = 0;
            List<string> machineNames = new List<string>();
            try
            {
                trackCount = host.Machine.TrackCount;
                IBuzz    buzz = Buzz;
                IMachine self = host.Machine;
                if (buzz?.Song?.Machines != null)
                    foreach (IMachine m in buzz.Song.Machines)
                        if (m != self && !string.IsNullOrEmpty(m.Name))
                            machineNames.Add(m.Name);
            }
            catch { /* proceed with whatever we collected */ }

            PedalChordState stateRef = _state;

            // ── Show on ReBuzz's own WPF UI thread ─────────────────────────────
            // Using a secondary STA thread causes WPF to try to reach back to
            // Application.Current.MainWindow across thread boundaries → NRE.
            // BeginInvoke posts the action to the UI thread's dispatcher queue
            // and returns immediately; the dialog then runs as a proper modal.
            Action showAction = () =>
            {
                TargetSettingsWindow win = null;
                try
                {
                    win          = new TargetSettingsWindow(stateRef, machineNames, trackCount);
                    win.Owner    = Application.Current?.MainWindow;
                    _settingsWin = win;
                    win.Closed  += (s2, e2) =>
                    {
                        if (win.DialogResult == true) _state = win.Result;
                        _settingsWin = null;
                    };
                    win.ShowDialog();
                }
                catch (Exception ex)
                {
                    _settingsWin = null;
                    try
                    {
                        MessageBox.Show(ex.Message, "Pedal Chord \u2013 Settings Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch { }
                }
            };

            try
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null)
                    dispatcher.BeginInvoke(showAction);
                else
                    showAction();   // fallback (shouldn't happen inside ReBuzz)
            }
            catch { }
        }

        // ── Chord Reference window ────────────────────────────────────────────
        void ShowChordRef()
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                try
                {
                    var win = new ChordReferenceWindow();
                    win.Owner = Application.Current?.MainWindow;
                    win.Show();
                }
                catch { }
            }));
        }

        void ShowDiagnostics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Voice routing diagnostics:");
            sb.AppendLine("");
            for (int v = 0; v < MaxVoices; v++)
            {
                string name = _state.Get(v).MachineName;
                if (string.IsNullOrEmpty(name)) continue;  // skip unconfigured
                IMachine   tgt = ResolveTarget(v);
                IParameter np  = tgt != null ? FindNoteParam(tgt) : null;
                int _tc = tgt?.TrackCount ?? -1;
                sb.AppendFormat("Voice {0}: target=[{1}]  resolved={2}  noteParam={3} (hash={4})  tracks={5}  active={6}\n",
                    v + 1, name,
                    tgt != null ? "YES" : "NO - name not found in Song",
                    np  != null ? "YES" : "NO - param not found",
                    np  != null ? np.GetHashCode().ToString() : "n/a",
                    _tc,
                    _vs[v].Active);
            }
            if (sb.Length == 0 || !sb.ToString().Contains("Voice"))
                sb.AppendLine("  (no voices configured - open Target Settings first)");
            string diagMsg = sb.ToString();
            // Write to ReBuzz debug console (View → Debug Console or Ctrl+D)
            try
            {
                IBuzz buzz = Buzz;
                if (buzz != null)
                    foreach (string line in diagMsg.Split('\n'))
                        if (line.Trim().Length > 0)
                            buzz.DCWriteLine("[PedalChord] " + line.Trim());
            }
            catch { }
            // Also open the debug console so the user can see it
            Application.Current?.Dispatcher?.BeginInvoke((Action)(() =>
            {
                try { Buzz?.ExecuteCommand(BuzzCommand.DebugConsole); } catch { }
            }));
        }

        void ShowAbout()
        {
            try
            {
                MessageBox.Show(
                    "Pedal Chord v1.0\n" +
                    "Chord and arpeggio trigger for ReBuzz\n\n" +
                    "This is a controller machine with no audio connections.\n" +
                    "Right-click \u2192 Target Settings\u2026 to configure each voice track.",
                    "About Pedal Chord",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }
    }

    // =========================================================================
    // IMenuItem - plain POCO, no DependencyObject (safe on any thread)
    // =========================================================================

    sealed class MenuEntry : IMenuItem, INotifyPropertyChanged
    {
        readonly Action _action;
        bool _isChecked;
        bool _isEnabled = true;

        public MenuEntry(int id, string text, Action action)
        { ID = id; Text = text; _action = action; }

        public int    ID               { get; }
        public string Text             { get; }
        public string GestureText      => null;
        public object CommandParameter => null;
        public IEnumerable<IMenuItem> Children => null;

        public bool IsCheckable      { get; set; } = false;
        public bool IsDefault        { get; set; } = false;
        public bool IsSeparator      { get; set; } = false;
        public bool IsLabel          { get; set; } = false;
        public bool StaysOpenOnClick { get; set; } = false;

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled))); }
        }

        public ICommand Command => new RelayCmd(() => _action?.Invoke());

        public event PropertyChangedEventHandler PropertyChanged;
    }

    sealed class RelayCmd : ICommand
    {
        readonly Action _exec;
        public RelayCmd(Action e) => _exec = e;
        public bool CanExecute(object p) => true;
        public void Execute(object p)    => _exec();
        public event EventHandler CanExecuteChanged;
    }

    // =========================================================================
    // Target Settings window
    //
    // Receives pre-captured plain data (machine name list, voice track count)
    // rather than IBuzz / IMachine references, so it is completely safe to run
    // on a dedicated STA thread without touching ReBuzz's WPF object graph.
    // =========================================================================

    class TargetSettingsWindow : Window
    {
        public PedalChordState Result { get; private set; }

        readonly PedalChordState _work;
        readonly List<string>    _machineNames;
        readonly int             _trackCount;

        readonly ComboBox _voiceBox = new ComboBox { Width = 220, Margin = new Thickness(4,0,0,0) };
        readonly ComboBox _machBox  = new ComboBox { Width = 220, Margin = new Thickness(4,0,0,0) };
        readonly ComboBox _baseTrk  = new ComboBox { Width = 220, Margin = new Thickness(4,0,0,0) };

        int  _sel     = 0;
        bool _loading = false;

        public TargetSettingsWindow(PedalChordState state,
                                    List<string> machineNames,
                                    int trackCount)
        {
            _machineNames = machineNames ?? new List<string>();
            _trackCount   = (trackCount > 0) ? trackCount : 16;
            _work         = DeepCopy(state);
            Result        = _work;

            Title                 = "Pedal Chord \u2013 Target Settings";
            Width                 = 440;
            SizeToContent         = SizeToContent.Height;
            ResizeMode            = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Content               = BuildUI();
            Loaded               += (s, e) => Populate();
        }

        static PedalChordState DeepCopy(PedalChordState src)
        {
            var dst = new PedalChordState { Targets = new List<TrackTarget>() };
            if (src?.Targets != null)
                foreach (TrackTarget t in src.Targets)
                    dst.Targets.Add(new TrackTarget { MachineName = t.MachineName, BaseTrack = t.BaseTrack });
            return dst;
        }

        UIElement BuildUI()
        {
            var outer = new StackPanel { Margin = new Thickness(12) };

            outer.Children.Add(MakeRow("Voice track:",      _voiceBox));
            outer.Children.Add(MakeRow("Target machine:",   _machBox));
            outer.Children.Add(MakeRow("Base track index:", _baseTrk));

            outer.Children.Add(new TextBlock
            {
                Text         = "Chord mode fires all chord notes on consecutive target tracks\n" +
                               "(Base, Base+1, ... up to Base+4 for 5-note chords).\n" +
                               "Arpeggio modes use only the single Base Track.",
                Foreground   = System.Windows.Media.Brushes.Gray,
                Margin       = new Thickness(0, 8, 0, 12),
                TextWrapping = TextWrapping.Wrap,
                FontSize     = 11
            });

            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var btnOK  = new Button { Content = "OK",     Width = 80, Margin = new Thickness(0,0,6,0), IsDefault = true };
            var btnCan = new Button { Content = "Cancel", Width = 80, IsCancel = true };
            btnOK.Click  += (s, e) => { Save(); Result = _work; DialogResult = true; };
            btnCan.Click += (s, e) => { DialogResult = false; };
            btnRow.Children.Add(btnOK);
            btnRow.Children.Add(btnCan);
            outer.Children.Add(btnRow);

            _voiceBox.SelectionChanged += (s, e) => { if (!_loading) { Save(); _sel = _voiceBox.SelectedIndex; Load(); } };
            _machBox.SelectionChanged  += (s, e) => { if (!_loading) Save(); };
            _baseTrk.SelectionChanged  += (s, e) => { if (!_loading) Save(); };

            return outer;
        }

        static StackPanel MakeRow(string label, UIElement ctrl)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,6) };
            row.Children.Add(new TextBlock
            {
                Text              = label,
                Width             = 110,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment     = TextAlignment.Right,
                Margin            = new Thickness(0,0,8,0)
            });
            row.Children.Add(ctrl);
            return row;
        }

        void Populate()
        {
            _loading = true;

            _voiceBox.Items.Clear();
            for (int i = 0; i < _trackCount; i++)
                _voiceBox.Items.Add(string.Format("Voice {0}", i + 1));
            if (_voiceBox.Items.Count > 0) _voiceBox.SelectedIndex = 0;

            _machBox.Items.Clear();
            _machBox.Items.Add("(none)");
            foreach (string name in _machineNames)
                _machBox.Items.Add(name);

            _baseTrk.Items.Clear();
            for (int i = 0; i < 64; i++)
                _baseTrk.Items.Add(string.Format("Track {0}", i));

            _loading = false;
            Load();
        }

        void Load()
        {
            _loading = true;
            TrackTarget t = _work.Get(_sel);

            int mi = _machBox.Items.IndexOf(t.MachineName);
            _machBox.SelectedIndex = (mi > 0) ? mi : 0;
            _baseTrk.SelectedIndex = Math.Max(0, Math.Min(63, t.BaseTrack));

            _loading = false;
        }

        void Save()
        {
            TrackTarget t = _work.Get(_sel);
            t.MachineName = (_machBox.SelectedIndex <= 0) ? "" : _machBox.SelectedItem?.ToString() ?? "";
            t.BaseTrack   = Math.Max(0, Math.Min(63, _baseTrk.SelectedIndex));
        }
    }

    // =========================================================================
    // Chord Reference window  (right-click → Chord Reference…)
    // =========================================================================

    sealed class ChordReferenceWindow : Window
    {
        static readonly string[] NoteNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        static string IntervalsToNotes(int[] semitones)
        {
            return string.Join(" ", semitones.Select(s => NoteNames[s % 12]));
        }

        static string IntervalsLabel(int[] semitones)
        {
            // Show intervals relative to root, e.g. "0  4  7  10"
            return string.Join("  ", semitones.Select(s => s.ToString()));
        }

        public ChordReferenceWindow()
        {
            Title                 = "Pedal Chord – Chord Reference";
            Width                 = 480;
            Height                = 600;
            ResizeMode            = ResizeMode.CanResizeWithGrip;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // ── Layout ────────────────────────────────────────────────────
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // ── Header ────────────────────────────────────────────────────
            var header = new TextBlock
            {
                Text       = "Dec and Hex columns show the value to type in the pattern editor. Intervals relative to root (C shown as example).",
                Margin     = new Thickness(12, 10, 12, 6),
                TextWrapping = TextWrapping.Wrap,
                Foreground = SystemColors.GrayTextBrush,
                FontSize   = 11,
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // ── Scrollable chord list ──────────────────────────────────────
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(8, 0, 8, 0),
            };
            Grid.SetRow(scroll, 1);
            grid.Children.Add(scroll);

            var stack = new StackPanel();
            scroll.Content = stack;

            string currentSection = null;
            var sections = new (string Section, string Name, int[] Intervals)[]
            {
                ("Triads",         "Major",         new[]{0,4,7}),
                ("Triads",         "Minor",         new[]{0,3,7}),
                ("Triads",         "Aug",           new[]{0,4,8}),
                ("Triads",         "Dim",           new[]{0,3,6}),
                ("Triads",         "Sus 2",         new[]{0,2,7}),
                ("Triads",         "Sus 4",         new[]{0,5,7}),
                ("Triads",         "Power",         new[]{0,7}),
                ("Triads",         "b5 Triad",      new[]{0,4,6}),

                ("Seventh",        "Dom 7",         new[]{0,4,7,10}),
                ("Seventh",        "Maj 7",         new[]{0,4,7,11}),
                ("Seventh",        "Min 7",         new[]{0,3,7,10}),
                ("Seventh",        "Dim 7",         new[]{0,3,6,9}),
                ("Seventh",        "Half Dim 7",    new[]{0,3,6,10}),
                ("Seventh",        "Min Maj 7",     new[]{0,3,7,11}),
                ("Seventh",        "Aug Maj 7",     new[]{0,4,8,11}),
                ("Seventh",        "Aug 7",         new[]{0,4,8,10}),
                ("Seventh",        "Dim Maj 7",     new[]{0,3,6,11}),
                ("Seventh",        "7 Sus4",        new[]{0,5,7,10}),
                ("Seventh",        "7 Sus2",        new[]{0,2,7,10}),

                ("Sixth",          "Maj 6",         new[]{0,4,7,9}),
                ("Sixth",          "Min 6",         new[]{0,3,7,9}),

                ("Ninth",          "Dom 9",         new[]{0,4,7,10,14}),
                ("Ninth",          "Maj 9",         new[]{0,4,7,11,14}),
                ("Ninth",          "Min 9",         new[]{0,3,7,10,14}),
                ("Ninth",          "Add 9",         new[]{0,4,7,14}),
                ("Ninth",          "Min Add 9",     new[]{0,3,7,14}),
                ("Ninth",          "6/9",           new[]{0,4,7,9,14}),
                ("Ninth",          "Min 6/9",       new[]{0,3,7,9,14}),
                ("Ninth",          "7 b9",          new[]{0,4,7,10,13}),
                ("Ninth",          "7 #9",          new[]{0,4,7,10,15}),
                ("Ninth",          "9 Sus4",        new[]{0,5,7,10,14}),

                ("Altered",        "7 b5",          new[]{0,4,6,10}),
                ("Altered",        "7 b5 b9",       new[]{0,4,6,10,13}),
                ("Altered",        "7 b5 #9",       new[]{0,4,6,10,15}),

                ("Eleventh",       "Dom 11",        new[]{0,4,7,10,17}),
                ("Eleventh",       "Maj 11",        new[]{0,4,7,11,17}),
                ("Eleventh",       "Min 11",        new[]{0,3,7,10,17}),
                ("Eleventh",       "7 #11",         new[]{0,4,7,10,18}),
                ("Eleventh",       "Maj7 #11",      new[]{0,4,7,11,18}),
                ("Eleventh",       "Add 11",        new[]{0,4,7,17}),

                ("Thirteenth",     "Dom 13",        new[]{0,4,7,10,14,21}),
                ("Thirteenth",     "Maj 13",        new[]{0,4,7,11,14,21}),
                ("Thirteenth",     "Min 13",        new[]{0,3,7,10,14,21}),
                ("Thirteenth",     "Dom 13 Full",   new[]{0,4,7,10,14,17,21}),

                ("Shell",          "Dom Shell",     new[]{0,4,10}),
                ("Shell",          "Maj7 Shell",    new[]{0,4,11}),
                ("Shell",          "Min7 Shell",    new[]{0,3,10}),

                ("Exotic / Modal", "Quartal",       new[]{0,5,10,15}),
                ("Exotic / Modal", "Quintal",       new[]{0,7,14,21}),
                ("Exotic / Modal", "Whole Tone",    new[]{0,2,4,6,8,10}),
                ("Exotic / Modal", "Cluster",       new[]{0,1,2}),
            };

            foreach (var (section, name, ivals) in sections)
            {
                if (section != currentSection)
                {
                    currentSection = section;
                    var hdr = new TextBlock
                    {
                        Text            = section,
                        FontWeight      = FontWeights.Bold,
                        Margin          = new Thickness(6, 10, 6, 2),
                        Foreground      = SystemColors.HighlightBrush,
                        FontSize        = 12,
                    };
                    stack.Children.Add(hdr);
                    stack.Children.Add(new Separator { Margin = new Thickness(6,0,6,4) });
                }

                // Row: [number]  [name]  [notes from C]  [semitone intervals]
                int idx = System.Array.IndexOf(ChordLib.Intervals, ivals);

                var row = new Grid { Margin = new Thickness(6, 1, 6, 1) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Find the chord index matching these intervals
                int chordIdx = -1;
                for (int ci = 0; ci < ChordLib.Intervals.Length; ci++)
                    if (ChordLib.Intervals[ci].SequenceEqual(ivals)) { chordIdx = ci; break; }

                string hexLabel = chordIdx >= 0 ? chordIdx.ToString("X2") : "——";

                Add(row, 0, chordIdx >= 0 ? chordIdx.ToString() : "—",
                    HorizontalAlignment.Right, SystemColors.GrayTextBrush);
                Add(row, 1, hexLabel,
                    HorizontalAlignment.Center, Brushes.DarkOrange);
                Add(row, 2, name,
                    HorizontalAlignment.Left, SystemColors.ControlTextBrush, bold: true);
                Add(row, 3, IntervalsToNotes(ivals),
                    HorizontalAlignment.Left, SystemColors.ControlTextBrush);
                Add(row, 4, IntervalsLabel(ivals),
                    HorizontalAlignment.Left, SystemColors.GrayTextBrush);

                stack.Children.Add(row);
            }

            // ── Close button ──────────────────────────────────────────────
            var closeBtn = new Button
            {
                Content            = "Close",
                Width              = 80,
                Margin             = new Thickness(0, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault          = true,
                IsCancel           = true,
            };
            closeBtn.Click += (s, e) => Close();
            Grid.SetRow(closeBtn, 2);
            grid.Children.Add(closeBtn);

            Content = grid;
        }

        static void Add(Grid row, int col, string text,
            HorizontalAlignment align, Brush fg, bool bold = false)
        {
            var tb = new TextBlock
            {
                Text                = text,
                HorizontalAlignment = align,
                Foreground          = fg,
                FontSize            = 11,
                FontWeight          = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Margin              = new Thickness(4, 0, 4, 0),
                VerticalAlignment   = VerticalAlignment.Center,
            };
            Grid.SetColumn(tb, col);
            row.Children.Add(tb);
        }
    }

}
