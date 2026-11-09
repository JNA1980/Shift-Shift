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
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;
            if (kb.f1Key.wasPressedThisFrame) visible = !visible;
            if (kb.f2Key.wasPressedThisFrame)
            {
                // Latency calibration (playbook 1.4). It returns to the Run scene.
                if (Application.CanStreamedLevelBeLoaded("Calibrate"))
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Calibrate");
                else
                    Debug.LogWarning("Calibrate scene is not in the build list (Shift+Shift > Set Up Run Scenes).");
            }
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

        /// <summary>
        /// Beat cue for testing on-beat input before the Conductor's click track exists: lit for the first 20% of each
        /// beat, with the beat's position in the bar (1-4). Debug aid only.
        /// </summary>
        private static string BeatDot(double beat)
        {
            if (beat < 0) return "";
            double whole = System.Math.Floor(beat);
            bool lit = beat - whole < 0.2;
            int inBar = (int)(whole % 4) + 1;
            string dot = lit ? (inBar == 1 ? "<color=yellow>●</color>" : "<color=lime>●</color>") : "<color=#555555>●</color>";
            return $"<size=26>{dot}</size> {inBar}";
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
            double beat = input.Clock.BeatAt(now);
            GUILayout.Label($"Clock {now:F3}s   beat {beat:F2}   {BeatDot(beat)}", _style);
            GUILayout.Label(
                $"Thresholds: tap/hold {s.tapHoldMs:F0} ms · both {s.bothWindowMs:F0} ms · alternate ±{s.alternateBeatTolerance:F2} beat · charge {s.chargeTaps}",
                _style);
            GUILayout.Space(6);
            var sb = new StringBuilder();
            foreach (string l in _lines) sb.AppendLine(l);
            GUILayout.Label(sb.ToString(), _style);
            GUILayout.FlexibleSpace();
            GUILayout.Label("F1: hide   ·   F2: latency calibration", _style);
            GUILayout.EndArea();
        }
    }
}
