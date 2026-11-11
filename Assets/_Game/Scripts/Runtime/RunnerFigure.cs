using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// The runner as a blocky running figure (John, day 17: instead of the capsule), with dust puffs from the heels.
    /// Built from boxes by Shift+Shift > Set Up Visuals; this component only animates it.
    /// The stride follows the runner's distance along the track, which follows the song: one full stride (two
    /// footfalls) per beat, so the feet land in time with the music. No frame time involved.
    /// The Runner still squashes the whole figure for a slide; this adds the tuck for a jump, the lean for a sprint
    /// and the phase material on every part.
    /// </summary>
    public sealed class RunnerFigure : MonoBehaviour
    {
        [SerializeField] private Runner runner;
        [Tooltip("Pivots at the hips and shoulders; they swing around their local X axis.")]
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform armLeft;
        [SerializeField] private Transform armRight;
        [Tooltip("Pivot at the hips for the upper body: leans forward when sprinting.")]
        [SerializeField] private Transform upperBody;
        [SerializeField] private Renderer[] parts = new Renderer[0];
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material phaseMaterial;

        [Header("Stride")]
        [Tooltip("Metres per full stride (left + right step). 5 = one stride per beat at the Runner's 5 m per beat.")]
        [SerializeField, Min(0.5f)] private float strideMetres = 5f;
        [SerializeField, Range(0f, 80f)] private float legSwingDegrees = 42f;
        [SerializeField, Range(0f, 80f)] private float armSwingDegrees = 50f;
        [SerializeField, Range(0f, 45f)] private float runLeanDegrees = 8f;
        [SerializeField, Range(0f, 45f)] private float sprintLeanDegrees = 22f;

        [Tooltip("Lean into bends (John, day 17): degrees of lean per degree the track turns over the next few metres.")]
        [SerializeField, Range(0f, 8f)] private float bendLean = 3f;
        [SerializeField, Range(0f, 30f)] private float maxBendLeanDegrees = 16f;

        [Header("Dust from the heels")]
        [SerializeField] private ParticleSystem dust;
        [SerializeField, Range(0, 12)] private int puffsPerStep = 4;
        [Tooltip("Where the feet touch down, relative to this object.")]
        [SerializeField] private Vector3 footOffset = new Vector3(0.14f, 0.05f, -0.25f);

        private int _lastStep = int.MinValue;
        private bool _phased;
        private float _lean;

        private void Start()
        {
            if (runner == null) runner = GetComponentInParent<Runner>();
            if (runner == null) enabled = false;
        }

        private void LateUpdate()
        {
            // Half-strides since the start: the whole part counts footfalls, the fraction drives the swing.
            float half = runner.Distance / strideMetres * 2f;
            float swing = Mathf.Sin(half * Mathf.PI);
            bool inAir = runner.Height > 0.05f;
            bool running = runner.State == Runner.RunState.Running;

            float leg = running ? (inAir ? 0f : swing * legSwingDegrees) : 0f;
            float tuck = inAir ? -55f : 0f; // both knees up in a jump
            if (legLeft != null) legLeft.localRotation = Quaternion.Euler(leg + tuck, 0f, 0f);
            if (legRight != null) legRight.localRotation = Quaternion.Euler(-leg + tuck, 0f, 0f);
            float arm = running ? swing * armSwingDegrees : 0f;
            if (armLeft != null) armLeft.localRotation = Quaternion.Euler(-arm, 0f, 0f);
            if (armRight != null) armRight.localRotation = Quaternion.Euler(arm, 0f, 0f);

            float wantedLean = !running ? 0f : runner.IsSprinting ? sprintLeanDegrees : runLeanDegrees;
            _lean = Mathf.MoveTowards(_lean, wantedLean, 120f * Time.deltaTime); // look only, not rhythm timing
            if (upperBody != null) upperBody.localRotation = Quaternion.Euler(_lean, 0f, 0f);

            // Lean into the bend: how much the track's heading changes from 3 m behind to 3 m ahead.
            if (runner.Track != null)
            {
                double s = runner.Distance;
                float turn = (float)((runner.Track.Path.YawAt(s + 3) - runner.Track.Path.YawAt(s - 3)) * 180.0 / System.Math.PI);
                float lean = running ? Mathf.Clamp(turn * bendLean, -maxBendLeanDegrees, maxBendLeanDegrees) : 0f;
                transform.localRotation = Quaternion.Euler(0f, 0f, -lean); // right bend: lean right
            }

            // A puff each time a foot lands (every half stride), on the ground only.
            int step = Mathf.FloorToInt(half);
            if (step != _lastStep)
            {
                bool first = _lastStep == int.MinValue;
                _lastStep = step;
                if (!first && running && !inAir && dust != null && puffsPerStep > 0)
                {
                    float side = (step & 1) == 0 ? -1f : 1f;
                    dust.transform.localPosition = new Vector3(footOffset.x * side, footOffset.y, footOffset.z);
                    dust.Emit(puffsPerStep);
                }
            }

            bool phased = runner.IsPhased;
            if (phased != _phased && normalMaterial != null && phaseMaterial != null)
            {
                _phased = phased;
                foreach (Renderer r in parts)
                    if (r != null) r.sharedMaterial = phased ? phaseMaterial : normalMaterial;
            }
        }
    }
}
