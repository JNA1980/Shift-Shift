using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>Parses a level chart from a TextAsset with JsonUtility (WebGL-safe: no System.IO).</summary>
    public static class LevelLoader
    {
        /// <returns>The level, or null if it is missing or invalid (errors are logged).</returns>
        public static LevelData Load(TextAsset json)
        {
            if (json == null)
            {
                Debug.LogError("[Level] No level JSON assigned.");
                return null;
            }

            LevelData level;
            try
            {
                level = JsonUtility.FromJson<LevelData>(json.text);
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError($"[Level] {json.name}: not valid JSON ({e.Message}).");
                return null;
            }

            var errors = level?.Validate();
            if (level == null || errors.Count > 0)
            {
                Debug.LogError($"[Level] {json.name} is invalid:\n- " + string.Join("\n- ", errors ?? new System.Collections.Generic.List<string> { "empty" }));
                return null;
            }
            return level;
        }
    }
}
