using System.Collections.Generic;
using System.IO;
using ShiftShift.Core;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Chart editor (playbook 2.1): Shift+Shift > Chart Editor. Open a level JSON, see its audio on a beat grid,
    /// place obstacles in three lane rows, play/scrub/loop, save sorted readable JSON. Logic lives in Core.Chart
    /// (tested); this window draws and handles input. Docs/CHART_EDITOR.md.
    /// </summary>
    public sealed class ChartEditorWindow : EditorWindow
    {
        private const string LevelsFolder = "Assets/_Game/Levels";
        private const float RulerH = 22f, WaveH = 48f, LaneH = 34f, LabelW = 64f;
        private static readonly string[] LaneNames = { "Left", "Middle", "Right" };

        private string _path;
        private Chart _chart;
        private bool _dirty;
        private AudioClip _clip;
        private float[] _peaks; // max |sample| per wave column, rebuilt on zoom/resize

        private string[] _types;
        private int _typeIndex;
        private bool _eighths;
        private bool _strictSpacing = true;
        private float _pixelsPerBeat = 40f;
        private float _scrollBeat;

        private AudioSource _source;
        private double _playheadBeat;
        private bool _loop;
        private double _loopStart = -1, _loopEnd = -1;
        private LevelEvent _dragging;
        private Vector2 _reportScroll;

        [MenuItem("Shift+Shift/Chart Editor")]
        public static void Open() => GetWindow<ChartEditorWindow>("Chart Editor").Show();

        private void OnEnable()
        {
            _types = new List<string>(ObstacleTypes.Names).ToArray();
            EditorApplication.update += Tick;
            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            StopAudio();
            if (_source != null) DestroyImmediate(_source.gameObject);
        }

        // ---------- file ----------

        private void Load(string path)
        {
            LevelData level = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
            if (level == null)
            {
                Debug.LogError($"[ChartEditor] Could not read {path}.");
                return;
            }
            _path = path;
            _chart = new Chart(level);
            _dirty = false;
            _playheadBeat = 0;
            _scrollBeat = 0;
            _loopStart = _loopEnd = -1;
            StopAudio();
            _samples = null;
            _samplesFailed = false;
            _clip = Resources.Load<AudioClip>(level.audioClip);
            if (_clip == null) Debug.LogWarning($"[ChartEditor] Audio '{level.audioClip}' not found in a Resources folder.");
            _peaks = null;
        }

        private void Save()
        {
            if (_chart == null) return;
            Chart.Report r = _chart.Check(_strictSpacing);
            if (r.Errors.Count > 0)
            {
                EditorUtility.DisplayDialog("Chart Editor", "Not saved, fix these first:\n\n- " + string.Join("\n- ", r.Errors), "OK");
                return;
            }
            File.WriteAllText(_path, _chart.ToJson());
            AssetDatabase.ImportAsset(_path);
            _dirty = false;
            Debug.Log($"[ChartEditor] Saved {_path} ({_chart.Events.Count} events" +
                      (r.Warnings.Count > 0 ? $", {r.Warnings.Count} warnings)" : ")"));
        }

        // ---------- audio (edit mode) ----------

        private double SecondsAtBeat(double beat) => beat * 60.0 / _chart.Level.bpm + _chart.Level.offsetMs / 1000.0;
        private double BeatAtSeconds(double s) => (s - _chart.Level.offsetMs / 1000.0) * _chart.Level.bpm / 60.0;

        private void EnsureSource()
        {
            if (_source != null) return;
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags("ChartEditorAudio", HideFlags.HideAndDontSave,
                typeof(AudioSource));
            _source = go.GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        private bool Playing => _source != null && _source.isPlaying;

        private void PlayFrom(double beat)
        {
            if (_clip == null) return;
            EnsureSource();
            _source.clip = _clip;
            double s = System.Math.Max(0, SecondsAtBeat(beat));
            if (s >= _clip.length) return;
            _source.timeSamples = (int)(s * _clip.frequency);
            _source.Play();
        }

        private void StopAudio()
        {
            if (_source != null) _source.Stop();
        }

        private void Tick()
        {
            if (_chart == null || !Playing) return;
            // Playhead from the audio's own position, so the grid stays in sync with what is heard.
            _playheadBeat = BeatAtSeconds((double)_source.timeSamples / _clip.frequency);
            if (_loop && _loopEnd > _loopStart && _playheadBeat >= _loopEnd) PlayFrom(_loopStart);
            // Keep the playhead on screen.
            float visibleBeats = (position.width - LabelW) / _pixelsPerBeat;
            if (_playheadBeat > _scrollBeat + visibleBeats * 0.85f || _playheadBeat < _scrollBeat)
                _scrollBeat = Mathf.Max(0, (float)_playheadBeat - visibleBeats * 0.15f);
            Repaint();
        }

        // ---------- GUI ----------

        private void OnGUI()
        {
            DrawToolbar();
            if (_chart == null)
            {
                EditorGUILayout.HelpBox("Pick a level from the Level menu above.", MessageType.Info);
                return;
            }
            DrawInfo();
            Rect area = GUILayoutUtility.GetRect(position.width, RulerH + WaveH + LaneH * 3 + 16, GUILayout.ExpandWidth(true));
            DrawTimeline(area);
            DrawReport();
            HandleKeys();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (EditorGUILayout.DropdownButton(new GUIContent(_path != null ? Path.GetFileName(_path) : "Level"),
                        FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(170)))
                {
                    var menu = new GenericMenu();
                    foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelsFolder }))
                    {
                        string p = AssetDatabase.GUIDToAssetPath(guid);
                        if (!p.EndsWith(".json")) continue;
                        menu.AddItem(new GUIContent(Path.GetFileName(p)), p == _path, () =>
                        {
                            if (!_dirty || EditorUtility.DisplayDialog("Chart Editor", "Discard unsaved changes?", "Discard", "Cancel"))
                                Load(p);
                        });
                    }
                    menu.ShowAsContext();
                }

                GUI.enabled = _chart != null;
                if (GUILayout.Button(_dirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(50))) Save();
                if (GUILayout.Button("Revert", EditorStyles.toolbarButton, GUILayout.Width(50)) && _path != null) Load(_path);
                GUILayout.Space(8);
                if (GUILayout.Button(Playing ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(50)))
                    TogglePlay();
                if (GUILayout.Button("Stop", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    StopAudio();
                    _playheadBeat = _loop && _loopStart >= 0 ? _loopStart : 0;
                }
                _loop = GUILayout.Toggle(_loop, "Loop", EditorStyles.toolbarButton, GUILayout.Width(40));
                GUILayout.Space(8);
                _eighths = GUILayout.Toggle(_eighths, _eighths ? "Snap 1/8" : "Snap 1/4", EditorStyles.toolbarButton, GUILayout.Width(62));
                GUILayout.Label("Type", GUILayout.Width(32));
                _typeIndex = EditorGUILayout.Popup(_typeIndex, _types, EditorStyles.toolbarPopup, GUILayout.Width(80));
                GUILayout.Space(8);
                _strictSpacing = GUILayout.Toggle(_strictSpacing, "Spacing check (levels 1-4)", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Zoom", GUILayout.Width(34));
                float zoom = GUILayout.HorizontalSlider(_pixelsPerBeat, 12f, 160f, GUILayout.Width(100));
                if (!Mathf.Approximately(zoom, _pixelsPerBeat)) { _pixelsPerBeat = zoom; _peaks = null; }
                GUI.enabled = true;
            }
        }

        private void DrawInfo()
        {
            LevelData l = _chart.Level;
            var counts = _chart.Counts();
            var parts = new List<string>();
            foreach (var kv in counts) if (kv.Value > 0) parts.Add($"{kv.Key} {kv.Value}");
            EditorGUILayout.LabelField(
                $"{l.name}   ·   {l.bpm:0.##} BPM   ·   offset {l.offsetMs:0} ms   ·   {l.lengthBeats:0.##} beats = {_chart.LengthSeconds:0.0} s" +
                $"   ·   {_chart.Events.Count} events" + (parts.Count > 0 ? $" ({string.Join(", ", parts)})" : ""),
                EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(
                "Click a lane: place selected type · drag: move · right-click: remove · ruler: click = playhead, drag = loop range · " +
                "Space: play/pause · Ctrl+S: save · 1-8: type",
                EditorStyles.miniLabel);
        }

        private void DrawTimeline(Rect area)
        {
            float visibleBeats = (area.width - LabelW) / _pixelsPerBeat;
            float maxScroll = Mathf.Max(0, (float)_chart.Level.lengthBeats - visibleBeats + 2);
            Rect scrollRect = new Rect(area.x + LabelW, area.yMax - 14, area.width - LabelW, 14);
            _scrollBeat = GUI.HorizontalScrollbar(scrollRect, _scrollBeat, Mathf.Min(visibleBeats, maxScroll + visibleBeats), 0, maxScroll + visibleBeats);

            Rect ruler = new Rect(area.x + LabelW, area.y, area.width - LabelW, RulerH);
            Rect wave = new Rect(ruler.x, ruler.yMax, ruler.width, WaveH);
            Rect lanes = new Rect(ruler.x, wave.yMax, ruler.width, LaneH * 3);

            EditorGUI.DrawRect(new Rect(area.x, area.y, area.width, RulerH + WaveH + LaneH * 3), new Color(0.16f, 0.16f, 0.17f));
            for (int i = 0; i < 3; i++)
            {
                var row = new Rect(area.x, lanes.y + i * LaneH, LabelW - 4, LaneH);
                GUI.Label(row, LaneNames[i], EditorStyles.miniLabel);
                EditorGUI.DrawRect(new Rect(lanes.x, lanes.y + i * LaneH, lanes.width, LaneH - 1),
                    i % 2 == 0 ? new Color(0.21f, 0.21f, 0.23f) : new Color(0.19f, 0.19f, 0.21f));
            }
            GUI.Label(new Rect(area.x, wave.y, LabelW - 4, WaveH), "Audio", EditorStyles.miniLabel);

            DrawWave(wave);
            DrawGrid(ruler, wave, lanes);
            DrawLoopRange(ruler, lanes);
            DrawEvents(lanes);
            DrawPlayhead(ruler, lanes);
            HandleMouse(ruler, lanes);
        }

        private float X(double beat, Rect r) => r.x + (float)((beat - _scrollBeat) * _pixelsPerBeat);
        private double BeatAtX(float x, Rect r) => _scrollBeat + (x - r.x) / _pixelsPerBeat;

        private void DrawWave(Rect r)
        {
            if (_clip == null)
            {
                GUI.Label(r, "  (no audio)", EditorStyles.miniLabel);
                return;
            }
            int columns = Mathf.Max(1, (int)r.width);
            if (_peaks == null || _peaks.Length != columns || _peaksScroll != _scrollBeat) BuildPeaks(columns);
            if (_peaks == null) return;
            float mid = r.center.y;
            for (int i = 0; i < _peaks.Length; i++)
            {
                float h = _peaks[i] * (r.height * 0.5f - 2);
                if (h > 0.5f) EditorGUI.DrawRect(new Rect(r.x + i, mid - h, 1, h * 2), new Color(0.45f, 0.65f, 0.9f));
            }
        }

        private float _peaksScroll = -1;
        private float[] _samples;
        private bool _samplesFailed;

        private void BuildPeaks(int columns)
        {
            _peaksScroll = _scrollBeat;
            if (_samplesFailed) return;
            if (_samples == null || _samples.Length != _clip.samples * _clip.channels)
            {
                _samples = new float[_clip.samples * _clip.channels];
                bool ok;
                try { ok = _clip.GetData(_samples, 0); }
                catch (System.Exception) { ok = false; }
                if (!ok)
                {
                    _samplesFailed = true; // e.g. clip not decompressed on load; editing still works
                    _peaks = null;
                    Debug.LogWarning("[ChartEditor] Could not read the audio samples for the waveform (check the clip's Load Type).");
                    return;
                }
            }
            _peaks = new float[columns];
            int ch = _clip.channels;
            for (int i = 0; i < columns; i++)
            {
                double s0 = SecondsAtBeat(_scrollBeat + i / (double)_pixelsPerBeat);
                double s1 = SecondsAtBeat(_scrollBeat + (i + 1) / (double)_pixelsPerBeat);
                int a = Mathf.Clamp((int)(s0 * _clip.frequency), 0, _clip.samples);
                int b = Mathf.Clamp((int)(s1 * _clip.frequency), 0, _clip.samples);
                float peak = 0;
                for (int k = a; k < b; k++) peak = Mathf.Max(peak, Mathf.Abs(_samples[k * ch]));
                _peaks[i] = peak;
            }
        }

        private void DrawGrid(Rect ruler, Rect wave, Rect lanes)
        {
            float step = _eighths ? 0.5f : 1f;
            int first = Mathf.FloorToInt(_scrollBeat / step);
            for (int i = first; ; i++)
            {
                double beat = i * step;
                if (beat > _chart.Level.lengthBeats) break;
                float x = X(beat, ruler);
                if (x > ruler.xMax) break;
                if (x < ruler.x) continue;
                bool bar = System.Math.Abs(beat % 4) < 1e-6;
                bool whole = System.Math.Abs(beat % 1) < 1e-6;
                Color c = bar ? new Color(1, 1, 1, 0.35f) : whole ? new Color(1, 1, 1, 0.15f) : new Color(1, 1, 1, 0.06f);
                EditorGUI.DrawRect(new Rect(x, wave.y, 1, wave.height + lanes.height), c);
                if (bar) GUI.Label(new Rect(x + 2, ruler.y + 2, 60, 18), $"{beat:0}", EditorStyles.miniLabel);
                if (beat < 8) EditorGUI.DrawRect(new Rect(x, lanes.y, (float)(step * _pixelsPerBeat), lanes.height), new Color(1f, 0.6f, 0.1f, 0.04f));
            }
            float end = X(_chart.Level.lengthBeats, ruler);
            if (end >= ruler.x && end <= ruler.xMax) EditorGUI.DrawRect(new Rect(end, ruler.y, 2, ruler.height + wave.height + lanes.height), Color.red);
        }

        private void DrawLoopRange(Rect ruler, Rect lanes)
        {
            if (_loopEnd <= _loopStart) return;
            float x0 = X(_loopStart, ruler), x1 = X(_loopEnd, ruler);
            EditorGUI.DrawRect(new Rect(x0, ruler.y, x1 - x0, ruler.height), new Color(0.3f, 0.8f, 0.4f, 0.35f));
            EditorGUI.DrawRect(new Rect(x0, lanes.y, x1 - x0, lanes.height), new Color(0.3f, 0.8f, 0.4f, 0.06f));
        }

        private void DrawEvents(Rect lanes)
        {
            float w = Mathf.Max(10f, _pixelsPerBeat * 0.4f);
            foreach (LevelEvent e in _chart.Events)
            {
                float x = X(e.beat, lanes) - w * 0.5f;
                if (x + w < lanes.x || x > lanes.xMax) continue;
                Color c = TypeColor(e.type);
                ObstacleTypes.TryGet(e.type, out ObstacleTypes.Spec spec);
                for (int lane = 0; lane < 3; lane++)
                {
                    // Barrier with ghost blocks: the barrier fills the beat; the ghost blocks' lanes are drawn light pink.
                    bool ghostBlocks = spec.GhostBlocks != ObstacleTypes.GhostBlocks.None;
                    bool ghostBlock = ghostBlocks && (spec.GhostBlocks == ObstacleTypes.GhostBlocks.EventLane ? lane == e.lane : lane != e.lane);
                    bool covered = spec.AllLanes || ghostBlocks || (spec.LaneIsOpening ? lane != e.lane : lane == e.lane);
                    var cell = new Rect(x, lanes.y + lane * LaneH + 3, w, LaneH - 7);
                    if (covered) EditorGUI.DrawRect(cell, e == _dragging ? Color.white : ghostBlock ? new Color(1f, 0.71f, 0.85f) : c);
                    else if (spec.LaneIsOpening) EditorGUI.DrawRect(new Rect(cell.x, cell.y + cell.height * 0.45f, cell.width, 2), c);
                }
                GUI.Label(new Rect(x, lanes.y + (spec.AllLanes || spec.LaneIsOpening ? 0 : e.lane * LaneH), 60, 16),
                    e.type, EditorStyles.miniBoldLabel);
            }
        }

        private void DrawPlayhead(Rect ruler, Rect lanes)
        {
            float x = X(_playheadBeat, ruler);
            if (x < ruler.x || x > ruler.xMax) return;
            EditorGUI.DrawRect(new Rect(x - 1, ruler.y, 2, ruler.height + WaveH + lanes.height), new Color(0.3f, 1f, 0.4f));
        }

        private static Color TypeColor(string type)
        {
            switch (type)
            {
                case "bar": return new Color(0.95f, 0.45f, 0.2f);
                case "wide": return new Color(0.85f, 0.38f, 0.15f);
                case "low": return new Color(0.95f, 0.75f, 0.2f);
                case "gate": return new Color(0.95f, 0.95f, 0.2f);
                case "gap": return new Color(0.1f, 0.1f, 0.12f);
                case "hurdle": return new Color(1f, 0.45f, 0.75f);
                case "ghost":
                case "ghostbar":
                case "ghostwide": return new Color(0.25f, 0.35f, 0.9f);
                case "wall": return new Color(0.85f, 0.15f, 0.2f);
                default: return Color.gray;
            }
        }

        // ---------- input ----------

        private double _rulerDragStart = -1;

        private void HandleMouse(Rect ruler, Rect lanes)
        {
            Event ev = Event.current;
            int step = _eighths ? 2 : 1;

            if (ruler.Contains(ev.mousePosition))
            {
                double beat = Chart.Snap(BeatAtX(ev.mousePosition.x, ruler), step);
                if (ev.type == EventType.MouseDown && ev.button == 0)
                {
                    _rulerDragStart = beat;
                    _playheadBeat = beat;
                    if (Playing) PlayFrom(beat);
                    ev.Use();
                }
                else if (ev.type == EventType.MouseDrag && _rulerDragStart >= 0)
                {
                    _loopStart = System.Math.Min(_rulerDragStart, beat);
                    _loopEnd = System.Math.Max(_rulerDragStart, beat);
                    ev.Use();
                }
            }
            if (ev.type == EventType.MouseUp) _rulerDragStart = -1;

            if (!lanes.Contains(ev.mousePosition) && _dragging == null) return;
            double at = Chart.Snap(BeatAtX(ev.mousePosition.x, lanes), step);
            int lane = Mathf.Clamp((int)((ev.mousePosition.y - lanes.y) / LaneH), 0, 2);

            switch (ev.type)
            {
                case EventType.MouseDown when ev.button == 0:
                {
                    LevelEvent hit = _chart.At(at, lane);
                    if (hit != null) _dragging = hit;
                    else
                    {
                        _chart.Place(at, lane, _types[_typeIndex]);
                        _dirty = true;
                    }
                    ev.Use();
                    break;
                }
                case EventType.MouseDrag when _dragging != null:
                    ev.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when _dragging != null:
                    if (System.Math.Abs(_dragging.beat - at) > 1e-6 || _dragging.lane != lane)
                    {
                        _chart.Move(_dragging, at, lane);
                        _dirty = true;
                    }
                    _dragging = null;
                    ev.Use();
                    break;
                case EventType.MouseDown when ev.button == 1:
                    if (_chart.Remove(at, lane)) _dirty = true;
                    ev.Use();
                    break;
            }
        }

        private void HandleKeys()
        {
            Event ev = Event.current;
            if (ev.type != EventType.KeyDown) return;
            if (ev.keyCode == KeyCode.Space) { TogglePlay(); ev.Use(); }
            else if (ev.keyCode == KeyCode.S && (ev.control || ev.command)) { Save(); ev.Use(); }
            else if (ev.keyCode >= KeyCode.Alpha1 && ev.keyCode <= KeyCode.Alpha8)
            {
                int i = ev.keyCode - KeyCode.Alpha1;
                if (i < _types.Length) _typeIndex = i;
                ev.Use();
            }
        }

        private void TogglePlay()
        {
            if (_chart == null) return;
            if (Playing) StopAudio();
            else PlayFrom(_loop && _loopEnd > _loopStart ? _loopStart : _playheadBeat);
        }

        private void DrawReport()
        {
            Chart.Report r = _chart.Check(_strictSpacing);
            if (r.Errors.Count == 0 && r.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("No problems.", MessageType.None);
                return;
            }
            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.MaxHeight(110));
            foreach (string e in r.Errors) EditorGUILayout.HelpBox(e, MessageType.Error);
            foreach (string w in r.Warnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }
    }
}
