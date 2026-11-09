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
    }

    /// <summary>Which world an obstacle lives in. Phased runners (Both hold) ignore Solid and collide with Ghost.</summary>
    public enum ObstacleLayer { Solid, Ghost }

    public enum ObstacleOutcome { Pass, Hit, Break }

    public struct ObstacleInfo
    {
        public ObstacleKind Kind;
        public ObstacleLayer Layer;
        /// <summary>-1 left, 0 middle, 1 right. Ignored when AllLanes.</summary>
        public int Lane;
        public bool AllLanes;
        /// <summary>Song time (s) at which the runner reaches this obstacle.</summary>
        public double Time;
    }

    public sealed class RunnerRules
    {
        /// <summary>How long a jump keeps the runner in the air (s).</summary>
        public double JumpSeconds;
        /// <summary>Alternate streak needed for one charge.</summary>
        public int ChargeTaps;

        public void Validate()
        {
            if (JumpSeconds <= 0) throw new ArgumentException("JumpSeconds must be > 0");
            if (ChargeTaps < 2) throw new ArgumentException("ChargeTaps must be >= 2");
        }
    }

    /// <summary>
    /// Engine-free runner state over song time, built from gesture events (Docs/RUNNER.md).
    /// Decision A (John, day 10): every action counts from its key-down time, even though taps and holds are
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
        private readonly List<double> _jumps = new List<double>();
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
                case GestureType.BothTap: _jumps.Add(time); break;
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
            foreach (double start in _jumps)
                if (time >= start && time <= start + _rules.JumpSeconds) return true;
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
        public ObstacleOutcome Resolve(ObstacleInfo o)
        {
            double t = o.Time;
            // A lane change is "in between" from key-down to key-up: safe if either the lane being left or the lane
            // being entered is clear (day 11 fix: moving back just before a block, released after it, looked fine
            // on screen but failed).
            if (!o.AllLanes && (LaneAt(t) != o.Lane || RecognizedLaneAt(t) != o.Lane)) return ObstacleOutcome.Pass;

            bool phased = IsPhased(t);
            if (o.Layer == ObstacleLayer.Solid && phased) return ObstacleOutcome.Pass;
            if (o.Layer == ObstacleLayer.Ghost && !phased) return ObstacleOutcome.Pass;

            switch (o.Kind)
            {
                case ObstacleKind.Low: return IsSliding(t) ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Gate: return IsSprinting(t) ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Gap: return IsAirborne(t) ? ObstacleOutcome.Pass : ObstacleOutcome.Hit;
                case ObstacleKind.Wall:
                    foreach (var c in _charges)
                    {
                        if (c.Used || c.Time > t) continue;
                        c.Used = true;
                        return ObstacleOutcome.Break;
                    }
                    return ObstacleOutcome.Hit;
                default: return ObstacleOutcome.Hit; // Block
            }
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
