using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Marks a GameObject as an obstacle. The Runner judges it by rules (RunnerModel), not physics: lane comes from
    /// this object's X position, timing from its Z position. Colliders are not used.
    /// Tags from the playbook map to <see cref="layer"/> (Solid / Ghost) and <see cref="kind"/> == Wall.
    /// </summary>
    public sealed class Obstacle : MonoBehaviour
    {
        public ObstacleKind kind = ObstacleKind.Block;
        public ObstacleLayer layer = ObstacleLayer.Solid;

        [Tooltip("Blocks every lane (gaps, gates, walls). Otherwise only the lane under this object's X position.")]
        public bool allLanes;

        [Tooltip("Length along the track in metres. Gaps are checked at both edges; other kinds at their centre.")]
        [Min(0.1f)] public float depth = 0.5f;
    }
}
