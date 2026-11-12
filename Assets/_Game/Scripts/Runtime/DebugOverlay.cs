using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// One switch for the developer overlays (gesture log, runner state box). On by default in the editor, off in
    /// builds; F1 toggles it anywhere (GestureDebugHud reads the key).
    /// </summary>
    public static class DebugOverlay
    {
        public static bool Visible = Application.isEditor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Visible = Application.isEditor;
    }
}
