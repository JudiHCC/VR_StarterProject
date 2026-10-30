using UnityEngine;
using EzySlice;

// Destrucció del SwordCube quan el talla l'espasa.
// Amb EzySlice el cub es parteix en dues meitats pel pla de la fulla.
// El moviment el porta CubePhysics.
public class SwordCubeScript : MonoBehaviour
{
    // Efecte que surt quan l'espasa talla el cub (p. ex. Slash_Hit1).
    public GameObject hitVFX;
    // Segons que dura l'efecte abans d'esborrar-lo.
    public float vfxLifetime = 2f;

    // So del tall (p. ex. SliceSound).
    public AudioClip sliceSound;
    [Range(0f, 1f)] public float sliceVolume = 1f;

    [Header("Tall amb EzySlice")]
    public bool useEzySlice = true;
    // Material de la cara de dins del tall. Buit = el mateix material del cub.
    public Material crossSectionMaterial;
    // Segons que les meitats es queden a l'escena abans de desaparèixer.
    public float pieceLifetime = 3f;
    // Velocitat amb què se separen les dues meitats (m/s).
    public float separationSpeed = 1.5f;
    // Part de la velocitat de l'espasa que s'enduen les meitats.
    [Range(0f, 1f)] public float swordVelocityTransfer = 0.3f;
    public bool piecesUseGravity = true;

    private bool sliced = false;

    // Col·lisió entre el collider primitiu de la fulla i el cub.
    private void OnTriggerEnter(Collider other)
    {
        SwordSlicer slicer = other.GetComponentInParent<SwordSlicer>();

        if (slicer == null || !slicer.CanSlice())
            return;

        if (slicer.debugLogs)
            Debug.Log("[Slicer] Cub tallat: " + name);

        if (Slice(slicer))
            slicer.OnSliced();
    }

    public bool Slice()
    {
        return Slice(null);
    }

    // Retorna true si el cub s'ha tallat ara (i no ja abans).
    public bool Slice(SwordSlicer slicer)
    {
        if (sliced)
            return false;

        sliced = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.SwordCubeDestroyed();

        // Si és un cub especial de l'onada 3, el seu efecte s'acaba ara.
        SpecialCube special = GetComponent<SpecialCube>();

        if (special != null)
            special.End();

        // Punt on la fulla ha tocat el cub (si no hi ha espasa, el centre).
        Vector3 hitPoint = transform.position;

        if (slicer != null)
        {
            Vector3 onBlade = slicer.ClosestPointOnBlade(transform.position);
            Collider cubeCollider = GetComponent<Collider>();
            hitPoint = cubeCollider != null ? cubeCollider.ClosestPoint(onBlade) : onBlade;
        }

        if (hitVFX != null)
        {
            GameObject vfx = Instantiate(hitVFX, hitPoint, transform.rotation);
            Destroy(vfx, vfxLifetime);
        }

        // PlayClipAtPoint perquè el so continuï encara que el cub es destrueixi.
        if (sliceSound != null)
            AudioSource.PlayClipAtPoint(sliceSound, hitPoint, sliceVolume);

        if (useEzySlice && slicer != null)
            CreatePieces(slicer);

        Destroy(gameObject);
        return true;
    }

    private void CreatePieces(SwordSlicer slicer)
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null || GetComponent<MeshFilter>() == null)
        {
            Debug.LogWarning("[Slicer] " + name + " no té MeshFilter/MeshRenderer a l'arrel, no el puc tallar.", this);
            return;
        }

        Material insideMaterial = crossSectionMaterial != null ? crossSectionMaterial : meshRenderer.sharedMaterial;

        Rigidbody cubeBody = GetComponent<Rigidbody>();
        Vector3 cubeVelocity = cubeBody != null && !cubeBody.isKinematic ? cubeBody.linearVelocity : Vector3.zero;

        // El pla de tall passa per la fulla i segueix la direcció del cop
        // relativa al cub, així el cub es parteix just per on passa l'espasa.
        Vector3 planeNormal = slicer.CutNormal(cubeVelocity);
        Vector3 planePoint = slicer.ClosestPointOnBlade(transform.position);
        SlicedHull hull = gameObject.Slice(planePoint, planeNormal, insideMaterial);

        // Si només l'ha fregat i el pla no travessa la malla, el tallem pel centre.
        if (hull == null)
            hull = gameObject.Slice(transform.position, planeNormal, insideMaterial);

        if (hull == null)
        {
            if (slicer.debugLogs)
                Debug.Log("[Slicer] El pla no travessa la malla, el cub es destrueix sense tallar.");

            return;
        }

        Vector3 baseVelocity = cubeVelocity + slicer.BladeVelocity * swordVelocityTransfer;

        SetupPiece(hull.CreateUpperHull(gameObject, insideMaterial), baseVelocity + planeNormal * separationSpeed);
        SetupPiece(hull.CreateLowerHull(gameObject, insideMaterial), baseVelocity - planeNormal * separationSpeed);
    }

    private void SetupPiece(GameObject piece, Vector3 velocity)
    {
        if (piece == null)
            return;

        piece.name = name + "_Piece";
        // Les meitats tallades no han de bloquejar els trets de la pistola.
        piece.layer = 2; // Ignore Raycast
        piece.transform.SetPositionAndRotation(transform.position, transform.rotation);
        piece.transform.localScale = transform.lossyScale;

        MeshCollider pieceCollider = piece.AddComponent<MeshCollider>();
        pieceCollider.convex = true;

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.useGravity = piecesUseGravity;
        body.linearVelocity = velocity;
        body.angularVelocity = Random.insideUnitSphere * 3f;

        Destroy(piece, pieceLifetime);
    }
}
