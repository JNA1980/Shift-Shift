using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Chase camera settings (playbook 2.2 step 3). An asset, so values tuned during Play mode are kept.
    /// Asset: Assets/_Game/Settings/CameraFeel.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Shift+Shift/Camera Feel", fileName = "CameraFeel")]
    public sealed class CameraFeel : ScriptableObject
    {
        [Header("Position")]
        [Tooltip("3.3 m up: under the tunnel ceiling (3.5 m), above every obstacle top (<= 3.2 m). See Docs/PALETTES.md.")]
        public Vector3 offset = new Vector3(0f, 3.3f, -7f);
        public float lookAhead = 8f;
        [Min(0f)] public float lateralLagSeconds = 0.12f;

        [Header("Slopes (day 17)")]
        [Tooltip("The camera lifts by the height the road gains between itself and this far ahead of the runner, " +
                 "so obstacles on a rise stay below eye level. Off inside tunnels.")]
        [Min(0f)] public float slopeLookMetres = 30f;
        [Tooltip("How much of that height gain the camera lifts by. 0 = off (John, day 17: the camera felt raised; " +
                 "back to the earlier angle). Try 0.3 if obstacles on rises bother you.")]
        [Range(0f, 2f)] public float slopeLiftAmount = 0f;

        [Header("Field of view")]
        public float baseFov = 60f;
        public float sprintFov = 72f;
        public float phaseFov = 78f;
        [Min(0.01f)] public float fovEaseSeconds = 0.15f;

        [Header("Roll on lane change")]
        [Range(0f, 10f)] public float rollDegrees = 2f;
        [Min(0.01f)] public float rollEaseSeconds = 0.12f;
        [Tooltip("Sideways speed (m/s) that counts as a full lane change for the roll.")]
        [Min(0.1f)] public float fullRollSpeed = 12f;
    }
}
