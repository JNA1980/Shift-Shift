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
    /// - Forward position comes from song time: z = speed * Clock.SongTime (no frame time; CLAUDE.md).
    /// - Collisions are judged by rules at the obstacle's song time (decision A: actions count from key-down),
    ///   so an obstacle may be judged up to one tap/hold threshold late while a press is still undecided.
    /// - Visuals start when a gesture is recognised (lane ease, jump arc, slide pose), which can be later than
    ///   the key-down the rules use.
    /// </summary>
    public sealed class Runner : MonoBehaviour
    {
        public enum RunState { Running, Failed, Completed }

        [SerializeField] private ShiftInput input;
        [SerializeField] private TrackBuilder track;
        [SerializeField] private Transform body;
        [SerializeField] private Renderer bodyRenderer;

        [Header("Movement")]
        [Tooltip("Metres per second until the Conductor sets it from the BPM (day 12).")]
        [SerializeField, Min(1f)] private float stubSpeed = 10f;
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

        public RunState State { get; private set; } = RunState.Running;
        public bool IsSprinting => _model != null && _model.IsSprinting(Now);
        public bool IsPhased => _model != null && _model.IsPhased(Now);

        private struct Check
        {
            public ObstacleInfo Info;
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

        private double Now => input.Clock.SongTime;

        private void Start()
        {
            if (input == null || !input.enabled || input.Clock == null || track == null || body == null)
            {
                Debug.LogError("Runner: assign ShiftInput (with GestureSettings), TrackBuilder and Body.", this);
                enabled = false;
                return;
            }

            _model = new RunnerModel(new RunnerRules
            {
                JumpSeconds = jumpSeconds,
                ChargeTaps = input.Settings.chargeTaps,
            });
            input.GestureFired += OnGestureFired;
            input.HoldStarted += _model.OnHoldStarted;
            input.HoldEnded += _model.OnHoldEnded;

            _bodyBaseScale = body.localScale;
            _bodyBasePos = body.localPosition;
            CollectObstacles();
        }

        private void OnDestroy()
        {
            if (input == null || _model == null) return;
            input.GestureFired -= OnGestureFired;
            input.HoldStarted -= _model.OnHoldStarted;
            input.HoldEnded -= _model.OnHoldEnded;
        }

        private void CollectObstacles()
        {
            _checks.Clear();
            foreach (Obstacle o in FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
            {
                Vector3 p = o.transform.position;
                int lane = Mathf.Clamp(Mathf.RoundToInt(p.x / laneWidth), -1, 1);
                var info = new ObstacleInfo { Kind = o.kind, Layer = o.layer, Lane = lane, AllLanes = o.allLanes };

                if (o.kind == ObstacleKind.Gap)
                {
                    // Must be in the air at both edges of a gap.
                    AddCheck(info, o, p.z - o.depth * 0.5f);
                    AddCheck(info, o, p.z + o.depth * 0.5f);
                }
                else
                {
                    AddCheck(info, o, p.z);
                }
            }
            _checks.Sort((a, b) => a.Info.Time.CompareTo(b.Info.Time));
            _nextCheck = 0;
            Debug.Log($"[Run] {_checks.Count} obstacle checks, track {track.LengthMetres:0} m at {stubSpeed:0.#} m/s.");
        }

        private void AddCheck(ObstacleInfo info, Obstacle o, float z)
        {
            info.Time = z / stubSpeed;
            _checks.Add(new Check { Info = info, Obstacle = o });
        }

        private void OnGestureFired(GestureType type, double time)
        {
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
            float z = (float)(now * stubSpeed);

            while (_nextCheck < _checks.Count && _checks[_nextCheck].Info.Time <= now)
            {
                Check c = _checks[_nextCheck];
                // Decision A: a press made before this obstacle may still turn out to be the answer. Wait for it.
                if (input.TryGetUnresolvedPress(out double down) && down <= c.Info.Time) break;

                ObstacleOutcome outcome = _model.Resolve(c.Info);
                if (outcome == ObstacleOutcome.Hit)
                {
                    Fail(c, z);
                    return;
                }
                if (outcome == ObstacleOutcome.Break) c.Obstacle.gameObject.SetActive(false);
                _nextCheck++;
            }

            if (z >= track.LengthMetres)
            {
                State = RunState.Completed;
                _frozenZ = track.LengthMetres;
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
            string status = State == RunState.Running ? "running"
                : State == RunState.Completed ? "<color=lime>COMPLETED</color>"
                : $"<color=red>FAILED</color> on {(_failedOn != null ? _failedOn.name : "?")}";

            GUILayout.BeginArea(new Rect(Screen.width - 360, 10, 350, 190), GUI.skin.box);
            GUILayout.Label($"Run: {status}", _style);
            GUILayout.Label($"z {transform.position.z:0.0} / {track.LengthMetres:0} m   lane {_model.LaneAt(now)}", _style);
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
