using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Follows the runner from behind. Sideways and vertical movement lag slightly; forward movement is locked so the
    /// camera never drifts back at speed. FOV kicks while sprinting.
    /// Uses frame time for smoothing only: camera feel, not rhythm timing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Runner target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -7f);
        [SerializeField] private float lookAhead = 8f;
        [SerializeField, Min(0f)] private float lateralLagSeconds = 0.12f;
        [SerializeField] private float baseFov = 60f;
        [SerializeField] private float sprintFov = 72f;
        [SerializeField, Min(0.01f)] private float fovEaseSeconds = 0.15f;

        private Camera _cam;
        private float _vx, _vy;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.fieldOfView = baseFov;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 t = target.transform.position;
            Vector3 cur = transform.position;
            float x = Mathf.SmoothDamp(cur.x, t.x + offset.x, ref _vx, lateralLagSeconds);
            float y = Mathf.SmoothDamp(cur.y, offset.y + t.y * 0.5f, ref _vy, lateralLagSeconds);
            transform.position = new Vector3(x, y, t.z + offset.z);
            transform.LookAt(new Vector3(x, 1f, t.z + lookAhead));

            float wanted = target.IsSprinting ? sprintFov : baseFov;
            float rate = Mathf.Abs(sprintFov - baseFov) / fovEaseSeconds;
            _cam.fieldOfView = Mathf.MoveTowards(_cam.fieldOfView, wanted, rate * Time.deltaTime);
        }
    }
}
