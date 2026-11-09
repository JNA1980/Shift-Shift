using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ShiftShift
{
    /// <summary>
    /// Latency calibration screen (playbook 1.4), reached with F2 from the debug overlay. Plays clicks scheduled on
    /// the audio clock; the player taps either Shift on each one. The mean (tap - click) covers audio output delay,
    /// input delay and the player's own timing; it is saved and the Conductor delays the game by that much.
    /// Right Shift saves, Left Shift retries, Esc goes back without saving.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class LatencyCalibration : MonoBehaviour
    {
        [SerializeField] private string clickClip = "click_100";
        [Tooltip("Must match the click clip.")]
        [SerializeField] private float bpm = 100f;
        [SerializeField, Min(8)] private int clicks = 20;
        [Tooltip("First clicks to let the player find the beat; not measured.")]
        [SerializeField, Min(0)] private int warmUp = 4;
        [SerializeField] private string returnScene = "Run";

        private enum State { Ready, Running, Result }

        private AudioSource _source;
        private AudioClip _clip;
        private State _state;
        private double _start;
        private readonly List<double> _clickTimes = new List<double>();
        private readonly List<double> _taps = new List<double>();
        private Calibration.Result _result;
        private double Beat => 60.0 / bpm;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _clip = Resources.Load<AudioClip>(clickClip);
            if (_clip == null) Debug.LogError($"[Calibration] Click clip '{clickClip}' not found in a Resources folder.");
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            bool left = kb.leftShiftKey.wasPressedThisFrame, right = kb.rightShiftKey.wasPressedThisFrame;
            if (kb.escapeKey.wasPressedThisFrame) { Back(); return; }

            switch (_state)
            {
                case State.Ready:
                    if ((left || right) && _clip != null) Begin();
                    break;
                case State.Running:
                    double now = AudioSettings.dspTime; // same clock the Conductor uses
                    if (left || right) _taps.Add(now);
                    if (now > _clickTimes[_clickTimes.Count - 1] + Beat) Finish();
                    break;
                case State.Result:
                    if (right && _result.Count >= clicks / 2) Save();
                    else if (left) Begin();
                    break;
            }
        }

        private void Begin()
        {
            _start = AudioSettings.dspTime + 0.5;
            _source.clip = _clip;
            _source.PlayScheduled(_start);
            _source.SetScheduledEndTime(_start + clicks * Beat);
            _clickTimes.Clear();
            for (int k = warmUp; k < clicks; k++) _clickTimes.Add(_start + k * Beat);
            _taps.Clear();
            _state = State.Running;
        }

        private void Finish()
        {
            _source.Stop();
            _result = Calibration.Compute(_clickTimes, _taps, Beat * 0.4);
            _state = State.Result;
            Debug.Log($"[Calibration] mean {_result.MeanMs:0.0} ms, spread {_result.SpreadMs:0.0} ms, {_result.Count} taps.");
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(Conductor.UserOffsetKey, (float)_result.MeanMs);
            PlayerPrefs.Save();
            Back();
        }

        private void Back()
        {
            if (Application.CanStreamedLevelBeLoaded(returnScene)) SceneManager.LoadScene(returnScene);
            else Debug.LogWarning($"[Calibration] Scene '{returnScene}' is not in the build list.");
        }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, richText = true };
            float w = Screen.width, h = Screen.height;
            float saved = PlayerPrefs.GetFloat(Conductor.UserOffsetKey, 0f);

            string text;
            switch (_state)
            {
                case State.Ready:
                    text = "<b>Latency calibration</b>\n\nTap either Shift on every click.\n" +
                           $"The first {warmUp} clicks are practice.\n\nPress Shift to begin\n\n" +
                           $"<size=18>Current offset {saved:+0;-0;0} ms   ·   Esc: back</size>";
                    break;
                case State.Running:
                    int heard = Mathf.Clamp((int)((AudioSettings.dspTime - _start) / Beat) + 1, 0, clicks);
                    text = $"<b>Tap on the click</b>\n\nclick {heard} / {clicks}\ntaps {_taps.Count}";
                    break;
                default:
                    bool enough = _result.Count >= clicks / 2;
                    text = $"<b>Result</b>\n\nOffset {_result.MeanMs:+0;-0;0} ms\nspread ±{_result.SpreadMs:0} ms   ({_result.Count} taps counted)\n\n" +
                           (enough ? "Right Shift: save   ·   Left Shift: try again   ·   Esc: back"
                                   : "Not enough taps counted.\nLeft Shift: try again   ·   Esc: back") +
                           $"\n\n<size=18>A spread over ±40 ms means the taps were uneven: try again.</size>";
                    break;
            }
            GUI.Label(new Rect(0, 0, w, h), text, _style);
        }
    }
}
