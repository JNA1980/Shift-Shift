using System.Collections.Generic;
using System.IO;
using ShiftShift.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Shift+Shift > Create Greybox Scene (playbook 1.2). Creates, only if missing:
    /// - greybox materials (Assets/_Game/Materials/Greybox),
    /// - track piece prefabs Straight/Tunnel/Bridge/Gap (Assets/_Game/Prefabs/Track),
    /// - obstacle prefabs Block/LowBar/Gate/Wall/GhostBlock/PhaseBarrier (Assets/_Game/Prefabs/Obstacles),
    /// - Assets/_Game/Scenes/Greybox.unity: input + debug HUD, runner, chase camera, 200 m track,
    ///   and a starter set of obstacles (one of each behaviour) to move around by hand.
    /// Never overwrites an existing scene or asset; delete one to regenerate it.
    /// </summary>
    public static class GreyboxSetup
    {
        private const string MatDir = "Assets/_Game/Materials/Greybox";
        private const string TrackDir = "Assets/_Game/Prefabs/Track";
        private const string ObstacleDir = "Assets/_Game/Prefabs/Obstacles";
        private const string ScenePath = "Assets/_Game/Scenes/Greybox.unity";
        private const string SettingsPath = "Assets/_Game/Settings/GestureSettings.asset";
        private const float TrackWidth = 7f;

        private static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        [MenuItem("Shift+Shift/Create Greybox Scene")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Greybox", $"{ScenePath} already exists. Delete it first to regenerate.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var settings = AssetDatabase.LoadAssetAtPath<GestureSettings>(SettingsPath);
            if (settings == null)
            {
                Debug.LogError($"[Greybox] Missing {SettingsPath}.");
                return;
            }

            EnsureFolder("Assets/_Game/Materials", "Greybox");
            EnsureFolder("Assets/_Game/Prefabs", "Track");
            EnsureFolder("Assets/_Game/Prefabs", "Obstacles");
            CreateMaterials();

            TrackPiece straight = TrackPrefab("Straight", 10f, BuildStraight);
            TrackPiece tunnel = TrackPrefab("Tunnel", 10f, BuildTunnel);
            TrackPiece bridge = TrackPrefab("Bridge", 10f, BuildBridge);
            TrackPiece gap = TrackPrefab("Gap", 2.5f, BuildGap);

            GameObject block = ObstaclePrefab("Block", ObstacleKind.Block, ObstacleLayer.Solid, false, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.6f), "Obstacle"));
            GameObject low = ObstaclePrefab("LowBar", ObstacleKind.Low, ObstacleLayer.Solid, false, 0.4f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(1.8f, 0.4f, 0.4f), "Low"));
            GameObject gate = ObstaclePrefab("Gate", ObstacleKind.Gate, ObstacleLayer.Solid, true, 0.4f, go =>
            {
                Box(go, "PostL", new Vector3(-3.3f, 1.75f, 0), new Vector3(0.3f, 3.5f, 0.3f), "Gate");
                Box(go, "PostR", new Vector3(3.3f, 1.75f, 0), new Vector3(0.3f, 3.5f, 0.3f), "Gate");
                Box(go, "Beam", new Vector3(0, 3.4f, 0), new Vector3(6.9f, 0.25f, 0.3f), "Gate");
            });
            GameObject wall = ObstaclePrefab("Wall", ObstacleKind.Wall, ObstacleLayer.Solid, true, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 3f, 0.6f), "Wall"));
            GameObject ghost = ObstaclePrefab("GhostBlock", ObstacleKind.Block, ObstacleLayer.Ghost, false, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.6f), "Ghost"));
            GameObject barrier = ObstaclePrefab("PhaseBarrier", ObstacleKind.Block, ObstacleLayer.Solid, true, 0.3f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 3f, 0.3f), "Barrier"));

            BuildScene(settings, straight, tunnel, bridge, gap, block, low, gate, wall, ghost, barrier);
        }

        // ---------- scene ----------

        private static void BuildScene(GestureSettings settings, TrackPiece straight, TrackPiece tunnel, TrackPiece bridge,
            TrackPiece gap, GameObject block, GameObject low, GameObject gate, GameObject wall, GameObject ghost,
            GameObject barrier)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera cam = Camera.main;

            // Input + HUD
            var inputGo = new GameObject("Input");
            var input = inputGo.AddComponent<ShiftInput>();
            Set(input, "settings", settings);
            inputGo.AddComponent<GestureDebugHud>();

            // Track: 0-60 straight, 60-70 tunnel, 70-100 straight, 100-102.5 gap, 102.5-112.5 bridge, then straight to 200.
            var trackGo = new GameObject("Track");
            var builder = trackGo.AddComponent<TrackBuilder>();
            builder.lengthMetres = 200f;
            builder.fillPiece = straight;
            builder.sequence = new List<TrackPiece>
            {
                straight, straight, straight, straight, straight, straight, tunnel, straight, straight, straight, gap, bridge,
            };
            builder.Build();

            // Runner
            var runnerGo = new GameObject("Runner");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(runnerGo.transform, false);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            var bodyRenderer = body.GetComponent<Renderer>();
            bodyRenderer.sharedMaterial = Mats["Runner"];
            var runner = runnerGo.AddComponent<Runner>();
            Set(runner, "input", input);
            Set(runner, "track", builder);
            Set(runner, "body", body.transform);
            Set(runner, "bodyRenderer", bodyRenderer);
            Set(runner, "normalMaterial", Mats["Runner"]);
            Set(runner, "phaseMaterial", Mats["Phase"]);

            // Camera
            var chase = cam.gameObject.AddComponent<ChaseCamera>();
            Set(chase, "target", runner);
            cam.transform.position = new Vector3(0, 4, -7);

            // Starter obstacles (10 m/s stub speed: 10 m = 1 s = 2 beats at 120 BPM)
            var parent = new GameObject("Obstacles").transform;
            Place(block, parent, 0, 30);                       // move left or right
            Place(block, parent, -1, 45); Place(block, parent, 1, 45); // back to the middle
            Place(low, parent, 0, 65);                         // slide (inside the tunnel)
            Place(gate, parent, 0, 85);                        // sprint
            // 100-102.5 m: gap piece (jump), placed by the track
            Place(barrier, parent, 0, 130);                    // phase through it...
            Place(ghost, parent, 0, 130);                      // ...but not in the middle lane
            Place(wall, parent, 0, 170);                       // charge: L,R,L,R on the beat before it

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Greybox] Created {ScenePath}. Press Play. Obstacles are under 'Obstacles'; move them by hand " +
                      "(X = lane: -2, 0, 2; Z = distance in metres).");
        }

        private static void Place(GameObject prefab, Transform parent, int lane, float z)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = new Vector3(lane * 2f, 0, z);
            go.name = $"{prefab.name} @{z:0}m" + (lane == 0 ? "" : lane < 0 ? " L" : " R");
        }

        // ---------- prefabs ----------

        private static TrackPiece TrackPrefab(string name, float length, System.Action<GameObject> build)
        {
            string path = $"{TrackDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<TrackPiece>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            go.AddComponent<TrackPiece>().length = length;
            build(go);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<TrackPiece>(path);
        }

        private static void BuildStraight(GameObject go) =>
            Box(go, "Floor", new Vector3(0, -0.25f, 5f), new Vector3(TrackWidth, 0.5f, 10f), "Track");

        private static void BuildTunnel(GameObject go)
        {
            Box(go, "Floor", new Vector3(0, -0.25f, 5f), new Vector3(TrackWidth, 0.5f, 10f), "Track");
            Box(go, "Ceiling", new Vector3(0, 3.75f, 5f), new Vector3(TrackWidth + 1f, 0.5f, 10f), "TrackDark");
            Box(go, "WallL", new Vector3(-3.75f, 1.75f, 5f), new Vector3(0.5f, 3.5f, 10f), "TrackDark");
            Box(go, "WallR", new Vector3(3.75f, 1.75f, 5f), new Vector3(0.5f, 3.5f, 10f), "TrackDark");
        }

        private static void BuildBridge(GameObject go)
        {
            Box(go, "Deck", new Vector3(0, -0.15f, 5f), new Vector3(TrackWidth - 0.6f, 0.3f, 10f), "Track");
            Box(go, "RailL", new Vector3(-3.3f, 0.4f, 5f), new Vector3(0.15f, 0.8f, 10f), "TrackDark");
            Box(go, "RailR", new Vector3(3.3f, 0.4f, 5f), new Vector3(0.15f, 0.8f, 10f), "TrackDark");
        }

        private static void BuildGap(GameObject go)
        {
            // No floor: the runner must be in the air across the whole gap. 2.5 m with a 0.5 s jump at 10 m/s
            // leaves 0.25 s to time the jump.
            var hole = new GameObject("GapObstacle");
            hole.transform.SetParent(go.transform, false);
            hole.transform.localPosition = new Vector3(0, 0, 1.25f);
            var o = hole.AddComponent<Obstacle>();
            o.kind = ObstacleKind.Gap;
            o.allLanes = true;
            o.depth = 2.5f;
        }

        private static GameObject ObstaclePrefab(string name, ObstacleKind kind, ObstacleLayer layer, bool allLanes,
            float depth, System.Action<GameObject> build)
        {
            string path = $"{ObstacleDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            var o = go.AddComponent<Obstacle>();
            o.kind = kind;
            o.layer = layer;
            o.allLanes = allLanes;
            o.depth = depth;
            build(go);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void Box(GameObject parent, string name, Vector3 pos, Vector3 size, string mat)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>()); // rules decide collisions, not physics
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = pos;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = Mats[mat];
        }

        // ---------- materials ----------

        private static void CreateMaterials()
        {
            Mats.Clear();
            Mat("Track", new Color(0.55f, 0.55f, 0.58f));
            Mat("TrackDark", new Color(0.3f, 0.3f, 0.34f));
            Mat("Runner", new Color(0.95f, 0.95f, 0.95f));
            Mat("Phase", new Color(0.3f, 0.95f, 1f));
            Mat("Obstacle", new Color(0.95f, 0.45f, 0.2f));
            Mat("Low", new Color(0.95f, 0.75f, 0.2f));
            Mat("Gate", new Color(0.95f, 0.95f, 0.2f));
            Mat("Wall", new Color(0.85f, 0.15f, 0.2f));
            Mat("Ghost", new Color(0.65f, 0.35f, 0.95f));
            Mat("Barrier", new Color(0.25f, 0.35f, 0.9f));
        }

        private static void Mat(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader);
                m.SetColor("_BaseColor", color);
                m.color = color;
                AssetDatabase.CreateAsset(m, path);
            }
            Mats[name] = m;
        }

        // ---------- helpers ----------

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Greybox] {target.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
