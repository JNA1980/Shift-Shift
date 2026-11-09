using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Playbook prompt 0.3 as a one-click, re-runnable setup: Shift+Shift > Apply Project Setup.
    /// Creates only what is missing (never overwrites scenes or the Volume profile) and logs every change.
    /// Not covered here (do in the editor): Windows x64 build profile, Web "Code Optimization" in the build profile.
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets/_Game";
        private static readonly string[] Folders = { "Scripts", "Prefabs", "Levels", "Palettes", "Audio", "Scenes", "Materials", "UI", "Settings" };
        private static readonly string[] SceneNames = { "Boot", "Menu", "Run" };
        private const string VolumeProfilePath = Root + "/Materials/PostProcessProfile.asset";

        [MenuItem("Shift+Shift/Apply Project Setup")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var log = new List<string>();
            string previousScene = EditorSceneManager.GetActiveScene().path;

            CreateFolders(log);
            CreateScenes(log);
            SetBuildList(log);
            CreateVolumeProfile(log);
            ApplyPlayerSettings(log);
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
                EditorSceneManager.OpenScene(previousScene);

            Debug.Log("[Setup] Done.\n- " + string.Join("\n- ", log));
        }

        private static void CreateFolders(List<string> log)
        {
            foreach (string f in Folders)
            {
                if (AssetDatabase.IsValidFolder($"{Root}/{f}")) continue;
                AssetDatabase.CreateFolder(Root, f);
                log.Add($"Created folder {Root}/{f}");
            }
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty"))
            {
                AssetDatabase.CreateFolder("Assets", "ThirdParty");
                log.Add("Created folder Assets/ThirdParty");
            }
        }

        private static void CreateScenes(List<string> log)
        {
            foreach (string name in SceneNames)
            {
                string path = $"{Root}/Scenes/{name}.unity";
                if (File.Exists(path)) { log.Add($"Scene exists, left alone: {path}"); continue; }

                // Boot is empty (GameFlow will live there); Menu and Run start with a camera and a light.
                var setup = name == "Boot" ? NewSceneSetup.EmptyScene : NewSceneSetup.DefaultGameObjects;
                var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
                log.Add($"Created scene {path}");
            }
        }

        private static void SetBuildList(List<string> log)
        {
            var wanted = SceneNames.Select(n => $"{Root}/Scenes/{n}.unity").ToList();
            var others = EditorBuildSettings.scenes.Where(s => !wanted.Contains(s.path)).ToList(); // e.g. InputTest, kept after
            var list = wanted.Select(p => new EditorBuildSettingsScene(p, true)).Concat(others).ToArray();
            EditorBuildSettings.scenes = list;
            log.Add("Build list: " + string.Join(", ", list.Select(s => Path.GetFileNameWithoutExtension(s.path) + (s.enabled ? "" : " (off)"))));
        }

        private static void CreateVolumeProfile(List<string> log)
        {
            if (File.Exists(VolumeProfilePath)) { log.Add($"Volume profile exists, left alone: {VolumeProfilePath}"); return; }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            // All effects present but neutral; palettes and juice tune them later (days 17 and 27).
            AddEffect<Bloom>(profile, b => b.intensity.Override(0f));
            AddEffect<ColorAdjustments>(profile, c =>
            {
                c.postExposure.Override(0f);
                c.contrast.Override(0f);
                c.saturation.Override(0f);
            });
            AddEffect<Vignette>(profile, v => v.intensity.Override(0f));
            AddEffect<ChromaticAberration>(profile, c => c.intensity.Override(0f));

            EditorUtility.SetDirty(profile);
            log.Add($"Created Volume profile {VolumeProfilePath} (bloom, colour adjustments, vignette, chromatic aberration at 0)");
        }

        private static void AddEffect<T>(VolumeProfile profile, Action<T> init) where T : VolumeComponent
        {
            T effect = profile.Add<T>();
            effect.name = typeof(T).Name;
            init(effect);
            AssetDatabase.AddObjectToAsset(effect, profile);
        }

        private static void ApplyPlayerSettings(List<string> log)
        {
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true; // itch.io serves .br without Content-Encoding
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            log.Add("Web player settings: 1280x720 canvas, Brotli + decompression fallback, data caching, " +
                    "explicitly thrown exceptions only, strip engine code, IL2CPP code generation optimised for size");
        }
    }
}
