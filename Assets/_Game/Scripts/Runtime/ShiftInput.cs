using System;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ShiftShift
{
    /// <summary>
    /// Reads Left/Right Shift each frame and feeds the engine-free <see cref="GestureClassifier"/>.
    /// All times are song time from <see cref="Clock"/>. Until the Conductor exists (day 12) the clock is a
    /// dspTime stub at <see cref="stubBpm"/>; the Conductor will call <see cref="SetClock"/>.
    ///
    /// Known limitation: key timestamps are taken once per frame (~16 ms at 60 fps). Revisit on day 13.
    /// </summary>
    [DefaultExecutionOrder(-100)] // classify before gameplay scripts read gestures this frame
    public sealed class ShiftInput : MonoBehaviour
    {
        [SerializeField] private GestureSettings settings;

        [Header("Stub clock, used until the Conductor exists (day 12)")]
        [SerializeField, Range(60f, 200f)] private float stubBpm = 120f;

        /// <summary>A gesture was recognised. Time = song time of the key-down.</summary>
        public event Action<GestureType, double> GestureFired;

        /// <summary>HoldLeft, HoldRight or BothHold began. Time = song time of the key-down.</summary>
        public event Action<GestureType, double> HoldStarted;

        /// <summary>HoldLeft, HoldRight or BothHold ended. Time = song time of the key-up.</summary>
        public event Action<GestureType, double> HoldEnded;

        public IClock Clock { get; private set; }
        public GestureSettings Settings => settings;
        public int AlternateStreak => _classifier?.AlternateStreak ?? 0;
        public bool IsDown(Side side) => _classifier != null && _classifier.IsDown(side);

        /// <summary>A key is down whose gesture is not known yet; see GestureClassifier.TryGetUnresolvedPress.</summary>
        public bool TryGetUnresolvedPress(out double downTime)
        {
            downTime = 0;
            return _classifier != null && _classifier.TryGetUnresolvedPress(out downTime);
        }

        private GestureClassifier _classifier;
        private int _settingsVersion;

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError("ShiftInput: no GestureSettings asset assigned (Assets/_Game/Settings/GestureSettings).", this);
                enabled = false;
                return;
            }
            if (Clock == null) Clock = new DspClock(stubBpm);
            Rebuild();
        }

        /// <summary>Swap the song clock (the Conductor calls this). Clears held-key state.</summary>
        public void SetClock(IClock clock)
        {
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (settings != null) Rebuild();
        }

        /// <summary>Call on restart so a key held through a fail does not leak into the new run.</summary>
        public void ResetState() => _classifier?.Reset();

        private void Rebuild()
        {
            _classifier = new GestureClassifier(settings.ToThresholds(), Clock);
            _classifier.GestureFired += (t, s) => GestureFired?.Invoke(t, s);
            _classifier.HoldStarted += (t, s) => HoldStarted?.Invoke(t, s);
            _classifier.HoldEnded += (t, s) => HoldEnded?.Invoke(t, s);
            _settingsVersion = settings.Version;
        }

        private void Update()
        {
            if (settings.Version != _settingsVersion) Rebuild(); // threshold tuned in the Inspector

            double now = Clock.SongTime;
            if (ShiftKeys.FromBrowser)
            {
                // Web build: the browser's own view of the keys (see ShiftKeys: Unity's can stick on Windows).
                ShiftKeys.Read(out bool leftDown, out bool rightDown, out bool leftHit, out bool rightHit,
                    out bool leftAgain, out bool rightAgain);
                Poll(leftDown, leftHit, leftAgain, Side.Left, Side.Right, now);
                Poll(rightDown, rightHit, rightAgain, Side.Right, Side.Left, now);
            }
            else
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    Poll(kb.leftShiftKey, Side.Left, now);
                    Poll(kb.rightShiftKey, Side.Right, now);
                }
            }
            _classifier.Update(now);
        }

        // Web build only. Index 0 = left, 1 = right.
        private readonly double[] _downAt = new double[2];
        private readonly bool[] _countedAsTap = new bool[2];

        private void Poll(bool down, bool hit, bool again, Side side, Side other, double now)
        {
            int i = side == Side.Left ? 0 : 1, o = 1 - i;
            if (!down) _countedAsTap[i] = false;

            if (hit)
            {
                // A Shift pressed while the other has been held for longer than the both-window: the browser will
                // not report its release until BOTH are up (measured on ELIA's machine, day 17: Docs/LOG.md), so
                // it cannot be told from a hold. Count it as a tap now (lane change during a slide or sprint).
                // Cost: in Web builds a second HOLD cannot start while the first is still held.
                bool otherHeld = _classifier.IsDown(other) && now - _downAt[o] > settings.bothWindowMs / 1000.0;
                if (otherHeld)
                {
                    if (_classifier.IsDown(side)) _classifier.Release(side, now);
                    _classifier.Press(side, now);
                    _classifier.Release(side, now);
                    _countedAsTap[i] = true;
                    return;
                }
                // Pressed while we still had it down: the browser never told us it was let go. Let it go now.
                if (again && _classifier.IsDown(side)) _classifier.Release(side, now);
                if (!_classifier.IsDown(side))
                {
                    _classifier.Press(side, now);
                    _downAt[i] = now;
                }
            }
            // While counted as a tap the browser still shows the key down; the classifier already let it go.
            if (!down && _classifier.IsDown(side)) _classifier.Release(side, now);
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
            if (!hasFocus && _classifier != null) _classifier.ReleaseAll(Clock.SongTime);
        }
    }
}
