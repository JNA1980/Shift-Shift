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
        [Tooltip("2.8 m up (ELIA, day 17): the gate's top beam (2.95-3.15 m) passes overhead instead of across the " +
                 "view. Was 3.3 m, above every obstacle top; the red wall, barrier and gate (about 3 m) now reach " +
                 "just above eye level. Tunnel ceiling is at 3.5 m.")]
        public Vector3 offset = new Vector3(0f, 2.8f, -5.5f);
        [Tooltip("How far ahead of the runner the camera aims. Smaller = looks down at the road more, which makes " +
                 "distances easier to judge (6 since the camera came down to 2.8 m; was 8).")]
        public float lookAhead = 6f;
        [Min(0f)] public float lateralLagSeconds = 0.12f;

        [Header("Slopes (day 17)")]
        [Tooltip("The camera lifts by the height the road gains between itself and this far ahead of the runner, " +
                 "so obstacles on a rise stay below eye level. Off inside tunnels.")]
        [Min(0f)] public float slopeLookMetres = 30f;
        [Tooltip("How much of that height gain the camera lifts by. 0 = off (ELIA, day 17: the camera felt raised; " +
                 "back to the earlier angle). Try 0.3 if obstacles on rises bother you.")]
        [Range(0f, 2f)] public float slopeLiftAmount = 0f;

        [Tooltip("The camera drops by this much inside a tunnel (ELIA, day 17: at 3.3 m it sat right under the 3.5 m " +
                 "ceiling). Gate tops (3.1 m) are then above the camera while in the tunnel.")]
        [Range(0f, 1.5f)] public float tunnelDrop = 0f; // 0 since the camera itself came down to 2.8 m

        [Tooltip("The drop eases in over this distance before a tunnel and out over it after (no sudden rise at the exit).")]
        [Min(0.5f)] public float tunnelEaseMetres = 12f;
        [Tooltip("How much of the runner's jump height the camera follows. 0 = stays level (ELIA, day 17: the camera " +
                 "should not rise at the pink wall). It was 0.5 before.")]
        [Range(0f, 1f)] public float jumpFollow = 0f;

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
