using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Follows the runner from behind (settings in <see cref="CameraFeel"/>). Sideways and vertical movement lag
    /// slightly; forward movement is locked so the camera never drifts back at speed. Field of view widens while
    /// sprinting and more while phased; the camera rolls slightly into lane changes.
    /// Uses frame time for smoothing only: camera feel, not rhythm timing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Runner target;
        [SerializeField] private CameraFeel feel;

        private Camera _cam;
        private float _vx, _vy, _roll, _rollVel, _lastTargetX;
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
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            Vector3 t = target.transform.position;
            Vector3 cur = transform.position;
            float x = Mathf.SmoothDamp(cur.x, t.x + feel.offset.x, ref _vx, feel.lateralLagSeconds);
            float y = Mathf.SmoothDamp(cur.y, feel.offset.y + t.y * 0.5f, ref _vy, feel.lateralLagSeconds);
            transform.position = new Vector3(x, y, t.z + feel.offset.z);
            transform.LookAt(new Vector3(x, 1f, t.z + feel.lookAhead));

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
