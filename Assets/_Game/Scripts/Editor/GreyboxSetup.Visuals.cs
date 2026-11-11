using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Shift+Shift > Set Up Visuals (playbook 2.2, step 1): six Palette assets (Docs/PALETTES.md, chosen with John on
    /// day 16), road edge and lane lines on the track pieces, and a PaletteApplier in the Run, LevelTest and Greybox
    /// scenes (tracks rebuilt, fog enabled in each scene's lighting so builds keep the fog shader variants).
    /// Re-runnable: creates what is missing, refreshes the rest. Never changes an existing palette's colours.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private const string PaletteDir = "Assets/_Game/Palettes";

        private struct PaletteSpec
        {
            public string Name, Background, Accent;
        }

        // Skies and accents per level; road, lines and the six action colours are the same on every level.
        private static readonly PaletteSpec[] PaletteSpecs =
        {
            new PaletteSpec { Name = "dawn", Background = "#FBD9C0", Accent = "#1E5FA8" },
            new PaletteSpec { Name = "overcast", Background = "#C9D1D8", Accent = "#0E5E6F" },
            new PaletteSpec { Name = "dusk", Background = "#3B2550", Accent = "#FFD166" },
            new PaletteSpec { Name = "night", Background = "#0B1026", Accent = "#5CE1E6" },
            new PaletteSpec { Name = "neon", Background = "#07020F", Accent = "#00F5D4" },
            new PaletteSpec { Name = "fullshift", Background = "#101418", Accent = "#F7F7F2" },
        };

        [MenuItem("Shift+Shift/Set Up Visuals")]
        public static void SetUpVisuals()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var log = new List<string>();

            List<Palette> palettes = EnsurePalettes(log);
            EnsureAssets();                 // materials (incl. TrackLines) and prefabs exist
            AddTrackLines(log);
            AddGapRims(log);
            RebuildOpeningLayouts(log); // step 5: solid wide bar slabs, framed gate doorway (before edges are added)
            AddGlowEdges(log);       // step 2: glowing obstacle edges
            MakeBarrierSeeThrough(log); // day 17 (John): see the road behind the phase barrier
            MakeWallBrick(log);         // day 17 (John): the charge wall looks like bricks
            EnableBloomInProfile(log);
            EnsureCameraFeel(log);   // step 3: camera feel asset
            EnsureLongClick(log);    // playbook 2.3: audio for the track test level

            foreach (string scenePath in new[] { RunScenePath, LevelScenePath, ScenePath })
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                // Load assets AFTER opening the scene (NewScene/OpenScene can unload earlier references).
                palettes = LoadPalettes();
                EnsureAssets();
                CreateGlowMaterials();
                WireScene(scene, palettes, log);
            }
            if (File.Exists(RunScenePath)) EditorSceneManager.OpenScene(RunScenePath, OpenSceneMode.Single);
            Debug.Log("[Visuals] Done.\n- " + string.Join("\n- ", log));
        }

        private static List<Palette> EnsurePalettes(List<string> log)
        {
            if (!AssetDatabase.IsValidFolder(PaletteDir)) AssetDatabase.CreateFolder("Assets/_Game", "Palettes");
            foreach (PaletteSpec spec in PaletteSpecs)
            {
                string path = $"{PaletteDir}/{spec.Name}.asset";
                if (AssetDatabase.LoadAssetAtPath<Palette>(path) != null) continue;
                var p = ScriptableObject.CreateInstance<Palette>();
                p.background = Hex(spec.Background);
                p.accent = Hex(spec.Accent);
                p.road = Hex("#333333");
                p.roadDetail = Hex("#2A2A2A");
                p.lines = Hex("#BFBFBF");
                p.lane = Hex("#F27017");
                p.slide = Hex("#FCC41E");
                p.sprint = Hex("#00FCF9");
                p.jump = Hex("#F40E9D");
                p.phase = Hex("#0E79F1");
                p.charge = Hex("#F21E3D");
                AssetDatabase.CreateAsset(p, path);
                log.Add($"Created palette {path}");
            }
            AssetDatabase.SaveAssets();
            return LoadPalettes();
        }

        private static List<Palette> LoadPalettes()
        {
            var list = new List<Palette>();
            foreach (PaletteSpec spec in PaletteSpecs)
            {
                var p = AssetDatabase.LoadAssetAtPath<Palette>($"{PaletteDir}/{spec.Name}.asset");
                if (p != null) list.Add(p);
            }
            return list;
        }

        private static void AddTrackLines(List<string> log)
        {
            foreach (string piece in new[] { "Straight", "Tunnel", "Bridge" })
            {
                string path = $"{TrackDir}/{piece}.prefab";
                if (!File.Exists(path)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root.transform.Find("Line_EdgeL") == null)
                {
                    float len = root.GetComponent<TrackPiece>().length;
                    // Edges at the road sides, lane dividers between the lanes (lanes at -2, 0, 2).
                    Box(root, "Line_EdgeL", new Vector3(-3.35f, 0.01f, len * 0.5f), new Vector3(0.15f, 0.02f, len), "TrackLines");
                    Box(root, "Line_EdgeR", new Vector3(3.35f, 0.01f, len * 0.5f), new Vector3(0.15f, 0.02f, len), "TrackLines");
                    Box(root, "Line_LaneL", new Vector3(-1f, 0.01f, len * 0.5f), new Vector3(0.07f, 0.02f, len), "TrackLines");
                    Box(root, "Line_LaneR", new Vector3(1f, 0.01f, len * 0.5f), new Vector3(0.07f, 0.02f, len), "TrackLines");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    log.Add($"Added edge and lane lines to {piece}");
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Pink (jump colour) rims on the existing GapMarker prefab, and the hole lifted above the lane lines.</summary>
        private static void AddGapRims(List<string> log)
        {
            string path = $"{ObstacleDir}/GapMarker.prefab";
            if (!File.Exists(path)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            Transform hole = root.transform.Find("Hole");
            if (hole != null && hole.localPosition.y < 0.025f)
            {
                hole.localPosition = new Vector3(0, 0.03f, 0);
                changed = true;
            }
            if (root.transform.Find("RimFront") == null)
            {
                Box(root, "RimFront", new Vector3(0, 0.05f, -1.25f), new Vector3(TrackWidth, 0.03f, 0.15f), "Hurdle");
                Box(root, "RimBack", new Vector3(0, 0.05f, 1.25f), new Vector3(TrackWidth, 0.03f, 0.15f), "Hurdle");
                changed = true;
            }
            if (root.transform.Find("Pit") == null)
            {
                // Day 17 (John): depth for the gap. The road is cut at play time; this pit shows through the cut.
                // Wall tops sit just under the ground level (-0.05) so nothing lies in the same plane as it.
                var pit = new GameObject("Pit");
                pit.transform.SetParent(root.transform, false);
                Box(pit, "WallFront", new Vector3(0, -1.56f, -1.4f), new Vector3(TrackWidth + 0.6f, 3f, 0.3f), "TrackDark");
                Box(pit, "WallBack", new Vector3(0, -1.56f, 1.4f), new Vector3(TrackWidth + 0.6f, 3f, 0.3f), "TrackDark");
                Box(pit, "WallL", new Vector3(-3.65f, -1.56f, 0), new Vector3(0.3f, 3f, 2.5f), "TrackDark");
                Box(pit, "WallR", new Vector3(3.65f, -1.56f, 0), new Vector3(0.3f, 3f, 2.5f), "TrackDark");
                Box(pit, "Water", new Vector3(0, -1.2f, 0), new Vector3(TrackWidth, 0.05f, 2.5f), "Water");
                changed = true;
                log.Add("GapMarker: pit with water added");
            }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add("GapMarker: saved (hole above the lane lines, pink rims, pit)");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void WireScene(UnityEngine.SceneManagement.Scene scene, List<Palette> palettes, List<string> log)
        {
            // Playbook 2.3: the track is built along its path when the scene plays. Remove the pieces saved in the
            // scene by earlier versions and point the builder at its materials, dressing prefabs, level and runner.
            foreach (TrackBuilder tb in Object.FindObjectsByType<TrackBuilder>())
                WireTrack(tb, scene.path == ScenePath, log, Path.GetFileNameWithoutExtension(scene.path));

            var applier = Object.FindAnyObjectByType<PaletteApplier>();
            if (applier == null) applier = new GameObject("Visuals").AddComponent<PaletteApplier>();

            var so = new SerializedObject(applier);
            so.FindProperty("conductor").objectReferenceValue = Object.FindAnyObjectByType<Conductor>();
            so.FindProperty("cam").objectReferenceValue = Camera.main;
            Palette dawn = palettes.Find(p => p.name == "dawn");
            Palette night = palettes.Find(p => p.name == "night");
            // No level in the Greybox scene: show it at night so the lines are easy to judge.
            so.FindProperty("fallback").objectReferenceValue = scene.path == ScenePath ? night : dawn;
            SerializedProperty list = so.FindProperty("palettes");
            list.arraySize = palettes.Count;
            for (int i = 0; i < palettes.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = palettes[i];

            var roles = new (string mat, PaletteApplier.Role role)[]
            {
                ("Track", PaletteApplier.Role.Road), ("TrackDark", PaletteApplier.Role.RoadDetail),
                ("TrackLines", PaletteApplier.Role.Lines), ("Obstacle", PaletteApplier.Role.Lane),
                ("Low", PaletteApplier.Role.Slide), ("Gate", PaletteApplier.Role.Sprint),
                ("Hurdle", PaletteApplier.Role.Jump), ("Barrier", PaletteApplier.Role.Phase),
                ("Ghost", PaletteApplier.Role.Ghost), ("Wall", PaletteApplier.Role.Charge), ("Hole", PaletteApplier.Role.Hole),
                ("Foliage", PaletteApplier.Role.Scenery),
            };
            var all = new List<(Material mat, PaletteApplier.Role role, bool glow)>();
            foreach (var r in roles) all.Add((Mats[r.mat], r.role, false));
            all.AddRange(GlowRoles());
            SerializedProperty rp = so.FindProperty("roles");
            rp.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++)
            {
                SerializedProperty e = rp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("material").objectReferenceValue = all[i].mat;
                e.FindPropertyRelative("role").enumValueIndex = (int)all[i].role;
                e.FindPropertyRelative("glow").boolValue = all[i].glow;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            WirePostProcessing(Camera.main, log, Path.GetFileNameWithoutExtension(scene.path));
            WireFeelAndTrail(applier, log, Path.GetFileNameWithoutExtension(scene.path));
            if (scene.path == RunScenePath) PutTrackTestLevelFirst(log);

            // Fog on in the scene's own lighting settings, so the fog shader variants are kept in builds.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 170f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.Add($"{Path.GetFileNameWithoutExtension(scene.path)}: palette applier wired ({palettes.Count} palettes), fog on");
        }

        private static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }
    }
}
