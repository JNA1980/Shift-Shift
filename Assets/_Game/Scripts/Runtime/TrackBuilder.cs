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
        [Tooltip("Tall block in the distance, alternating sides, at varied distances and heights.")]
        [SerializeField] private GameObject backdropPrefab;
        [SerializeField, Min(5f)] private float backdropEveryMetres = 25f;
        [SerializeField, Min(8f)] private float backdropNear = 14f;
        [SerializeField, Min(8f)] private float backdropFar = 30f;

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
                    if (InTunnel(s)) continue;
                    foreach (float side in new[] { -roadsideOffset, roadsideOffset })
                    {
                        Instantiate(roadsidePrefab, Point(s, side, 0f), Rotation(s), transform).name = $"Post @{s:0}m";
                        count++;
                    }
                }
            if (backdropPrefab != null)
            {
                int i = 0;
                for (double s = _path.Start; s <= _path.End + 80; s += backdropEveryMetres, i++)
                {
                    float side = (i % 2 == 0 ? -1f : 1f) * Mathf.Lerp(backdropNear, backdropFar, Hash(i, 1));
                    // Sunk by a varied amount, so the tops differ; the blocks reach far below the road.
                    GameObject go = Instantiate(backdropPrefab, Point(s, side, -9f * Hash(i, 2)), Rotation(s), transform);
                    float w = Mathf.Lerp(0.7f, 1.6f, Hash(i, 3));
                    go.transform.localScale = new Vector3(w, 1f, Mathf.Lerp(0.7f, 1.6f, Hash(i, 4)));
                    go.name = $"Block @{s:0}m";
                    count++;
                }
            }
            return count;
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
        }

        /// <summary>
        /// A band along the chunk between two points of the road's cross-section (x sideways, y up).
        /// It faces the side given by forward x (b - a): a left of b faces up.
        /// </summary>
        private static void Strip(List<Vector3> v, List<int> tri, Vector3[] centre, Vector3[] right,
            float ax, float ay, float bx, float by)
        {
            int start = v.Count;
            for (int i = 0; i < centre.Length; i++)
            {
                v.Add(centre[i] + right[i] * ax + Vector3.up * ay);
                v.Add(centre[i] + right[i] * bx + Vector3.up * by);
            }
            for (int i = 0; i + 1 < centre.Length; i++)
            {
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
