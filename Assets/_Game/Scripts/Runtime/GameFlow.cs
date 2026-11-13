using System.Collections;
using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// Run loop for the Run scene (playbook 1.4): loads a level by name, two-bar count-in with beat pulses, play,
    /// fail (flash + hit-stop, then any Shift restarts), complete (results panel, local best). Restart reloads the
    /// scene and starts at once, so the run is moving again well under a second after the press.
    /// Flow (playbook 2.5): the menu starts a level with <see cref="Play"/>. Results: Left = retry, Right = next
    /// level, Both = menu. Esc pauses (Left = resume, Right = restart, Both = menu) or leaves a failed or finished run.
    /// Keys come from ShiftInput, so Web builds use the browser key reader (Docs/INPUT.md).
    /// </summary>
    [DefaultExecutionOrder(-300)] // give the Conductor its level before it (and the spawner) wake up
    public sealed class GameFlow : MonoBehaviour
    {
        public enum Phase { Ready, CountIn, Playing, Failed, Results }

        // V2: point values changed on day 17 (50 / 20 / 5); bests saved under the old values are left behind.
        public const string BestKeyPrefix = "ShiftShift.BestV2.";

        [SerializeField] private List<TextAsset> levels = new List<TextAsset>();
        [SerializeField] private Conductor conductor;
        [SerializeField] private Runner runner;
        [Tooltip("Optional; found in the scene if empty. Menu keys (retry, next, pause) are read through it.")]
        [SerializeField] private ShiftInput input;
        [SerializeField] private string menuScene = "Menu";
        [SerializeField, Min(0f)] private float hitStopSeconds = 0.06f;
        [SerializeField, Min(0.01f)] private float flashSeconds = 0.3f;

        /// <summary>Level asset name to play next (the menu sets it later). Null = first in the list.</summary>
        public static string LevelToLoad;
        private static bool _autoStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Statics survive between Play sessions when domain reload is off: start clean each time.
            LevelToLoad = null;
            _autoStart = false;
        }

        /// <summary>Start a level from another scene (the menu). The run starts at once: no "press Shift" screen.</summary>
        public static void Play(string levelAssetName, string runScene = "Run")
        {
            LevelToLoad = levelAssetName;
            _autoStart = true;
            SceneManager.LoadScene(runScene);
        }

        /// <summary>"level03" -> "level04". Null when the name has no number at the end.</summary>
        public static string NextLevelName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            int i = name.Length;
            while (i > 0 && char.IsDigit(name[i - 1])) i--;
            if (i == name.Length || !int.TryParse(name.Substring(i), out int n)) return null;
            return name.Substring(0, i) + (n + 1).ToString(new string('0', name.Length - i));
        }

        public Phase Current { get; private set; } = Phase.Ready;
        public bool IsPaused => conductor != null && conductor.IsPaused;
        public ScoreKeeper Score { get; } = new ScoreKeeper();

        private string _bestKey;
        private int _best;
        private bool _newBest;
        private bool _canRestart;
        private float _flash;
        private string _lastGrade;
        private float _lastGradeAt = -10f;
        private readonly ShiftMenuKeys _keys = new ShiftMenuKeys();
        private bool _prevLeft, _prevRight;
        private string _nextLevel; // asset name of the next level, if it is in the list

        private void Awake()
        {
            TextAsset json = Pick();
            if (json == null || conductor == null || runner == null)
            {
                Debug.LogError("[GameFlow] Needs levels, a Conductor and a Runner.", this);
                enabled = false;
                return;
            }
            if (!conductor.SetLevel(json))
            {
                enabled = false;
                return;
            }
            _bestKey = BestKeyPrefix + json.name;
            _best = PlayerPrefs.GetInt(_bestKey, 0);

            if (input == null) input = FindAnyObjectByType<ShiftInput>();
            string next = NextLevelName(json.name);
            foreach (TextAsset t in levels)
                if (t != null && t.name == next) _nextLevel = next;
        }

        private TextAsset Pick()
        {
            if (levels.Count == 0) return null;
            if (!string.IsNullOrEmpty(LevelToLoad))
            {
                foreach (TextAsset t in levels)
                    if (t != null && t.name == LevelToLoad) return t;
                Debug.LogWarning($"[GameFlow] Level '{LevelToLoad}' is not in the list; playing the first one.");
            }
            return levels[0];
        }

        private void Start()
        {
            if (!enabled) return;
            runner.RunFailed += OnFailed;
            runner.RunCompleted += OnCompleted;
            runner.Judged += OnJudged;
            if (_autoStart)
            {
                _autoStart = false;
                conductor.StartNow();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (runner == null) return;
            runner.RunFailed -= OnFailed;
            runner.RunCompleted -= OnCompleted;
            runner.Judged -= OnJudged;
        }

        private void Update()
        {
            if (Current == Phase.Ready && conductor.IsStarted) Current = Phase.CountIn;
            if (Current == Phase.CountIn && conductor.CurrentBeat >= 0) Current = Phase.Playing;
            if (_flash > 0f) _flash = Mathf.Max(0f, _flash - Time.unscaledDeltaTime / flashSeconds);

            bool left = input != null && input.IsDown(Side.Left), right = input != null && input.IsDown(Side.Right);
            bool pressed = (left && !_prevLeft) || (right && !_prevRight);
            _prevLeft = left;
            _prevRight = right;
            MenuKey key = _keys.Poll(left, right);
            Keyboard kb = Keyboard.current;
            bool esc = kb != null && kb.escapeKey.wasPressedThisFrame;

            if (IsPaused)
            {
                if (esc || key == MenuKey.Left) SetPaused(false);
                else if (key == MenuKey.Right) Restart();
                else if (key == MenuKey.Both) ToMenu();
            }
            else if (Current == Phase.CountIn || Current == Phase.Playing)
            {
                if (esc) SetPaused(true);
            }
            else if (Current == Phase.Failed)
            {
                if (esc) ToMenu();
                else if (_canRestart && pressed) Restart(); // any Shift, at once: the fast retry
            }
            else if (Current == Phase.Results)
            {
                if (key == MenuKey.Left) Restart();
                else if (key == MenuKey.Right) { if (_nextLevel != null) Play(_nextLevel, gameObject.scene.name); else ToMenu(); }
                else if (key == MenuKey.Both || esc) ToMenu();
            }
        }

        private void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            if (paused)
            {
                conductor.Pause();
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
                if (input != null) input.ResetState(); // presses made in the pause menu are not gestures
                conductor.Resume();
            }
            _keys.Reset(input != null && input.IsDown(Side.Left), input != null && input.IsDown(Side.Right));
        }

        private void ToMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
            else
            {
                Debug.LogWarning($"[GameFlow] Scene '{menuScene}' is not in the build list; restarting the level.");
                Restart();
            }
        }

        private void OnJudged(Obstacle o, Grade g)
        {
            Score.Add(g);
            if (g == Grade.None) return;
            _lastGrade = g.ToString().ToUpperInvariant();
            _lastGradeAt = Time.unscaledTime;
        }

        private void OnFailed(Obstacle o)
        {
            Current = Phase.Failed;
            _flash = 1f;
            StartCoroutine(HitStop());
        }

        private IEnumerator HitStop()
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(hitStopSeconds);
            Time.timeScale = 1f;
            _canRestart = true;
        }

        private void OnCompleted()
        {
            Current = Phase.Results;
            // A key still held from the last obstacle must not pick a results option.
            _keys.Reset(input != null && input.IsDown(Side.Left), input != null && input.IsDown(Side.Right));
            int points = (int)Score.Points;
            if (points > _best)
            {
                _best = points;
                _newBest = true;
                PlayerPrefs.SetInt(_bestKey, points);
                PlayerPrefs.Save();
            }
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            _autoStart = true; // audio is already unlocked by the first press
            LevelToLoad = conductor.LevelAssetName;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        // ---------- placeholder UI ----------

        private GUIStyle _big, _mid, _small;

        private void OnGUI()
        {
            if (!enabled || conductor.Level == null) return;
            EnsureStyles();
            float w = Screen.width, h = Screen.height;

            switch (Current)
            {
                case Phase.Ready:
                    GUI.Label(new Rect(0, h * 0.30f, w, 60), $"<b>{conductor.Level.name}</b>", _mid);
                    GUI.Label(new Rect(0, h * 0.40f, w, 80), "Press Shift to start", _big);
                    GUI.Label(new Rect(0, h * 0.55f, w, 30),
                        $"Best {_best}   ·   F1 debug   ·   F2 calibrate (offset {conductor.UserOffsetMs:+0;-0;0} ms)", _small);
                    break;

                case Phase.CountIn:
                {
                    // Last bar counts down 4-3-2-1; any earlier count-in beats show "Get ready" (ELIA, day 17: two
                    // bars of 1-2-3-4 read as counting twice).
                    double beat = conductor.CurrentBeat;
                    if (beat >= -conductor.CountInBeats)
                    {
                        int whole = (int)System.Math.Floor(beat);   // -8 .. -1
                        float frac = (float)(beat - whole);
                        if (whole >= -4)
                        {
                            // Last bar: 4-3-2-1, each number pulsing on its beat.
                            var style = new GUIStyle(_big) { fontSize = 90 + Mathf.RoundToInt(70 * (1f - frac)) };
                            GUI.Label(new Rect(0, h * 0.3f, w, 200), (-whole).ToString(), style);
                        }
                        else
                        {
                            // Earlier bar: "Get ready" stays put (ELIA: pulsing read as showing 4 times); a dot per beat.
                            var style = new GUIStyle(_big) { fontSize = 64 };
                            GUI.Label(new Rect(0, h * 0.3f, w, 120), "Get ready", style);
                            int lit = (whole + conductor.CountInBeats) % 4 + 1; // 1..4 across each "Get ready" bar
                            string dots = "";
                            for (int i = 1; i <= 4; i++) dots += i <= lit ? "● " : "○ ";
                            GUI.Label(new Rect(0, h * 0.3f + 110, w, 60), dots.TrimEnd(), _mid);
                        }
                    }
                    DrawScore(w);
                    break;
                }

                case Phase.Playing:
                    if (conductor.CurrentBeat < 1.0)
                    {
                        float fade = 1f - (float)conductor.CurrentBeat;   // "GO" for the first beat
                        var go = new GUIStyle(_big) { fontSize = 140 };
                        Color old = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, fade);
                        GUI.Label(new Rect(0, h * 0.3f, w, 200), "GO", go);
                        GUI.color = old;
                    }
                    DrawScore(w);
                    break;

                case Phase.Failed:
                    DrawScore(w);
                    if (_canRestart)
                    {
                        GUI.Label(new Rect(0, h * 0.45f, w, 80), "Shift to retry", _big);
                        GUI.Label(new Rect(0, h * 0.45f + 70, w, 30), "Esc: menu", _small);
                    }
                    break;

                case Phase.Results:
                    GUI.Box(new Rect(w * 0.3f, h * 0.2f, w * 0.4f, h * 0.55f), GUIContent.none);
                    GUI.Label(new Rect(0, h * 0.23f, w, 60), "<b>Level complete</b>", _mid);
                    GUI.Label(new Rect(0, h * 0.33f, w, 40), $"Score  {Score.Points}" + (_newBest ? "   <color=yellow>NEW BEST</color>" : ""), _mid);
                    GUI.Label(new Rect(0, h * 0.41f, w, 40), $"Best streak  {Score.BestStreak}", _mid);
                    GUI.Label(new Rect(0, h * 0.49f, w, 40), $"Perfect  {Score.PerfectPercent:0}%", _mid);
                    GUI.Label(new Rect(0, h * 0.57f, w, 40), $"Best score  {_best}", _mid);
                    GUI.Label(new Rect(0, h * 0.67f, w, 30),
                        "Left Shift: retry   ·   Right Shift: " + (_nextLevel != null ? "next level" : "menu") + "   ·   Both: menu", _small);
                    break;
            }

            if (IsPaused)
            {
                GUI.Box(new Rect(w * 0.3f, h * 0.28f, w * 0.4f, h * 0.4f), GUIContent.none);
                GUI.Label(new Rect(0, h * 0.32f, w, 60), "<b>Paused</b>", _big);
                GUI.Label(new Rect(0, h * 0.45f, w, 30), "Left Shift or Esc: resume", _mid);
                GUI.Label(new Rect(0, h * 0.51f, w, 30), "Right Shift: restart", _mid);
                GUI.Label(new Rect(0, h * 0.57f, w, 30), "Both: menu", _mid);
                return;
            }

            if (Time.unscaledTime - _lastGradeAt < 0.5f && (Current == Phase.Playing || Current == Phase.Failed))
                GUI.Label(new Rect(0, h * 0.62f, w, 60), _lastGrade, _mid);

            if (_flash > 0f)
            {
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, _flash * 0.8f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                GUI.color = old;
            }
        }

        private void DrawScore(float w)
        {
            string mult = Score.Multiplier > 1 ? $"   <color=yellow>x{Score.Multiplier}</color>" : "";
            GUI.Label(new Rect(0, 12, w, 40), $"<b>{Score.Points}</b>   streak {Score.Streak}{mult}", _mid);
        }

        private void EnsureStyles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, richText = true };
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, richText = true };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, richText = true };
        }
    }
}
