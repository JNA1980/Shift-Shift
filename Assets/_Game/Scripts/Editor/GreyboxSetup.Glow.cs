using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Playbook 2.2 step 2, run by Shift+Shift > Set Up Visuals: glowing top-edge strips on every obstacle (one emissive
    /// "glow" material per action colour), bloom in the post-processing profile, a global Volume and post-processing on
    /// the camera in each scene.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private const string ProfilePath = "Assets/_Game/Materials/PostProcessProfile.asset";
        private const float EdgeThickness = 0.07f; // metres

        // Obstacle material -> glow material for its edges (colour = what you press).
        private static readonly (string body, string glow, PaletteApplier.Role role)[] GlowFor =
        {
            ("Obstacle", "LaneGlow", PaletteApplier.Role.Lane), ("Low", "SlideGlow", PaletteApplier.Role.Slide),
            ("Gate", "SprintGlow", PaletteApplier.Role.Sprint), ("Hurdle", "JumpGlow", PaletteApplier.Role.Jump),
            ("Barrier", "PhaseGlow", PaletteApplier.Role.Phase), ("Ghost", "PhaseGlow", PaletteApplier.Role.Phase),
            ("Wall", "ChargeGlow", PaletteApplier.Role.Charge),
        };

        private static void CreateGlowMaterials()
        {
            foreach (var (_, glow, _) in GlowFor)
            {
                if (Mats.ContainsKey(glow)) continue;
                string path = $"{MatDir}/{glow}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    m = new Material(shader);
                    m.EnableKeyword("_EMISSION"); // on in the asset, so builds keep the emission shader variant
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    m.SetColor("_EmissionColor", Color.white);
                    AssetDatabase.CreateAsset(m, path);
                }
                Mats[glow] = m;
            }
        }

        private static void AddGlowEdges(List<string> log)
        {
            CreateGlowMaterials();
            var bodyToGlow = new Dictionary<Material, Material>();
            foreach (var (body, glow, _) in GlowFor) bodyToGlow[Mats[body]] = Mats[glow];

            foreach (string file in Directory.GetFiles(ObstacleDir, "*.prefab"))
            {
                string path = file.Replace('\\', '/');
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                int added = 0;
                foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.name == "Edge" || r.transform.Find("Edge") != null) continue;
                    Material body = r.sharedMaterial;
                    if (body == null) continue;
                    if (body == Mats["Hurdle"] && r.name.StartsWith("Rim"))
                    {
                        r.sharedMaterial = Mats["JumpGlow"]; // gap rims glow whole
                        added++;
                        continue;
                    }
                    if (!bodyToGlow.TryGetValue(body, out Material glow)) continue;

                    // Thin strip around the top face, slightly wider than the box so no faces coincide (no flicker).
                    Vector3 s = r.transform.localScale;
                    var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    edge.name = "Edge";
                    Object.DestroyImmediate(edge.GetComponent<Collider>());
                    edge.transform.SetParent(r.transform, false);
                    edge.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    edge.transform.localScale = new Vector3(1f + 0.04f / Mathf.Max(s.x, 0.01f),
                        EdgeThickness / Mathf.Max(s.y, 0.01f), 1f + 0.04f / Mathf.Max(s.z, 0.01f));
                    edge.GetComponent<Renderer>().sharedMaterial = glow;
                    added++;
                }
                if (added > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    log.Add($"{Path.GetFileNameWithoutExtension(path)}: {added} glowing edge(s)");
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnableBloomInProfile(List<string> log)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                log.Add($"No {ProfilePath}: run Shift+Shift > Apply Project Setup first");
                return;
            }
            if (profile.TryGet(out Bloom bloom) && bloom.intensity.value <= 0f)
            {
                // Only when still at the neutral 0 from project setup: never overwrite hand tuning.
                bloom.intensity.Override(0.8f);
                bloom.threshold.Override(0.9f);
                bloom.scatter.Override(0.7f);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                log.Add("Bloom on in PostProcessProfile (intensity 0.8, threshold 0.9, scatter 0.7)");
            }
        }

        /// <summary>Global Volume with the profile, post-processing on the main camera.</summary>
        private static void WirePostProcessing(Camera cam, List<string> log, string label)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            Volume volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null) volume = new GameObject("PostProcess").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
            if (cam != null)
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(data);
            }
            log.Add($"{label}: global Volume + camera post-processing on");
        }

        private static IEnumerable<(Material mat, PaletteApplier.Role role, bool glow)> GlowRoles()
        {
            var seen = new HashSet<string>();
            foreach (var (_, glow, role) in GlowFor)
                if (seen.Add(glow)) yield return (Mats[glow], role, true);
        }
    }
}
