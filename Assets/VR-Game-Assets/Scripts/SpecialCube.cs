using UnityEngine;
using System.Collections.Generic;

public enum SpecialCubeEffect
{
    SlowDown,
    SpeedUp,
    SwapHands
}

public static class GameSpeed
{
    public static float Multiplier = 1f;

    public static void Reset()
    {
        Multiplier = 1f;
    }
}

public class SpecialCube : MonoBehaviour
{
    public SpecialCubeEffect effect = SpecialCubeEffect.SlowDown;

    public float slowMultiplier = 0.5f;
    public float fastMultiplier = 1.35f;

    public float outlineScale = 1.1f;
    public float blinkSpeed = 3f;
    [Range(0f, 1f)] public float glowWhiteness = 0.6f;
    public float pulseThickness = 0.05f;

    private bool active = false;
    private Material outlineMaterial;
    private Color baseColor;
    private readonly List<Transform> outlines = new List<Transform>();
    private readonly List<Vector3> outlineCenters = new List<Vector3>();

    private static Shader outlineShader;

    public static Color ColorFor(SpecialCubeEffect effect)
    {
        switch (effect)
        {
            case SpecialCubeEffect.SlowDown: return new Color(0.2f, 0.6f, 1f);
            case SpecialCubeEffect.SpeedUp: return new Color(1f, 0.2f, 0.15f);
            default: return new Color(1f, 0.85f, 0.1f);
        }
    }

    private void Start()
    {
        CreateOutline(ColorFor(effect));

        active = true;
        SpecialCubeEffects.Add(this);
    }

    private void CreateOutline(Color color)
    {
        if (outlineShader == null)
            outlineShader = Resources.Load<Shader>("SpecialCubeOutline");

        baseColor = color;
        outlineMaterial = new Material(outlineShader);
        outlineMaterial.color = color;

        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(false))
        {
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(filter.transform, false);

            Vector3 center = filter.sharedMesh.bounds.center;
            outline.transform.localPosition = center * (1f - outlineScale);
            outline.transform.localScale = Vector3.one * outlineScale;
            outlines.Add(outline.transform);
            outlineCenters.Add(center);

            outline.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

            MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
        }
    }

    private void Update()
    {
        float glow = (Mathf.Sin(Time.time * blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

        Color bright = Color.Lerp(baseColor, Color.white, glowWhiteness);
        outlineMaterial.color = Color.Lerp(baseColor, bright, glow);

        float scale = outlineScale + pulseThickness * glow;

        for (int i = 0; i < outlines.Count; i++)
        {
            outlines[i].localScale = Vector3.one * scale;
            outlines[i].localPosition = outlineCenters[i] * (1f - scale);
        }
    }

    public void End()
    {
        if (!active)
            return;

        active = false;
        SpecialCubeEffects.Remove(this);
    }

    private void OnDestroy()
    {
        End();
    }
}

public static class SpecialCubeEffects
{
    private static readonly List<SpecialCube> speedCubes = new List<SpecialCube>();
    private static int swapCount = 0;
    private static bool handsSwapped = false;

    public static void Add(SpecialCube cube)
    {
        switch (cube.effect)
        {
            case SpecialCubeEffect.SlowDown:
            case SpecialCubeEffect.SpeedUp:
                speedCubes.Add(cube);
                UpdateSpeed();
                break;

            case SpecialCubeEffect.SwapHands:
                swapCount++;
                UpdateSwap();
                break;
        }
    }

    public static void Remove(SpecialCube cube)
    {
        switch (cube.effect)
        {
            case SpecialCubeEffect.SlowDown:
            case SpecialCubeEffect.SpeedUp:
                speedCubes.Remove(cube);
                UpdateSpeed();
                break;

            case SpecialCubeEffect.SwapHands:
                swapCount = Mathf.Max(0, swapCount - 1);
                UpdateSwap();
                break;
        }
    }

    private static void UpdateSpeed()
    {
        if (speedCubes.Count == 0)
        {
            GameSpeed.Reset();
            return;
        }

        SpecialCube newest = speedCubes[speedCubes.Count - 1];

        GameSpeed.Multiplier = newest.effect == SpecialCubeEffect.SlowDown
            ? newest.slowMultiplier
            : newest.fastMultiplier;
    }

    private static void UpdateSwap()
    {
        bool shouldSwap = swapCount > 0;

        if (shouldSwap == handsSwapped)
            return;

        WeaponHandManager manager = Object.FindFirstObjectByType<WeaponHandManager>();

        if (manager == null)
            return;

        handsSwapped = shouldSwap;
        manager.SwapHands();
    }

    public static void ResetAll()
    {
        speedCubes.Clear();
        swapCount = 0;
        handsSwapped = false;
        GameSpeed.Reset();
    }
}
