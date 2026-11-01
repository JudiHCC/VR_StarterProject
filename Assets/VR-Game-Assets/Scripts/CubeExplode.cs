using UnityEngine;

// Destrucció del GunCube quan el toca un tret.
// Les peces del cub segueixen movent-se amb física (gravetat + impuls).
// Per no carregar l'ordinador, les peces NO tenen colliders
// (no xoquen amb res, només cauen) i desapareixen en poc temps.
public class CubeExplode : MonoBehaviour
{
    public GameObject pieces;
    public GameObject mainCube;

    [Header("Física de les peces")]
    // Segons que duren les peces abans de desaparèixer.
    public float pieceLifetime = 1.5f;
    // Velocitat amb què les peces surten disparades cap a fora (m/s).
    public float explosionSpeed = 1.5f;
    // Empenta en la direcció del tret (m/s).
    public float shotPush = 2.5f;
    // Les peces conserven la velocitat que portava el cub.
    public bool keepCubeVelocity = true;
    // Gir aleatori màxim de les peces.
    public float maxSpin = 8f;

    private bool broken = false;

    // Un cub trencat ja no ha d'aturar els trets.
    public bool IsBroken => broken;

    void Start()
    {
        if (pieces != null)
            pieces.SetActive(false);

        if (mainCube != null)
            mainCube.SetActive(true);
    }

    public bool BreakCube()
    {
        return BreakCube(Vector3.zero);
    }

    // Retorna true si el cub s'ha trencat ara (i no ja abans).
    // shotDirection: cap on anava el tret (per empènyer les peces).
    public bool BreakCube(Vector3 shotDirection)
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

        // Velocitat del cub abans de trencar-se.
        Vector3 cubeVelocity = Vector3.zero;
        Rigidbody cubeBody = GetComponent<Rigidbody>();

        if (keepCubeVelocity && cubeBody != null && !cubeBody.isKinematic)
            cubeVelocity = cubeBody.linearVelocity;

        if (mainCube != null)
            mainCube.SetActive(false);

        if (pieces != null)
            LaunchPieces(cubeVelocity, shotDirection.normalized);

        // El cub en si ja no cal: les peces van soles.
        Destroy(gameObject);
        return true;
    }

    private void LaunchPieces(Vector3 cubeVelocity, Vector3 shotDirection)
    {
        pieces.SetActive(true);

        // Aturem l'animació: ara les peces les mou la física.
        foreach (Animation anim in pieces.GetComponentsInChildren<Animation>(true))
        {
            anim.Stop();
            anim.enabled = false;
        }

        foreach (Animator animator in pieces.GetComponentsInChildren<Animator>(true))
            animator.enabled = false;

        Vector3 center = transform.position;

        foreach (MeshRenderer piece in pieces.GetComponentsInChildren<MeshRenderer>(true))
        {
            Transform t = piece.transform;
            t.SetParent(null, true);

            // Les peces no han de bloquejar els trets.
            piece.gameObject.layer = 2; // Ignore Raycast

            Vector3 outward = t.position - center;
            outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Random.onUnitSphere;

            Rigidbody body = piece.gameObject.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.linearVelocity = cubeVelocity
                + outward * explosionSpeed * Random.Range(0.6f, 1.2f)
                + shotDirection * shotPush;
            body.angularVelocity = Random.insideUnitSphere * maxSpin;

            Destroy(piece.gameObject, pieceLifetime * Random.Range(0.8f, 1.2f));
        }

        // Si queda alguna cosa sense malla (contenidors), també s'esborra.
        Destroy(pieces, pieceLifetime * 1.3f);
    }
}
