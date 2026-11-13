using System;

namespace ShiftShift.Core
{
    /// <summary>
    /// Turns raw Left/Right Shift key-down/key-up events into the seven gestures.
    /// Engine-free: the caller supplies timestamps (song time, seconds) and must call
    /// <see cref="Update"/> every frame so holds are detected while keys are still down.
    /// Rules and reasoning: Docs/INPUT.md.
    ///
    /// Events (all times are song time in seconds):
    /// - GestureFired(type, t): once per gesture. Taps, holds and both-gestures report the key-down time.
    /// - HoldStarted(type, t) / HoldEnded(type, t): for HoldLeft, HoldRight, BothHold. Start reports key-down time,
    ///   end reports key-up time.
    /// - Alternate: GestureFired(Alternate, t) after each tap that continues an L/R streak (streak >= 2): sides
    ///   alternate and each key-down is within AlternateMaxGap of the previous one. No beat needed (ELIA, day 13).
    ///   Read <see cref="AlternateStreak"/> for the count (Charge Taps = charge).
    /// </summary>
    public sealed class GestureClassifier
    {
        private struct KeyState
        {
            public bool IsDown;
            public double DownTime;
            public bool HoldStarted;
            /// <summary>Key is part of a both-gesture or pressed during lockout; its own release emits nothing.</summary>
            public bool Consumed;
        }

        private enum BothState { None, Pending, Holding }

        private readonly GestureThresholds _t;
        private readonly IClock _clock;
        private KeyState _left, _right;
        private BothState _both;
        private double _bothStart;
        private bool _lockout;

        private Side _lastAltSide;
        private double _lastAltTime;

        public event Action<GestureType, double> GestureFired;
        public event Action<GestureType, double> HoldStarted;
        public event Action<GestureType, double> HoldEnded;

        /// <summary>Consecutive on-beat alternating taps, one per beat. 0 when no streak is live.</summary>
        public int AlternateStreak { get; private set; }

        public GestureClassifier(GestureThresholds thresholds, IClock clock)
        {
            _t = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _t.Validate();
        }

        public bool IsDown(Side side) => Key(side).IsDown;

        /// <summary>
        /// True while a press is down whose gesture is not yet known (it could still become a tap, hold or
        /// both-gesture). <paramref name="downTime"/> is the earliest such key-down. The Runner waits on this before
        /// judging an obstacle reached after that key-down (decision A: actions count from key-down).
        /// </summary>
        public bool TryGetUnresolvedPress(out double downTime)
        {
            downTime = double.MaxValue;
            bool any = false;
            if (_both == BothState.Pending) { downTime = _bothStart; any = true; }
            if (Unresolved(_left)) { downTime = Math.Min(downTime, _left.DownTime); any = true; }
            if (Unresolved(_right)) { downTime = Math.Min(downTime, _right.DownTime); any = true; }
            return any;
        }

        private static bool Unresolved(KeyState k) => k.IsDown && !k.Consumed && !k.HoldStarted;

        public void Reset()
        {
            _left = default;
            _right = default;
            _both = BothState.None;
            _lockout = false;
            AlternateStreak = 0;
        }

        public void Press(Side side, double time)
        {
            Update(time);
            ref KeyState key = ref Key(side);
            if (key.IsDown) return; // OS key repeat or duplicate event

            key.IsDown = true;
            key.DownTime = time;
            key.HoldStarted = false;
            key.Consumed = false;

            if (_lockout)
            {
                key.Consumed = true;
                return;
            }

            ref KeyState other = ref Key(Other(side));
            if (other.IsDown && !other.Consumed && !other.HoldStarted &&
                time - other.DownTime <= _t.BothWindowSeconds)
            {
                _both = BothState.Pending;
                _bothStart = other.DownTime;
                key.Consumed = true;
                other.Consumed = true;
                AlternateStreak = 0;
            }
        }

        public void Release(Side side, double time)
        {
            Update(time);
            ref KeyState key = ref Key(side);
            if (!key.IsDown) return;
            key.IsDown = false;

            bool otherDown = Key(Other(side)).IsDown;

            if (_both == BothState.Pending)
            {
                GestureFired?.Invoke(GestureType.BothTap, _bothStart);
                EndBoth(otherDown);
            }
            else if (_both == BothState.Holding)
            {
                HoldEnded?.Invoke(GestureType.BothHold, time);
                EndBoth(otherDown);
            }
            else if (key.Consumed)
            {
                // Lockout key: nothing to emit.
            }
            else if (key.HoldStarted)
            {
                HoldEnded?.Invoke(HoldType(side), time);
            }
            else
            {
                Tap(side, key.DownTime);
            }

            if (!_left.IsDown && !_right.IsDown) _lockout = false;
        }

        /// <summary>Releases any held keys, e.g. when the window loses focus and key-up events may never arrive.</summary>
        public void ReleaseAll(double time)
        {
            if (_left.IsDown) Release(Side.Left, time);
            if (_right.IsDown) Release(Side.Right, time);
        }

        /// <summary>Call once per frame with the current song time.</summary>
        public void Update(double time)
        {
            // A streak is over once the gap has passed with no tap. Not while a key is down: taps resolve on key-up
            // but are judged at key-down, so a pending press may still continue the streak.
            if (AlternateStreak > 0 && !_left.IsDown && !_right.IsDown &&
                time - _lastAltTime > _t.AlternateMaxGapSeconds)
                AlternateStreak = 0;

            double threshold = _t.TapHoldSeconds;

            if (_both == BothState.Pending && time - _bothStart >= threshold)
            {
                _both = BothState.Holding;
                GestureFired?.Invoke(GestureType.BothHold, _bothStart);
                HoldStarted?.Invoke(GestureType.BothHold, _bothStart);
            }

            bool leftDue = HoldDue(ref _left, time);
            bool rightDue = HoldDue(ref _right, time);
            if (leftDue && rightDue && _right.DownTime < _left.DownTime)
            {
                StartHold(Side.Right);
                StartHold(Side.Left);
            }
            else
            {
                if (leftDue) StartHold(Side.Left);
                if (rightDue) StartHold(Side.Right);
            }
        }

        private bool HoldDue(ref KeyState key, double time) =>
            key.IsDown && !key.Consumed && !key.HoldStarted && time - key.DownTime >= _t.TapHoldSeconds;

        private void StartHold(Side side)
        {
            ref KeyState key = ref Key(side);
            key.HoldStarted = true;
            AlternateStreak = 0;
            GestureType type = HoldType(side);
            GestureFired?.Invoke(type, key.DownTime);
            HoldStarted?.Invoke(type, key.DownTime);
        }

        private void EndBoth(bool otherStillDown)
        {
            _both = BothState.None;
            _lockout = otherStillDown;
        }

        private void Tap(Side side, double onset)
        {
            GestureFired?.Invoke(side == Side.Left ? GestureType.TapLeft : GestureType.TapRight, onset);

            // Alternate (ELIA, day 13: option A): sides alternate, each key-down within the max gap of the last.
            if (AlternateStreak > 0 && side != _lastAltSide && onset - _lastAltTime <= _t.AlternateMaxGapSeconds)
                AlternateStreak++;
            else
                AlternateStreak = 1;

            _lastAltSide = side;
            _lastAltTime = onset;

            if (AlternateStreak >= 2)
                GestureFired?.Invoke(GestureType.Alternate, onset);
        }

        private static GestureType HoldType(Side side) => side == Side.Left ? GestureType.HoldLeft : GestureType.HoldRight;

        private ref KeyState Key(Side side)
        {
            if (side == Side.Left) return ref _left;
            return ref _right;
        }

        private static Side Other(Side side) => side == Side.Left ? Side.Right : Side.Left;
    }
}
