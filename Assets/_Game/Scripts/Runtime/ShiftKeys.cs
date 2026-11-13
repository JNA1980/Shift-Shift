#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ShiftShift
{
    /// <summary>
    /// The two Shift keys as the browser sees them (Assets/Plugins/WebGL/ShiftKeys.jslib). Used only in Web builds:
    /// on Windows the browser gets no key-up for one Shift while the other is still down, which left Unity's own
    /// keyboard state showing a key held for good and swallowed its next press (ELIA, day 17: jump at a gap not
    /// seen; left taps ignored). Elsewhere <see cref="FromBrowser"/> is false and ShiftInput reads the Input System.
    /// Known limit that remains: letting go of ONE Shift while the other stays down is not seen until both are up
    /// or that key is pressed again.
    /// </summary>
    public static class ShiftKeys
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        public static readonly bool FromBrowser = true;

        [DllImport("__Internal")] private static extern void ShiftKeys_Init();
        [DllImport("__Internal")] private static extern int ShiftKeys_State();

        private static bool _ready;

        /// <summary>
        /// Call once per frame. "Hit" = pressed since the last call (so a tap shorter than a frame counts).
        /// "Again" = pressed while still marked as down: its key-up was lost, so it is a release and a new press.
        /// </summary>
        public static void Read(out bool leftDown, out bool rightDown, out bool leftHit, out bool rightHit,
            out bool leftAgain, out bool rightAgain)
        {
            if (!_ready)
            {
                ShiftKeys_Init();
                _ready = true;
            }
            int bits = ShiftKeys_State();
            leftDown = (bits & 1) != 0;
            rightDown = (bits & 2) != 0;
            leftHit = (bits & 4) != 0;
            rightHit = (bits & 8) != 0;
            leftAgain = (bits & 16) != 0;
            rightAgain = (bits & 32) != 0;
        }
#else
        public static readonly bool FromBrowser = false;

        public static void Read(out bool leftDown, out bool rightDown, out bool leftHit, out bool rightHit,
            out bool leftAgain, out bool rightAgain)
        {
            leftDown = rightDown = leftHit = rightHit = leftAgain = rightAgain = false;
        }
#endif
    }
}
