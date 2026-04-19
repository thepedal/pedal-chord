# Pedal Chord — ReBuzz Managed Controller Machine

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
| **Chord** | 0–15 | Chord type (see below). |
| **Mode** | 0–4 | Chord / ArpUp / ArpDown / ArpUpDown / ArpRandom |
| **Speed** | 1–32 | Ticks between arpeggio steps. |
| **Length** | 1–64 | Note duration in ticks before auto note-off. |
| **Octaves** | 1–4 | How many octaves the arpeggio spans upward. |

### Chord types (Chord parameter)

| Value | Name | Value | Name |
|-------|------|-------|------|
| 0 | Major | 8 | Diminished |
| 1 | Minor | 9 | Dim7 |
| 2 | Dom7 | 10 | Major 6 |
| 3 | Major 7 | 11 | Minor 6 |
| 4 | Minor 7 | 12 | Add9 |
| 5 | Sus2 | 13 | Major 9 |
| 6 | Sus4 | 14 | Minor 9 |
| 7 | Augmented | 15 | Power (5th) |

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
ReBuzz calls `Work()` once per tick after delivering pattern data via the
parameter setters (`SetNote`, `SetChord`, …). Notes are forwarded to the target
by calling `IParameter.SetValue()` on the target's note parameter, followed by
`IMachine.SendControlChanges()` to ensure the target's audio engine processes
the value within the same tick.

`IBuzzMachine.Tick()` is **not called** by ReBuzz for managed machines —
all per-tick logic lives in `Work()`.
