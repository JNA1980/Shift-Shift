using ShiftShift.Core;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// One level's colours (playbook 2.2; chosen with John, day 16, Docs/PALETTES.md): sky, road, lines, accent and
    /// "colour = what you press" action colours. Picked by the level JSON's "palette" field (asset name).
    /// </summary>
    [CreateAssetMenu(menuName = "Shift+Shift/Palette", fileName = "Palette")]
    public sealed class Palette : ScriptableObject
    {
        [Header("World")]
        public Color background = Color.black;   // sky, camera clear colour, fog
        public Color road = new Color(0.2f, 0.2f, 0.2f);
        public Color roadDetail = new Color(0.16f, 0.16f, 0.16f); // tunnel walls, bridge rails
        public Color lines = new Color(0.75f, 0.75f, 0.75f);      // road edges and lane lines
        public Color accent = Color.white;        // runner trail, UI highlights

        [Header("Colour = what you press")]
        public Color lane = new Color(0.949f, 0.439f, 0.090f);    // bar, wide bar
        public Color slide = new Color(0.988f, 0.769f, 0.118f);   // low bar
        public Color sprint = new Color(0.0f, 0.988f, 0.976f);    // gate
        public Color jump = new Color(0.957f, 0.055f, 0.616f);    // hurdle, gap rim
        public Color phase = new Color(0.055f, 0.475f, 0.945f);   // barrier, ghost-layer blocks
        public Color charge = new Color(0.949f, 0.118f, 0.239f);  // wall

        public static double Contrast(Color a, Color b) => ColorContrast.Ratio(a.r, a.g, a.b, b.r, b.g, b.b);

        private void OnValidate()
        {
            // Agreed floor: every action colour at least 3:1 against the road it sits on.
            foreach (var (n, c) in new[] { ("lane", lane), ("slide", slide), ("sprint", sprint), ("jump", jump), ("phase", phase), ("charge", charge) })
            {
                double r = Contrast(c, road);
                if (r < 3.0) Debug.LogWarning($"[Palette] {name}: {n} vs road {r:0.0}:1 (below 3:1).", this);
            }
        }
    }
}
