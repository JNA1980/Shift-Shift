using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// The menu (playbook 2.5): a row of entries (the levels, then Calibrate and Credits) chosen with the two Shift
    /// keys only: Left / Right Shift = previous / next, both together = open. Every level is open from the start
    /// (jam raters should be able to see any level). The first key press here is also the browser's audio unlock.
    /// IMGUI, like the rest of the UI.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private ShiftInput input;
        [Tooltip("In play order: level 1 first.")]
        [SerializeField] private List<TextAsset> levels = new List<TextAsset>();
        [SerializeField] private string runScene = "Run";
        [SerializeField] private string calibrateScene = "Calibrate";
        [SerializeField, TextArea(6, 20)] private string credits = "Shift+Shift";

        private struct Entry
        {
            public string Title, Detail, LevelAsset;
            public bool Calibrate, Credits;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly ShiftMenuKeys _keys = new ShiftMenuKeys();
        private int _index;
        private bool _showCredits;
        private GUIStyle _title, _big, _mid, _small;

        private void Start()
        {
            Time.timeScale = 1f;
            if (input == null) input = FindAnyObjectByType<ShiftInput>();

            for (int i = 0; i < levels.Count; i++)
            {
                TextAsset json = levels[i];
                if (json == null) continue;
                LevelData level = JsonUtility.FromJson<LevelData>(json.text);
                if (level == null) continue;
                int best = PlayerPrefs.GetInt(GameFlow.BestKeyPrefix + json.name, 0);
                double seconds = level.bpm > 0 ? new TempoMap(level.bpm, level.tempo).TimeAtBeat(level.lengthBeats) : 0;
                _entries.Add(new Entry
                {
                    Title = $"{_entries.Count + 1}   {level.name}",
                    Detail = $"{seconds:0} seconds   ·   " + (best > 0 ? $"best {best}" : "not played yet"),
                    LevelAsset = json.name,
                });
            }
            float offset = PlayerPrefs.GetFloat(Conductor.UserOffsetKey, 0f);
            _entries.Add(new Entry { Title = "Calibrate timing", Detail = $"Tap along to a click to line the game up with your sound (now {offset:+0;-0;0} ms)", Calibrate = true });
            _entries.Add(new Entry { Title = "Credits", Detail = "Who and what made this", Credits = true });

            // Come back to the level just played.
            if (!string.IsNullOrEmpty(GameFlow.LevelToLoad))
                for (int i = 0; i < _entries.Count; i++)
                    if (_entries[i].LevelAsset == GameFlow.LevelToLoad) _index = i;

            _keys.Reset(Down(Side.Left), Down(Side.Right));
        }

        private bool Down(Side side) => input != null && input.IsDown(side);

        private void Update()
        {
            MenuKey key = _keys.Poll(Down(Side.Left), Down(Side.Right));
            if (key == MenuKey.None) return;
            if (_showCredits)
            {
                _showCredits = false; // any key closes the credits
                return;
            }
            if (key == MenuKey.Left) _index = (_index + _entries.Count - 1) % _entries.Count;
            else if (key == MenuKey.Right) _index = (_index + 1) % _entries.Count;
            else Open(_entries[_index]);
        }

        private void Open(Entry e)
        {
            if (e.Credits) _showCredits = true;
            else if (e.Calibrate)
            {
                if (Application.CanStreamedLevelBeLoaded(calibrateScene)) SceneManager.LoadScene(calibrateScene);
                else Debug.LogWarning($"[Menu] Scene '{calibrateScene}' is not in the build list.");
            }
            else GameFlow.Play(e.LevelAsset, runScene);
        }

        private void OnGUI()
        {
            if (_entries.Count == 0) return;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 84, alignment = TextAnchor.MiddleCenter, richText = true, fontStyle = FontStyle.Bold };
                _big = new GUIStyle(GUI.skin.label) { fontSize = 46, alignment = TextAnchor.MiddleCenter, richText = true };
                _mid = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
                _small = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
            }
            float w = Screen.width, h = Screen.height;

            if (_showCredits)
            {
                GUI.Label(new Rect(0, h * 0.08f, w, 70), "Credits", _big);
                GUI.Label(new Rect(w * 0.15f, h * 0.22f, w * 0.7f, h * 0.6f), credits, _mid);
                GUI.Label(new Rect(0, h * 0.88f, w, 30), "Any Shift: back", _small);
                return;
            }

            GUI.Label(new Rect(0, h * 0.08f, w, 110), "SHIFT+SHIFT", _title);
            GUI.Label(new Rect(0, h * 0.08f + 105, w, 30), "A rhythm runner played with two keys", _small);

            Entry e = _entries[_index];
            GUI.Label(new Rect(0, h * 0.40f, w, 70), $"<color=#888888>◀</color>     {e.Title}     <color=#888888>▶</color>", _big);
            GUI.Label(new Rect(w * 0.1f, h * 0.40f + 75, w * 0.8f, 40), e.Detail, _mid);

            string dots = "";
            for (int i = 0; i < _entries.Count; i++) dots += i == _index ? "● " : "○ ";
            GUI.Label(new Rect(0, h * 0.40f + 130, w, 30), dots.TrimEnd(), _mid);

            GUI.Label(new Rect(0, h * 0.74f, w, 34), "<b>Left Shift</b> / <b>Right Shift</b>: choose      <b>Both together</b>: open", _mid);
            GUI.Label(new Rect(w * 0.1f, h * 0.86f, w * 0.8f, 50),
                "Windows: pressing Shift five times in a row opens a Sticky Keys box. Choose No, or turn the shortcut off " +
                "in Settings > Accessibility > Keyboard. Keep this page in focus while you play.", _small);
        }
    }
}
