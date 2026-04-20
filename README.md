# Pedal Chord v1.2 — ReBuzz Managed Controller Machine

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

| Parameter   | Range   | Description |
|-------------|---------|-------------|
| **Note**    | C-0–B-9 | Root note. Standard Buzz piano keyboard (z/s/x/d/…). |
| **Velocity**| 1–127   | Note velocity sent to the target machine. |
| **Chord**   | 0–50    | Chord type — 51 chords. Right-click → *Chord Reference…* for the full list with hex values. |
| **Mode**    | 0–5     | Chord / Arp Up / Arp Down / Arp Up+Down / Arp Down+Up / Arp Random |
| **Speed**   | 1–1024  | Pattern ticks between arpeggio steps (1 tick = 1 pattern row). |
| **Length**  | 1–64    | Note duration in ticks before auto note-off. |
| **Octaves** | 1–4     | Octave spread — arp note list duplicated upward over 1–4 octaves. |
| **Swing**   | 0–100   | Shuffle amount. 0 = straight, 100 ≈ 2:1 triplet shuffle. |
| **Swing On**| 0–1     | Which beat of the alternating pair gets the long wait (0 = 1st, 1 = 2nd). |
| **Humanize**| 0–100   | Random ±timing drift per arp step, scales with Speed. |
| **Hum. Vel**| 0–100   | Random ±velocity variation per arp step, scales with Velocity. |
| **Arp Reset**| 0–1   | Write 1 to restart the arp sequence from the first note on this step. |

---

## Speed and timing

Speed is measured in **pattern ticks** (rows) and is exact regardless of BPM
or audio buffer size. Speed=2 in a 32-tick loop gives exactly 16 triggers.

---

## Swing

Swing alternates between a long and short wait using integer tick counts:

```
longTicks  = Round(2 × Speed × ratio / (ratio + 1))
shortTicks = 2 × Speed − longTicks
```

`long + short = 2 × Speed` always, so average tempo is locked regardless of
swing amount. Effective swing granularity increases with Speed — at Speed=2
only Swing=100 produces an audible effect (3:1 ratio); at Speed=4+ the full
0–100 range gives meaningfully distinct feels.

**Swing On** shifts which beat of the pair gets the long wait, useful for
landing the shuffle accent on a specific chord tone.

---

## Humanize

**Humanize** adds random ±timing drift to each arp step:
- Drift range = `±Round(Speed × Humanize / 200)` ticks
- At Speed=8, Humanize=50: ±2 ticks per step
- Non-cumulative — varies around the swing-adjusted base, so tempo never drifts

**Hum. Vel** adds random ±velocity variation to each arp step:
- Drift range = `±Round(Velocity × HumanizeVel / 200)`
- At Velocity=100, Hum. Vel=50: ±25 per step, clamped to [1, 127]

Both humanize controls only affect arp modes — chord mode fires all notes on
the original note trigger with no timing drift.

---

## Arp Reset

Write `01` into the **Arp Reset** column on any pattern row to restart the arp
sequence from the first note (or last note for Down/Down+Up modes) at that
point. The reset fires on the next tick after the row, so the restart is
immediate.

`Arp Reset` is stateless — it only fires when explicitly written. Use it to
snap a free-running arp back to note 1 at bar boundaries, or to create accent
patterns by forcing the arp back to the root at specific moments.

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

## Changelog

### v1.2
- **Velocity** track parameter (1–127) — sets note velocity on the target
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
resetting to zero. Notes and parameter changes are delivered by ReBuzz via
`IBuzzMachineHost.Tick()` before the first `Work()` call of each tick.

Swing uses integer `ArpTicks` with rounded `longTicks`/`shortTicks` values that
sum exactly to `2 × Speed`, ensuring tempo is locked at all swing amounts and
speed values. `ArpStepParity` toggles strictly between 0 and 1.

Velocity is delivered best-effort: the target's velocity/volume parameter is
found by name ("Volume", "Velocity", "Vol", "Vel") or by position (parameter
immediately after the note param in the same track group).

`IBuzzMachine.Tick()` is **not called** for managed machines — all per-tick
logic lives in `Work()`.
