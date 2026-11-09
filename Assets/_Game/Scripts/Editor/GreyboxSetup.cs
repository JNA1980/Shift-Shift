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
    /// Also Shift+Shift > Create Level Test Scene (playbook 1.3), see CreateLevelTestScene.
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

            if (AssetDatabase.LoadAssetAtPath<GestureSettings>(SettingsPath) == null)
            {
                Debug.LogError($"[Greybox] Missing {SettingsPath}.");
                return;
            }

            Kit k = EnsureAssets();
            BuildScene(k.Straight, k.Tunnel, k.Bridge, k.Gap, k.Block, k.Low, k.Gate, k.Wall, k.Ghost, k.Barrier);
        }

        // ---------- shared asset kit ----------

        private sealed class Kit
        {
            public TrackPiece Straight, Tunnel, Bridge, Gap;
            public GameObject Block, Low, Gate, Wall, Ghost, Barrier, GapMarker;
        }

        /// <summary>Materials, track pieces and obstacle prefabs; creates only what is missing.</summary>
        private static Kit EnsureAssets()
        {
            var k = new Kit();
            EnsureFolder("Assets/_Game/Materials", "Greybox");
            EnsureFolder("Assets/_Game/Prefabs", "Track");
            EnsureFolder("Assets/_Game/Prefabs", "Obstacles");
            CreateMaterials();

            k.Straight = TrackPrefab("Straight", 10f, BuildStraight);
            k.Tunnel = TrackPrefab("Tunnel", 10f, BuildTunnel);
            k.Bridge = TrackPrefab("Bridge", 10f, BuildBridge);
            k.Gap = TrackPrefab("Gap", 2.5f, BuildGap);

            k.Block = ObstaclePrefab("Block", ObstacleKind.Block, ObstacleLayer.Solid, false, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.6f), "Obstacle"));
            k.Low = ObstaclePrefab("LowBar", ObstacleKind.Low, ObstacleLayer.Solid, false, 0.4f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(1.8f, 0.4f, 0.4f), "Low"));
            k.Gate = ObstaclePrefab("Gate", ObstacleKind.Gate, ObstacleLayer.Solid, true, 0.4f, go =>
            {
                Box(go, "PostL", new Vector3(-3.3f, 1.75f, 0), new Vector3(0.3f, 3.5f, 0.3f), "Gate");
                Box(go, "PostR", new Vector3(3.3f, 1.75f, 0), new Vector3(0.3f, 3.5f, 0.3f), "Gate");
                Box(go, "Beam", new Vector3(0, 3.4f, 0), new Vector3(6.9f, 0.25f, 0.3f), "Gate");
            });
            k.Wall = ObstaclePrefab("Wall", ObstacleKind.Wall, ObstacleLayer.Solid, true, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 3f, 0.6f), "Wall"));
            k.Ghost = ObstaclePrefab("GhostBlock", ObstacleKind.Block, ObstacleLayer.Ghost, false, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.6f), "Ghost"));
            k.Barrier = ObstaclePrefab("PhaseBarrier", ObstacleKind.Block, ObstacleLayer.Solid, true, 0.3f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 3f, 0.3f), "Barrier"));

            k.GapMarker = ObstaclePrefab("GapMarker", ObstacleKind.Gap, ObstacleLayer.Solid, true, 2.5f,
                go => Box(go, "Hole", new Vector3(0, 0.01f, 0), new Vector3(TrackWidth, 0.02f, 2.5f), "Hole"));
            return k;
        }

        // ---------- scene ----------

        private static void BuildScene(TrackPiece straight, TrackPiece tunnel, TrackPiece bridge,
            TrackPiece gap, GameObject block, GameObject low, GameObject gate, GameObject wall, GameObject ghost,
            GameObject barrier)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera cam = Camera.main;

            ShiftInput input = CreateInput();

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

            CreateRunnerAndCamera(cam, input, builder, null);

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

        private static ShiftInput CreateInput()
        {
            var inputGo = new GameObject("Input");
            var input = inputGo.AddComponent<ShiftInput>();
            // Load just before assigning: creating prefabs and materials can reload assets and leave an earlier
            // reference stale (day 11: the field ended up empty).
            Set(input, "settings", AssetDatabase.LoadAssetAtPath<GestureSettings>(SettingsPath));
            inputGo.AddComponent<GestureDebugHud>();
            return input;
        }

        private static Runner CreateRunnerAndCamera(Camera cam, ShiftInput input, TrackBuilder builder, Conductor conductor)
        {
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
            if (conductor != null) Set(runner, "conductor", conductor);
            Set(runner, "track", builder);
            Set(runner, "body", body.transform);
            Set(runner, "bodyRenderer", bodyRenderer);
            Set(runner, "normalMaterial", Mats["Runner"]);
            Set(runner, "phaseMaterial", Mats["Phase"]);

            var chase = cam.gameObject.AddComponent<ChaseCamera>();
            Set(chase, "target", runner);
            cam.transform.position = new Vector3(0, 4, -7);
            return runner;
        }

        // ---------- level test scene (playbook 1.3) ----------

        private const string LevelScenePath = "Assets/_Game/Scenes/LevelTest.unity";
        private const string LevelJsonPath = "Assets/_Game/Levels/level00.json";
        private const string ClickPath = "Assets/_Game/Audio/Resources/click_100.wav";

        /// <summary>
        /// Shift+Shift > Create Level Test Scene: generates the 100 BPM click track if missing (Audio/Resources) and a
        /// scene where the Conductor plays level00.json and the ObstacleSpawner places its obstacles.
        /// </summary>
        [MenuItem("Shift+Shift/Create Level Test Scene")]
        public static void CreateLevelTestScene()
        {
            if (File.Exists(LevelScenePath))
            {
                EditorUtility.DisplayDialog("Level test", $"{LevelScenePath} already exists. Delete it first to regenerate.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelJsonPath);
            LevelData level = json != null ? JsonUtility.FromJson<LevelData>(json.text) : null;
            if (level == null || level.Validate().Count > 0)
            {
                Debug.LogError($"[LevelTest] {LevelJsonPath} is missing or invalid.");
                return;
            }

            EnsureClickTrack();
            Kit k = EnsureAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Camera cam = Camera.main;
            ShiftInput input = CreateInput();

            var conductorGo = new GameObject("Conductor");
            var conductor = conductorGo.AddComponent<Conductor>(); // adds its AudioSource
            Set(conductor, "levelJson", AssetDatabase.LoadAssetAtPath<TextAsset>(LevelJsonPath));
            Set(conductor, "input", input);

            // Track from 20 m behind the start (lead-in) to 20 m past the last beat; 5 m per beat (Runner default).
            const float metresPerBeat = 5f;
            var trackGo = new GameObject("Track");
            trackGo.transform.position = new Vector3(0, 0, -20f);
            var builder = trackGo.AddComponent<TrackBuilder>();
            builder.lengthMetres = (float)level.lengthBeats * metresPerBeat + 40f;
            builder.fillPiece = k.Straight;
            builder.Build();

            Runner runner = CreateRunnerAndCamera(cam, input, builder, conductor);

            var spawnerGo = new GameObject("Obstacles");
            var spawner = spawnerGo.AddComponent<ObstacleSpawner>();
            Set(spawner, "conductor", conductor);
            Set(spawner, "runner", runner);
            SetTypePrefabs(spawner, new[]
            {
                ("bar", k.Block), ("low", k.Low), ("gate", k.Gate), ("gap", k.GapMarker), ("ghost", k.Barrier), ("wall", k.Wall),
            });

            EditorSceneManager.SaveScene(scene, LevelScenePath);
            Debug.Log($"[LevelTest] Created {LevelScenePath} for {level.name}. Press Play, then Shift to start.");
        }

        private static void EnsureClickTrack()
        {
            if (File.Exists(ClickPath)) return;
            EnsureFolder("Assets/_Game", "Audio");
            EnsureFolder("Assets/_Game/Audio", "Resources");
            File.WriteAllBytes(ClickPath, ClickTrack.GenerateWav(100, 40)); // 32-beat level + 8 spare beats
            AssetDatabase.ImportAsset(ClickPath);
            Debug.Log($"[LevelTest] Generated {ClickPath} (100 BPM, 40 beats, beat 0 at 0 ms).");
        }

        private static void SetTypePrefabs(ObstacleSpawner spawner, (string type, GameObject prefab)[] map)
        {
            var so = new SerializedObject(spawner);
            SerializedProperty list = so.FindProperty("prefabs");
            list.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("type").stringValue = map[i].type;
                e.FindPropertyRelative("prefab").objectReferenceValue = map[i].prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
            Mat("Hole", new Color(0.05f, 0.05f, 0.07f));
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
            if (new SerializedObject(target).FindProperty(field).objectReferenceValue == null)
                Debug.LogError($"[Greybox] Could not assign {target.GetType().Name}.{field} " +
                               $"({(value == null ? "value was null" : value.name)}). Assign it by hand in the Inspector.");
        }
    }
}
