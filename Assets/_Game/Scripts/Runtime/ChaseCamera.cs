using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Follows the runner from behind ALONG THE TRACK (settings in <see cref="CameraFeel"/>): it sits on the track's
    /// path behind the runner and looks at the path ahead, so it swings through bends and tips over rises with the
    /// road. Sideways and vertical movement lag slightly; forward movement is locked so it never drifts back. Field of view widens while
    /// sprinting and more while phased; the camera rolls slightly into lane changes.
    /// Uses frame time for smoothing only: camera feel, not rhythm timing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Runner target;
        [SerializeField] private CameraFeel feel;

        private Camera _cam;
        private float _x, _y, _vx, _vy, _roll, _rollVel, _lastTargetX;
        private bool _hasLast;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (feel == null)
            {
                Debug.LogWarning("ChaseCamera: no CameraFeel asset assigned; using defaults.", this);
                feel = ScriptableObject.CreateInstance<CameraFeel>();
            }
            _cam.fieldOfView = feel.baseFov;
            _x = feel.offset.x;
            _y = feel.offset.y;
        }

        /// <summary>Extra camera height when the road ahead climbs (John, day 17: obstacles poked above the horizon).</summary>
        private float SlopeLift(TrackBuilder track, float s)
        {
            if (feel.slopeLiftAmount <= 0f) return 0f;
            double camS = s + feel.offset.z;
            // Not inside a tunnel: the ceiling is 0.2 m above the camera.
            foreach (double at in new[] { camS, (double)s })
                if (track.Path.TryGetSpan(at, out ShiftShift.Core.TrackPath.Span span) &&
                    span.Spec.Dressing == ShiftShift.Core.TrackPieces.Dressing.Tunnel) return 0f;
            double gain = track.Path.HeightAt(s + feel.slopeLookMetres) - track.Path.HeightAt(camS);
            return Mathf.Max(0f, (float)gain) * feel.slopeLiftAmount;
        }

        private void LateUpdate()
        {
            if (target == null || target.Track == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // Track positions: sideways and height relative to the road, distance along it.
            TrackBuilder track = target.Track;
            float s = target.Distance;
            var t = new Vector3(target.Sideways, target.Height, 0f);
            _x = Mathf.SmoothDamp(_x, t.x + feel.offset.x, ref _vx, feel.lateralLagSeconds);
            _y = Mathf.SmoothDamp(_y, feel.offset.y + t.y * 0.5f + SlopeLift(track, s), ref _vy, feel.lateralLagSeconds);
            transform.position = track.Point(s + feel.offset.z, _x, _y);
            transform.LookAt(track.Point(s + feel.lookAhead, _x, 1f));

            // Roll into the direction of a lane change: tilt proportional to the runner's sideways speed.
            float sideways = _hasLast ? (t.x - _lastTargetX) / dt : 0f;
            _lastTargetX = t.x;
            _hasLast = true;
            float wantedRoll = -feel.rollDegrees * Mathf.Clamp(sideways / feel.fullRollSpeed, -1f, 1f);
            _roll = Mathf.SmoothDamp(_roll, wantedRoll, ref _rollVel, feel.rollEaseSeconds);
            transform.Rotate(0f, 0f, _roll, Space.Self);

            float wanted = target.IsPhased ? feel.phaseFov : target.IsSprinting ? feel.sprintFov : feel.baseFov;
            float rate = Mathf.Max(Mathf.Abs(feel.phaseFov - feel.baseFov), 1f) / feel.fovEaseSeconds;
            _cam.fieldOfView = Mathf.MoveTowards(_cam.fieldOfView, wanted, rate * dt);
        }
    }
}
