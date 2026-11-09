using System;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ShiftShift
{
    /// <summary>
    /// Reads Left/Right Shift each frame and feeds the engine-free <see cref="GestureClassifier"/>.
    /// Timestamps come from <see cref="Clock"/>; until the Conductor exists (day 13) this is
    /// real time since startup. The Conductor should set Clock to its song-time function.
    ///
    /// Known limitation: key timestamps are quantised to the frame (~16 ms at 60 fps).
    /// Revisit on day 13 if the latency test shows it matters.
    /// </summary>
    [DefaultExecutionOrder(-100)] // classify before gameplay scripts read gestures this frame
    public sealed class ShiftInput : MonoBehaviour
    {
        [Header("Timing (ms) — starting values, tune on day 15")]
        [SerializeField, Range(20, 200)] private float bothWindowMs = 80f;
        [SerializeField, Range(100, 500)] private float holdThresholdMs = 250f;
        [SerializeField, Range(200, 1500)] private float alternateMaxGapMs = 600f;

        public event Action<GestureEvent> GestureDetected;

        /// <summary>Current time in seconds used to stamp key events. Replace with song time once the Conductor exists.</summary>
        public Func<double> Clock { get; set; }

        public bool IsDown(Side side) => _classifier != null && _classifier.IsDown(side);

        private GestureClassifier _classifier;

        private void Awake()
        {
            if (Clock == null) Clock = () => Time.realtimeSinceStartupAsDouble;
            Rebuild();
        }

        /// <summary>Recreates the classifier with the current inspector values (also clears held state).</summary>
        public void Rebuild()
        {
            _classifier = new GestureClassifier(new GestureSettings
            {
                BothWindow = bothWindowMs / 1000.0,
                HoldThreshold = holdThresholdMs / 1000.0,
                AlternateMaxGap = alternateMaxGapMs / 1000.0,
            });
            _classifier.Detected += e => GestureDetected?.Invoke(e);
        }

        /// <summary>Call on restart so a key held through a fail does not leak into the new run.</summary>
        public void ResetState() => _classifier.Reset();

        private void Update()
        {
            double now = Clock();
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                Poll(kb.leftShiftKey, Side.Left, now);
                Poll(kb.rightShiftKey, Side.Right, now);
            }
            _classifier.Update(now);
        }

        private void Poll(KeyControl key, Side side, double now)
        {
            if (key.wasPressedThisFrame) _classifier.Press(side, now);
            if (key.wasReleasedThisFrame) _classifier.Release(side, now);

            // Safety net: a key-up lost to a browser shortcut or Sticky Keys dialog would otherwise leave the key stuck.
            if (_classifier.IsDown(side) && !key.isPressed) _classifier.Release(side, now);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _classifier != null) _classifier.ReleaseAll(Clock());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (holdThresholdMs <= bothWindowMs) holdThresholdMs = bothWindowMs + 1f;
            if (Application.isPlaying && _classifier != null) Rebuild();
        }
#endif
    }
}
