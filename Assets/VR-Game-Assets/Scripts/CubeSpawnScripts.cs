using UnityEngine;
using System.Collections;

public enum Wave1CubeType
{
    Sword,
    Gun
}

public class CubeSpawnScripts : MonoBehaviour
{
    public Transform[] spawnPoints;

    public GameObject gunCubePrefab;
    public GameObject swordCubePrefab;

    public float wave1Duration = 20f;
    public float wave2Duration = 40f;

    public Wave1CubeType wave1CubeType = Wave1CubeType.Sword;
    public float wave1MinDelay = 3f;
    public float wave1MaxDelay = 4f;
    public float wave1SpeedLevel = 1f;

    public float wave2MinDelay = 2f;
    public float wave2MaxDelay = 3f;
    public float wave2SpeedLevel = 1.3f;
    [Range(0f, 1f)] public float wave2DoubleChance = 0.3f;

    public float wave3MinDelay = 1.2f;
    public float wave3MaxDelay = 2f;
    public float wave3StartSpeedLevel = 1.8f;
    public float wave3EndSpeedLevel = 2.5f;
    public float wave3RampTime = 30f;
    [Range(0f, 1f)] public float wave3DoubleChance = 0.4f;
    [Range(0f, 1f)] public float specialCubeChance = 0.25f;

    public float slowMultiplier = 0.5f;
    public float fastMultiplier = 1.35f;

    private float startTime;
    private int currentWave = 0;

    void Start()
    {
        SpecialCubeEffects.ResetAll();

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

    IEnumerator SpawnCubes()
    {
        currentWave = WaveAt(Elapsed);

        while (true)
        {
            float minDelay, maxDelay;

            switch (currentWave)
            {
                case 1: minDelay = wave1MinDelay; maxDelay = wave1MaxDelay; break;
                case 2: minDelay = wave2MinDelay; maxDelay = wave2MaxDelay; break;
                default: minDelay = wave3MinDelay; maxDelay = wave3MaxDelay; break;
            }

            float delay = Random.Range(minDelay, maxDelay);
            float waited = 0f;

            while (waited < delay)
            {
                waited += Time.deltaTime * GameSpeed.Multiplier;
                yield return null;
            }

            currentWave = WaveAt(Elapsed);
            SpawnForCurrentWave();
        }
    }

    void SpawnForCurrentWave()
    {
        switch (currentWave)
        {
            case 1:
                GameObject prefab = wave1CubeType == Wave1CubeType.Sword ? swordCubePrefab : gunCubePrefab;
                SpawnCube(prefab, spawnPoints[RandomPointIndex(-1)], wave1SpeedLevel);
                break;

            case 2:
                SpawnGroup(wave2SpeedLevel, wave2DoubleChance, false);
                break;

            default:
                float t = Mathf.Clamp01((Elapsed - wave1Duration - wave2Duration) / wave3RampTime);
                SpawnGroup(Mathf.Lerp(wave3StartSpeedLevel, wave3EndSpeedLevel, t), wave3DoubleChance, true);
                break;
        }
    }

    void SpawnGroup(float speedLevel, float doubleChance, bool allowSpecial)
    {
        int first = RandomPointIndex(-1);
        SpawnRandomCube(first, speedLevel, allowSpecial);

        if (Random.value < doubleChance)
            SpawnRandomCube(RandomPointIndex(first), speedLevel, allowSpecial);
    }

    void SpawnRandomCube(int pointIndex, float speedLevel, bool allowSpecial)
    {
        GameObject prefab = Random.Range(0, 2) == 0 ? gunCubePrefab : swordCubePrefab;
        GameObject cube = SpawnCube(prefab, spawnPoints[pointIndex], speedLevel);

        if (allowSpecial && Random.value < specialCubeChance)
            MakeSpecial(cube);
    }

    void MakeSpecial(GameObject cube)
    {
        SpecialCube special = cube.AddComponent<SpecialCube>();
        special.effect = (SpecialCubeEffect)Random.Range(0, 3);
        special.slowMultiplier = slowMultiplier;
        special.fastMultiplier = fastMultiplier;

        cube.name += "_" + special.effect;
    }

    int RandomPointIndex(int except)
    {
        int index = Random.Range(0, spawnPoints.Length);

        if (index == except)
            index = (index + 1 + Random.Range(0, spawnPoints.Length - 1)) % spawnPoints.Length;

        return index;
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
        enabled = false;
    }

    GameObject SpawnCube(GameObject prefab, Transform point, float speedLevel)
    {
        GameObject cube = Instantiate(prefab, point.position, point.rotation);
        cube.GetComponent<CubePhysics>().speedLevel = speedLevel;
        return cube;
    }
}
