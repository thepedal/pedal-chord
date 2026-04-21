# Pedal Chord v1.3 — ReBuzz Managed Controller Machine

A chord and arpeggio trigger for ReBuzz. Write a root note into Pedal Chord's
pattern and it fires the full chord (or arpeggiated notes) on any target
generator machine in your song. Up to 16 independent voices, each routable to
a different target machine.

---

## Quick start

1. **Add to machine view** — drag from the Generators tab.
2. **Connect targets** — right-click Pedal Chord → *Target Settings…*
   Set the target machine and base track for each voice you want to use.
3. **Write notes** — open a pattern on Pedal Chord. Each horizontal track row
   is an independent voice. Write root notes in the Note column (z=C-4,
   s=C#-4, x=D-4 …).
4. **Press Play** — each voice fires its chord or arpeggio on its target.

Target machines must be connected to the Master (or an effect chain reaching
Master) so their audio is heard.

---

## Track parameters

| Parameter    | Range    | Description |
|--------------|----------|-------------|
| **Note**     | C-0–B-9  | Root note. Standard Buzz piano keyboard. |
| **Velocity** | 1–127    | Note velocity sent to the target machine. |
| **Chord**    | 0–50     | Chord type. Right-click → *Chord Reference…* for full list with hex values. |
| **Mode**     | 0–5      | Chord / Arp Up / Arp Down / Arp Up+Down / Arp Down+Up / Arp Random |
| **Speed**    | 1–1024   | Pattern ticks between arpeggio steps (1 tick = 1 pattern row). |
| **Length**   | 0–16384  | Note duration in ticks. 0 = no auto note-off (sustain until next note). |
| **Octaves**  | 1–4      | Octave range for Oct Walk; or full pre-expanded span when Oct Walk is Off. |
| **Step**     | 1–8      | Chord tones advanced per arp step. |
| **Oct Walk** | 0–2      | Off / Up / Ping-pong — octave cycling after each chord cycle. |
| **Swing**    | 0–100    | Shuffle amount. 0 = straight, 100 ≈ 2:1 triplet shuffle. |
| **Swing On** | 0–1      | Which beat gets the long wait (0 = 1st, 1 = 2nd). |
| **Humanize** | 0–100    | Random ±timing drift per arp step, scales with Speed. |
| **Hum. Vel** | 0–100    | Random ±velocity variation per arp step, scales with Velocity. |
| **Arp Reset**| 0–1      | Write 1 to restart the arp sequence from the first note on this step. |

---

## Speed and timing

Speed is in **pattern ticks** (rows) — exact regardless of BPM or buffer size.
Speed=2 in a 32-tick loop gives exactly 16 triggers.

---

## Swing

```
longTicks  = Round(2 × Speed × ratio / (ratio + 1))
shortTicks = 2 × Speed − longTicks
```

`long + short = 2 × Speed` always — average tempo locked regardless of swing.
Swing granularity increases with Speed; at Speed=2, Swing=100 gives a 3:1
ratio; at Speed=4+ the full 0–100 range is useful.

---

## Step and Oct Walk

**Step** advances the arp note index by N chord tones per tick. All modes
(Up, Down, Ping-pong) respect Step.

**Oct Walk** cycles the octave independently of note position after each
complete chord cycle — Off / Up (wraps) / Ping-pong. The Octaves parameter
becomes the octave range when Oct Walk is active.

---

## Humanize

**Humanize** adds random ±timing drift per arp step:
`±Round(Speed × Humanize / 200)` ticks, non-cumulative.

**Hum. Vel** adds random ±velocity variation:
`±Round(Velocity × HumanizeVel / 200)`, clamped to [1, 127].

Both only affect arp modes — chord mode fires on the original trigger tick.

---

## Arp Reset

Write `01` to restart the arp sequence from the first note at that step.
Stateless — only fires when explicitly written.

---

## Chord types

51 chords (hex 00–32). Right-click → **Chord Reference…** for the full table.

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

Arp mode fires on a single base track (monophonic on the target). Chord mode
uses consecutive target tracks (Base, Base+1, … one per chord tone).

---

## Per-voice target settings

Each voice routes independently to a different target machine and base track.
Right-click → *Target Settings…* to configure. Right-click → *Diagnostics…*
to verify routing (writes to the ReBuzz debug console).

---

## Build from source

Requirements: .NET 10 SDK, ReBuzz at `C:\Program Files\ReBuzz`.

```powershell
dotnet build PedalChord.csproj -c Release
# Non-default location:
dotnet build PedalChord.csproj -c Release /p:BuzzDir="D:\ReBuzz"
```

Output: `<BuzzDir>\Gear\Generators\Pedal Chord.NET.dll`

---

## Changelog

### v1.3
- **Step** (1–8) — advance N chord tones per arp step
- **Oct Walk** (Off/Up/Ping-pong) — independent octave cycling
- **Multi-voice simultaneous trigger fix** — up to 16 voices can now all fire
  at the same pattern tick position. Workaround for a ReBuzz engine bug where
  `machine.parametersChanged` (a `Dictionary<IParameter,int>`) can only hold
  one track index per parameter — simultaneous multi-track notes overwrite each
  other. Fixed by polling `ParameterCore.pvalues` (a `ConcurrentDictionary`)
  directly via cached field reflection inside `SetNote`.
- **Length** expanded to 0–16384 ticks; 0 = no auto note-off

### v1.2
- **Velocity** (1–127), **Humanize** (0–100), **Hum. Vel** (0–100), **Arp Reset**

### v1.1
- **Swing** (0–100), **Swing On** (0/1)
- Fixed: Arp Random (Mode=5) was unreachable
- Chord Reference shows hex values for pattern entry

### v1.0
- Initial release: 51 chords, 6 arp modes, 16 voices, per-voice target routing

---

## Architecture notes

Control machine (`void Work()`) — ticked first in the audio graph each buffer.
Timing is tick-accurate via `IBuzzMachineHost.MasterInfo.PosInTick`.

**Multi-voice delivery workaround:** ReBuzz's `machine.parametersChanged`
maps `IParameter → int (track)`. All 16 voice tracks share one Note
`IParameter`, so simultaneous notes overwrite. Inside `SetNote`, the machine
polls `ParameterCore.pvalues` (a `ConcurrentDictionary<int,int>`) for all
other tracks via a field reference obtained once by reflection. `pvalues` holds
freshly-delivered values before the post-Tick reset, so all simultaneous tracks
are recovered. The reflection lookup is cached; the hot path is a single
`ConcurrentDictionary.TryGetValue` per track — allocation-free.

Swing uses integer `longTicks`/`shortTicks` summing to `2 × Speed`.
`ArpStepParity` toggles 0↔1 — never grows.

`IBuzzMachine.Tick()` is not called for managed machines — all logic is in
`Work()`.
