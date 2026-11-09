using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Places the level's obstacles (playbook 1.3): each event's beat becomes a Z position using the runner's metres
    /// per beat, its lane an X position, and the prefab for its type is instantiated with the Obstacle rules from
    /// <see cref="ObstacleTypes"/>. Gaps are shifted half a jump later (their beat is the take-off). Runs in Awake,
    /// before the Runner collects obstacles in Start.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class ObstacleSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TypePrefab
        {
            public string type;
            public GameObject prefab;
        }

        [SerializeField] private Conductor conductor;
        [SerializeField] private Runner runner;
        [SerializeField] private List<TypePrefab> prefabs = new List<TypePrefab>();

        private void Awake()
        {
            if (conductor == null || runner == null || conductor.Level == null)
            {
                Debug.LogError("[Spawner] Needs a Conductor with a valid level and a Runner.", this);
                return;
            }

            var byType = new Dictionary<string, GameObject>();
            foreach (TypePrefab p in prefabs)
                if (!string.IsNullOrEmpty(p.type) && p.prefab != null) byType[p.type] = p.prefab;

            int spawned = 0;
            foreach (LevelEvent e in conductor.Level.events)
            {
                if (!ObstacleTypes.TryGet(e.type, out ObstacleTypes.Spec spec)) continue; // validated on load
                if (!byType.TryGetValue(e.type, out GameObject prefab))
                {
                    Debug.LogError($"[Spawner] No prefab for type '{e.type}'.", this);
                    continue;
                }

                float x = spec.AllLanes ? 0f : e.RunnerLane * runner.LaneWidth;
                float z = (float)(e.beat * runner.MetresPerBeat);
                // A gap's beat is the take-off: place the hole half a jump later so jumping on the click clears it.
                if (spec.Kind == ObstacleKind.Gap)
                    z += (float)(runner.JumpSeconds * conductor.Bpm / 60.0 * runner.MetresPerBeat * 0.5);
                GameObject go = Instantiate(prefab, new Vector3(x, 0f, z), Quaternion.identity, transform);
                go.name = $"{e.type} b{e.beat:0.##}" + (spec.AllLanes ? "" : $" lane {e.lane}");

                Obstacle o = go.GetComponent<Obstacle>();
                if (o == null) o = go.AddComponent<Obstacle>();
                o.kind = spec.Kind;
                o.layer = spec.Layer;
                o.allLanes = spec.AllLanes;
                o.hasTargetBeat = true;
                o.targetBeat = e.beat;
                spawned++;
            }
            Debug.Log($"[Spawner] Spawned {spawned} obstacles for {conductor.Level.name}.");
        }
    }
}
