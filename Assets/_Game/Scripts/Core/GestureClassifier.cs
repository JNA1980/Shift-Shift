using System;

namespace ShiftShift.Core
{
    /// <summary>
    /// Turns raw Left/Right Shift key-down/key-up events into the seven gestures.
    /// Engine-free: the caller supplies timestamps (song time, in seconds) and must call
    /// <see cref="Update"/> every frame so holds are detected while keys are still down.
    ///
    /// Rules (see Docs/INPUT.md for the reasoning):
    /// - Single press shorter than HoldThreshold  -> Tap (emitted on key-up, timed at key-down).
    /// - Single press reaching HoldThreshold      -> HoldStart (timed at key-down), HoldEnd on key-up.
    /// - Second key down within BothWindow of the first, while the first is still down -> "both" gesture:
    ///   first key-up before HoldThreshold -> BothTap, otherwise BothHoldStart ... BothHoldEnd (on first key-up).
    /// - After a both-gesture ends, the key still held is ignored, and new presses are ignored,
    ///   until both keys are up (lockout). This stops a phase-shift release from turning into a sprint.
    /// - Taps that alternate sides with onsets no more than AlternateMaxGap apart also emit Alternate(count).
    ///   Holds and both-gestures reset the streak.
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

        private readonly GestureSettings _settings;
        private KeyState _left, _right;
        private BothState _both;
        private double _bothStart;
        private bool _lockout;

        private bool _hasLastTap;
        private Side _lastTapSide;
        private double _lastTapTime;
        private int _alternateCount;

        public event Action<GestureEvent> Detected;

        public GestureClassifier(GestureSettings settings = null)
        {
            _settings = settings ?? new GestureSettings();
            _settings.Validate();
        }

        public bool IsDown(Side side) => Key(side).IsDown;

        public void Reset()
        {
            _left = default;
            _right = default;
            _both = BothState.None;
            _lockout = false;
            ResetAlternate();
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
                time - other.DownTime <= _settings.BothWindow)
            {
                _both = BothState.Pending;
                _bothStart = other.DownTime;
                key.Consumed = true;
                other.Consumed = true;
                ResetAlternate();
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
                Emit(GestureType.BothTap, _bothStart, time);
                EndBoth(otherDown);
            }
            else if (_both == BothState.Holding)
            {
                Emit(GestureType.BothHoldEnd, time, time);
                EndBoth(otherDown);
            }
            else if (key.Consumed)
            {
                // Lockout key: nothing to emit.
            }
            else if (key.HoldStarted)
            {
                Emit(side == Side.Left ? GestureType.HoldLeftEnd : GestureType.HoldRightEnd, time, time);
            }
            else
            {
                EmitTap(side, key.DownTime, time);
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
            double threshold = _settings.HoldThreshold;

            if (_both == BothState.Pending && time - _bothStart >= threshold)
            {
                _both = BothState.Holding;
                Emit(GestureType.BothHoldStart, _bothStart, _bothStart + threshold);
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
            key.IsDown && !key.Consumed && !key.HoldStarted && time - key.DownTime >= _settings.HoldThreshold;

        private void StartHold(Side side)
        {
            ref KeyState key = ref Key(side);
            key.HoldStarted = true;
            ResetAlternate();
            Emit(side == Side.Left ? GestureType.HoldLeftStart : GestureType.HoldRightStart,
                key.DownTime, key.DownTime + _settings.HoldThreshold);
        }

        private void EndBoth(bool otherStillDown)
        {
            _both = BothState.None;
            _lockout = otherStillDown;
        }

        private void EmitTap(Side side, double onset, double releaseTime)
        {
            Emit(side == Side.Left ? GestureType.TapLeft : GestureType.TapRight, onset, releaseTime);

            if (_hasLastTap && _lastTapSide != side && onset - _lastTapTime <= _settings.AlternateMaxGap)
                _alternateCount++;
            else
                _alternateCount = 1;

            _hasLastTap = true;
            _lastTapSide = side;
            _lastTapTime = onset;

            if (_alternateCount >= 2)
                Emit(GestureType.Alternate, onset, releaseTime, _alternateCount);
        }

        private void ResetAlternate()
        {
            _hasLastTap = false;
            _alternateCount = 0;
        }

        private void Emit(GestureType type, double time, double emitTime, int alternateCount = 0) =>
            Detected?.Invoke(new GestureEvent(type, time, emitTime, alternateCount));

        private ref KeyState Key(Side side)
        {
            if (side == Side.Left) return ref _left;
            return ref _right;
        }

        private static Side Other(Side side) => side == Side.Left ? Side.Right : Side.Left;
    }
}
