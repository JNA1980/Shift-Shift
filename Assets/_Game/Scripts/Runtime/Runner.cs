using System;
using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// The auto-runner (playbook 1.2). Rules live in RunnerModel (engine-free, tested); this component feeds it
    /// gestures, moves the visuals, and judges obstacles as the runner reaches them. Docs/RUNNER.md.
    ///
    /// - Forward position comes from the song: z = metresPerBeat * beat (no frame time; CLAUDE.md), so obstacles
    ///   placed at beat * metresPerBeat are reached exactly on their beat, whatever the BPM.
    /// - With a Conductor: waits (and ignores gestures) until it starts; the run ends at the level's last beat.
    ///   Without one (Greybox scene): runs on ShiftInput's stub clock until the end of the track.
    /// - Collisions are judged by rules at the obstacle's song time (decision A: actions count from key-down),
    ///   so an obstacle may be judged up to one tap/hold threshold late while a press is still undecided.
    /// - Visuals start when a gesture is recognised (lane ease, jump arc, slide pose), which can be later than
    ///   the key-down the rules use.
    /// </summary>
    public sealed class Runner : MonoBehaviour
    {
        public enum RunState { Running, Failed, Completed }

        [SerializeField] private ShiftInput input;
        [Tooltip("Optional. When set, the runner waits for it to start and uses its clock and level length.")]
        [SerializeField] private Conductor conductor;
        [SerializeField] private TrackBuilder track;
        [SerializeField] private Transform body;
        [SerializeField] private Renderer bodyRenderer;

        [Header("Movement")]
        [Tooltip("Distance per beat. Speed = this x BPM / 60 (5 m/beat: 10 m/s at 120 BPM, 8.3 m/s at 100 BPM).")]
        [SerializeField, Min(1f)] private float metresPerBeat = 5f;
        [SerializeField, Min(0.5f)] private float laneWidth = 2f;
        [SerializeField, Min(0.01f)] private float laneEaseSeconds = 0.12f;
        [SerializeField, Min(0.1f)] private float jumpSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;
        [SerializeField, Range(0.2f, 1f)] private float slideHeightScale = 0.5f;

        [Header("Materials")]
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material phaseMaterial;

        public event Action<Obstacle> RunFailed;
        public event Action RunCompleted;
        /// <summary>Each obstacle once, when it is cleared (or Miss when hit). See Judge / Docs/SCORING.md.</summary>
        public event Action<Obstacle, Grade> Judged;

        public RunState State { get; private set; } = RunState.Running;
        public bool IsSprinting => _model != null && _model.IsSprinting(Now);
        public bool IsPhased => _model != null && _model.IsPhased(Now);
        public float MetresPerBeat => metresPerBeat;
        public float LaneWidth => laneWidth;
        public float JumpSeconds => jumpSeconds;

        private struct Check
        {
            public ObstacleInfo Info; // Info.Time is filled in from Beat when judged (the clock can change at start)
            public double Beat;
            public double TargetBeat; // what the answer is graded against
            public bool Final;        // the obstacle's last check: grade it here
            public Obstacle Obstacle;
        }

        private RunnerModel _model;
        private readonly List<Check> _checks = new List<Check>();
        private int _nextCheck;
        private Obstacle _failedOn;

        // Visual state (song time).
        private float _visualX, _easeFromX, _easeToX;
        private double _easeStart = double.NegativeInfinity;
        private double _jumpVisualStart = double.NegativeInfinity;
        private Vector3 _bodyBaseScale, _bodyBasePos;
        private float _frozenZ;

        private IClock Clock => conductor != null ? conductor : input.Clock;
        private double Now => Clock.SongTime;
        private bool Waiting => conductor != null && !conductor.IsStarted;

        private void Start()
        {
            if (input == null || !input.enabled || input.Clock == null || track == null || body == null)
            {
                Debug.LogError("Runner: assign ShiftInput (with GestureSettings), TrackBuilder and Body.", this);
                enabled = false;
                return;
            }
            if (conductor != null && conductor.Level == null)
            {
                Debug.LogError("Runner: the Conductor has no valid level.", this);
                enabled = false;
                return;
            }

            _model = new RunnerModel(new RunnerRules
            {
                JumpSeconds = jumpSeconds,
                ChargeTaps = input.Settings.chargeTaps,
                ChargeGraceSeconds = input.Settings.goodMs / 1000.0,
            });
            input.GestureFired += OnGestureFired;
            input.HoldStarted += OnHoldStarted;
            input.HoldEnded += OnHoldEnded;

            _bodyBaseScale = body.localScale;
            _bodyBasePos = body.localPosition;
            CollectObstacles();
        }

        private void OnDestroy()
        {
            if (input == null || _model == null) return;
            input.GestureFired -= OnGestureFired;
            input.HoldStarted -= OnHoldStarted;
            input.HoldEnded -= OnHoldEnded;
        }

        private float EndZ => conductor != null ? (float)(conductor.Level.lengthBeats * metresPerBeat) : track.LengthMetres;

        private void CollectObstacles()
        {
            _checks.Clear();
            foreach (Obstacle o in FindObjectsByType<Obstacle>())
            {
                Vector3 p = o.transform.position;
                int lane = o.laneIsOpening ? o.openLane : Mathf.Clamp(Mathf.RoundToInt(p.x / laneWidth), -1, 1);
                var info = new ObstacleInfo
                {
                    Kind = o.kind, Layer = o.layer, Lane = lane, AllLanes = o.allLanes, LaneIsOpening = o.laneIsOpening,
                };
                double target = o.hasTargetBeat ? o.targetBeat : p.z / metresPerBeat;

                if (o.kind == ObstacleKind.Gap)
                {
                    // Must be in the air at both edges of a gap.
                    AddCheck(info, o, p.z - o.depth * 0.5f, target, false);
                    AddCheck(info, o, p.z + o.depth * 0.5f, target, true);
                }
                else
                {
                    AddCheck(info, o, p.z, target, true);
                }
            }
            _checks.Sort((a, b) => a.Beat.CompareTo(b.Beat));
            _nextCheck = 0;
            Debug.Log($"[Run] {_checks.Count} obstacle checks, run ends at {EndZ:0} m, {metresPerBeat:0.#} m per beat.");
        }

        private void AddCheck(ObstacleInfo info, Obstacle o, float z, double targetBeat, bool final)
        {
            _checks.Add(new Check { Info = info, Beat = z / metresPerBeat, TargetBeat = targetBeat, Final = final, Obstacle = o });
        }

        private void OnHoldStarted(GestureType type, double time)
        {
            if (!Waiting) _model.OnHoldStarted(type, time);
        }

        private void OnHoldEnded(GestureType type, double time)
        {
            if (!Waiting) _model.OnHoldEnded(type, time);
        }

        private void OnGestureFired(GestureType type, double time)
        {
            if (Waiting) return; // gestures before the start are on another clock
            double now = Now;
            _model.OnGestureFired(type, time, input.AlternateStreak, now);
            if (State != RunState.Running) return;

            if (type == GestureType.TapLeft || type == GestureType.TapRight)
            {
                _easeFromX = _visualX;
                _easeToX = _model.LaneAt(now) * laneWidth;
                _easeStart = now;
            }
            else if (type == GestureType.BothTap)
            {
                _jumpVisualStart = now;
            }
        }

        private void Update()
        {
            if (State != RunState.Running) return;

            double now = Now;
            float z = (float)(Clock.BeatAt(now) * metresPerBeat);
            if (Waiting)
            {
                ApplyVisuals(now, z);
                return;
            }

            while (_nextCheck < _checks.Count && Clock.SongTimeAtBeat(_checks[_nextCheck].Beat) <= now)
            {
                Check c = _checks[_nextCheck];
                c.Info.Time = Clock.SongTimeAtBeat(c.Beat);
                // Decision A: a press made before this obstacle may still turn out to be the answer. Wait for it.
                if (input.TryGetUnresolvedPress(out double down) && down <= c.Info.Time) break;
                // A charge in progress may still complete within the grace window after a wall's moment.
                if (c.Info.Kind == ObstacleKind.Wall && input.AlternateStreak > 0 && !_model.HasCharge(c.Info.Time) &&
                    now < c.Info.Time + input.Settings.goodMs / 1000.0) break;

                ObstacleResolution r = _model.ResolveWithAnswer(c.Info);
                if (r.Outcome == ObstacleOutcome.Hit)
                {
                    Judged?.Invoke(c.Obstacle, Grade.Miss);
                    Fail(c, z);
                    return;
                }
                if (r.Outcome == ObstacleOutcome.Break) c.Obstacle.gameObject.SetActive(false);
                if (c.Final)
                {
                    GestureSettings gs = input.Settings;
                    Grade g = Judge.GradeOf(r, Clock.SongTimeAtBeat(c.TargetBeat), gs.perfectMs / 1000.0, gs.goodMs / 1000.0);
                    Judged?.Invoke(c.Obstacle, g);
                }
                _nextCheck++;
            }

            if (z >= EndZ)
            {
                State = RunState.Completed;
                _frozenZ = EndZ;
                Debug.Log("[Run] Completed.");
                RunCompleted?.Invoke();
            }

            ApplyVisuals(now, State == RunState.Running ? z : _frozenZ);
        }

        private void Fail(Check c, float z)
        {
            State = RunState.Failed;
            _failedOn = c.Obstacle;
            _frozenZ = z;
            if (conductor != null) conductor.StopSong();
            Debug.Log($"[Run] Failed on {c.Obstacle.name} ({c.Info.Kind}, {c.Info.Layer}, " +
                      $"{(c.Info.AllLanes ? "all lanes" : "lane " + c.Info.Lane)}) at {c.Info.Time:F3}s.");
            RunFailed?.Invoke(c.Obstacle);
        }

        private void ApplyVisuals(double now, float z)
        {
            float p = Mathf.Clamp01((float)((now - _easeStart) / laneEaseSeconds));
            _visualX = Mathf.Lerp(_easeFromX, _easeToX, p * p * (3f - 2f * p));

            float y = 0f;
            float jp = (float)((now - _jumpVisualStart) / jumpSeconds);
            if (jp >= 0f && jp <= 1f) y = 4f * jumpHeight * jp * (1f - jp);

            transform.position = new Vector3(_visualX, y, z);

            bool sliding = _model.IsSliding(now);
            float sy = sliding ? slideHeightScale : 1f;
            body.localScale = new Vector3(_bodyBaseScale.x, _bodyBaseScale.y * sy, _bodyBaseScale.z);
            body.localPosition = new Vector3(_bodyBasePos.x, _bodyBasePos.y * sy, _bodyBasePos.z);

            if (bodyRenderer != null && normalMaterial != null && phaseMaterial != null)
                bodyRenderer.sharedMaterial = _model.IsPhased(now) ? phaseMaterial : normalMaterial;
        }

        private GUIStyle _style;

        private void OnGUI()
        {
            if (_model == null) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };

            double now = Now;
            string status = Waiting ? "waiting for start" : State == RunState.Running ? "running"
                : State == RunState.Completed ? "<color=lime>COMPLETED</color>"
                : $"<color=red>FAILED</color> on {(_failedOn != null ? _failedOn.name : "?")}";

            GUILayout.BeginArea(new Rect(Screen.width - 360, 10, 350, 190), GUI.skin.box);
            GUILayout.Label($"Run: {status}", _style);
            GUILayout.Label($"z {transform.position.z:0.0} / {EndZ:0} m   lane {_model.LaneAt(now)}   beat {Clock.BeatAt(now):0.00}", _style);
            GUILayout.Label(
                $"{Flag("slide", _model.IsSliding(now))} {Flag("sprint", _model.IsSprinting(now))} " +
                $"{Flag("air", _model.IsAirborne(now))} {Flag("phase", _model.IsPhased(now))} " +
                $"{Flag("charge", _model.HasCharge(now))}", _style);
            if (State != RunState.Running) GUILayout.Label("Stop and press Play to retry", _style);
            GUILayout.EndArea();
        }

        private static string Flag(string name, bool on) => on ? $"<color=lime>{name}</color>" : $"<color=grey>{name}</color>";
    }
}
