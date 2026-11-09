using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Gesture thresholds (CLAUDE.md: thresholds live in a ScriptableObject).
    /// Asset: Assets/_Game/Settings/GestureSettings.asset. Starting values from the plan; tune on day 15.
    /// Edits during Play mode apply immediately (ShiftInput rebuilds its classifier).
    /// </summary>
    [CreateAssetMenu(menuName = "Shift+Shift/Gesture Settings", fileName = "GestureSettings")]
    public sealed class GestureSettings : ScriptableObject
    {
        [Tooltip("Press shorter than this = tap, longer = hold.")]
        [Range(100f, 500f)] public float tapHoldMs = 250f;

        [Tooltip("Second key down within this of the first = a 'both' gesture.")]
        [Range(20f, 200f)] public float bothWindowMs = 80f;

        [Tooltip("How far from a beat line (in beats) an Alternate tap may land and still count.")]
        [Range(0.05f, 0.45f)] public float alternateBeatTolerance = 0.25f;

        /// <summary>Bumped on every Inspector edit so listeners can pick up changes in Play mode.</summary>
        public int Version { get; private set; }

        public GestureThresholds ToThresholds() => new GestureThresholds
        {
            TapHoldSeconds = tapHoldMs / 1000.0,
            BothWindowSeconds = bothWindowMs / 1000.0,
            AlternateBeatTolerance = alternateBeatTolerance,
        };

        private void OnValidate()
        {
            if (tapHoldMs <= bothWindowMs) tapHoldMs = bothWindowMs + 1f;
            Version++;
        }
    }
}
