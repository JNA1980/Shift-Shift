using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftShift
{
    /// <summary>
    /// Applies the level's palette (playbook 2.2): camera background, fog, trail and the colour of every renderer using
    /// a mapped material. It recolours runtime COPIES of the materials, so the material assets are never modified by
    /// playing. Runs in Start, after the spawner has created the obstacles (Awake).
    /// Step 4, phase look: while the runner is phased, the sky shifts to the phase colour, ghost-layer blocks (the ones
    /// that can hit you now) turn bright, everything else dims, and scanlines fade in.
    /// </summary>
    public sealed class PaletteApplier : MonoBehaviour
    {
        // Saved as numbers in scenes: add new roles at the end only.
        public enum Role { Road, RoadDetail, Lines, Lane, Slide, Sprint, Jump, Phase, Charge, Hole, Ghost, Scenery }

        [Serializable]
        public struct MaterialRole
        {
            public Material material;
            public Role role;
            [Tooltip("Glow material (edge strips): also sets its emission to the role colour x the palette's glow.")]
            public bool glow;
        }

        [Tooltip("Optional: takes the palette name from the playing level.")]
        [SerializeField] private Conductor conductor;
        [Tooltip("Optional: drives the phase look.")]
        [SerializeField] private Runner runner;
        [SerializeField] private List<Palette> palettes = new List<Palette>();
        [Tooltip("Used when there is no level (Greybox) or its palette is not in the list.")]
        [SerializeField] private Palette fallback;
        [SerializeField] private Camera cam;
        [Tooltip("Runner trail: coloured with the palette's accent, fading to transparent.")]
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private List<MaterialRole> roles = new List<MaterialRole>();
        [SerializeField] private float fogStart = 40f;
        [SerializeField] private float fogEnd = 170f;
        [Tooltip("Seconds to blend into and out of the phase look.")]
        [SerializeField, Min(0.01f)] private float phaseBlendSeconds = 0.1f;

        public Palette Current { get; private set; }

        private struct Entry
        {
            public Material Copy;
            public Role Role;
            public bool Glow;
            public float Alpha; // the material's own transparency (see-through barrier), kept when recolouring
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private float _phase;          // 0 normal .. 1 phased
        private float _appliedPhase = -1f;
        private Texture2D _scanlines;

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
            if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;
            if (trail != null)
            {
                trail.startColor = p.accent;
                trail.endColor = new Color(p.accent.r, p.accent.g, p.accent.b, 0f);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            var map = new Dictionary<Material, MaterialRole>();
            foreach (MaterialRole r in roles)
                if (r.material != null) map[r.material] = r;

            var copies = new Dictionary<Material, Material>();
            _entries.Clear();
            int recoloured = 0;
            foreach (Renderer rend in FindObjectsByType<Renderer>())
            {
                Material[] mats = rend.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || !map.TryGetValue(m, out MaterialRole role)) continue;
                    if (!copies.TryGetValue(m, out Material copy))
                    {
                        copy = new Material(m) { name = m.name + " (" + p.name + ")" };
                        copies[m] = copy;
                        float alpha = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").a : 1f;
                        _entries.Add(new Entry { Copy = copy, Role = role.role, Glow = role.glow, Alpha = alpha });
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
            Colourise(p, 0f);
            Debug.Log($"[Palette] Applied '{p.name}' to {recoloured} renderers.");
        }

        private void Update()
        {
            if (Current == null) return;
            float target = runner != null && runner.IsPhased ? 1f : 0f;
            _phase = Mathf.MoveTowards(_phase, target, Time.unscaledDeltaTime / phaseBlendSeconds);
            if (!Mathf.Approximately(_phase, _appliedPhase)) Colourise(Current, _phase);
        }

        /// <param name="phase">0 = normal palette, 1 = full phase look.</param>
        private void Colourise(Palette p, float phase)
        {
            _appliedPhase = phase;
            Color sky = Color.Lerp(p.background, p.phase * p.phaseSkyDarken, phase);
            sky.a = 1f;
            if (cam != null) cam.backgroundColor = sky;
            RenderSettings.fogColor = sky;

            foreach (Entry e in _entries)
            {
                Color normal = ColourFor(e.Role, p);
                Color phased = PhasedColour(e.Role, p, normal);
                Color c = Color.Lerp(normal, phased, phase);
                c.a = e.Alpha;
                e.Copy.color = c;
                if (e.Copy.HasProperty("_BaseColor")) e.Copy.SetColor("_BaseColor", c);
                // The _EMISSION keyword is already on in the glow material asset (so builds keep the variant).
                if (e.Glow && e.Copy.HasProperty("_EmissionColor")) e.Copy.SetColor("_EmissionColor", c * p.glowIntensity);
            }
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
                case Role.Ghost: return p.phase;
                case Role.Charge: return p.charge;
                // Trees and shrubs: a dark, muted tint of the level's accent, so they never read as an action colour.
                case Role.Scenery: return Color.Lerp(p.accent, p.roadDetail, 0.55f);
                default: return Color.black; // Hole
            }
        }

        private static Color PhasedColour(Role role, Palette p, Color normal)
        {
            switch (role)
            {
                case Role.Road:
                case Role.RoadDetail:
                case Role.Lines:
                case Role.Hole:
                case Role.Scenery:
                    return normal;
                case Role.Ghost:
                    return Color.Lerp(p.phase, Color.white, 0.75f); // ghost-layer blocks: the danger while phased
                default:
                    Color d = normal * p.phaseDim;
                    d.a = normal.a;
                    return d;
            }
        }

        private void OnGUI()
        {
            if (Current == null || _phase <= 0.001f || Current.scanlineStrength <= 0f) return;
            if (_scanlines == null)
            {
                // 1x4 texture: two dark rows, two clear rows, repeated down the screen.
                _scanlines = new Texture2D(1, 4) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                _scanlines.SetPixels(new[] { Color.black, Color.black, Color.clear, Color.clear });
                _scanlines.Apply();
            }
            GUI.depth = 10; // behind the other overlays
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _phase * Current.scanlineStrength);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, Screen.width, Screen.height), _scanlines,
                new Rect(0, 0, 1, Screen.height / 4f));
            GUI.color = old;
        }

        private void OnDestroy()
        {
            if (_scanlines != null) Destroy(_scanlines);
        }
    }
}
