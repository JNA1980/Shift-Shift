using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Day 9–10 debug overlay: live key state plus the last gestures with their timing.
    /// Toggle with F1. IMGUI so it needs no Canvas setup and works in WebGL.
    /// </summary>
    public sealed class GestureDebugHud : MonoBehaviour
    {
        [SerializeField] private ShiftInput input;
        [SerializeField] private int maxLines = 14;
        [SerializeField] private bool visible = true;

        private readonly List<string> _lines = new List<string>();
        private readonly Dictionary<GestureType, int> _counts = new Dictionary<GestureType, int>();
        private GUIStyle _style;

        private void OnEnable()
        {
            if (input == null) input = FindAnyObjectByType<ShiftInput>();
            if (input != null) input.GestureDetected += OnGesture;
            else Debug.LogWarning("GestureDebugHud: no ShiftInput in scene.");
        }

        private void OnDisable()
        {
            if (input != null) input.GestureDetected -= OnGesture;
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current?.f1Key.wasPressedThisFrame == true) visible = !visible;
        }

        private void OnGesture(GestureEvent e)
        {
            _counts.TryGetValue(e.Type, out int n);
            _counts[e.Type] = n + 1;

            double decideMs = (e.EmitTime - e.Time) * 1000.0;
            _lines.Insert(0, $"{e}   (+{decideMs:F0} ms to decide)");
            if (_lines.Count > maxLines) _lines.RemoveAt(_lines.Count - 1);
            Debug.Log($"[Gesture] {e}");
        }

        private void OnGUI()
        {
            if (!visible || input == null) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
            }

            GUILayout.BeginArea(new Rect(10, 10, 520, Screen.height - 20), GUI.skin.box);
            GUILayout.Label(
                $"L-Shift: {(input.IsDown(Side.Left) ? "<color=lime>DOWN</color>" : "up")}    " +
                $"R-Shift: {(input.IsDown(Side.Right) ? "<color=lime>DOWN</color>" : "up")}", _style);

            var counts = new System.Text.StringBuilder();
            foreach (var kv in _counts) counts.Append(kv.Key).Append('=').Append(kv.Value).Append("  ");
            GUILayout.Label(counts.ToString(), _style);
            GUILayout.Space(6);
            foreach (string line in _lines) GUILayout.Label(line, _style);
            GUILayout.FlexibleSpace();
            GUILayout.Label("F1: hide   |   Windows: disable Sticky Keys for testing", _style);
            GUILayout.EndArea();
        }
    }
}
