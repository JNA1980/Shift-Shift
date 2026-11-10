using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Applies the level's palette at run start (playbook 2.2): camera background, fog, and the colour of every
    /// renderer using a mapped material. It recolours runtime COPIES of the materials, so the material assets are never
    /// modified by playing. Runs in Start, after the spawner has created the obstacles (Awake).
    /// </summary>
    public sealed class PaletteApplier : MonoBehaviour
    {
        public enum Role { Road, RoadDetail, Lines, Lane, Slide, Sprint, Jump, Phase, Charge, Hole }

        [Serializable]
        public struct MaterialRole
        {
            public Material material;
            public Role role;
        }

        [Tooltip("Optional: takes the palette name from the playing level.")]
        [SerializeField] private Conductor conductor;
        [SerializeField] private List<Palette> palettes = new List<Palette>();
        [Tooltip("Used when there is no level (Greybox) or its palette is not in the list.")]
        [SerializeField] private Palette fallback;
        [SerializeField] private Camera cam;
        [SerializeField] private List<MaterialRole> roles = new List<MaterialRole>();
        [SerializeField] private float fogStart = 40f;
        [SerializeField] private float fogEnd = 170f;

        public Palette Current { get; private set; }

        private void Start()
        {
            Current = Pick();
            if (Current == null)
            {
                Debug.LogWarning("[Palette] No palette to apply.", this);
                return;
            }
            Apply(Current);
        }

        private Palette Pick()
        {
            string wanted = conductor != null && conductor.Level != null ? conductor.Level.palette : null;
            if (!string.IsNullOrEmpty(wanted))
            {
                string key = Normalize(wanted);
                foreach (Palette p in palettes)
                    if (p != null && Normalize(p.name) == key) return p;
                Debug.LogWarning($"[Palette] Level palette '{wanted}' not found; using {(fallback != null ? fallback.name : "none")}.", this);
            }
            return fallback;
        }

        private static string Normalize(string s) => s.Replace("-", "").Replace("_", "").Replace(" ", "").ToLowerInvariant();

        private void Apply(Palette p)
        {
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = p.background;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.background;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            var map = new Dictionary<Material, Role>();
            foreach (MaterialRole r in roles)
                if (r.material != null) map[r.material] = r.role;

            var copies = new Dictionary<Material, Material>();
            int recoloured = 0;
            foreach (Renderer rend in FindObjectsByType<Renderer>())
            {
                Material[] mats = rend.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || !map.TryGetValue(m, out Role role)) continue;
                    if (!copies.TryGetValue(m, out Material copy))
                    {
                        copy = new Material(m) { name = m.name + " (" + p.name + ")" };
                        Color c = ColourFor(role, p);
                        copy.color = c;
                        if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", c);
                        copies[m] = copy;
                    }
                    mats[i] = copy;
                    changed = true;
                }
                if (changed)
                {
                    rend.sharedMaterials = mats;
                    recoloured++;
                }
            }
            Debug.Log($"[Palette] Applied '{p.name}' to {recoloured} renderers.");
        }

        private static Color ColourFor(Role role, Palette p)
        {
            switch (role)
            {
                case Role.Road: return p.road;
                case Role.RoadDetail: return p.roadDetail;
                case Role.Lines: return p.lines;
                case Role.Lane: return p.lane;
                case Role.Slide: return p.slide;
                case Role.Sprint: return p.sprint;
                case Role.Jump: return p.jump;
                case Role.Phase: return p.phase;
                case Role.Charge: return p.charge;
                default: return Color.black; // Hole
            }
        }
    }
}
