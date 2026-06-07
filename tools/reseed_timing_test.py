#!/usr/bin/env python3
"""
Regression test for the arp re-seed off-by-one (§10.5).

Faithfully models PedalChord's Work()/Start()/StepArp()/PendingReset countdown
(step units = sub-ticks when sub-tick timing is active, else whole ticks) and
checks that re-seeding the arp mid-stream — via a NoteOn or via Arp Reset —
produces the same gap sequence as a free-running arp.

  * `guard=True`  models the shipped fix (_firedThisStep gate).
  * `guard=False` + `seed_returns=False` models the pre-fix fault (seed StepArp
    and the per-step decrement both consume the seed edge) and is expected to
    FAIL, proving the test has teeth.

Matrix: Speed in {2,4,8}, Swing in {0,50}, R in {1,8}, re-seed every {16,32}.
Exit code is non-zero if any guarded case fails.
"""
import math, sys

def cround(x):                       # banker's rounding, matches Math.Round
    f = math.floor(x); d = x - f
    if abs(d - 0.5) < 1e-9: return int(f if f % 2 == 0 else f + 1)
    return int(math.floor(x + 0.5))

class Arp:
    def __init__(self, speed, swing, R, swingon=0, guard=True, seed_returns=True):
        self.speed, self.swing, self.R = speed, swing, R
        self.swingon, self.guard, self.seed_returns = swingon, guard, seed_returns
        self.active = False; self.ArpTicks = 0; self.parity = 0
        self.has_new = False; self.pending = 0; self.pending_reset = False
        self.fired_this_step = False
        self.fires = []              # absolute step-unit index of each fire

    def stepArp(self, ai):
        self.fires.append(ai); self.fired_this_step = True
        R = max(1, self.R); period = 2 * self.speed * R
        ratio = 1.0 + self.swing / 100.0
        longT = cround(period * ratio / (ratio + 1.0))
        shortT = max(1, period - longT); longT = max(1, period - shortT)
        isLong = ((self.parity + self.swingon) % 2 == 0)
        baseT = longT if isLong else shortT
        self.parity = 1 - self.parity
        self.ArpTicks = max(1, baseT)            # Humanize 0

    def start(self, ai):
        self.active = True; self.ArpTicks = 0; self.parity = 0
        self.stepArp(ai)                         # Mode != 0

    def work(self, ai, newTick, newStep):
        if newStep and self.guard:
            self.fired_this_step = False
        if self.has_new:
            self.has_new = False
            if self.pending > 0:
                self.start(ai)
            if self.seed_returns:
                return
        if self.pending_reset and newTick and self.active:
            self.pending_reset = False; self.ArpTicks = 1
        else:
            self.pending_reset = False
        if not self.active:
            return
        gate = (not self.fired_this_step) if self.guard else True
        if newStep and gate and self.ArpTicks > 0:
            self.ArpTicks -= 1
            if self.ArpTicks == 0:
                self.stepArp(ai)

def run(speed, swing, R, ticks, *, note_every=10**9, reset_every=10**9,
        guard=True, seed_returns=True):
    a = Arp(speed, swing, R, guard=guard, seed_returns=seed_returns)
    ai = 0
    for t in range(ticks):
        note  = (t % note_every  == 0)            # NoteOn at top of bar (t=0 too)
        reset = (t > 0 and t % reset_every == 0)  # explicit Arp Reset, mid-stream
        for sidx in range(R):
            nt = (sidx == 0)
            if nt and note:  a.has_new = True; a.pending = 60
            if nt and reset: a.pending_reset = True
            a.work(ai, nt, True)
            ai += 1
    fires = [round(f / R, 4) for f in a.fires]    # back to tick units
    return fires

def gaps_after(fires, every):
    out = []
    for seed in range(0, int(fires[-1]) + 1, every):
        nxt = [f for f in fires if f > seed + 1e-9]
        if nxt: out.append(round(nxt[0] - seed, 4))
    return out

def check(guard):
    failures = []
    for R in (1, 8):
        for speed in (2, 4, 8):
            for swing in (0, 50):
                free = run(speed, swing, R, 256, guard=guard)
                free_gaps = {round(free[i+1]-free[i], 4) for i in range(len(free)-1)}
                expect = {float(speed)} if swing == 0 else free_gaps
                for label, kw in (("NoteOn", "note_every"), ("ArpReset", "reset_every")):
                    for every in (16, 32):
                        seq = run(speed, swing, R, 256, guard=guard, **{kw: every})
                        seedgaps = set(gaps_after(seq, every))
                        bad = [g for g in seedgaps if g not in expect]
                        cnt_ok = abs(len(seq) - len(free)) <= 2
                        if bad or not cnt_ok:
                            failures.append(
                                f"R{R} Speed{speed} Sw{swing} {label}/{every}: "
                                f"seedgaps={sorted(seedgaps)} expect~{sorted(expect)} "
                                f"count {len(seq)} vs free {len(free)}")
    return failures

if __name__ == "__main__":
    print("Guarded (shipped fix) — expect PASS:")
    fg = check(guard=True)
    print("  " + ("PASS" if not fg else "FAIL\n   " + "\n   ".join(fg)))

    # Control: pre-fix fault must be caught (seed falls through, no guard).
    print("Unguarded fault model — expect FAIL (proves the test bites):")
    a = run(8, 0, 1, 40, note_every=32, guard=False, seed_returns=False)
    caught = a[:4] == [0.0, 7.0, 15.0, 23.0]
    print(f"  reseed fires={a[:5]} -> {'fault reproduced' if caught else 'NOT reproduced'}")

    sys.exit(1 if fg else 0)
