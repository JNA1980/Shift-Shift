using System;
using System.Collections.Generic;
using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Builds the level's track along its path (playbook 2.3). The path comes from the level's "track" piece list
    /// (bends, rises, drops; ShiftShift.Core.TrackPath), or from <see cref="pieces"/> when there is no Conductor
    /// (Greybox). The road and its lines are generated as meshes that follow the path; tunnels, bridges and pillars
    /// are prefabs repeated along their piece.
    /// Built when the scene starts (the level is only known then), before the spawner places obstacles on it.
    /// Everything else asks this component where a track position is in the world: <see cref="Point"/>,
    /// <see cref="Rotation"/>. Track positions are (distance along the track, sideways offset, height above the road).
    /// </summary>
    [DefaultExecutionOrder(-150)] // after GameFlow (-300) and Conductor (-200) have the level; before the spawner (-50)
    public sealed class TrackBuilder : MonoBehaviour
    {
        [Serializable]
        public struct Piece
        {
            public double beat;
            public string piece;
        }

        [Tooltip("Optional: takes the track pieces and the length from the playing level.")]
        [SerializeField] private Conductor conductor;
        [Tooltip("Metres per beat come from the Runner (one place).")]
        [SerializeField] private Runner runner;
        [Tooltip("Used when there is no Conductor (Greybox). Hand-placed obstacles need a straight track.")]
        public List<Piece> pieces = new List<Piece>();
        [Tooltip("Track length without a Conductor.")]
        [Min(10f)] public float lengthMetres = 200f;
        [Tooltip("Track before beat 0 (start delay and count-in) and after the last beat.")]
        [SerializeField, Min(0f)] private float leadInMetres = 60f;
        [SerializeField, Min(0f)] private float tailMetres = 60f;

        [Header("Road")]
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material lineMaterial;
        [SerializeField, Min(1f)] private float roadWidth = 7f;
        [SerializeField, Min(0.05f)] private float roadThickness = 0.5f;
        [SerializeField, Min(0.25f)] private float ringMetres = 1f;

        [Header("Dressing prefabs (each one covers Dressing Metres along +Z from its origin)")]
        [SerializeField] private GameObject tunnelPrefab;
        [SerializeField] private GameObject bridgePrefab;
        [SerializeField] private GameObject pillarsPrefab;
        [SerializeField, Min(1f)] private float dressingMetres = 10f;

        [Header("Roadside scenery (John, day 17: gives the world depth)")]
        [Tooltip("Marker post, repeated on both sides of the road. Not placed in tunnels.")]
        [SerializeField] private GameObject roadsidePrefab;
        [SerializeField, Min(2f)] private float roadsideEveryMetres = 10f;
        [SerializeField, Min(3.6f)] private float roadsideOffset = 5f;
        [Tooltip("Ground beside the road (not under bridges), so the scenery stands on something.")]
        [SerializeField] private Material groundMaterial;
        [SerializeField, Min(5f)] private float groundHalfWidth = 45f;
        [Header("Plots (John, day 17): walled plots along both sides, each with a building and trees, or trees only")]
        [SerializeField] private List<GameObject> backdropPrefabs = new List<GameObject>();
        [SerializeField] private List<GameObject> plantPrefabs = new List<GameObject>();
        [Tooltip("Compound wall around each plot. Empty = no walls.")]
        [SerializeField] private Material wallMaterial;
        [SerializeField, Min(10f)] private float plotMetres = 30f;
        [Tooltip("Lane between two plots.")]
        [SerializeField, Min(0f)] private float plotGapMetres = 4f;
        [Tooltip("Distance from the road's centre to the plot's front wall and to its back wall.")]
        [SerializeField, Min(6f)] private float plotFront = 7.5f;
        [SerializeField, Min(20f)] private float plotBack = 40f;
        [SerializeField, Min(0.2f)] private float plotWallHeight = 1.1f;
        [Tooltip("Share of plots that get a building (the rest are trees only).")]
        [SerializeField, Range(0f, 1f)] private float buildingPlotShare = 0.55f;

        public float LengthMetres => conductor != null && conductor.Level != null
            ? (float)(conductor.Level.lengthBeats * MetresPerBeat) : lengthMetres;

        public TrackPath Path
        {
            get
            {
                if (_path == null) _path = CreatePath();
                return _path;
            }
        }

        private float MetresPerBeat => runner != null ? runner.MetresPerBeat : 5f;

        private TrackPath _path;
        private readonly List<Mesh> _meshes = new List<Mesh>();

        private void Awake() => Build();

        private TrackPath CreatePath()
        {
            TrackEvent[] list;
            if (conductor != null && conductor.Level != null) list = conductor.Level.track;
            else
            {
                list = new TrackEvent[pieces.Count];
                for (int i = 0; i < pieces.Count; i++) list[i] = new TrackEvent { beat = pieces[i].beat, piece = pieces[i].piece };
                List<string> errors = TrackPieces.Validate(list, double.MaxValue);
                if (errors.Count > 0) Debug.LogError("[Track] " + string.Join("; ", errors), this);
            }
            return new TrackPath(list, MetresPerBeat, -leadInMetres, LengthMetres + tailMetres);
        }

        /// <summary>World position of a track position.</summary>
        public Vector3 Point(double distance, float sideways, float height)
        {
            TrackPose p = Path.PoseAt(distance);
            float cos = Mathf.Cos((float)p.Yaw), sin = Mathf.Sin((float)p.Yaw);
            return new Vector3((float)p.X + cos * sideways, (float)p.Y + height, (float)p.Z - sin * sideways);
        }

        /// <summary>Facing along the track at a distance (follows bends and slopes; never banks).</summary>
        public Quaternion Rotation(double distance)
        {
            TrackPose p = Path.PoseAt(distance);
            float cp = Mathf.Cos((float)p.Pitch);
            var forward = new Vector3(Mathf.Sin((float)p.Yaw) * cp, Mathf.Sin((float)p.Pitch), Mathf.Cos((float)p.Yaw) * cp);
            return Quaternion.LookRotation(forward, Vector3.up);
        }

        /// <summary>Facing along the track but standing straight up (buildings and trees do not lean on slopes).</summary>
        public Quaternion Upright(double distance) =>
            Quaternion.Euler(0f, (float)(Path.PoseAt(distance).Yaw * 180.0 / Math.PI), 0f);

        [ContextMenu("Build Track (preview)")]
        public void Build()
        {
            Clear();
            _path = CreatePath();
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // track positions are world positions
            if (roadMaterial == null || lineMaterial == null)
            {
                Debug.LogError("[Track] Road and line materials are not assigned (run Shift+Shift > Set Up Visuals).", this);
                return;
            }

            const float chunk = 40f;
            int chunks = 0;
            for (double s = _path.Start; s < _path.End - 0.01; s += chunk, chunks++)
                BuildChunk(s, Math.Min(s + chunk, _path.End));

            int dressed = 0;
            foreach (TrackPath.Span span in _path.Spans)
            {
                GameObject prefab = span.Spec.Dressing == TrackPieces.Dressing.Tunnel ? tunnelPrefab
                    : span.Spec.Dressing == TrackPieces.Dressing.Bridge ? bridgePrefab
                    : span.Spec.Dressing == TrackPieces.Dressing.Pillars ? pillarsPrefab : null;
                if (span.Spec.Dressing == TrackPieces.Dressing.None) continue;
                if (prefab == null)
                {
                    Debug.LogWarning($"[Track] No prefab for '{span.Piece}'.", this);
                    continue;
                }
                for (double s = span.Start; s < span.End - 0.01; s += dressingMetres, dressed++)
                {
                    GameObject go = Instantiate(prefab, Point(s, 0f, 0f), Rotation(s), transform);
                    go.name = $"{span.Piece} @{s:0}m";
                }
            }
            int scenery = BuildScenery();
            Debug.Log($"[Track] {scenery} scenery objects.");
            Debug.Log($"[Track] Built {_path.End - _path.Start:0} m in {chunks} chunks, {_path.Spans.Count} pieces, {dressed} dressing parts.");
        }

        private bool InTunnel(double s) =>
            _path.TryGetSpan(s, out TrackPath.Span span) && span.Spec.Dressing == TrackPieces.Dressing.Tunnel;

        private bool OnBridge(double s) =>
            _path.TryGetSpan(s, out TrackPath.Span span) && span.Spec.Dressing == TrackPieces.Dressing.Bridge;

        /// <summary>Same "random" value every run for the same index, 0..1 (no System.Random state to carry).</summary>
        private static float Hash(int i, int salt)
        {
            double v = Math.Sin(i * 12.9898 + salt * 78.233) * 43758.5453;
            return (float)(v - Math.Floor(v));
        }

        private int BuildScenery()
        {
            int count = 0;
            if (roadsidePrefab != null)
                for (double s = _path.Start; s <= _path.End; s += roadsideEveryMetres)
                {
                    if (InTunnel(s) || OnBridge(s)) continue; // bridges have rails instead
                    foreach (float side in new[] { -roadsideOffset, roadsideOffset })
                    {
                        Instantiate(roadsidePrefab, Point(s, side, 0f), Rotation(s), transform).name = $"Post @{s:0}m";
                        count++;
                    }
                }
            backdropPrefabs.RemoveAll(p => p == null);
            plantPrefabs.RemoveAll(p => p == null);
            float pitch = plotMetres + plotGapMetres;
            int index = 0;
            for (double s0 = _path.Start + 2; s0 + plotMetres <= _path.End; s0 += pitch, index++)
            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                float side = sideIndex == 0 ? -1f : 1f;
                int id = index * 2 + sideIndex + 1;
                double s1 = s0 + plotMetres, mid = (s0 + s1) * 0.5;
                if (OnBridge(s0) || OnBridge(mid) || OnBridge(s1)) continue; // no ground there
                if (wallMaterial != null) BuildPlotWall(s0, s1, side);

                // Building plots: one building, set back from the front wall, with a few trees in the space around
                // it. Tree plots: more trees, nothing else.
                bool hasBuilding = backdropPrefabs.Count > 0 && Hash(id, 20) < buildingPlotShare;
                double buildingS = 0;
                float buildingSide = 0f, keepClear = 0f;
                if (hasBuilding)
                {
                    GameObject prefab = backdropPrefabs[Mathf.Min((int)(Hash(id, 5) * backdropPrefabs.Count), backdropPrefabs.Count - 1)];
                    float w = Mathf.Lerp(0.75f, 1.15f, Hash(id, 3));
                    buildingS = mid + (Hash(id, 1) - 0.5f) * 4f;
                    buildingSide = Mathf.Lerp(plotFront + 12f, plotBack - 12f, Hash(id, 2));
                    keepClear = 9f * w + 1.5f;
                    Quaternion turn = Quaternion.Euler(0f, 90f * (int)(Hash(id, 6) * 4f), 0f);
                    GameObject go = Instantiate(prefab, Point(buildingS, side * buildingSide, -2f * Hash(id, 4)),
                        Upright(buildingS) * turn, transform);
                    go.transform.localScale = new Vector3(w, Mathf.Lerp(0.8f, 1.3f, Hash(id, 7)), w);
                    go.name = $"{prefab.name} @{s0:0}m";
                    count++;
                }
                if (plantPrefabs.Count == 0) continue;
                int plants = hasBuilding ? 2 + (int)(Hash(id, 8) * 4f) : 5 + (int)(Hash(id, 8) * 5f);
                for (int k = 0; k < plants; k++)
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    int seed = id * 131 + k * 17 + attempt;
                    double ps = s0 + 3 + (plotMetres - 6) * Hash(seed, 9);
                    float pl = Mathf.Lerp(plotFront + 2f, plotBack - 3f, Hash(seed, 10));
                    if (hasBuilding && Math.Abs(ps - buildingS) < keepClear && Mathf.Abs(pl - buildingSide) < keepClear)
                        continue; // inside the building: try another spot
                    GameObject prefab = plantPrefabs[Mathf.Min((int)(Hash(seed, 11) * plantPrefabs.Count), plantPrefabs.Count - 1)];
                    Quaternion turn = Quaternion.Euler(0f, 360f * Hash(seed, 12), 0f);
                    GameObject go = Instantiate(prefab, Point(ps, side * pl, -0.05f), Upright(ps) * turn, transform);
                    float size = Mathf.Lerp(0.8f, 1.4f, Hash(seed, 13));
                    go.transform.localScale = new Vector3(size, size, size);
                    go.name = $"{prefab.name} @{ps:0}m";
                    count++;
                    break;
                }
            }
            return count;
        }

        /// <summary>
        /// Low compound wall around one plot: front (with a gateway in the middle) and back follow the road, the two
        /// ends run straight out from it. One mesh per plot.
        /// </summary>
        private void BuildPlotWall(double s0, double s1, float side)
        {
            var v = new List<Vector3>();
            var tri = new List<int>();
            double mid = (s0 + s1) * 0.5;
            const double gate = 2.0; // half the gateway's width
            WallAlong(v, tri, s0, mid - gate, side * plotFront);
            WallAlong(v, tri, mid + gate, s1, side * plotFront);
            WallAlong(v, tri, s0, s1, side * plotBack);
            WallAcross(v, tri, s0, side);
            WallAcross(v, tri, s1, side);
            AddMesh($"Plot wall @{s0:0}m", v, tri, wallMaterial);
        }

        private void WallAlong(List<Vector3> v, List<int> tri, double sA, double sB, float sideways)
        {
            int rings = Mathf.Max(2, Mathf.CeilToInt((float)(sB - sA) / ringMetres) + 1);
            var centre = new Vector3[rings];
            var across = new Vector3[rings];
            for (int i = 0; i < rings; i++)
            {
                double s = Math.Min(sA + i * (double)ringMetres, sB);
                centre[i] = Point(s, sideways, 0f);
                across[i] = Point(s, sideways + 1f, 0f) - centre[i];
            }
            WallBox(v, tri, centre, across);
        }

        private void WallAcross(List<Vector3> v, List<int> tri, double s, float side)
        {
            var centre = new[] { Point(s, side * plotFront, 0f), Point(s, side * plotBack, 0f) };
            Vector3 along = Point(s + 0.5, side * plotFront, 0f) - Point(s - 0.5, side * plotFront, 0f);
            WallBox(v, tri, centre, new[] { along, along });
        }

        /// <summary>Two faces and a top along a line of points; drawn from both sides, so the winding cannot be wrong.</summary>
        private void WallBox(List<Vector3> v, List<int> tri, Vector3[] centre, Vector3[] across)
        {
            const float t = 0.2f, low = -0.1f; // half thickness; starts just under the ground
            float h = plotWallHeight;
            Strip(v, tri, centre, across, -t, low, -t, h);
            Strip(v, tri, centre, across, -t, h, -t, low);
            Strip(v, tri, centre, across, t, low, t, h);
            Strip(v, tri, centre, across, t, h, t, low);
            Strip(v, tri, centre, across, -t, h, t, h);
            Strip(v, tri, centre, across, t, h, -t, h);
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false); // gone this frame, so nothing collects it before it is destroyed
                    Destroy(child);
                }
                else DestroyImmediate(child);
            }
            DestroyMeshes();
        }

        private void DestroyMeshes()
        {
            foreach (Mesh m in _meshes)
            {
                if (m == null) continue;
                if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
            }
            _meshes.Clear();
        }

        private void OnDestroy() => DestroyMeshes();

        private void BuildChunk(double s0, double s1)
        {
            int rings = Mathf.Max(2, Mathf.CeilToInt((float)(s1 - s0) / ringMetres) + 1);
            var centre = new Vector3[rings];
            var right = new Vector3[rings];
            for (int i = 0; i < rings; i++)
            {
                double s = Math.Min(s0 + i * (double)ringMetres, s1);
                centre[i] = Point(s, 0f, 0f);
                right[i] = Point(s, 1f, 0f) - centre[i];
            }

            float h = roadWidth * 0.5f, t = roadThickness;
            var v = new List<Vector3>();
            var tri = new List<int>();
            Strip(v, tri, centre, right, -h, 0f, h, 0f);     // top
            Strip(v, tri, centre, right, -h, -t, -h, 0f);    // left side
            Strip(v, tri, centre, right, h, 0f, h, -t);      // right side
            Strip(v, tri, centre, right, h, -t, -h, -t);     // underside
            AddMesh($"Road @{s0:0}m", v, tri, roadMaterial);

            // Lines sit just above the road (the gap hole sits above them, at 0.03).
            v = new List<Vector3>();
            tri = new List<int>();
            const float y = 0.02f;
            Strip(v, tri, centre, right, -h + 0.075f, y, -h + 0.225f, y);  // left edge
            Strip(v, tri, centre, right, h - 0.225f, y, h - 0.075f, y);    // right edge
            Strip(v, tri, centre, right, -1.035f, y, -0.965f, y);          // lane dividers (lanes at -2, 0, 2)
            Strip(v, tri, centre, right, 0.965f, y, 1.035f, y);
            AddMesh($"Lines @{s0:0}m", v, tri, lineMaterial);

            // Ground on both sides, just below the road surface; left out under bridges (open air below).
            if (groundMaterial == null) return;
            var skip = new bool[rings - 1];
            bool any = false;
            for (int i = 0; i < rings - 1; i++)
            {
                skip[i] = OnBridge(Math.Min(s0 + (i + 0.5) * ringMetres, s1));
                any |= !skip[i];
            }
            if (!any) return;
            v = new List<Vector3>();
            tri = new List<int>();
            const float gy = -0.05f;
            Strip(v, tri, centre, right, -groundHalfWidth, gy, -h, gy, skip);
            Strip(v, tri, centre, right, h, gy, groundHalfWidth, gy, skip);
            AddMesh($"Ground @{s0:0}m", v, tri, groundMaterial);
        }

        /// <summary>
        /// A band along the chunk between two points of the road's cross-section (x sideways, y up).
        /// It faces the side given by forward x (b - a): a left of b faces up.
        /// </summary>
        private static void Strip(List<Vector3> v, List<int> tri, Vector3[] centre, Vector3[] right,
            float ax, float ay, float bx, float by, bool[] skip = null)
        {
            int start = v.Count;
            for (int i = 0; i < centre.Length; i++)
            {
                v.Add(centre[i] + right[i] * ax + Vector3.up * ay);
                v.Add(centre[i] + right[i] * bx + Vector3.up * by);
            }
            for (int i = 0; i + 1 < centre.Length; i++)
            {
                if (skip != null && skip[i]) continue;
                int a0 = start + i * 2, b0 = a0 + 1, a1 = a0 + 2, b1 = a0 + 3;
                tri.Add(a0); tri.Add(a1); tri.Add(b0);
                tri.Add(b0); tri.Add(a1); tri.Add(b1);
            }
        }

        private void AddMesh(string name, List<Vector3> v, List<int> tri, Material material)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
