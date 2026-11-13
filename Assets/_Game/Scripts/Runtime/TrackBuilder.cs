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
        [Tooltip("Length of the hole cut in the road for a gap. Must match the GapMarker prefab's depth.")]
        [SerializeField, Min(0.5f)] private float gapMetres = 2.5f;

        [Header("Dressing prefabs (each one covers Dressing Metres along +Z from its origin)")]
        [SerializeField] private GameObject tunnelPrefab;
        [SerializeField] private GameObject bridgePrefab;
        [SerializeField] private GameObject pillarsPrefab;
        [SerializeField, Min(1f)] private float dressingMetres = 10f;

        [Header("Roadside scenery (ELIA, day 17: gives the world depth)")]
        [Tooltip("Marker post, repeated on both sides of the road. Not placed in tunnels.")]
        [SerializeField] private GameObject roadsidePrefab;
        [SerializeField, Min(2f)] private float roadsideEveryMetres = 10f;
        [SerializeField, Min(3.6f)] private float roadsideOffset = 5f;
        [Tooltip("Ground beside the road (not under bridges), so the scenery stands on something.")]
        [SerializeField] private Material groundMaterial;
        [SerializeField, Min(5f)] private float groundHalfWidth = 45f;
        [Header("Plots (ELIA, day 17): walled plots along both sides, each with a building and trees, or trees only")]
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
        private readonly List<Vector2> _holes = new List<Vector2>(); // x = start, y = end, in metres along the track

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
            FindHoles();
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

        /// <summary>True where the road is cut for a gap.</summary>
        public bool InHole(double s)
        {
            foreach (Vector2 hole in _holes)
                if (s > hole.x && s < hole.y) return true;
            return false;
        }

        /// <summary>True on a bridge piece (no ground beside or below the road).</summary>
        public bool IsBridge(double s) => OnBridge(s);

        /// <summary>
        /// How far past its beat a gap or hurdle sits: half a jump, so jumping on the click clears it. One place,
        /// used by the spawner (where the obstacle goes) and here (where the road is cut).
        /// </summary>
        public double JumpShiftMetres(double bpm) =>
            runner != null ? runner.JumpSeconds * bpm / 60.0 * MetresPerBeat * 0.5 : 0.0;

        private void FindHoles()
        {
            _holes.Clear();
            if (conductor == null || conductor.Level == null || conductor.Level.events == null) return;
            foreach (LevelEvent e in conductor.Level.events)
            {
                if (!ObstacleTypes.TryGet(e.type, out ObstacleTypes.Spec spec) || spec.Kind != ObstacleKind.Gap) continue;
                double centre = e.beat * MetresPerBeat + JumpShiftMetres(conductor.BpmAtBeat(e.beat));
                _holes.Add(new Vector2((float)(centre - gapMetres * 0.5), (float)(centre + gapMetres * 0.5)));
            }
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
                    if (InTunnel(s) || OnBridge(s) || AtGate(s)) continue; // bridges have rails; gates stay clear
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
        /// Compound wall along the road side of one plot (ELIA, day 17: not around it), with a lit top edge and a
        /// closed gate in the middle between two taller gate posts.
        /// </summary>
        private void BuildPlotWall(double s0, double s1, float side)
        {
            double mid = (s0 + s1) * 0.5;
            float x = side * plotFront, h = plotWallHeight;
            const double gate = GateHalfWidth, post = 0.5;
            const float t = 0.2f, lit = 0.06f;

            var wall = new List<Vector3>(); var wallTri = new List<int>();
            var cap = new List<Vector3>(); var capTri = new List<int>();
            var door = new List<Vector3>(); var doorTri = new List<int>();

            // Wall either side of the gate, gate posts 0.5 m taller, each with a lit strip on top.
            WallAlong(wall, wallTri, s0, mid - gate - post, x, t, -0.1f, h);
            WallAlong(wall, wallTri, mid + gate + post, s1, x, t, -0.1f, h);
            WallAlong(cap, capTri, s0, mid - gate - post, x, t + 0.03f, h, h + lit);
            WallAlong(cap, capTri, mid + gate + post, s1, x, t + 0.03f, h, h + lit);
            foreach (double at in new[] { mid - gate - post, mid + gate })
            {
                WallAlong(wall, wallTri, at, at + post, x, t + 0.08f, -0.1f, h + 0.5f);
                WallAlong(cap, capTri, at, at + post, x, t + 0.11f, h + 0.5f, h + 0.5f + lit);
            }
            // The gate itself: closed, thinner and darker than the wall, with a lit bar across the top.
            WallAlong(door, doorTri, mid - gate, mid + gate, x, 0.07f, 0.05f, h + 0.2f);
            WallAlong(cap, capTri, mid - gate, mid + gate, x, 0.09f, h + 0.2f, h + 0.2f + lit);

            AddMesh($"Plot wall @{s0:0}m", wall, wallTri, wallMaterial);
            AddMesh($"Plot wall edge @{s0:0}m", cap, capTri, lineMaterial);
            AddMesh($"Plot gate @{s0:0}m", door, doorTri, groundMaterial != null ? groundMaterial : wallMaterial);
        }

        private const double GateHalfWidth = 2.0;

        /// <summary>True where a plot's gate faces the road (no marker post there).</summary>
        private bool AtGate(double s)
        {
            double pitch = plotMetres + plotGapMetres;
            double along = (s - (_path.Start + 2)) % pitch;
            if (along < 0) along += pitch;
            return wallMaterial != null && Math.Abs(along - plotMetres * 0.5) < GateHalfWidth + 1.5;
        }

        /// <summary>A wall section along the road at a sideways offset: both faces and the top, between two heights.</summary>
        private void WallAlong(List<Vector3> v, List<int> tri, double sA, double sB, float sideways,
            float halfThickness, float low, float high)
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
            // Drawn from both sides, so the winding cannot be wrong.
            float t = halfThickness;
            Strip(v, tri, centre, across, -t, low, -t, high);
            Strip(v, tri, centre, across, -t, high, -t, low);
            Strip(v, tri, centre, across, t, low, t, high);
            Strip(v, tri, centre, across, t, high, t, low);
            Strip(v, tri, centre, across, -t, high, t, high);
            Strip(v, tri, centre, across, t, high, -t, high);
            // Close both ends (the camera looks along the walls, so open ends would show).
            // Each side gets its OWN four corners. Sharing corners between the two sides made their directions
            // cancel out to nothing, which the lighting turned into invalid pixels that bloom smeared across the
            // whole screen (day 17: white screen).
            foreach (int i in new[] { 0, rings - 1 })
            foreach (bool flip in new[] { false, true })
            {
                int n = v.Count;
                v.Add(centre[i] + across[i] * -t + Vector3.up * low);
                v.Add(centre[i] + across[i] * -t + Vector3.up * high);
                v.Add(centre[i] + across[i] * t + Vector3.up * high);
                v.Add(centre[i] + across[i] * t + Vector3.up * low);
                foreach (int k in flip ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 }) tri.Add(n + k);
            }
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
            // Cross-sections every ringMetres, plus one exactly at each gap edge inside this chunk, so the road can be
            // cut cleanly there (ELIA, day 17: gaps are real holes with a pit below).
            var at = new List<double>();
            int regular = Mathf.Max(2, Mathf.CeilToInt((float)(s1 - s0) / ringMetres) + 1);
            for (int i = 0; i < regular; i++) at.Add(Math.Min(s0 + i * (double)ringMetres, s1));
            foreach (Vector2 hole in _holes)
                foreach (double edge in new[] { (double)hole.x, (double)hole.y })
                    if (edge > s0 + 0.01 && edge < s1 - 0.01) at.Add(edge);
            at.Sort();
            for (int i = at.Count - 1; i > 0; i--)
                if (at[i] - at[i - 1] < 0.01) at.RemoveAt(i);

            int rings = at.Count;
            var centre = new Vector3[rings];
            var right = new Vector3[rings];
            for (int i = 0; i < rings; i++)
            {
                centre[i] = Point(at[i], 0f, 0f);
                right[i] = Point(at[i], 1f, 0f) - centre[i];
            }
            var inHole = new bool[rings - 1];
            for (int i = 0; i < rings - 1; i++) inHole[i] = InHole((at[i] + at[i + 1]) * 0.5);

            float h = roadWidth * 0.5f, t = roadThickness;
            var v = new List<Vector3>();
            var tri = new List<int>();
            Strip(v, tri, centre, right, -h, 0f, h, 0f, inHole);     // top
            Strip(v, tri, centre, right, -h, -t, -h, 0f, inHole);    // left side
            Strip(v, tri, centre, right, h, 0f, h, -t, inHole);      // right side
            Strip(v, tri, centre, right, h, -t, -h, -t, inHole);     // underside
            // Close the slab where it is cut. Each face has its own corners, one set per side (shared corners
            // facing both ways caused the day-17 white screen).
            for (int i = 0; i < rings; i++)
            {
                // At the chunk's own ends, look just outside it: a gap may run across two chunks.
                bool before = i > 0 ? inHole[i - 1] : InHole(at[i] - 0.005);
                bool after = i < rings - 1 ? inHole[i] : InHole(at[i] + 0.005);
                if (before == after) continue;
                foreach (bool flip in new[] { false, true })
                {
                    int n = v.Count;
                    v.Add(centre[i] + right[i] * -h);
                    v.Add(centre[i] + right[i] * h);
                    v.Add(centre[i] + right[i] * h + Vector3.up * -t);
                    v.Add(centre[i] + right[i] * -h + Vector3.up * -t);
                    foreach (int k in flip ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 }) tri.Add(n + k);
                }
            }
            AddMesh($"Road @{s0:0}m", v, tri, roadMaterial);

            // Lines sit just above the road (the gap hole sits above them, at 0.03).
            v = new List<Vector3>();
            tri = new List<int>();
            const float y = 0.02f;
            Strip(v, tri, centre, right, -h + 0.075f, y, -h + 0.225f, y, inHole);  // left edge
            Strip(v, tri, centre, right, h - 0.225f, y, h - 0.075f, y, inHole);    // right edge
            Strip(v, tri, centre, right, -1.035f, y, -0.965f, y, inHole);          // lane dividers (lanes at -2, 0, 2)
            Strip(v, tri, centre, right, 0.965f, y, 1.035f, y, inHole);
            AddMesh($"Lines @{s0:0}m", v, tri, lineMaterial);

            // Ground on both sides, just below the road surface; left out under bridges (open air below).
            if (groundMaterial == null) return;
            var skip = new bool[rings - 1];
            bool any = false;
            for (int i = 0; i < rings - 1; i++)
            {
                skip[i] = OnBridge((at[i] + at[i + 1]) * 0.5);
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
