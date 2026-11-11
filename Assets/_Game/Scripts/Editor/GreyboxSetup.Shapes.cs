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
        private const string LayoutVersion = "LayoutV2";

        private static void RebuildOpeningLayouts(List<string> log)
        {
            Rebuild($"{ObstacleDir}/WideBar.prefab", "Obstacle", 2f, false, log);
            Rebuild($"{ObstacleDir}/GateOpening.prefab", "Gate", 2.8f, true, log); // walls 2.8, posts 3.0, beam top 3.15
        }

        private static void Rebuild(string path, string mat, float height, bool frame, List<string> log)
        {
            if (!File.Exists(path)) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root.transform.Find(LayoutVersion) != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                return; // already rebuilt to this version
            }
            // Remove the day-13 per-lane panels and any earlier layouts (with their glow edges).
            var old = new List<GameObject>();
            foreach (Transform child in root.transform)
                if (child.name.StartsWith("Panel") || child.name == "Beam" || child.name.StartsWith("Open") ||
                    child.name.StartsWith("LayoutV")) old.Add(child.gameObject);
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
                    // Doorway frame. Posts stand 0.2 m above the walls and the beam 0.15 m above the posts, each a
                    // little deeper than the last, so no two faces (or glow strips) share a plane: v1 had them level
                    // and the gate flickered (John, day 17). Beam top at 3.15 m (<= 3.2 m).
                    Box(layout, "PostL", new Vector3(a, (height + 0.2f) * 0.5f, 0f), new Vector3(0.25f, height + 0.2f, 0.5f), mat);
                    Box(layout, "PostR", new Vector3(b, (height + 0.2f) * 0.5f, 0f), new Vector3(0.25f, height + 0.2f, 0.5f), mat);
                    Box(layout, "Beam", new Vector3((a + b) * 0.5f, height + 0.25f, 0f), new Vector3(b - a + 0.35f, 0.2f, 0.6f), mat);
                }
            }
            new GameObject(LayoutVersion).transform.SetParent(root.transform, false); // marker: which layout this is
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
