using UnityEngine;
using System.Collections;
using TMPro;

// Tipus de cub de l'onada 1 (només un dels dos).
public enum Wave1CubeType
{
    Sword,
    Gun
}

// Genera els cubs per onades:
//  Onada 1 (0-20 s): tutorial suau, 1 cub cada 3-4 s, només un tipus.
//  Onada 2 (20-60 s): pistola + espasa, una mica més ràpid, poden sortir 2 alhora.
//  Onada 3 (60 s - final): més velocitat i cubs especials (lent, ràpid, intercanvi d'armes).
public class CubeSpawnScripts : MonoBehaviour
{
    public Transform[] spawnPoints;

    public GameObject gunCubePrefab;
    public GameObject swordCubePrefab;

    [Header("Durada de les onades (segons)")]
    public float wave1Duration = 20f;
    public float wave2Duration = 40f;
    // L'onada 3 dura la resta de la partida.

    [Header("Onada 1: Introducció")]
    public Wave1CubeType wave1CubeType = Wave1CubeType.Sword;
    public float wave1MinDelay = 3f;
    public float wave1MaxDelay = 4f;
    public float wave1SpeedLevel = 1f;

    [Header("Onada 2: Combinació bàsica")]
    public float wave2MinDelay = 2f;
    public float wave2MaxDelay = 3f;
    public float wave2SpeedLevel = 1.3f;
    // Probabilitat que surtin dos cubs alhora (0-1).
    [Range(0f, 1f)] public float wave2DoubleChance = 0.3f;

    [Header("Onada 3: Increment de ritme i cubs nous")]
    public float wave3MinDelay = 1.2f;
    public float wave3MaxDelay = 2f;
    // La velocitat puja de start a end durant l'onada 3.
    public float wave3StartSpeedLevel = 1.8f;
    public float wave3EndSpeedLevel = 2.5f;
    // Segons de l'onada 3 fins arribar a la velocitat màxima.
    public float wave3RampTime = 30f;
    [Range(0f, 1f)] public float wave3DoubleChance = 0.4f;
    // Probabilitat que un cub sigui especial (0-1).
    [Range(0f, 1f)] public float specialCubeChance = 0.25f;

    [Header("Cubs especials")]
    // L'efecte dura des que apareix el cub fins que es destrueix.
    // 0.5 = la meitat de velocitat; 1.35 = un 35% més ràpid.
    public float slowMultiplier = 0.5f;
    public float fastMultiplier = 1.35f;

    [Header("UI (opcional)")]
    // Text on es mostra "WAVE 1", "WAVE 2"...
    public TMP_Text waveText;

    public bool debugLogs = true;

    private float startTime;
    private int currentWave = 0;

    public int CurrentWave => currentWave;

    void Start()
    {
        SpecialCubeEffects.ResetAll();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Spawner] Falten els spawn points.");
            return;
        }

        startTime = Time.time;
        StartCoroutine(SpawnCubes());
    }

    float Elapsed => Time.time - startTime;

    int WaveAt(float elapsed)
    {
        if (elapsed < wave1Duration)
            return 1;

        if (elapsed < wave1Duration + wave2Duration)
            return 2;

        return 3;
    }

    void UpdateWave()
    {
        int wave = WaveAt(Elapsed);

        if (wave == currentWave)
            return;

        currentWave = wave;

        if (waveText != null)
            waveText.text = "WAVE " + wave;

        if (debugLogs)
            Debug.Log("[Spawner] Comença l'onada " + wave + " (segon " + Elapsed.ToString("0") + ").");
    }

    IEnumerator SpawnCubes()
    {
        UpdateWave();

        while (true)
        {
            float minDelay, maxDelay;

            switch (currentWave)
            {
                case 1: minDelay = wave1MinDelay; maxDelay = wave1MaxDelay; break;
                case 2: minDelay = wave2MinDelay; maxDelay = wave2MaxDelay; break;
                default: minDelay = wave3MinDelay; maxDelay = wave3MaxDelay; break;
            }

            // L'espera també es veu afectada pels cubs lent/ràpid.
            float delay = Random.Range(minDelay, maxDelay);
            float waited = 0f;

            while (waited < delay)
            {
                waited += Time.deltaTime * GameSpeed.Multiplier;
                yield return null;
            }

            UpdateWave();
            SpawnForCurrentWave();
        }
    }

    void SpawnForCurrentWave()
    {
        switch (currentWave)
        {
            case 1:
                GameObject prefab = wave1CubeType == Wave1CubeType.Sword ? swordCubePrefab : gunCubePrefab;
                SpawnCube(prefab, RandomPoint(-1), wave1SpeedLevel);
                break;

            case 2:
                SpawnGroup(wave2SpeedLevel, wave2DoubleChance, false);
                break;

            default:
                float t = wave3RampTime > 0f
                    ? Mathf.Clamp01((Elapsed - wave1Duration - wave2Duration) / wave3RampTime)
                    : 1f;

                SpawnGroup(Mathf.Lerp(wave3StartSpeedLevel, wave3EndSpeedLevel, t), wave3DoubleChance, true);
                break;
        }
    }

    // Un cub, o dos alhora en spawn points diferents.
    void SpawnGroup(float speedLevel, float doubleChance, bool allowSpecial)
    {
        int first = RandomPointIndex(-1);
        SpawnRandomCube(first, speedLevel, allowSpecial);

        if (spawnPoints.Length > 1 && Random.value < doubleChance)
            SpawnRandomCube(RandomPointIndex(first), speedLevel, allowSpecial);
    }

    void SpawnRandomCube(int pointIndex, float speedLevel, bool allowSpecial)
    {
        GameObject prefab = Random.Range(0, 2) == 0 ? gunCubePrefab : swordCubePrefab;
        GameObject cube = SpawnCube(prefab, spawnPoints[pointIndex], speedLevel);

        if (cube != null && allowSpecial && Random.value < specialCubeChance)
            MakeSpecial(cube);
    }

    void MakeSpecial(GameObject cube)
    {
        SpecialCube special = cube.AddComponent<SpecialCube>();
        special.effect = (SpecialCubeEffect)Random.Range(0, 3);
        special.slowMultiplier = slowMultiplier;
        special.fastMultiplier = fastMultiplier;

        cube.name += "_" + special.effect;

        if (debugLogs)
            Debug.Log("[Spawner] Cub especial: " + special.effect);
    }

    // Índex aleatori de spawn point, diferent de "except" si es pot.
    int RandomPointIndex(int except)
    {
        int index = Random.Range(0, spawnPoints.Length);

        if (index == except && spawnPoints.Length > 1)
            index = (index + 1 + Random.Range(0, spawnPoints.Length - 1)) % spawnPoints.Length;

        return index;
    }

    Transform RandomPoint(int except)
    {
        return spawnPoints[RandomPointIndex(except)];
    }

    // Cridat pel ScoreManager quan s'acaba el temps.
    public void StopSpawning()
    {
        // Els efectes s'acaben sols quan el ScoreManager esborra els cubs que queden.
        StopAllCoroutines();
        enabled = false;
    }

    GameObject SpawnCube(GameObject prefab, Transform point, float speedLevel)
    {
        if (prefab == null || point == null)
            return null;

        GameObject cube = Instantiate(prefab, point.position, point.rotation);

        CubePhysics physics = cube.GetComponent<CubePhysics>();

        if (physics != null)
            physics.speedLevel = speedLevel;
        else
            Debug.LogWarning("[Spawner] " + prefab.name + " no té CubePhysics.", cube);

        if (debugLogs)
            Debug.Log("[Spawner] Onada " + currentWave + ": " + prefab.name + " amb nivell " + speedLevel.ToString("0.0"));

        return cube;
    }
}
