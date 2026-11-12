using System;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftShift
{
    /// <summary>
    /// Owns the song clock (playbooks 1.3, 1.4). Waits for the first Shift press (also the browser's audio unlock) or
    /// <see cref="StartNow"/>, then schedules the level's audio with PlayScheduled after a short delay plus a count-in,
    /// and becomes ShiftInput's clock. All timing comes from AudioSettings.dspTime; never Time.time.
    ///
    /// SongTime = seconds since beat 0 = (dspTime - audio start) - level offsetMs - player's calibrated offset.
    /// Negative during the delay and count-in (beats -countInBeats..0).
    /// </summary>
    [DefaultExecutionOrder(-200)] // parse the level before the spawner (Awake) and input (Update) need it
    [RequireComponent(typeof(AudioSource))]
    public sealed class Conductor : MonoBehaviour, IClock
    {
        public const string UserOffsetKey = "ShiftShift.UserOffsetMs";

        [Tooltip("Level to play. GameFlow sets this from code in the Run scene.")]
        [SerializeField] private TextAsset levelJson;
        [SerializeField] private ShiftInput input;
        [Tooltip("Seconds between the start and the first count-in beat (lets PlayScheduled be exact).")]
        [SerializeField, Min(0.1f)] private float startDelay = 0.5f;
        [Tooltip("Beats before beat 0 (two bars = 8). The level's audio starts on beat 0.")]
        [SerializeField, Min(0)] private int countInBeats = 8;
        [SerializeField] private bool showStartPrompt = true;

        /// <summary>Fires once per whole beat after the start, including the negative count-in beats.</summary>
        public event Action<int> Beat;

        public LevelData Level { get; private set; }
        public string LevelAssetName { get; private set; }
        public bool IsStarted { get; private set; }
        public int CountInBeats => countInBeats;
        /// <summary>The level's starting tempo (also the count-in's). See <see cref="BpmAtBeat"/> for levels that change tempo.</summary>
        public double Bpm => Level != null ? Level.bpm : 120.0;
        public double BpmAtBeat(double beat) => _tempo != null ? _tempo.BpmAtBeat(beat) : Bpm;
        public double CurrentBeat => BeatAt(SongTime);
        public double UserOffsetMs => _userOffsetMs;

        public bool IsPaused { get; private set; }

        public double SongTime => IsStarted
            ? (IsPaused ? _pausedAtDsp : AudioSettings.dspTime) - _audioStartDsp - _offsetSeconds
            : -(startDelay + CountInSeconds) - _offsetSeconds; // frozen at the start value until the run begins

        // Through the tempo map: identical to beat x 60 / bpm for a level with one tempo.
        public double BeatAt(double songTime) => _tempo != null ? _tempo.BeatAtTime(songTime) : songTime * Bpm / 60.0;
        public double SongTimeAtBeat(double beat) => _tempo != null ? _tempo.TimeAtBeat(beat) : beat * 60.0 / Bpm;

        private double CountInSeconds => countInBeats * 60.0 / Bpm;

        private TempoMap _tempo;
        private AudioSource _source;
        private AudioClip _clip;
        private double _audioStartDsp;
        private double _pausedAtDsp;
        private double _offsetSeconds;
        private double _userOffsetMs;
        private int _lastBeat = int.MinValue;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            if (Level == null) SetLevel(levelJson);
        }

        /// <summary>Loads and validates a level. Call before Start (GameFlow does this in its Awake).</summary>
        public bool SetLevel(TextAsset json)
        {
            Level = LevelLoader.Load(json);
            if (Level == null)
            {
                enabled = false;
                return false;
            }
            levelJson = json;
            _tempo = new TempoMap(Level.bpm, Level.tempo);
            LevelAssetName = json.name;
            _userOffsetMs = PlayerPrefs.GetFloat(UserOffsetKey, 0f);
            _offsetSeconds = (Level.offsetMs + _userOffsetMs) / 1000.0;

            _clip = Resources.Load<AudioClip>(Level.audioClip);
            if (_clip == null)
                Debug.LogWarning($"[Conductor] Audio '{Level.audioClip}' not found in a Resources folder; running silent.");
            Debug.Log($"[Conductor] {Level.name}: {Level.bpm} BPM, {Level.lengthBeats} beats, {Level.events.Length} events, " +
                      $"user offset {_userOffsetMs:0} ms.");
            return true;
        }

        private void LateUpdate()
        {
            // Start in LateUpdate, after ShiftInput has handled this frame's press on the old clock, so the swap in
            // StartNow discards the start press instead of it becoming a gesture at the start of the song.
            if (IsStarted || Level == null) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
                StartNow();
        }

        private void Update()
        {
            if (!IsStarted) return;

            int beat = (int)Math.Floor(CurrentBeat);
            if (_lastBeat == int.MinValue) _lastBeat = beat - 1;
            while (_lastBeat < beat)
            {
                _lastBeat++;
                Beat?.Invoke(_lastBeat);
            }
        }

        /// <summary>Starts the delay + count-in now. Needs a prior user gesture in browsers (audio unlock).</summary>
        public void StartNow()
        {
            if (IsStarted || Level == null) return;
            _audioStartDsp = AudioSettings.dspTime + startDelay + CountInSeconds;
            if (_clip != null)
            {
                _source.clip = _clip;
                _source.PlayScheduled(_audioStartDsp);
            }
            IsStarted = true;
            // From here on, gestures are stamped in song time. A press still held is discarded by the swap.
            if (input != null) input.SetClock(this);
            Debug.Log($"[Conductor] Started. Audio at dspTime {_audioStartDsp:F3}, count-in {countInBeats} beats.");
        }

        /// <summary>
        /// Freezes the song clock and the audio (pause menu). <see cref="Resume"/> moves the song's start forward by
        /// the time spent paused, so beats and audio line up again.
        /// </summary>
        public void Pause()
        {
            if (!IsStarted || IsPaused) return;
            _pausedAtDsp = AudioSettings.dspTime;
            IsPaused = true;
            if (_clip == null) return;
            // Audio not started yet (count-in): cancel the scheduled start; Resume schedules it again.
            if (_pausedAtDsp < _audioStartDsp) _source.Stop();
            else _source.Pause();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            double now = AudioSettings.dspTime;
            bool audioHadStarted = _pausedAtDsp >= _audioStartDsp;
            _audioStartDsp += now - _pausedAtDsp;
            IsPaused = false;
            if (_clip == null) return;
            if (audioHadStarted) _source.UnPause();
            else
            {
                _source.clip = _clip;
                _source.PlayScheduled(_audioStartDsp);
            }
        }

        public void StopSong()
        {
            if (_source != null) _source.Stop();
        }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (!showStartPrompt || IsStarted || Level == null) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, richText = true };
            GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, 60), $"<b>{Level.name}</b>  Press Shift to start", _style);
        }
    }
}
