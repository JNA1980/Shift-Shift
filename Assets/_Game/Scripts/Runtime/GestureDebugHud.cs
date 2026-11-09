using System.Collections.Generic;
using System.Text;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Debug overlay: key states, clock, current thresholds and the last five gestures with timestamps.
    /// Toggle with F1. IMGUI so it needs no Canvas setup and works in WebGL.
    /// </summary>
    public sealed class GestureDebugHud : MonoBehaviour
    {
        private const int MaxLines = 5;

        [SerializeField] private ShiftInput input;
        [SerializeField] private bool visible = true;

        private readonly List<string> _lines = new List<string>();
        private GUIStyle _style;

        private void OnEnable()
        {
            if (input == null) input = FindAnyObjectByType<ShiftInput>();
            if (input == null)
            {
                Debug.LogWarning("GestureDebugHud: no ShiftInput in scene.");
                return;
            }
            input.GestureFired += OnFired;
            input.HoldStarted += OnHoldStarted;
            input.HoldEnded += OnHoldEnded;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.GestureFired -= OnFired;
            input.HoldStarted -= OnHoldStarted;
            input.HoldEnded -= OnHoldEnded;
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current?.f1Key.wasPressedThisFrame == true) visible = !visible;
        }

        private void OnFired(GestureType type, double t)
        {
            string extra = type == GestureType.Alternate ? $" x{input.AlternateStreak}" : "";
            Add($"{type}{extra}", t);
        }

        private void OnHoldStarted(GestureType type, double t) => Add($"  {type} start", t);
        private void OnHoldEnded(GestureType type, double t) => Add($"  {type} end", t);

        private void Add(string what, double t)
        {
            double beat = input.Clock.BeatAt(t);
            double decideMs = (input.Clock.SongTime - t) * 1000.0;
            string line = $"{what}  @{t:F3}s  beat {beat:F2}  (+{decideMs:F0} ms to decide)";
            _lines.Insert(0, line);
            if (_lines.Count > MaxLines) _lines.RemoveAt(_lines.Count - 1);
            Debug.Log("[Gesture] " + line);
        }

        private void OnGUI()
        {
            if (!visible || input == null || !input.enabled || input.Clock == null) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };

            GestureSettings s = input.Settings;
            double now = input.Clock.SongTime;

            GUILayout.BeginArea(new Rect(10, 10, 560, Screen.height - 20), GUI.skin.box);
            GUILayout.Label(
                $"L-Shift: {(input.IsDown(Side.Left) ? "<color=lime>DOWN</color>" : "up")}    " +
                $"R-Shift: {(input.IsDown(Side.Right) ? "<color=lime>DOWN</color>" : "up")}    " +
                $"Alternate streak: {input.AlternateStreak}", _style);
            GUILayout.Label($"Clock {now:F3}s   beat {input.Clock.BeatAt(now):F2}", _style);
            GUILayout.Label(
                $"Thresholds: tap/hold {s.tapHoldMs:F0} ms · both {s.bothWindowMs:F0} ms · alternate ±{s.alternateBeatTolerance:F2} beat · charge {s.chargeTaps}",
                _style);
            GUILayout.Space(6);
            var sb = new StringBuilder();
            foreach (string l in _lines) sb.AppendLine(l);
            GUILayout.Label(sb.ToString(), _style);
            GUILayout.FlexibleSpace();
            GUILayout.Label("F1: hide", _style);
            GUILayout.EndArea();
        }
    }
}
