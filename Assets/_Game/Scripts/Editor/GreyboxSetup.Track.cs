using System.Collections.Generic;
using ShiftShift.Core;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Playbook 2.3, the modular track kit. The road follows the level's path and is generated when the scene plays
    /// (TrackBuilder); this file makes the dressing prefabs for the tunnel, bridge and pillars pieces and wires the
    /// TrackBuilder in each scene. Each dressing prefab covers 10 m along +Z from its origin and has no floor.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private static void EnsureDressing(Kit k)
        {
            k.TunnelShell = DressingPrefab("TunnelShell", go =>
            {
                Box(go, "Ceiling", new Vector3(0, 3.75f, 5f), new Vector3(TrackWidth + 1f, 0.5f, 10f), "TrackDark");
                Box(go, "WallL", new Vector3(-3.75f, 1.75f, 5f), new Vector3(0.5f, 3.5f, 10f), "TrackDark");
                Box(go, "WallR", new Vector3(3.75f, 1.75f, 5f), new Vector3(0.5f, 3.5f, 10f), "TrackDark");
                Box(go, "LightL", new Vector3(-3.42f, 3.46f, 5f), new Vector3(0.12f, 0.06f, 10f), "TrackLines");
                Box(go, "LightR", new Vector3(3.42f, 3.46f, 5f), new Vector3(0.12f, 0.06f, 10f), "TrackLines");
            });
            k.BridgeRails = DressingPrefab("BridgeRails", go =>
            {
                Box(go, "RailL", new Vector3(-3.42f, 0.4f, 5f), new Vector3(0.15f, 0.8f, 10f), "TrackDark");
                Box(go, "RailR", new Vector3(3.42f, 0.4f, 5f), new Vector3(0.15f, 0.8f, 10f), "TrackDark");
                Box(go, "RailTopL", new Vector3(-3.42f, 0.83f, 5f), new Vector3(0.15f, 0.06f, 10f), "TrackLines");
                Box(go, "RailTopR", new Vector3(3.42f, 0.83f, 5f), new Vector3(0.15f, 0.06f, 10f), "TrackLines");
                Box(go, "CrossBeam", new Vector3(0, -0.8f, 5f), new Vector3(TrackWidth + 0.6f, 0.6f, 0.8f), "TrackDark");
                Box(go, "PierL", new Vector3(-3f, -11f, 5f), new Vector3(0.7f, 20f, 0.7f), "TrackDark");
                Box(go, "PierR", new Vector3(3f, -11f, 5f), new Vector3(0.7f, 20f, 0.7f), "TrackDark");
            });
            k.Pillars = DressingPrefab("Pillars", go =>
            {
                Box(go, "PostL", new Vector3(-4.2f, 2.5f, 5f), new Vector3(0.5f, 6f, 0.5f), "TrackDark");
                Box(go, "PostR", new Vector3(4.2f, 2.5f, 5f), new Vector3(0.5f, 6f, 0.5f), "TrackDark");
                Box(go, "Beam", new Vector3(0, 5.25f, 5f), new Vector3(8.9f, 0.5f, 0.5f), "TrackDark");
                Box(go, "Light", new Vector3(0, 4.97f, 5f), new Vector3(TrackWidth, 0.06f, 0.2f), "TrackLines");
            });
            EnsureScenery();
        }

        // ---------- day 17 look changes (John): roadside scenery, see-through barrier, brick wall ----------

        private const string BrickPath = MatDir + "/Brick.png";

        private static void EnsureScenery()
        {
            DressingPrefab("RoadsidePost", go =>
            {
                Box(go, "Post", new Vector3(0, 0.6f, 0), new Vector3(0.18f, 2.2f, 0.18f), "TrackDark"); // from -0.5 to 1.7 m
                Box(go, "Tip", new Vector3(0, 1.8f, 0), new Vector3(0.22f, 0.22f, 0.22f), "TrackLines");
            });
            // Buildings: six shapes (John, day 17: one repeated block looked repetitive). Each stands on the ground and
            // reaches below it, so it stays grounded where the builder sinks it or the road slopes.
            DressingPrefab("BackdropTower", go => Tall(go, "Body", 0, 0, 5f, 5f, 14f));
            DressingPrefab("BackdropSlab", go => Tall(go, "Body", 0, 0, 12f, 6f, 5f));
            DressingPrefab("BackdropStepped", go =>
            {
                Tall(go, "Base", 0, 0, 8f, 8f, 6f);
                Tall(go, "Mid", 0, 0, 5.5f, 5.5f, 11f);
                Tall(go, "Top", 0, 0, 3f, 3f, 15f);
            });
            DressingPrefab("BackdropTwin", go =>
            {
                Tall(go, "TowerA", -2.5f, 0, 3f, 3f, 10f);
                Tall(go, "TowerB", 2.5f, 0.5f, 3f, 3f, 13f);
                Tall(go, "Link", 0, 0, 8f, 2f, 4.5f);
            });
            DressingPrefab("BackdropSpire", go =>
            {
                Tall(go, "Body", 0, 0, 4f, 4f, 9f);
                Tall(go, "Spire", 0, 0, 1f, 1f, 17f);
            });
            DressingPrefab("BackdropArch", go =>
            {
                Tall(go, "LegA", -3.5f, 0, 2f, 2f, 8f);
                Tall(go, "LegB", 3.5f, 0, 2f, 2f, 8f);
                Box(go, "Lintel", new Vector3(0, 8.75f, 0), new Vector3(9f, 1.5f, 2f), "TrackDark");
                Box(go, "Cap", new Vector3(0, 9.6f, 0), new Vector3(9.2f, 0.2f, 2.2f), "TrackLines");
            });

            // Plants: blocky, to match the rest. "Foliage" is a muted tint of each palette's accent.
            DressingPrefab("TreeTiered", go =>
            {
                Box(go, "Trunk", new Vector3(0, 1f, 0), new Vector3(0.35f, 2.4f, 0.35f), "TrackDark");
                Box(go, "Crown1", new Vector3(0, 2.6f, 0), new Vector3(2.4f, 1.2f, 2.4f), "Foliage");
                Box(go, "Crown2", new Vector3(0, 3.7f, 0), new Vector3(1.7f, 1.1f, 1.7f), "Foliage");
                Box(go, "Crown3", new Vector3(0, 4.7f, 0), new Vector3(1f, 1f, 1f), "Foliage");
            });
            DressingPrefab("TreeRound", go =>
            {
                Box(go, "Trunk", new Vector3(0, 0.8f, 0), new Vector3(0.4f, 2f, 0.4f), "TrackDark");
                Box(go, "Crown", new Vector3(0, 2.9f, 0), new Vector3(2.6f, 2.2f, 2.6f), "Foliage");
                Box(go, "CrownTop", new Vector3(0.3f, 4.3f, -0.2f), new Vector3(1.6f, 1.1f, 1.6f), "Foliage");
            });
            DressingPrefab("Shrub", go =>
            {
                Box(go, "Low", new Vector3(0, 0.35f, 0), new Vector3(1.5f, 0.9f, 1.2f), "Foliage");
                Box(go, "High", new Vector3(0.3f, 0.95f, 0.1f), new Vector3(0.9f, 0.6f, 0.8f), "Foliage");
            });
        }

        private static readonly string[] BackdropNames =
            { "BackdropTower", "BackdropSlab", "BackdropStepped", "BackdropTwin", "BackdropSpire", "BackdropArch" };
        private static readonly string[] PlantNames = { "TreeTiered", "TreeRound", "Shrub", "Shrub" }; // shrubs twice as often

        /// <summary>A dark box from 30 m below the ground up to <paramref name="top"/>, with a lit cap.</summary>
        private static void Tall(GameObject go, string name, float x, float z, float width, float depth, float top)
        {
            Box(go, name, new Vector3(x, (top - 30f) * 0.5f, z), new Vector3(width, top + 30f, depth), "TrackDark");
            Box(go, name + "Cap", new Vector3(x, top + 0.1f, z), new Vector3(width + 0.2f, 0.2f, depth + 0.2f), "TrackLines");
        }

        private static void SetPrefabList(Object target, string field, string[] names)
        {
            var so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty(field);
            if (list == null) { Debug.LogError($"[Setup] {target.GetType().Name} has no field '{field}'."); return; }
            list.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{TrackDir}/{names[i]}.prefab");
                if (prefab == null) Debug.LogError($"[Setup] Missing {TrackDir}/{names[i]}.prefab.");
                list.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private const float BarrierAlpha = 0.5f; // John, day 17: 0.3 was a little too see-through

        /// <summary>The phase barrier becomes a see-through panel in a bright frame: you can see the road go on behind it.</summary>
        private static void MakeBarrierSeeThrough(List<string> log)
        {
            Material m = Mats["Barrier"];
            Color c = m.GetColor("_BaseColor");
            if (Mathf.Abs(c.a - BarrierAlpha) > 0.01f || m.renderQueue < 3000)
            {
                // URP Lit, transparent surface (what the Inspector's Surface Type = Transparent sets).
                c.a = BarrierAlpha;
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                EditorUtility.SetDirty(m);
                log.Add($"Barrier material: see-through ({BarrierAlpha * 100f:0}% solid)");
            }

            string path = $"{ObstacleDir}/PhaseBarrier.prefab";
            if (!System.IO.File.Exists(path)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root.transform.Find("FrameL") == null)
            {
                // Bright posts and a sill in the phase glow colour (the top edge strip is already there).
                Box(root, "FrameL", new Vector3(-3.45f, 1.5f, 0), new Vector3(0.12f, 3f, 0.34f), "PhaseGlow");
                Box(root, "FrameR", new Vector3(3.45f, 1.5f, 0), new Vector3(0.12f, 3f, 0.34f), "PhaseGlow");
                Box(root, "FrameBottom", new Vector3(0, 0.06f, 0), new Vector3(TrackWidth, 0.12f, 0.34f), "PhaseGlow");
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add("PhaseBarrier: bright frame added");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>Brick pattern (generated here, grey on white) on the charge wall; the palette's red tints it.</summary>
        private static void MakeWallBrick(List<string> log)
        {
            if (!System.IO.File.Exists(BrickPath))
            {
                // One tile = one brick wide, two rows high, the second row shifted half a brick.
                const int w = 64, h = 32, mortar = 2;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                var brick = new Color(1f, 1f, 1f);
                var joint = new Color(0.35f, 0.35f, 0.35f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int row = y / (h / 2);
                    bool rowJoint = y % (h / 2) < mortar;
                    int shifted = (x + (row == 1 ? w / 2 : 0)) % w;
                    tex.SetPixel(x, y, rowJoint || shifted < mortar ? joint : brick);
                }
                System.IO.File.WriteAllBytes(BrickPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(BrickPath);
                log.Add($"Generated {BrickPath}");
            }
            var brickTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BrickPath);
            Material m = Mats["Wall"];
            if (brickTex != null)
            {
                // Wall is 7 x 3 m: bricks 2 m long in rows of 0.5 m (John, day 17: the first, smaller bricks only
                // showed up close).
                m.SetTexture("_BaseMap", brickTex);
                m.SetTextureScale("_BaseMap", new Vector2(3.5f, 3f));
                EditorUtility.SetDirty(m);
                log.Add("Wall material: brick pattern, 2 m x 0.5 m bricks");
            }
        }

        private static GameObject DressingPrefab(string name, System.Action<GameObject> build)
        {
            string path = $"{TrackDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var go = new GameObject(name);
            build(go);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Points a TrackBuilder at its materials, dressing prefabs, level and runner. Call with the scene open.</summary>
        private static void ConfigureTrack(TrackBuilder builder, Conductor conductor, Runner runner)
        {
            builder.transform.position = Vector3.zero;
            if (conductor != null) Set(builder, "conductor", conductor);
            if (runner != null) Set(builder, "runner", runner);
            Set(builder, "roadMaterial", Mats["Track"]);
            Set(builder, "lineMaterial", Mats["TrackLines"]);
            Set(builder, "tunnelPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{TrackDir}/TunnelShell.prefab"));
            Set(builder, "bridgePrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{TrackDir}/BridgeRails.prefab"));
            Set(builder, "pillarsPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{TrackDir}/Pillars.prefab"));
            Set(builder, "roadsidePrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{TrackDir}/RoadsidePost.prefab"));
            Set(builder, "groundMaterial", Mats["TrackDark"]);
            SetPrefabList(builder, "backdropPrefabs", BackdropNames);
            SetPrefabList(builder, "plantPrefabs", PlantNames);
            if (conductor == null)
            {
                // Greybox: straight-shaped pieces only (its obstacles are hand-placed in world positions).
                builder.pieces = new List<TrackBuilder.Piece>
                {
                    new TrackBuilder.Piece { beat = 12, piece = "tunnel" },   // 60-100 m
                    new TrackBuilder.Piece { beat = 21, piece = "bridge" },   // 105-145 m
                };
            }
            EditorUtility.SetDirty(builder);
        }

        private const string TrackTestLevelPath = "Assets/_Game/Levels/test_track.json";
        private const string LongClickPath = "Assets/_Game/Audio/Resources/click_100_long.wav";

        /// <summary>Placeholder click for the track test level (112 beats at 100 BPM; 22 kHz keeps the file near 3 MB).</summary>
        private static void EnsureLongClick(List<string> log)
        {
            if (System.IO.File.Exists(LongClickPath)) return;
            EnsureFolder("Assets/_Game", "Audio");
            EnsureFolder("Assets/_Game/Audio", "Resources");
            System.IO.File.WriteAllBytes(LongClickPath, ClickTrack.GenerateWav(100, 112, 4, 22050));
            AssetDatabase.ImportAsset(LongClickPath);
            log.Add($"Generated {LongClickPath} (100 BPM, 112 beats)");
        }

        /// <summary>Run scene: the track test level goes first in GameFlow's list, so pressing Play shows the kit.</summary>
        private static void PutTrackTestLevelFirst(List<string> log)
        {
            var flow = Object.FindAnyObjectByType<GameFlow>();
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(TrackTestLevelPath);
            if (flow == null || json == null) return;
            var so = new SerializedObject(flow);
            SerializedProperty levels = so.FindProperty("levels");
            var list = new List<Object> { json };
            for (int i = 0; i < levels.arraySize; i++)
            {
                Object o = levels.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && o != json) list.Add(o);
            }
            levels.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) levels.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add("Run: 'Test - track kit' is now the first level (GameFlow > Levels); the others follow it");
        }

        /// <summary>Existing scenes: remove the pieces saved by the old builder, wire the new one.</summary>
        private static void WireTrack(TrackBuilder builder, bool greybox, List<string> log, string label)
        {
            // In the Greybox scene the old track carried the gap obstacle inside its gap piece: keep a gap there.
            bool hadTrackGap = false;
            foreach (Obstacle o in builder.GetComponentsInChildren<Obstacle>(true))
                if (o.kind == ObstacleKind.Gap) hadTrackGap = true;

            int removed = builder.transform.childCount;
            for (int i = builder.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(builder.transform.GetChild(i).gameObject);

            ConfigureTrack(builder, Object.FindAnyObjectByType<Conductor>(), Object.FindAnyObjectByType<Runner>());

            if (greybox && hadTrackGap)
            {
                var gap = AssetDatabase.LoadAssetAtPath<GameObject>($"{ObstacleDir}/GapMarker.prefab");
                GameObject parent = GameObject.Find("Obstacles");
                if (gap != null && parent != null) Place(gap, parent.transform, 0, 101.25f);
            }
            log.Add($"{label}: track now built along the level's path at play time ({removed} saved pieces removed)");
        }
    }
}
