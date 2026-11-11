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
    public static partial class GreyboxSetup
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

            // New scene FIRST: NewScene(Single) unloads assets nothing references yet, which left earlier references
            // empty (day 11 Settings field, day 12 track piece). Load everything after it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Kit k = EnsureAssets();
            BuildScene(scene, k.Straight, k.Tunnel, k.Bridge, k.Gap, k.Block, k.Low, k.Gate, k.Wall, k.Ghost, k.Barrier);
        }

        // ---------- shared asset kit ----------

        private sealed class Kit
        {
            public TrackPiece Straight, Tunnel, Bridge, Gap;
            public GameObject Block, Low, Gate, Wall, Ghost, Barrier, GapMarker, GateOpening, WideBar, Hurdle;
            public GameObject TunnelShell, BridgeRails, Pillars; // track dressing (playbook 2.3)
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
            EnsureDressing(k); // playbook 2.3: the road itself is generated; these dress tunnel/bridge/pillars pieces

            k.Block = ObstaclePrefab("Block", ObstacleKind.Block, ObstacleLayer.Solid, false, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.6f), "Obstacle"));
            k.Low = ObstaclePrefab("LowBar", ObstacleKind.Low, ObstacleLayer.Solid, true, 0.4f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 0.4f, 0.4f), "Low"));
            k.Gate = ObstaclePrefab("Gate", ObstacleKind.Gate, ObstacleLayer.Solid, true, 0.4f, go =>
            {
                Box(go, "PostL", new Vector3(-3.3f, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), "Gate");
                Box(go, "PostR", new Vector3(3.3f, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), "Gate");
                Box(go, "Beam", new Vector3(0, 3.1f, 0), new Vector3(6.9f, 0.2f, 0.3f), "Gate");
            });
            k.Wall = ObstaclePrefab("Wall", ObstacleKind.Wall, ObstacleLayer.Solid, true, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 3f, 0.6f), "Wall"));
            k.Ghost = ObstaclePrefab("GhostBlock", ObstacleKind.Block, ObstacleLayer.Ghost, false, 0.6f,
                go => Box(go, "Mesh", new Vector3(0, 1f, 0), new Vector3(1.6f, 2f, 0.6f), "Ghost"));
            k.Barrier = ObstaclePrefab("PhaseBarrier", ObstacleKind.Barrier, ObstacleLayer.Solid, true, 0.3f,
                go => Box(go, "Mesh", new Vector3(0, 1.5f, 0), new Vector3(TrackWidth, 3f, 0.3f), "Barrier"));

            // Hole sits just above the lane lines (y 0-0.02) to avoid z-fighting; pink (jump) rims mark its edges.
            k.GapMarker = ObstaclePrefab("GapMarker", ObstacleKind.Gap, ObstacleLayer.Solid, true, 2.5f, go =>
            {
                Box(go, "Hole", new Vector3(0, 0.03f, 0), new Vector3(TrackWidth, 0.02f, 2.5f), "Hole");
                Box(go, "RimFront", new Vector3(0, 0.05f, -1.25f), new Vector3(TrackWidth, 0.03f, 0.15f), "Hurdle");
                Box(go, "RimBack", new Vector3(0, 0.05f, 1.25f), new Vector3(TrackWidth, 0.03f, 0.15f), "Hurdle");
            });

            // Day 13 (John): gate with a one-lane opening, wide bar with one free lane, short pink hurdle.
            // Opening types have one panel per lane (Panel0/1/2 = chart lanes left/middle/right); the spawner hides
            // the open lane's panel.
            k.GateOpening = ObstaclePrefab("GateOpening", ObstacleKind.Gate, ObstacleLayer.Solid, false, 0.4f, go =>
            {
                for (int lane = 0; lane < 3; lane++)
                    Box(go, "Panel" + lane, new Vector3((lane - 1) * 2f, 1.5f, 0), new Vector3(1.95f, 3f, 0.3f), "Gate");
                Box(go, "Beam", new Vector3(0, 3.1f, 0), new Vector3(TrackWidth, 0.2f, 0.3f), "Gate");
            }, laneIsOpening: true);
            k.WideBar = ObstaclePrefab("WideBar", ObstacleKind.Block, ObstacleLayer.Solid, false, 0.6f, go =>
            {
                for (int lane = 0; lane < 3; lane++)
                    Box(go, "Panel" + lane, new Vector3((lane - 1) * 2f, 1f, 0), new Vector3(1.9f, 2f, 0.6f), "Obstacle");
            }, laneIsOpening: true);
            k.Hurdle = ObstaclePrefab("Hurdle", ObstacleKind.Hurdle, ObstacleLayer.Solid, true, 0.4f,
                go => Box(go, "Mesh", new Vector3(0, 0.4f, 0), new Vector3(TrackWidth, 0.8f, 0.4f), "Hurdle"));
            return k;
        }

        // ---------- scene ----------

        private static void BuildScene(UnityEngine.SceneManagement.Scene scene, TrackPiece straight, TrackPiece tunnel, TrackPiece bridge,
            TrackPiece gap, GameObject block, GameObject low, GameObject gate, GameObject wall, GameObject ghost,
            GameObject barrier)
        {
            Camera cam = Camera.main;

            ShiftInput input = CreateInput();

            // Track (built when the scene plays): tunnel 60-100 m, gap 100-102.5 m, bridge 105-145 m. Straight shape
            // only: the obstacles here are placed by hand in world positions.
            var trackGo = new GameObject("Track");
            var builder = trackGo.AddComponent<TrackBuilder>();
            builder.lengthMetres = 200f;

            Runner greyboxRunner = CreateRunnerAndCamera(cam, input, builder, null);
            ConfigureTrack(builder, null, greyboxRunner);

            // Starter obstacles (10 m/s stub speed: 10 m = 1 s = 2 beats at 120 BPM)
            var parent = new GameObject("Obstacles").transform;
            Place(block, parent, 0, 30);                       // move left or right
            Place(block, parent, -1, 45); Place(block, parent, 1, 45); // back to the middle
            Place(low, parent, 0, 65);                         // slide (inside the tunnel)
            Place(gate, parent, 0, 85);                        // sprint
            Place(AssetDatabase.LoadAssetAtPath<GameObject>($"{ObstacleDir}/GapMarker.prefab"), parent, 0, 101.25f); // jump
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
            cam.transform.position = new Vector3(0, 3.3f, -7);
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
            if (json == null)
            {
                Debug.LogError($"[LevelTest] {LevelJsonPath} not found as a text asset " +
                               $"(file on disk: {(File.Exists(LevelJsonPath) ? "yes, not imported yet: try Assets > Refresh" : "no")}).");
                return;
            }
            LevelData level = JsonUtility.FromJson<LevelData>(json.text);
            var errors = level != null ? level.Validate() : new List<string> { "could not parse JSON" };
            if (errors.Count > 0)
            {
                Debug.LogError($"[LevelTest] {LevelJsonPath} is invalid:\n- " + string.Join("\n- ", errors));
                return;
            }

            // New scene FIRST, then load assets (see Create()).
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EnsureClickTrack();
            Kit k = EnsureAssets();
            Camera cam = Camera.main;
            ShiftInput input = CreateInput();

            var conductorGo = new GameObject("Conductor");
            var conductor = conductorGo.AddComponent<Conductor>(); // adds its AudioSource
            Set(conductor, "levelJson", AssetDatabase.LoadAssetAtPath<TextAsset>(LevelJsonPath));
            Set(conductor, "input", input);

            // The track is built from the level when the scene plays (playbook 2.3).
            var trackGo = new GameObject("Track");
            var builder = trackGo.AddComponent<TrackBuilder>();

            Runner runner = CreateRunnerAndCamera(cam, input, builder, conductor);
            ConfigureTrack(builder, conductor, runner);

            var spawnerGo = new GameObject("Obstacles");
            var spawner = spawnerGo.AddComponent<ObstacleSpawner>();
            Set(spawner, "conductor", conductor);
            Set(spawner, "runner", runner);
            SetTypePrefabs(spawner, TypeMap(k));

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

        /// <summary>Chart type -> prefab. One place, used by every scene the tools build or refresh.</summary>
        private static (string type, GameObject prefab)[] TypeMap(Kit k) => new[]
        {
            ("bar", k.Block), ("wide", k.WideBar), ("low", k.Low), ("gate", k.GateOpening), ("gap", k.GapMarker),
            ("hurdle", k.Hurdle), ("ghost", k.Barrier), ("ghostbar", k.Barrier), ("ghostwide", k.Barrier), ("wall", k.Wall),
        };

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
                if (map[i].prefab == null) Debug.LogError($"[LevelTest] No prefab for type '{map[i].type}'.");
            }
            so.FindProperty("ghostBlockPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{ObstacleDir}/GhostBlock.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Run, Calibrate and Boot scenes (playbook 1.4) ----------

        private const string RunScenePath = "Assets/_Game/Scenes/Run.unity";
        private const string BootScenePath = "Assets/_Game/Scenes/Boot.unity";
        private const string CalibrateScenePath = "Assets/_Game/Scenes/Calibrate.unity";

        /// <summary>
        /// Shift+Shift > Set Up Run Scenes: fills the (empty) Run scene with the run loop, creates the Calibrate
        /// scene, makes Boot load Run, and puts Boot, Menu, Run, Calibrate (then anything else) in the build list.
        /// Leaves a scene alone if it is already set up.
        /// </summary>
        [MenuItem("Shift+Shift/Set Up Run Scenes")]
        public static void SetUpRunScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var levelJson = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelJsonPath);
            if (levelJson == null)
            {
                Debug.LogError($"[RunSetup] {LevelJsonPath} not found.");
                return;
            }
            var log = new List<string>();

            // Run scene (open it FIRST, then load assets: see Create()).
            var run = EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<GameFlow>() != null)
            {
                RefreshSpawner(run, log, "Run");
            }
            else
            {
                EnsureClickTrack();
                Kit k = EnsureAssets();
                Camera cam = Camera.main;
                ShiftInput input = CreateInput();

                var conductorGo = new GameObject("Conductor");
                var conductor = conductorGo.AddComponent<Conductor>();
                Set(conductor, "input", input);
                SetBool(conductor, "showStartPrompt", false); // GameFlow draws the prompt

                // Track: built from the playing level when the scene starts, from 60 m behind beat 0 (delay +
                // two-bar count-in) to 60 m past the last beat (playbook 2.3).
                var trackGo = new GameObject("Track");
                var builder = trackGo.AddComponent<TrackBuilder>();

                Runner runner = CreateRunnerAndCamera(cam, input, builder, conductor);
                ConfigureTrack(builder, conductor, runner);

                var spawner = new GameObject("Obstacles").AddComponent<ObstacleSpawner>();
                Set(spawner, "conductor", conductor);
                Set(spawner, "runner", runner);
                SetTypePrefabs(spawner, TypeMap(k));

                var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
                Set(flow, "conductor", conductor);
                Set(flow, "runner", runner);
                var so = new SerializedObject(flow);
                SerializedProperty levels = so.FindProperty("levels");
                levels.arraySize = 1;
                levels.GetArrayElementAtIndex(0).objectReferenceValue = levelJson;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.SaveScene(run);
                log.Add("Run scene set up: input, conductor, track, runner, camera, spawner, GameFlow (level00)");
            }

            // LevelTest scene: keep its obstacle list in step with the chart types
            if (File.Exists(LevelScenePath))
                RefreshSpawner(EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single), log, "LevelTest");

            // Calibrate scene
            if (!File.Exists(CalibrateScenePath))
            {
                var cal = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                new GameObject("Calibration").AddComponent<LatencyCalibration>();
                EditorSceneManager.SaveScene(cal, CalibrateScenePath);
                log.Add("Created Calibrate scene");
            }
            else log.Add("Calibrate scene exists: left alone");

            // Boot scene loads Run
            var boot = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<Boot>() == null)
            {
                new GameObject("Boot").AddComponent<Boot>();
                EditorSceneManager.SaveScene(boot);
                log.Add("Boot scene now loads Run");
            }
            else log.Add("Boot scene already has a Boot loader: left alone");

            // Build list
            string[] wanted = { BootScenePath, "Assets/_Game/Scenes/Menu.unity", RunScenePath, CalibrateScenePath };
            var list = new List<EditorBuildSettingsScene>();
            foreach (string p in wanted)
                if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (System.Array.IndexOf(wanted, s.path) < 0) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
            log.Add("Build list: " + string.Join(", ", list.ConvertAll(s => Path.GetFileNameWithoutExtension(s.path))));

            EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            Debug.Log("[RunSetup] Done.\n- " + string.Join("\n- ", log));
        }

        /// <summary>Re-points an existing scene's spawner at the current chart-type prefabs (new types since setup).</summary>
        private static void RefreshSpawner(UnityEngine.SceneManagement.Scene scene, List<string> log, string label)
        {
            var spawner = Object.FindAnyObjectByType<ObstacleSpawner>();
            if (spawner == null)
            {
                log.Add($"{label}: no ObstacleSpawner, left alone");
                return;
            }
            Kit k = EnsureAssets(); // after the scene is open (see Create())
            SetTypePrefabs(spawner, TypeMap(k));
            EditorSceneManager.SaveScene(scene);
            log.Add($"{label}: already set up; obstacle types refreshed ({TypeMap(k).Length} types)");
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] {target.GetType().Name} has no field '{field}'."); return; }
            p.boolValue = value;
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
            float depth, System.Action<GameObject> build, bool laneIsOpening = false)
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
            o.laneIsOpening = laneIsOpening;
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
            Mat("Hurdle", new Color(1f, 0.45f, 0.75f)); // pink: short wall (orange = obstacles, red = tall wall)
            Mat("TrackLines", new Color(0.75f, 0.75f, 0.75f)); // road edges and lane lines (day 16)
            Mat("Foliage", new Color(0.2f, 0.3f, 0.25f)); // trees and shrubs (day 17); palettes tint it
            Mat("Water", new Color(0.10f, 0.32f, 0.42f)); // in the gap pits (day 17); same on every level, darker and
                                                          // greyer than the phase blue so it is not read as an action
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
