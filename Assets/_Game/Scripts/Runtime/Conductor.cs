using System;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftShift
{
    /// <summary>
    /// Owns the song clock (playbook 1.3). Waits for the first Shift press (also the browser's audio unlock), then
    /// schedules the level's audio with PlayScheduled at dspTime + <see cref="startDelay"/> and becomes ShiftInput's
    /// clock. All timing comes from AudioSettings.dspTime; never Time.time.
    ///
    /// SongTime: seconds since beat 0 = (dspTime - scheduled start) - offsetMs. Negative during the lead-in.
    /// </summary>
    [DefaultExecutionOrder(-200)] // parse the level before the spawner (Awake) and input (Update) need it
    [RequireComponent(typeof(AudioSource))]
    public sealed class Conductor : MonoBehaviour, IClock
    {
        [SerializeField] private TextAsset levelJson;
        [SerializeField] private ShiftInput input;
        [Tooltip("Seconds between the start key press and the audio starting (lets PlayScheduled be exact).")]
        [SerializeField, Min(0.1f)] private float startDelay = 1f;

        /// <summary>Fires once per whole beat after the start (including negative beats of the lead-in).</summary>
        public event Action<int> Beat;

        public LevelData Level { get; private set; }
        public bool IsStarted { get; private set; }
        public double Bpm => Level != null ? Level.bpm : 120.0;
        public double CurrentBeat => BeatAt(SongTime);

        public double SongTime => IsStarted
            ? AudioSettings.dspTime - _startDsp - _offsetSeconds
            : -startDelay - _offsetSeconds; // frozen at the start value until the run begins

        public double BeatAt(double songTime) => songTime * Bpm / 60.0;
        public double SongTimeAtBeat(double beat) => beat * 60.0 / Bpm;

        private AudioSource _source;
        private AudioClip _clip;
        private double _startDsp;
        private double _offsetSeconds;
        private int _lastBeat = int.MinValue;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;

            Level = LevelLoader.Load(levelJson);
            if (Level == null)
            {
                enabled = false;
                return;
            }
            _offsetSeconds = Level.offsetMs / 1000.0;

            _clip = Resources.Load<AudioClip>(Level.audioClip);
            if (_clip == null)
                Debug.LogWarning($"[Conductor] Audio '{Level.audioClip}' not found in a Resources folder; running silent.");
            Debug.Log($"[Conductor] {Level.name}: {Level.bpm} BPM, {Level.lengthBeats} beats, {Level.events.Length} events. Press Shift to start.");
        }

        private void LateUpdate()
        {
            // Start in LateUpdate, after ShiftInput has handled this frame's press on the old clock, so the swap in
            // StartSong discards the start press instead of it becoming a gesture at the start of the song.
            if (IsStarted) return;
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
                StartSong();
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

        private void StartSong()
        {
            _startDsp = AudioSettings.dspTime + startDelay;
            if (_clip != null)
            {
                _source.clip = _clip;
                _source.PlayScheduled(_startDsp);
            }
            IsStarted = true;
            // From here on, gestures are stamped in song time. The start press itself is discarded by the swap.
            if (input != null) input.SetClock(this);
            Debug.Log($"[Conductor] Started. Audio at dspTime {_startDsp:F3}.");
        }

        public void StopSong()
        {
            if (_source != null) _source.Stop();
        }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (IsStarted || Level == null) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter, richText = true };
            GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, 60), $"<b>{Level.name}</b>  Press Shift to start", _style);
        }
    }
}
