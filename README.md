# Pedal Chord v1.0 — ReBuzz Managed Controller Machine

A chord and arpeggio trigger for ReBuzz. Write a root note into Pedal Chord's
pattern and it fires the full chord (or arpeggiated notes) on any target
generator machine in your song.

---

## Quick start

1. **Add to machine view** — drag from the Generators tab.  
2. **Connect a target** — right-click Pedal Chord → *Target Settings…*  
   Select the generator you want to play and which of its tracks to use as
   the base track.  
3. **Write notes** — open a pattern on Pedal Chord and enter root notes with
   the keyboard (z = C-4, s = C#-4, x = D-4 …).  
4. **Press Play** — the chord fires on the target on every pattern row that
   has a note.

The target machine must be connected to the Master (or to an effect chain
that reaches Master) so its audio is heard.

---

## Track parameters

| Parameter | Range | Description |
|-----------|-------|-------------|
| **Note** | C-0 – B-9 | Root note. Uses the standard Buzz piano keyboard (z/s/x/d/…). |
| **Chord** | 0–50 | Chord type — 51 chords. Right-click → *Chord Reference…* for the full list with hex values for pattern entry. |
| **Mode** | 0–5 | Chord / ArpUp / ArpDown / ArpUp+Down / ArpDown+Up / ArpRandom |
| **Speed** | 1–1024 | **Pattern ticks** between arpeggio steps (1 tick = 1 pattern row). |
| **Length** | 1–64 | Note duration in ticks before auto note-off. |
| **Octaves** | 1–4 | How many octaves the arpeggio spans upward. |

Speed is measured in **pattern ticks** (rows), so it is tempo-independent and
stays in sync regardless of BPM. E.g. Speed=2 fires every 2 rows; 16 steps ×
Speed 2 = 32-tick loop.

---

## Chord types

51 chords are available (values 0–50 / hex 00–32). Right-click Pedal Chord
and choose **Chord Reference…** for the full table showing the decimal index,
hex pattern value, chord name, notes from C, and semitone intervals.

Highlights: Major, Minor, all 7th/9th/11th/13th voicings, Sus2/4, Add9,
Augmented, Diminished, Shell chords, Quartal, Quintal, Whole Tone, Cluster.

---

## Arpeggio modes

| Value | Name | Pattern |
|-------|------|---------|
| 0 | Chord | All notes simultaneously |
| 1 | Arp Up | Low → High, wraps |
| 2 | Arp Down | High → Low, wraps |
| 3 | Arp Up+Down | Ping-pong, starts ascending |
| 4 | Arp Down+Up | Ping-pong, starts descending |
| 5 | Arp Random | Random note from chord each step |

---

## Per-voice target settings

Each pattern track (voice) can be routed to a **different** target machine on
a **different** base track, allowing you to drive multiple generators at once
from a single Pedal Chord instance.

Right-click → *Target Settings…* to configure each voice independently.

---

## Build from source

Requirements: .NET 10 SDK, ReBuzz installed at `C:\Program Files\ReBuzz`.

```powershell
dotnet build PedalChord.csproj -c Release

# If ReBuzz is in a non-default location:
dotnet build PedalChord.csproj -c Release /p:BuzzDir="D:\ReBuzz"
```

Output: `<BuzzDir>\Gear\Generators\Pedal Chord.NET.dll`

Restart ReBuzz (or use its machine reload feature) after building.

---

## Architecture notes

Pedal Chord is a **control machine** (`public void Work()` with no parameters).
ReBuzz calls `Work()` once per **audio buffer** — many times per pattern tick —
and delivers note values once per tick via `IBuzzMachineHost.Tick()` before the
first `Work()` call of that tick.

Timing (arp step and note-off countdowns) advances by one tick only on the
first `Work()` call of each pattern tick, detected via
`IBuzzMachineHost.MasterInfo.PosInTick` resetting to zero. This ensures Speed
and Length are always in exact pattern-tick units regardless of BPM, sample
rate, or audio buffer size.

Notes are forwarded to the target by calling `IParameter.SetValue()` on the
target's note parameter, followed by `IMachine.SendControlChanges()`.

`IBuzzMachine.Tick()` is **not called** by ReBuzz for managed machines —
all per-tick logic lives in `Work()`.
