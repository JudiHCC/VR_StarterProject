using UnityEngine;
using System.Collections.Generic;

// Tipus de cub especial de l'onada 3.
public enum SpecialCubeEffect
{
    SlowDown,   // El joc va més lent mentre el cub existeix.
    SpeedUp,    // El joc va més ràpid mentre el cub existeix.
    SwapHands   // L'espasa i la pistola s'intercanvien de mà mentre el cub existeix.
}

// Velocitat global del joc (cubs i ritme d'aparició).
// No fem servir Time.timeScale perquè en VR alentiria també
// la física de les mans, les vibracions i el temporitzador.
public static class GameSpeed
{
    public static float Multiplier = 1f;

    public static void Reset()
    {
        Multiplier = 1f;
    }
}

// Va a un cub (GunCube o SwordCube) per convertir-lo en especial.
// El CubeSpawnScripts l'afegeix en temps d'execució a l'onada 3.
// L'efecte comença quan apareix el cub i s'acaba quan es destrueix
// (tant si el trenca el jugador com si arriba al CubeKiller).
public class SpecialCube : MonoBehaviour
{
    public SpecialCubeEffect effect = SpecialCubeEffect.SlowDown;

    // Multiplicadors de velocitat dels efectes.
    public float slowMultiplier = 0.5f;
    public float fastMultiplier = 1.35f;

    [Header("Outline")]
    // Com més gran, més gruixut l'outline (1.1 = un 10% més gran que el cub).
    public float outlineScale = 1.1f;

    private bool active = false;

    private static Shader outlineShader;

    // Color de l'outline de cada efecte.
    public static Color ColorFor(SpecialCubeEffect effect)
    {
        switch (effect)
        {
            case SpecialCubeEffect.SlowDown: return new Color(0.2f, 0.6f, 1f);   // Blau
            case SpecialCubeEffect.SpeedUp: return new Color(1f, 0.2f, 0.15f);   // Vermell
            default: return new Color(1f, 0.85f, 0.1f);                          // Groc
        }
    }

    private void Start()
    {
        CreateOutline(ColorFor(effect));

        active = true;
        SpecialCubeEffects.Add(this);
    }

    // Còpia del cub una mica més gran, només amb les cares de darrere,
    // perquè es vegi una vora de color al voltant.
    private void CreateOutline(Color color)
    {
        if (outlineShader == null)
            outlineShader = Resources.Load<Shader>("SpecialCubeOutline");

        if (outlineShader == null)
        {
            Debug.LogWarning("[Special] No trobo el shader SpecialCubeOutline a Resources.");
            return;
        }

        Material outlineMaterial = new Material(outlineShader);
        outlineMaterial.color = color;

        // Només les malles visibles ara (al GunCube, el cub sencer i no les peces).
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(false))
        {
            MeshRenderer rend = filter.GetComponent<MeshRenderer>();

            if (filter.sharedMesh == null || rend == null || !rend.enabled)
                continue;

            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(filter.transform, false);

            // Escalem des del centre de la malla perquè la vora quedi igual a tots els costats.
            Vector3 center = filter.sharedMesh.bounds.center;
            outline.transform.localPosition = center * (1f - outlineScale);
            outline.transform.localScale = Vector3.one * outlineScale;

            outline.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

            MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
        }
    }

    // Cridat quan el jugador trenca el cub (SwordCubeScript i CubeExplode).
    // El GunCube tarda uns segons a desaparèixer per l'animació,
    // però l'efecte s'acaba en el moment de trencar-lo.
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

// Porta el compte dels cubs especials que hi ha ara a l'escena.
public static class SpecialCubeEffects
{
    // Cubs de velocitat vius, en ordre d'aparició: mana el més nou.
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
                Debug.Log("[Special] Apareix cub " + (cube.effect == SpecialCubeEffect.SlowDown ? "lent" : "ràpid") + ".");
                break;

            case SpecialCubeEffect.SwapHands:
                swapCount++;
                UpdateSwap();
                Debug.Log("[Special] Apareix cub d'intercanvi: armes canviades.");
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
        speedCubes.RemoveAll(c => c == null);

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

    // Les armes queden canviades mentre hi hagi algun cub d'intercanvi viu.
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

        Debug.Log("[Special] Armes " + (shouldSwap ? "intercanviades." : "tornen a la seva mà."));
    }

    // En començar o acabar una partida tot torna a la normalitat.
    public static void ResetAll()
    {
        speedCubes.Clear();
        swapCount = 0;
        handsSwapped = false;
        GameSpeed.Reset();
    }
}
