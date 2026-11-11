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
