using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Playbook 2.2 step 5, run by Shift+Shift > Set Up Visuals: the wide bar and the one-lane gate get one ready-made
    /// layout per open lane (Open0/Open1/Open2). Blocked lanes next to each other become ONE solid slab; the gate gets a
    /// framed doorway (posts + beam) around its opening. The spawner shows the matching layout.
    /// Lanes (chart 0/1/2) span x [-3,-1], [-1,1], [1,3]; the road edges are at +-3.5.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private const float RoadHalf = 3.5f;

        private static void RebuildOpeningLayouts(List<string> log)
        {
            Rebuild($"{ObstacleDir}/WideBar.prefab", "Obstacle", 2f, false, log);
            Rebuild($"{ObstacleDir}/GateOpening.prefab", "Gate", 3f, true, log);
        }

        private static void Rebuild(string path, string mat, float height, bool frame, List<string> log)
        {
            if (!File.Exists(path)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root.transform.Find("Open0") != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return; // already rebuilt
            }
            // Remove the day-13 per-lane panels (and their glow edges).
            var old = new List<GameObject>();
            foreach (Transform child in root.transform)
                if (child.name.StartsWith("Panel") || child.name == "Beam") old.Add(child.gameObject);
            foreach (GameObject g in old) Object.DestroyImmediate(g);

            for (int open = 0; open < 3; open++)
            {
                var layout = new GameObject("Open" + open);
                layout.transform.SetParent(root.transform, false);
                float a = -3f + open * 2f, b = a + 2f;   // the open lane's x range
                // Blocked parts: road edge -> opening, and opening -> road edge (each one solid piece).
                Piece(layout, "Left", -RoadHalf, a, height, mat);
                Piece(layout, "Right", b, RoadHalf, height, mat);
                if (frame)
                {
                    // Doorway frame: posts at the opening's sides, beam across the top (top at 3.1 m <= 3.2 m).
                    Box(layout, "PostL", new Vector3(a, height * 0.5f, 0f), new Vector3(0.25f, height, 0.45f), mat);
                    Box(layout, "PostR", new Vector3(b, height * 0.5f, 0f), new Vector3(0.25f, height, 0.45f), mat);
                    Box(layout, "Beam", new Vector3((a + b) * 0.5f, height, 0f), new Vector3(b - a + 0.25f, 0.2f, 0.45f), mat);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            log.Add($"{Path.GetFileNameWithoutExtension(path)}: one layout per open lane ({(frame ? "wall + framed doorway" : "solid slab")})");
        }

        private static void Piece(GameObject parent, string name, float x0, float x1, float height, string mat)
        {
            if (x1 - x0 < 0.01f) return;
            Box(parent, name, new Vector3((x0 + x1) * 0.5f, height * 0.5f, 0f), new Vector3(x1 - x0, height, 0.4f), mat);
        }
    }
}
