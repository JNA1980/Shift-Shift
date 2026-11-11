using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Playbook 2.2 step 3, run by Shift+Shift > Set Up Visuals: CameraFeel asset on the chase camera, and a runner
    /// trail (TrailRenderer, coloured by the palette's accent) in each scene.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private const string CameraFeelPath = "Assets/_Game/Settings/CameraFeel.asset";

        private static CameraFeel EnsureCameraFeel(List<string> log)
        {
            var feel = AssetDatabase.LoadAssetAtPath<CameraFeel>(CameraFeelPath);
            if (feel != null) return feel;
            feel = ScriptableObject.CreateInstance<CameraFeel>();
            AssetDatabase.CreateAsset(feel, CameraFeelPath);
            AssetDatabase.SaveAssets();
            log.Add($"Created {CameraFeelPath} (FOV 60/72/78, roll 2 deg, camera 3.3 m)");
            return AssetDatabase.LoadAssetAtPath<CameraFeel>(CameraFeelPath);
        }

        private static Material EnsureTrailMaterial()
        {
            string path = $"{MatDir}/Trail.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            // Sprites/Default: unlit, transparent, uses the trail's vertex colours (accent fading out); works in URP.
            m = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Call with the scene open (assets loaded after opening).</summary>
        private static void WireFeelAndTrail(PaletteApplier applier, List<string> log, string label)
        {
            CameraFeel feel = AssetDatabase.LoadAssetAtPath<CameraFeel>(CameraFeelPath);
            var chase = Object.FindAnyObjectByType<ChaseCamera>();
            if (chase != null && feel != null) Set(chase, "feel", feel);

            TrailRenderer trail = null;
            var runner = Object.FindAnyObjectByType<Runner>();
            if (runner != null)
            {
                Transform body = runner.transform.Find("Body");
                Transform existing = runner.transform.Find("Trail");
                if (existing == null)
                {
                    var go = new GameObject("Trail");
                    go.transform.SetParent(runner.transform, false);
                    go.transform.localPosition = new Vector3(0f, 0.6f, -0.35f); // low behind the capsule
                    trail = go.AddComponent<TrailRenderer>();
                    trail.time = 0.25f;
                    trail.minVertexDistance = 0.1f;
                    trail.widthMultiplier = 0.45f;
                    trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                    trail.sharedMaterial = EnsureTrailMaterial();
                    trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    trail.receiveShadows = false;
                }
                else trail = existing.GetComponent<TrailRenderer>();
            }
            if (applier != null && trail != null) Set(applier, "trail", trail);
            // John, day 17: with the running figure and its dust the yellow trail is not needed. Kept, switched off.
            if (trail != null && trail.gameObject.activeSelf)
            {
                trail.gameObject.SetActive(false);
                log.Add($"{label}: runner trail switched off (tick 'Trail' under Runner to bring it back)");
            }
            if (applier != null && runner != null) Set(applier, "runner", runner); // step 4: phase look
            WireFigure(runner, log, label); // day 17: running figure instead of the capsule
            log.Add($"{label}: CameraFeel on the chase camera, runner trail ({(trail != null ? "ok" : "no runner")})");
        }
    }
}
