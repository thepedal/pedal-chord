# Pedal Chord v1.3 — ReBuzz Managed Controller Machine

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

| Parameter    | Range  | Description |
|--------------|--------|-------------|
| **Note**     | C-0–B-9 | Root note. Standard Buzz piano keyboard (z/s/x/d/…). |
| **Velocity** | 1–127  | Note velocity sent to the target machine. |
| **Chord**    | 0–50   | Chord type — 51 chords. Right-click → *Chord Reference…* for the full list with hex values. |
| **Mode**     | 0–5    | Chord / Arp Up / Arp Down / Arp Up+Down / Arp Down+Up / Arp Random |
| **Speed**    | 1–1024 | Pattern ticks between arpeggio steps (1 tick = 1 pattern row). |
| **Length**   | 1–64   | Note duration in ticks before auto note-off. |
| **Octaves**  | 1–4    | Octave range for Oct Walk; or full pre-expanded span when Oct Walk is Off. |
| **Step**     | 1–8    | Chord tones advanced per arp step. 1 = every note, 2 = skip one, etc. |
| **Oct Walk** | 0–2    | Off / Up / Ping-pong — how the octave shifts after each full chord cycle. |
| **Swing**    | 0–100  | Shuffle amount. 0 = straight, 100 ≈ 2:1 triplet shuffle. |
| **Swing On** | 0–1    | Which beat of the alternating pair gets the long wait (0 = 1st, 1 = 2nd). |
| **Humanize** | 0–100  | Random ±timing drift per arp step, scales with Speed. |
| **Hum. Vel** | 0–100  | Random ±velocity variation per arp step, scales with Velocity. |
| **Arp Reset**| 0–1    | Write 1 to restart the arp sequence from the first note on this step. |

---

## Speed and timing

Speed is measured in **pattern ticks** (rows) and is exact regardless of BPM
or audio buffer size. Speed=2 in a 32-tick loop gives exactly 16 triggers.

---

## Step and Oct Walk

**Step** and **Oct Walk** work together to create evolving arpeggio patterns
without needing to write every note manually.

### Step

Advances the arp note index by N chord tones per tick rather than 1. All modes
(Up, Down, Ping-pong) respect Step; ping-pong uses reflection arithmetic so it
always stays in range regardless of step size.

Some useful intervals at Step=2 on common chords:
- Major triad [C E G]: alternates a third and a fourth
- Major 7 [C E G B]: cycles in fifths (C→G→C→G or E→B→E→B)
- Sus4 [C F G]: alternates a fourth and a second

### Oct Walk

When Oct Walk is active the **Octaves** parameter becomes the octave *range*,
and the octave advances independently after each complete cycle through the
note list:

- **Off** — current behaviour: all octaves pre-expanded into one flat note list
- **Up** — plays one full chord cycle at oct 0, then oct 1, oct 2… wrapping back
- **Ping-pong** — bounces up and down through the octave range

The octave advances on every wrap of the note cycle, so with a 3-note chord,
Octaves=3, Step=1: you hear 3 notes at oct 0, 3 at oct 1, 3 at oct 2, repeat.

Combine Oct Walk with Step and ping-pong arp mode for patterns that are
difficult to achieve with simple octave expansion.

---

## Swing

Swing alternates between a long and short wait using integer tick counts:

```
longTicks  = Round(2 × Speed × ratio / (ratio + 1))
shortTicks = 2 × Speed − longTicks
```

`long + short = 2 × Speed` always — average tempo is locked regardless of swing
amount. Effective granularity increases with Speed: at Speed=2 only Swing=100
produces an audible effect (3:1); at Speed=4+ the full 0–100 range is useful.

**Swing On** shifts which beat of the pair gets the long wait.

---

## Humanize

**Humanize** adds random ±timing drift per arp step:
- Drift range = `±Round(Speed × Humanize / 200)` ticks, non-cumulative

**Hum. Vel** adds random ±velocity variation per arp step:
- Drift range = `±Round(Velocity × HumanizeVel / 200)`, clamped to [1, 127]

Both only affect arp modes — chord mode fires all notes on the original trigger.

---

## Arp Reset

Write `01` into the **Arp Reset** column on any pattern row to restart the arp
sequence from the first note (or last note for Down/Down+Up modes) at that
point. Stateless — only fires when explicitly written. Useful for snapping a
free-running arp back to the root at bar boundaries or accent points.

---

## Chord types

51 chords available (values 0–50 / hex 00–32). Right-click → **Chord
Reference…** for the full table: decimal index, hex value, chord name, notes
from C, and semitone intervals.

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

**Note:** arp mode fires on a single target track (monophonic). For polyphonic
arp — where each step rings on its own track so notes can overlap — see the
v1.4 roadmap.

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

## Changelog

### v1.3
- **Step** track parameter (1–8) — advance N chord tones per arp tick
- **Oct Walk** track parameter (Off / Up / Ping-pong) — independent octave cycling across chord cycles

### v1.2
- **Velocity** track parameter (1–127) — note velocity on the target
- **Humanize** track parameter (0–100) — random ±timing drift per arp step
- **Hum. Vel** track parameter (0–100) — random ±velocity variation per arp step
- **Arp Reset** track parameter — write 1 to restart the arp sequence from this step

### v1.1
- **Swing** track parameter (0–100) — shuffle/swing timing
- **Swing On** track parameter (0/1) — selects which beat gets the long wait
- Fixed: Arp Random (Mode=5) was unreachable due to a SetMode clamping bug
- Chord Reference window now shows hex values for pattern entry

### v1.0
- Initial release: 51 chords, 6 arp modes, 16 voices, per-voice target routing

---

## Architecture notes

Pedal Chord is a **control machine** (`public void Work()` with no parameters).
ReBuzz calls `Work()` many times per pattern tick (once per audio buffer).
Timing advances exactly once per tick, detected via `IBuzzMachineHost.MasterInfo.PosInTick`
resetting to zero.

Swing uses integer `ArpTicks` with rounded `longTicks`/`shortTicks` values
summing exactly to `2 × Speed`. `ArpStepParity` toggles strictly between 0
and 1.

Oct Walk builds only base-octave chord tones when active, applying `OctOffset`
at fire time. The octave advances in `AdvOct()` whenever the note index wraps,
keeping note cycling and octave cycling fully independent.

Velocity is delivered best-effort: the target's velocity/volume parameter is
found by name ("Volume", "Velocity", "Vol", "Vel") or by position (parameter
immediately after the note param in the same track group).

`IBuzzMachine.Tick()` is **not called** for managed machines — all per-tick
logic lives in `Work()`.
