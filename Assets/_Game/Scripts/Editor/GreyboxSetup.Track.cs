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

            // Ten more shapes (John, day 17: six still looked alike).
            DressingPrefab("BackdropL", go =>
            {
                Tall(go, "Wing", -2f, 0, 4f, 10f, 9f);
                Tall(go, "Foot", 3f, -3f, 6f, 4f, 6f);
            });
            DressingPrefab("BackdropPyramid", go =>
            {
                Tall(go, "Tier1", 0, 0, 10f, 10f, 3f);
                Tall(go, "Tier2", 0, 0, 7.5f, 7.5f, 6f);
                Tall(go, "Tier3", 0, 0, 5f, 5f, 9f);
                Tall(go, "Tier4", 0, 0, 2.5f, 2.5f, 12f);
            });
            DressingPrefab("BackdropNeedle", go =>
            {
                Tall(go, "Base", 0, 0, 6f, 6f, 3f);
                Tall(go, "Needle", 0, 0, 2f, 2f, 20f);
            });
            DressingPrefab("BackdropCluster", go =>
            {
                Tall(go, "A", -3f, 0, 4f, 4f, 4f);
                Tall(go, "B", 2f, 1f, 3f, 5f, 7f);
                Tall(go, "C", 1f, -4f, 5f, 3f, 2.5f);
            });
            DressingPrefab("BackdropFrame", go =>
            {
                Tall(go, "LegA", -5f, 0, 1.5f, 1.5f, 12f);
                Tall(go, "LegB", 5f, 0, 1.5f, 1.5f, 12f);
                Box(go, "Top", new Vector3(0, 12.6f, 0), new Vector3(11.5f, 1.2f, 1.5f), "TrackDark");
                Box(go, "TopCap", new Vector3(0, 13.3f, 0), new Vector3(11.7f, 0.2f, 1.7f), "TrackLines");
            });
            DressingPrefab("BackdropT", go =>
            {
                Tall(go, "Stem", 0, 0, 3f, 3f, 10f);
                Box(go, "Deck", new Vector3(0, 11f, 0), new Vector3(9f, 2f, 5f), "TrackDark");
                Box(go, "DeckCap", new Vector3(0, 12.1f, 0), new Vector3(9.2f, 0.2f, 5.2f), "TrackLines");
            });
            DressingPrefab("BackdropWall", go =>
            {
                Tall(go, "Wall", 0, 0, 16f, 2f, 7f);
                Tall(go, "Keep", -5f, 0, 3f, 3f, 10f);
            });
            DressingPrefab("BackdropStairs", go =>
            {
                Tall(go, "Low", -3.5f, 0, 3.5f, 4f, 5f);
                Tall(go, "Mid", 0, 0, 3.5f, 4f, 9f);
                Tall(go, "High", 3.5f, 0, 3.5f, 4f, 13f);
            });
            DressingPrefab("BackdropCrown", go =>
            {
                Tall(go, "Body", 0, 0, 6f, 6f, 8f);
                Tall(go, "Spire1", -2.5f, -2.5f, 1f, 1f, 11f);
                Tall(go, "Spire2", 2.5f, -2.5f, 1f, 1f, 11f);
                Tall(go, "Spire3", -2.5f, 2.5f, 1f, 1f, 11f);
                Tall(go, "Spire4", 2.5f, 2.5f, 1f, 1f, 11f);
            });
            DressingPrefab("BackdropLinked", go =>
            {
                Tall(go, "TowerA", -4f, 0, 3f, 3f, 14f);
                Tall(go, "TowerB", 4f, 0, 3f, 3f, 14f);
                Box(go, "Walkway", new Vector3(0, 10f, 0), new Vector3(5f, 1f, 2f), "TrackDark");
            });

            // Conifers (John, day 17): tiers that narrow to a point, every other tier turned 45 degrees.
            DressingPrefab("ConiferTall", go => Conifer(go, 2f, new[] { 3.0f, 2.5f, 2.0f, 1.5f, 1.0f, 0.5f }, 1.0f));
            DressingPrefab("ConiferSmall", go => Conifer(go, 1f, new[] { 1.8f, 1.3f, 0.8f, 0.4f }, 0.7f));
            DressingPrefab("ConiferWide", go => Conifer(go, 1.2f, new[] { 3.6f, 2.9f, 2.1f, 1.3f, 0.6f }, 0.8f));
            DressingPrefab("ConiferSlim", go => Conifer(go, 1.6f, new[] { 1.6f, 1.4f, 1.2f, 1.0f, 0.8f, 0.6f, 0.3f }, 0.9f));

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
        {
            "BackdropTower", "BackdropSlab", "BackdropStepped", "BackdropTwin", "BackdropSpire", "BackdropArch",
            "BackdropL", "BackdropPyramid", "BackdropNeedle", "BackdropCluster", "BackdropFrame", "BackdropT",
            "BackdropWall", "BackdropStairs", "BackdropCrown", "BackdropLinked",
        };
        // Mostly conifers; the builder picks one of these per spot.
        private static readonly string[] PlantNames =
        {
            "ConiferTall", "ConiferSmall", "ConiferWide", "ConiferSlim", "ConiferTall", "ConiferSmall",
            "TreeTiered", "TreeRound", "Shrub", "Shrub",
        };

        /// <summary>A trunk and a stack of foliage tiers, widest at the bottom.</summary>
        private static void Conifer(GameObject go, float trunk, float[] widths, float tierHeight)
        {
            Box(go, "Trunk", new Vector3(0, trunk * 0.5f - 0.1f, 0), new Vector3(0.3f, trunk + 0.2f, 0.3f), "TrackDark");
            for (int i = 0; i < widths.Length; i++)
            {
                string name = "Tier" + i;
                // Tiers overlap a little so no gaps show between them.
                Box(go, name, new Vector3(0, trunk + (i + 0.5f) * tierHeight * 0.85f, 0),
                    new Vector3(widths[i], tierHeight, widths[i]), "Foliage");
                if (i % 2 == 1) go.transform.Find(name).localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
        }

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

        private const float BarrierAlpha = 0.65f; // John, day 17: 0.3, then 0.5, were too see-through

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
            // 256 x 128 with smooth filtering (John, day 17: the first 64 x 32 version shimmered as the wall came closer).
            const int w = 256, h = 128, mortar = 8;
            var current = AssetDatabase.LoadAssetAtPath<Texture2D>(BrickPath);
            if (current == null || current.width != w)
            {
                // One tile = one brick wide, two rows high, the second row shifted half a brick.
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
                log.Add($"Generated {BrickPath} ({w} x {h})");
            }
            if (AssetImporter.GetAtPath(BrickPath) is TextureImporter importer &&
                (importer.filterMode != FilterMode.Trilinear || importer.anisoLevel != 8 || !importer.mipmapEnabled))
            {
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
                log.Add("Brick texture: smooth filtering (mipmaps, trilinear, anisotropic 8)");
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
            Set(builder, "wallMaterial", Mats["Track"]); // compound walls: road grey, a shade lighter than the ground
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

        // The Run scene plays the first level in GameFlow's list (until the level select, days 21-22).
        private static readonly string[] RunLevelOrder =
        {
            // The level being worked on goes first for now (level 3).
            "Assets/_Game/Levels/level03.json", "Assets/_Game/Levels/level02.json", "Assets/_Game/Levels/level01.json",
            "Assets/_Game/Levels/test_track.json",
            "Assets/_Game/Levels/test_gestures.json", "Assets/_Game/Levels/level00.json",
        };
        private const string LongClickPath = "Assets/_Game/Audio/Resources/click_100_long.wav";

        /// <summary>Placeholder click for the track test level (112 beats at 100 BPM; 22 kHz keeps the file near 3 MB).</summary>
        private static void EnsureLongClick(List<string> log)
        {
            EnsureFolder("Assets/_Game", "Audio");
            EnsureFolder("Assets/_Game/Audio", "Resources");
            // Placeholder clicks until the audio pass (day 23): one per level tempo, a little longer than the level.
            foreach ((string path, double bpm, int beats) in new[]
                     {
                         (LongClickPath, 100.0, 112), ("Assets/_Game/Audio/Resources/click_110.wav", 110.0, 120),
                         ("Assets/_Game/Audio/Resources/click_120.wav", 120.0, 160),
                     })
            {
                if (System.IO.File.Exists(path)) continue;
                System.IO.File.WriteAllBytes(path, ClickTrack.GenerateWav(bpm, beats, 4, 22050));
                AssetDatabase.ImportAsset(path);
                log.Add($"Generated {path} ({bpm:0} BPM, {beats} beats)");
            }
        }

        /// <summary>Run scene: GameFlow's levels in RunLevelOrder (level 1 first), then any others already listed.</summary>
        private static void PutTrackTestLevelFirst(List<string> log)
        {
            var flow = Object.FindAnyObjectByType<GameFlow>();
            if (flow == null) return;
            var so = new SerializedObject(flow);
            SerializedProperty levels = so.FindProperty("levels");
            var list = new List<Object>();
            foreach (string path in RunLevelOrder)
            {
                var json = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (json != null) list.Add(json);
            }
            for (int i = 0; i < levels.arraySize; i++)
            {
                Object o = levels.GetArrayElementAtIndex(i).objectReferenceValue;
                if (o != null && !list.Contains(o)) list.Add(o);
            }
            if (list.Count == 0) return;
            levels.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) levels.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"Run: plays '{list[0].name}' (first of {list.Count} in GameFlow > Levels)");
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
