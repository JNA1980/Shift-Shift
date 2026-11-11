using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Places the level's obstacles (playbook 1.3): each event's beat becomes a distance along the track using the
    /// runner's metres per beat, its lane a sideways offset, and the prefab for its type is instantiated with the Obstacle rules from
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
            if (conductor == null || runner == null || conductor.Level == null || runner.Track == null)
            {
                Debug.LogError("[Spawner] Needs a Conductor with a valid level and a Runner with a track.", this);
                return;
            }
            TrackBuilder track = runner.Track;

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

                // Opening types are built across the whole track; their open lane's panel is hidden below.
                float x = spec.AllLanes || spec.LaneIsOpening ? 0f : e.RunnerLane * runner.LaneWidth;
                float z = (float)(e.beat * runner.MetresPerBeat);
                // A gap's or hurdle's beat is the take-off: place it half a jump later so jumping on the click clears it.
                if (spec.Kind == ObstacleKind.Gap || spec.Kind == ObstacleKind.Hurdle)
                    z += (float)(runner.JumpSeconds * conductor.Bpm / 60.0 * runner.MetresPerBeat * 0.5);
                // The track can bend and slope: place and turn the obstacle along it.
                GameObject go = Instantiate(prefab, track.Point(z, x, 0f), track.Rotation(z), transform);
                go.name = $"{e.type} b{e.beat:0.##}" + (spec.AllLanes ? "" : $" lane {e.lane}");

                Obstacle o = go.GetComponent<Obstacle>();
                if (o == null) o = go.AddComponent<Obstacle>();
                o.kind = spec.Kind;
                o.layer = spec.Layer;
                o.allLanes = spec.AllLanes;
                o.hasTargetBeat = true;
                o.targetBeat = e.beat;
                o.laneIsOpening = spec.LaneIsOpening;
                o.openLane = e.RunnerLane;
                o.hasTrackPosition = true;
                o.trackDistance = z;
                o.trackSideways = x;
                if (spec.LaneIsOpening)
                {
                    // Day 17: prefabs carry one ready-made layout per open lane (Open0/Open1/Open2): solid slab or
                    // framed doorway. Older prefabs have one panel per lane (Panel0/1/2): hide the open lane's panel.
                    Transform layout = go.transform.Find("Open" + e.lane);
                    if (layout != null)
                    {
                        foreach (Transform child in go.transform)
                            if (child.name.StartsWith("Open")) child.gameObject.SetActive(child == layout);
                    }
                    else
                    {
                        Transform panel = go.transform.Find("Panel" + e.lane);
                        if (panel != null) panel.gameObject.SetActive(false);
                        else Debug.LogWarning($"[Spawner] {prefab.name} has no Open{e.lane} or Panel{e.lane} child.", go);
                    }
                }
                spawned++;
            }
            Debug.Log($"[Spawner] Spawned {spawned} obstacles for {conductor.Level.name}.");
        }
    }
}
