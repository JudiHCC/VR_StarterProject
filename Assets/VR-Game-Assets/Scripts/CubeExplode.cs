using UnityEngine;

// Animació de destrucció del GunCube quan el toca una bala.
// El moviment el porta CubePhysics.
public class CubeExplode : MonoBehaviour
{
    public GameObject pieces;
    public GameObject mainCube;

    // Segons que tarda a desaparèixer el cub un cop trencat.
    public float destroyDelay = 2f;

    private bool broken = false;

    // Mentre fa l'animació de trencar-se, el cub no ha d'aturar els trets.
    public bool IsBroken => broken;

    void Start()
    {
        if (pieces != null)
            pieces.SetActive(false);

        if (mainCube != null)
            mainCube.SetActive(true);
    }

    // Retorna true si el cub s'ha trencat ara (i no ja abans).
    public bool BreakCube()
    {
        if (broken)
            return false;

        broken = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.GunCubeDestroyed();

        // Si és un cub especial de l'onada 3, el seu efecte s'acaba ara.
        SpecialCube special = GetComponent<SpecialCube>();

        if (special != null)
            special.End();

        // Aturem el cub perquè l'animació es faci on ha rebut el tret.
        CubePhysics physics = GetComponent<CubePhysics>();

        if (physics != null)
            physics.Stop();

        if (mainCube != null)
            mainCube.SetActive(false);

        if (pieces != null)
            pieces.SetActive(true);

        // Destrueix el cub després de l'animació.
        Destroy(gameObject, destroyDelay);
        return true;
    }
}
