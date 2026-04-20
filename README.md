# Pedal Chord v1.1 — ReBuzz Managed Controller Machine

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

| Parameter  | Range   | Description |
|------------|---------|-------------|
| **Note**   | C-0–B-9 | Root note. Standard Buzz piano keyboard (z/s/x/d/…). |
| **Chord**  | 0–50    | Chord type — 51 chords. Right-click → *Chord Reference…* for full list with hex values. |
| **Mode**   | 0–5     | Chord / Arp Up / Arp Down / Arp Up+Down / Arp Down+Up / Arp Random |
| **Speed**  | 1–1024  | Pattern ticks between arpeggio steps (1 tick = 1 pattern row). |
| **Length** | 1–64    | Note duration in ticks before auto note-off. |
| **Octaves**| 1–4     | Octave spread — arp note list duplicated upward over 1–4 octaves. |
| **Swing**  | 0–100   | Shuffle amount. 0 = straight, 100 ≈ 2:1 triplet shuffle. |
| **Swing On**| 0–1   | Which beat of the alternating pair gets the long wait (0 = 1st, 1 = 2nd). |

### Speed and timing

Speed is measured in **pattern ticks** (rows) and is exact regardless of BPM
or audio buffer size. With Speed=2 and a 32-tick loop you get exactly 16
triggers.

### Swing

Swing alternates between a long and short wait using integer tick counts:

```
longTicks  = Round(2 × Speed × ratio / (ratio + 1))
shortTicks = 2 × Speed − longTicks
```

`long + short = 2 × Speed` always, so average tempo is locked regardless of
swing amount. Effective swing granularity increases with Speed — at Speed=2
only Swing=100 produces an audible effect (3:1); at Speed=4 you get 5:3; at
Speed=8 and above the full 0–100 range gives meaningfully distinct feels.

---

## Chord types

51 chords available (values 0–50 / hex 00–32). Right-click → **Chord
Reference…** for the full table: decimal index, hex pattern value, chord name,
notes from C, and semitone intervals.

Includes: Major, Minor, all 7th/9th/11th/13th voicings, Sus2/4, Add9,
Augmented, Diminished, Half Dim, Shell chords, Quartal, Quintal, Whole Tone,
Cluster.

---

## Arpeggio modes

| Value | Name         | Pattern |
|-------|--------------|---------|
| 0     | Chord        | All notes simultaneously |
| 1     | Arp Up       | Low → High, wraps |
| 2     | Arp Down     | High → Low, wraps |
| 3     | Arp Up+Down  | Ping-pong, starts ascending |
| 4     | Arp Down+Up  | Ping-pong, starts descending |
| 5     | Arp Random   | Random note from chord each step |

---

## Per-voice target settings

Each pattern track (voice) routes independently to a different target machine
and base track. Right-click → *Target Settings…* to configure.

---

## Build from source

Requirements: .NET 10 SDK, ReBuzz installed at `C:\Program Files\ReBuzz`.

```powershell
dotnet build PedalChord.csproj -c Release

# Non-default ReBuzz location:
dotnet build PedalChord.csproj -c Release /p:BuzzDir="D:\ReBuzz"
```

Output: `<BuzzDir>\Gear\Generators\Pedal Chord.NET.dll`

---

## Architecture notes

Pedal Chord is a **control machine** (`public void Work()` with no parameters).
ReBuzz calls `Work()` many times per pattern tick (once per audio buffer).
Timing advances exactly once per tick, detected via `IBuzzMachineHost.MasterInfo.PosInTick`
resetting to zero. Notes and parameter changes are delivered by ReBuzz via
`IBuzzMachineHost.Tick()` before the first `Work()` call of each tick.

Swing uses integer `ArpTicks` with rounded `longTicks`/`shortTicks` values that
sum exactly to `2 × Speed`, ensuring tempo is locked at all swing amounts and
speed values. `ArpStepParity` toggles strictly between 0 and 1 so alternation
never drifts regardless of pattern length or loop count.

`IBuzzMachine.Tick()` is **not called** for managed machines — all per-tick
logic lives in `Work()`.
