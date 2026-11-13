using System;

namespace ShiftShift.Core
{
    public enum Side { Left, Right }

    /// <summary>The seven gestures (project rules).</summary>
    public enum GestureType
    {
        TapLeft,
        TapRight,
        HoldLeft,   // slide
        HoldRight,  // sprint
        BothTap,    // jump
        BothHold,   // phase shift
        Alternate,  // charge step; see GestureClassifier.AlternateStreak
    }

    /// <summary>
    /// Song clock. The Conductor implements this (day 12); until then ShiftInput uses a dspTime stub.
    /// </summary>
    public interface IClock
    {
        /// <summary>Current song time in seconds.</summary>
        double SongTime { get; }

        /// <summary>Beat number (0 = first beat) at a given song time. Used to judge whether a tap was on the beat.</summary>
        double BeatAt(double songTime);

        /// <summary>Song time (s) of a beat number; the inverse of <see cref="BeatAt"/>.</summary>
        double SongTimeAtBeat(double beat);
    }

    /// <summary>
    /// Plain copy of the GestureSettings ScriptableObject values, so the classifier stays engine-free and testable.
    /// No defaults here on purpose: the values come from the asset.
    /// </summary>
    public sealed class GestureThresholds
    {
        /// <summary>Press duration (s) at which a press stops being a tap and becomes a hold.</summary>
        public double TapHoldSeconds;

        /// <summary>Max gap (s) between the two key-downs for them to count as one "both" gesture.</summary>
        public double BothWindowSeconds;

        /// <summary>Max time (s) between alternating tap key-downs for an Alternate streak to continue (no beat needed).</summary>
        public double AlternateMaxGapSeconds;

        public void Validate()
        {
            if (BothWindowSeconds <= 0) throw new ArgumentException("BothWindowSeconds must be > 0");
            if (TapHoldSeconds <= BothWindowSeconds) throw new ArgumentException("TapHoldSeconds must be greater than BothWindowSeconds");
            if (AlternateMaxGapSeconds <= 0) throw new ArgumentException("AlternateMaxGapSeconds must be > 0");
        }
    }
}
