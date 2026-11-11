using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShiftShift.EditorTools
{
    /// <summary>
    /// Day 17 (John): the runner becomes a blocky running figure with dust from the heels, built here from boxes
    /// (no model files). The old capsule ("Body") is kept in the scene, switched off.
    /// </summary>
    public static partial class GreyboxSetup
    {
        private static void WireFigure(Runner runner, List<string> log, string label)
        {
            if (runner == null || runner.transform.Find("Figure") != null) return;

            var figure = new GameObject("Figure");
            figure.transform.SetParent(runner.transform, false);

            // About 2 m tall, like the capsule. Limbs hang from pivots so they can swing.
            Transform legL = Limb(figure.transform, "LegLeft", new Vector3(-0.14f, 0.95f, 0f), new Vector3(0.2f, 0.95f, 0.22f));
            Transform legR = Limb(figure.transform, "LegRight", new Vector3(0.14f, 0.95f, 0f), new Vector3(0.2f, 0.95f, 0.22f));
            var upper = new GameObject("UpperBody");
            upper.transform.SetParent(figure.transform, false);
            upper.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            Box(upper, "Torso", new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.7f, 0.28f), "Runner");
            Box(upper, "Head", new Vector3(0f, 0.9f, 0f), new Vector3(0.32f, 0.32f, 0.32f), "Runner");
            Transform armL = Limb(upper.transform, "ArmLeft", new Vector3(-0.33f, 0.64f, 0f), new Vector3(0.14f, 0.64f, 0.16f));
            Transform armR = Limb(upper.transform, "ArmRight", new Vector3(0.33f, 0.64f, 0f), new Vector3(0.14f, 0.64f, 0.16f));

            // Dust: white squares that grow and fade behind the heels, left in the world as the runner moves on.
            var dustGo = new GameObject("Dust");
            dustGo.transform.SetParent(figure.transform, false);
            var dust = dustGo.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = dust.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 0.6f;
            main.startSize = 0.28f;
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emission = dust.emission;
            emission.enabled = false; // RunnerFigure emits a puff per footfall
            ParticleSystem.ShapeModule shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            ParticleSystem.SizeOverLifetimeModule size = dust.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            ParticleSystem.ColorOverLifetimeModule colour = dust.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colour.color = new ParticleSystem.MinMaxGradient(fade);
            var dustRenderer = dustGo.GetComponent<ParticleSystemRenderer>();
            dustRenderer.sharedMaterial = EnsureTrailMaterial(); // Sprites/Default: plain white, uses particle colour
            dustRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var anim = figure.AddComponent<RunnerFigure>();
            Set(anim, "runner", runner);
            Set(anim, "legLeft", legL);
            Set(anim, "legRight", legR);
            Set(anim, "armLeft", armL);
            Set(anim, "armRight", armR);
            Set(anim, "upperBody", upper.transform);
            Set(anim, "dust", dust);
            Set(anim, "normalMaterial", Mats["Runner"]);
            Set(anim, "phaseMaterial", Mats["Phase"]);
            var so = new SerializedObject(anim);
            SerializedProperty parts = so.FindProperty("parts");
            MeshRenderer[] renderers = figure.GetComponentsInChildren<MeshRenderer>(true);
            parts.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) parts.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // The Runner squashes "body" for a slide: point it at the figure, and let the figure do the phase look.
            Transform capsule = runner.transform.Find("Body");
            if (capsule != null) capsule.gameObject.SetActive(false);
            var rso = new SerializedObject(runner);
            rso.FindProperty("body").objectReferenceValue = figure.transform;
            rso.FindProperty("bodyRenderer").objectReferenceValue = null;
            rso.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"{label}: runner is now a running figure with heel dust (capsule kept, switched off)");
        }

        /// <summary>A pivot at <paramref name="at"/> with a box hanging down from it.</summary>
        private static Transform Limb(Transform parent, string name, Vector3 at, Vector3 size)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = at;
            Box(pivot, "Mesh", new Vector3(0f, -size.y * 0.5f, 0f), size, "Runner");
            return pivot.transform;
        }
    }
}
