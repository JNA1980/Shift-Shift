using System;

namespace ShiftShift.Core
{
    public enum Side { Left, Right }

    public enum GestureType
    {
        TapLeft,
        TapRight,
        HoldLeftStart,
        HoldLeftEnd,
        HoldRightStart,
        HoldRightEnd,
        BothTap,
        BothHoldStart,
        BothHoldEnd,
        /// <summary>Emitted after a tap that alternates sides with the previous tap. See <see cref="GestureEvent.AlternateCount"/>.</summary>
        Alternate,
    }

    public readonly struct GestureEvent
    {
        /// <summary>
        /// When the gesture happened, for judging. Taps, hold starts and both-gestures use the
        /// key-down time (onset). End events use the key-up time.
        /// </summary>
        public readonly double Time;

        /// <summary>When the classifier was able to decide (e.g. key-up for a tap, onset + threshold for a hold).</summary>
        public readonly double EmitTime;

        public readonly GestureType Type;

        /// <summary>For <see cref="GestureType.Alternate"/>: number of consecutive alternating taps (2 = L,R). 0 otherwise.</summary>
        public readonly int AlternateCount;

        public GestureEvent(GestureType type, double time, double emitTime, int alternateCount = 0)
        {
            Type = type;
            Time = time;
            EmitTime = emitTime;
            AlternateCount = alternateCount;
        }

        public override string ToString() =>
            AlternateCount > 0 ? $"{Type} x{AlternateCount} @{Time:F3}" : $"{Type} @{Time:F3}";
    }

    /// <summary>All values in seconds. Defaults are the plan's starting values, to be tuned on day 15.</summary>
    public sealed class GestureSettings
    {
        /// <summary>Max gap between the two key-downs for them to count as one "both" gesture.</summary>
        public double BothWindow = 0.080;

        /// <summary>Press duration at which a press stops being a tap and becomes a hold.</summary>
        public double HoldThreshold = 0.250;

        /// <summary>Max gap between consecutive tap onsets for an alternation streak to continue.</summary>
        public double AlternateMaxGap = 0.600;

        public void Validate()
        {
            if (BothWindow <= 0) throw new ArgumentException("BothWindow must be > 0");
            if (HoldThreshold <= BothWindow) throw new ArgumentException("HoldThreshold must be greater than BothWindow");
            if (AlternateMaxGap <= 0) throw new ArgumentException("AlternateMaxGap must be > 0");
        }
    }
}
