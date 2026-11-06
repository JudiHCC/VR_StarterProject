using UnityEngine;
using EzySlice;

public class SwordCubeScript : MonoBehaviour
{
    public GameObject hitVFX;
    public float vfxLifetime = 2f;

    public AudioClip sliceSound;
    [Range(0f, 1f)] public float sliceVolume = 1f;

    public bool useEzySlice = true;
    public Material crossSectionMaterial;
    public float pieceLifetime = 3f;
    public float separationSpeed = 1.5f;
    [Range(0f, 1f)] public float swordVelocityTransfer = 0.3f;
    public bool piecesUseGravity = true;

    private bool sliced = false;

    private void OnTriggerEnter(Collider other)
    {
        SwordSlicer slicer = other.GetComponentInParent<SwordSlicer>();

        if (slicer != null && slicer.CanSlice() && Slice(slicer))
            slicer.OnSliced();
    }

    public bool Slice(SwordSlicer slicer)
    {
        if (sliced)
            return false;

        sliced = true;
        ScoreManager.Instance.SwordCubeDestroyed();

        SpecialCube special = GetComponent<SpecialCube>();

        if (special != null)
            special.End();

        Vector3 onBlade = slicer.ClosestPointOnBlade(transform.position);
        Vector3 hitPoint = GetComponent<Collider>().ClosestPoint(onBlade);

        GameObject vfx = Instantiate(hitVFX, hitPoint, transform.rotation);
        Destroy(vfx, vfxLifetime);

        AudioSource.PlayClipAtPoint(sliceSound, hitPoint, sliceVolume);

        if (useEzySlice)
            CreatePieces(slicer);

        Destroy(gameObject);
        return true;
    }

    private void CreatePieces(SwordSlicer slicer)
    {
        Material insideMaterial = crossSectionMaterial != null
            ? crossSectionMaterial
            : GetComponent<MeshRenderer>().sharedMaterial;

        Vector3 cubeVelocity = GetComponent<Rigidbody>().linearVelocity;

        Vector3 planeNormal = slicer.CutNormal(cubeVelocity);
        Vector3 planePoint = slicer.ClosestPointOnBlade(transform.position);
        SlicedHull hull = gameObject.Slice(planePoint, planeNormal, insideMaterial);

        if (hull == null)
            hull = gameObject.Slice(transform.position, planeNormal, insideMaterial);

        if (hull == null)
            return;

        Vector3 baseVelocity = cubeVelocity + slicer.BladeVelocity * swordVelocityTransfer;

        SetupPiece(hull.CreateUpperHull(gameObject, insideMaterial), baseVelocity + planeNormal * separationSpeed);
        SetupPiece(hull.CreateLowerHull(gameObject, insideMaterial), baseVelocity - planeNormal * separationSpeed);
    }

    private void SetupPiece(GameObject piece, Vector3 velocity)
    {
        piece.name = name + "_Piece";
        piece.layer = 2;
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
