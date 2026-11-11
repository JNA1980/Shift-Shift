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
        [Tooltip("Ghost-layer block placed inside a barrier for the ghostbar / ghostwide types (only hits while phased).")]
        [SerializeField] private GameObject ghostCorePrefab;

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
                bool hasCores = spec.Cores != ObstacleTypes.Cores.None;
                float x = spec.AllLanes || spec.LaneIsOpening || hasCores ? 0f : e.RunnerLane * runner.LaneWidth;
                float z = (float)(e.beat * runner.MetresPerBeat);
                // A gap's or hurdle's beat is the take-off: place it half a jump later so jumping on the click clears it.
                if (spec.Kind == ObstacleKind.Gap || spec.Kind == ObstacleKind.Hurdle)
                    z += (float)track.JumpShiftMetres(conductor.Bpm);
                // The track can bend and slope: place and turn the obstacle along it.
                GameObject go = Instantiate(prefab, track.Point(z, x, 0f), track.Rotation(z), transform);
                go.name = $"{e.type} b{e.beat:0.##}" + (spec.AllLanes ? "" : $" lane {e.lane}");
                bool allLanes = spec.AllLanes || hasCores; // a barrier with cores still blocks every lane

                Obstacle o = go.GetComponent<Obstacle>();
                if (o == null) o = go.AddComponent<Obstacle>();
                o.kind = spec.Kind;
                o.layer = spec.Layer;
                o.allLanes = allLanes;
                o.hasTargetBeat = true;
                o.targetBeat = e.beat;
                o.laneIsOpening = spec.LaneIsOpening;
                o.openLane = e.RunnerLane;
                o.hasTrackPosition = true;
                o.trackDistance = z;
                o.trackSideways = x;
                if (spec.Kind == ObstacleKind.Gap)
                {
                    // The road is really cut here (TrackBuilder): hide the flat black stand-in; the prefab's pit
                    // (walls and water) shows through the hole. On a bridge there is only open air below.
                    Transform flat = go.transform.Find("Hole");
                    if (flat != null && track.InHole(z)) flat.gameObject.SetActive(false);
                    Transform pit = go.transform.Find("Pit");
                    if (pit != null && track.IsBridge(z)) pit.gameObject.SetActive(false);
                }
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

                if (!hasCores) continue;
                if (ghostCorePrefab == null)
                {
                    Debug.LogError($"[Spawner] '{e.type}' needs the Ghost Core Prefab (run Shift+Shift > Set Up Visuals).", this);
                    continue;
                }
                // The second layer: a solid core in the event's lane, or in the other two. Judged on their own.
                for (int lane = -1; lane <= 1; lane++)
                {
                    bool core = spec.Cores == ObstacleTypes.Cores.EventLane ? lane == e.RunnerLane : lane != e.RunnerLane;
                    if (!core) continue;
                    float cx = lane * runner.LaneWidth;
                    GameObject cg = Instantiate(ghostCorePrefab, track.Point(z, cx, 0f), track.Rotation(z), transform);
                    cg.name = $"{e.type} b{e.beat:0.##} core lane {lane + 1}";
                    Obstacle co = cg.GetComponent<Obstacle>();
                    if (co == null) co = cg.AddComponent<Obstacle>();
                    co.kind = ObstacleKind.Block;
                    co.layer = ObstacleLayer.Ghost;
                    co.allLanes = false;
                    co.laneIsOpening = false;
                    co.hasTargetBeat = true;
                    co.targetBeat = e.beat;
                    co.hasTrackPosition = true;
                    co.trackDistance = z;
                    co.trackSideways = cx;
                    spawned++;
                }
            }
            Debug.Log($"[Spawner] Spawned {spawned} obstacles for {conductor.Level.name}.");
        }
    }
}
