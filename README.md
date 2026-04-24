# Pedal Chord v1.7 — ReBuzz Managed Generator Controller Machine

A chord and arpeggio trigger for ReBuzz. Write a root note in the pattern and
Pedal Chord fires the full chord (or arpeggiated notes) on any target generator
in your song. One instance controls one generator — use multiple instances to
control multiple generators simultaneously.

---

## Quick start

1. **Add to machine view** — drag from the Generators tab.
2. **Connect a target** — right-click → *Target Settings…* Select the generator
   and base track to use.
3. **Write notes** — open a pattern and enter root notes in the Note column.
4. **Press Play** — the chord fires on the target on every row that has a note.

The target machine must be connected to the Master (or an effect chain reaching
Master) so its audio is heard.

---

## Track parameters

| Parameter    | Range    | Description |
|--------------|----------|-------------|
| **Note**     | C-0–B-9  | Root note. Standard Buzz piano keyboard. |
| **Velocity** | 1–127    | Note velocity sent to the target machine. |
| **Chord**    | 0–50     | Chord type. Right-click → *Chord Reference…* for full list with hex values. |
| **Mode**     | 0–5      | Chord / Arp Up / Arp Down / Arp Up+Down / Arp Down+Up / Arp Random |
| **Speed**    | 1–1024   | Pattern ticks between arpeggio steps (1 tick = 1 pattern row). |
| **Length**   | 0–16384  | Note duration in ticks. 0 = no auto note-off. |
| **Octaves**  | 1–4      | Octave range for Oct Walk; or pre-expanded span when Oct Walk is Off. |
| **Step**     | 1–8      | Chord tones advanced per arp step. |
| **Oct Walk** | 0–2      | Off / Up / Ping-pong — octave cycling after each chord cycle. |
| **Swing**    | 0–100    | Shuffle. 0 = straight, 100 ≈ 2:1 triplet shuffle. |
| **Swing On** | 0–1      | Which beat gets the long wait (0 = 1st, 1 = 2nd). |
| **Humanize** | 0–100    | Random ±timing drift per arp step, scales with Speed. |
| **Hum. Vel** | 0–100    | Random ±velocity variation per arp step, scales with Velocity. |
| **Arp Reset**| 0–1      | Write 1 to restart the arp sequence from the first note on this step. |

---

## Speed and timing

Speed is in **pattern ticks** (rows) — exact regardless of BPM or buffer size.

---

## Swing

Long and short waits alternate, summing to exactly `2 × Speed`:
```
longTicks  = Round(2 × Speed × ratio / (ratio + 1))
shortTicks = 2 × Speed − longTicks
```
Average tempo is locked at all swing values.

---

## Step and Oct Walk

**Step** advances the arp index by N chord tones per tick rather than 1.

**Oct Walk** cycles the octave independently after each full chord cycle.
**Octaves** sets the range when Oct Walk is active.

---

## Arpeggio modes

| Value | Name         | Pattern |
|-------|--------------|---------|
| 0     | Chord        | All notes simultaneously |
| 1     | Arp Up       | Low → High, wraps |
| 2     | Arp Down     | High → Low, wraps |
| 3     | Arp Up+Down  | Ping-pong, starts ascending |
| 4     | Arp Down+Up  | Ping-pong, starts descending |
| 5     | Arp Random   | Random note each step |

Arp mode fires on a single target track. Chord mode uses consecutive tracks
(Base, Base+1… one per chord tone).

---

## Build from source

Requirements: .NET 10 SDK, ReBuzz at `C:\Program Files\ReBuzz`.

```powershell
dotnet build PedalChord.csproj -c Release
dotnet build PedalChord.csproj -c Release /p:BuzzDir="D:\ReBuzz"
```

Output: `<BuzzDir>\Gear\Generators\Pedal Chord.NET.dll`

---

## Changelog

### v1.5
- **Architecture simplified to single-voice/single-target** — one Pedal Chord
  instance controls one generator. For multiple generators, use multiple
  instances. Eliminates all multi-voice complexity and instability.
- All reflection hacks (`pvalues`, `VoiceCache`, `TryInitPValues`) removed.
- `MaxTracks = 1` — clean single pattern track.
- Target settings simplified: one machine name + base track.
- Performance: target machine + parameters cached, invalidated on settings
  change and every 100 ticks.

### v1.4
- **Step** (1–8), **Oct Walk** (Off/Up/Ping-pong)
- Multi-voice simultaneous trigger fix (superseded by v1.5 simplification)
- Length 0–16384 (0 = sustain)

### v1.3
- **Swing**, **Swing On**
- Fixed Arp Random (Mode=5)
- Chord Reference shows hex values

### v1.2
- **Velocity**, **Humanize**, **Hum. Vel**, **Arp Reset**

### v1.1
- Swing timing (superseded)

### v1.0
- Initial release: 51 chords, 6 arp modes

---

## Architecture notes

Control machine (`void Work()` with no parameters) — ticked first each audio
buffer. Timing is tick-accurate via `MasterInfo.PosInTick`.

Target machine and note parameter are cached after first use. Cache invalidates
on settings change and every 100 ticks. `IBuzzMachine.Tick()` is not called for
managed machines — all logic is in `Work()`.
