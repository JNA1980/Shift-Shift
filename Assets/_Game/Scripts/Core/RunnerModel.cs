using System;
using System.Collections.Generic;

namespace ShiftShift.Core
{
    public enum ObstacleKind
    {
        Block, // full-height block in a lane: avoid by changing lane
        Low,   // low bar: slide under (Hold L)
        Gate,  // speed gate: sprint through it (Hold R)
        Gap,   // missing floor: be in the air (Both tap)
        Wall,  // breakable wall: needs a charge (Alternate x ChargeTaps)
        Barrier, // phase barrier: only passable while phased (Both hold). Added last: kinds are saved as numbers.
        Hurdle,  // short wall: jump it (Both tap). The tall Wall stays charge-only (ELIA, day 13).
    }

    /// <summary>
    /// Which world an obstacle lives in. Ghost obstacles only hit a phased runner. Solid obstacles hit whether phased or
    /// not (day 13 fix: phase used to pass every Solid obstacle, so holding both skipped the level); only a Barrier
    /// is passed by phasing.
    /// </summary>
    public enum ObstacleLayer { Solid, Ghost }

    public enum ObstacleOutcome { Pass, Hit, Break }

    /// <summary>Outcome plus the key-down time of the gesture that answered the obstacle (NaN if none was needed).</summary>
    public readonly struct ObstacleResolution
    {
        public readonly ObstacleOutcome Outcome;
        public readonly double AnswerTime;

        public ObstacleResolution(ObstacleOutcome outcome, double answerTime)
        {
            Outcome = outcome;
            AnswerTime = answerTime;
        }

        public bool HasAnswer => !double.IsNaN(AnswerTime);
    }

    public struct ObstacleInfo
    {
        public ObstacleKind Kind;
        public ObstacleLayer Layer;
        /// <summary>-1 left, 0 middle, 1 right. Ignored when AllLanes.</summary>
        public int Lane;
        public bool AllLanes;
        /// <summary>
        /// The obstacle blocks every lane EXCEPT <see cref="Lane"/>: the runner must be in that lane (gate opening,
        /// free lane of a wide bar). ELIA, day 13.
        /// </summary>
        public bool LaneIsOpening;
        /// <summary>Song time (s) at which the runner reaches this obstacle.</summary>
        public double Time;
    }

    public sealed class RunnerRules
    {
        /// <summary>How long a jump keeps the runner in the air (s).</summary>
        public double JumpSeconds;
        /// <summary>Alternate streak needed for one charge.</summary>
        public int ChargeTaps;
        /// <summary>A charge completed this long after a wall's moment still breaks it (the Good window).</summary>
        public double ChargeGraceSeconds;

        public void Validate()
        {
            if (JumpSeconds <= 0) throw new ArgumentException("JumpSeconds must be > 0");
            if (ChargeTaps < 2) throw new ArgumentException("ChargeTaps must be >= 2");
            if (ChargeGraceSeconds < 0) throw new ArgumentException("ChargeGraceSeconds must be >= 0");
        }
    }

    /// <summary>
    /// Engine-free runner state over song time, built from gesture events (Docs/RUNNER.md).
    /// Decision A (ELIA, day 10): every action counts from its key-down time, even though taps and holds are
    /// only recognised later. So state is a timeline that can be queried at any past song time, and an obstacle
    /// is judged by the state at the moment the runner reaches it.
    /// Lanes: three lanes, -1..1, start in the middle.
    /// </summary>
    public sealed class RunnerModel
    {
        private struct LaneChange { public double Time; public double RecognizedAt; public int Delta; }
        private struct Interval { public double Start; public double End; }
        private sealed class Charge { public double Time; public bool Used; }

        private readonly RunnerRules _rules;
        private readonly List<LaneChange> _laneChanges = new List<LaneChange>();
        private struct Jump { public double Start; public double End; }
        private readonly List<Jump> _jumps = new List<Jump>();
        private readonly List<Charge> _charges = new List<Charge>();
        private readonly Dictionary<GestureType, List<Interval>> _holds = new Dictionary<GestureType, List<Interval>>
        {
            { GestureType.HoldLeft, new List<Interval>() },
            { GestureType.HoldRight, new List<Interval>() },
            { GestureType.BothHold, new List<Interval>() },
        };

        public RunnerModel(RunnerRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _rules.Validate();
        }

        // ---------- input ----------

        /// <param name="time">Song time of the key-down (what the gesture is judged by).</param>
        /// <param name="alternateStreak">ShiftInput.AlternateStreak at the time the event fired.</param>
        /// <param name="recognizedAt">Song time the gesture was recognised (key-up for taps). Defaults to <paramref name="time"/>.</param>
        public void OnGestureFired(GestureType type, double time, int alternateStreak, double recognizedAt = double.NaN)
        {
            if (double.IsNaN(recognizedAt) || recognizedAt < time) recognizedAt = time;
            switch (type)
            {
                case GestureType.TapLeft: AddLaneChange(time, recognizedAt, -1); break;
                case GestureType.TapRight: AddLaneChange(time, recognizedAt, +1); break;
                // Airborne from key-down (decision A) until the visual landing (recognised + jump), so the rules never
                // land the runner before the screen does (day 13: a gap failed while the capsule was still in the air).
                case GestureType.BothTap: _jumps.Add(new Jump { Start = time, End = recognizedAt + _rules.JumpSeconds }); break;
                case GestureType.Alternate:
                    if (alternateStreak > 0 && alternateStreak % _rules.ChargeTaps == 0)
                        _charges.Add(new Charge { Time = time });
                    break;
            }
        }

        public void OnHoldStarted(GestureType type, double time)
        {
            if (_holds.TryGetValue(type, out var list))
                list.Add(new Interval { Start = time, End = double.PositiveInfinity });
        }

        public void OnHoldEnded(GestureType type, double time)
        {
            if (!_holds.TryGetValue(type, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!double.IsPositiveInfinity(list[i].End)) continue;
                list[i] = new Interval { Start = list[i].Start, End = time };
                return;
            }
        }

        // ---------- state at a song time ----------

        /// <summary>Lane counting every lane change from its key-down (decision A).</summary>
        public int LaneAt(double time)
        {
            int lane = 0;
            foreach (var c in _laneChanges)
            {
                if (c.Time > time) break; // list is kept sorted by key-down time
                lane = Math.Max(-1, Math.Min(1, lane + c.Delta));
            }
            return lane;
        }

        /// <summary>Lane counting lane changes only once recognised (key-up): where the runner is shown.</summary>
        public int RecognizedLaneAt(double time)
        {
            int lane = 0;
            foreach (var c in _laneChanges)
            {
                if (c.Time > time) break;
                if (c.RecognizedAt > time) continue;
                lane = Math.Max(-1, Math.Min(1, lane + c.Delta));
            }
            return lane;
        }

        public bool IsSliding(double time) => HoldActive(GestureType.HoldLeft, time);
        public bool IsSprinting(double time) => HoldActive(GestureType.HoldRight, time);
        public bool IsPhased(double time) => HoldActive(GestureType.BothHold, time);

        public bool IsAirborne(double time)
        {
            foreach (Jump j in _jumps)
                if (time >= j.Start && time <= j.End) return true;
            return false;
        }

        /// <summary>An unused charge earned at or before this time.</summary>
        public bool HasCharge(double time)
        {
            foreach (var c in _charges)
                if (!c.Used && c.Time <= time) return true;
            return false;
        }

        // ---------- obstacles ----------

        /// <summary>
        /// Judges the runner against an obstacle at the moment it is reached. A Break uses up one charge.
        /// Call in obstacle-time order, and only once no key-down at or before obstacle.Time is still undecided.
        /// </summary>
        public ObstacleOutcome Resolve(ObstacleInfo o) => ResolveWithAnswer(o).Outcome;

        /// <summary>
        /// Like <see cref="Resolve"/>, plus which gesture answered it, for the Judge:
        /// bar = the lane change out of its lane; low/gate/phase barrier = start of the slide/sprint/phase hold covering
        /// it; gap = the jump; wall = the tap that earned the charge. NaN when the runner never needed to act
        /// (e.g. a bar in another lane).
        /// </summary>
        public ObstacleResolution ResolveWithAnswer(ObstacleInfo o)
        {
            ObstacleOutcome outcome = ResolveOutcome(o, out double answer);
            return new ObstacleResolution(outcome, outcome == ObstacleOutcome.Hit ? double.NaN : answer);
        }

        private ObstacleOutcome ResolveOutcome(ObstacleInfo o, out double answer)
        {
            answer = double.NaN;
            double t = o.Time;
            // A lane change is "in between" from key-down to key-up: either lane counts, whichever is safe (day 11
            // fix: moving back just before a block, released after it, looked fine on screen but failed).
            if (o.LaneIsOpening)
            {
                // Must be in the opening (either lane of a change in progress may be it).
                if (LaneAt(t) != o.Lane && RecognizedLaneAt(t) != o.Lane) return ObstacleOutcome.Hit;
                if (o.Kind == ObstacleKind.Block)
                {
                    answer = LastMoveInto(o.Lane, t); // wide bar: the move into its free lane
                    return ObstacleOutcome.Pass;
                }
                // Gate: in the opening; the kind rule below still needs the sprint.
            }
            else if (!o.AllLanes)
            {
                answer = LastDodgeFrom(o.Lane, t);
                if (LaneAt(t) != o.Lane || RecognizedLaneAt(t) != o.Lane) return ObstacleOutcome.Pass;
            }

            answer = double.NaN;
            bool phased = IsPhased(t);
            if (o.Layer == ObstacleLayer.Ghost && !phased) return ObstacleOutcome.Pass;

            // Every obstacle has exactly one answer (playbook 1.4); phase is the answer only for a Barrier.
            switch (o.Kind)
            {
                case ObstacleKind.Barrier:
                    answer = HoldStartCovering(GestureType.BothHold, t);
                    return phased ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Low:
                    answer = HoldStartCovering(GestureType.HoldLeft, t);
                    return IsSliding(t) ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Gate:
                    answer = HoldStartCovering(GestureType.HoldRight, t);
                    return IsSprinting(t) ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Gap:
                case ObstacleKind.Hurdle:
                    answer = JumpCovering(t);
                    return IsAirborne(t) ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Wall:
                    // A last tap slightly after the wall's click still counts (day 13: aiming the last tap at the
                    // click failed half the time when it landed a little late).
                    foreach (var c in _charges)
                    {
                        if (c.Used || c.Time > t + _rules.ChargeGraceSeconds) continue;
                        c.Used = true;
                        answer = c.Time;
                        return ObstacleOutcome.Break;
                    }
                    return ObstacleOutcome.Hit;
                default: return ObstacleOutcome.Hit; // Block
            }
        }

        /// <summary>Key-down of the last lane change at or before t that left <paramref name="lane"/>; NaN if none.</summary>
        private double LastDodgeFrom(int lane, double t)
        {
            double found = double.NaN;
            int current = 0;
            foreach (var c in _laneChanges)
            {
                if (c.Time > t) break;
                int before = current;
                current = Math.Max(-1, Math.Min(1, current + c.Delta));
                if (before == lane && current != lane) found = c.Time;
            }
            return found;
        }

        /// <summary>Key-down of the last lane change at or before t that entered <paramref name="lane"/>; NaN if none.</summary>
        private double LastMoveInto(int lane, double t)
        {
            double found = double.NaN;
            int current = 0;
            foreach (var c in _laneChanges)
            {
                if (c.Time > t) break;
                int before = current;
                current = Math.Max(-1, Math.Min(1, current + c.Delta));
                if (before != lane && current == lane) found = c.Time;
            }
            return found;
        }

        private double HoldStartCovering(GestureType type, double t)
        {
            foreach (var i in _holds[type])
                if (t >= i.Start && t < i.End) return i.Start;
            return double.NaN;
        }

        private double JumpCovering(double t)
        {
            foreach (Jump j in _jumps)
                if (t >= j.Start && t <= j.End) return j.Start;
            return double.NaN;
        }

        private bool HoldActive(GestureType type, double time)
        {
            foreach (var i in _holds[type])
                if (time >= i.Start && time < i.End) return true;
            return false;
        }

        private void AddLaneChange(double time, double recognizedAt, int delta)
        {
            // Taps are reported on key-up but timed at key-down, so keep the list sorted by key-down time.
            int idx = _laneChanges.Count;
            while (idx > 0 && _laneChanges[idx - 1].Time > time) idx--;
            _laneChanges.Insert(idx, new LaneChange { Time = time, RecognizedAt = recognizedAt, Delta = delta });
        }
    }
}
