using UnityEngine;

public class CubeExplode : MonoBehaviour
{
    public GameObject pieces;
    public GameObject mainCube;

    public float pieceLifetime = 1.5f;
    public float explosionSpeed = 1.5f;
    public float shotPush = 2.5f;
    public bool keepCubeVelocity = true;
    public float maxSpin = 8f;

    private bool broken = false;

    public bool IsBroken => broken;

    void Start()
    {
        pieces.SetActive(false);
        mainCube.SetActive(true);
    }

    public bool BreakCube()
    {
        return BreakCube(Vector3.zero);
    }

    public bool BreakCube(Vector3 shotDirection)
    {
        if (broken)
            return false;

        broken = true;
        ScoreManager.Instance.GunCubeDestroyed();

        SpecialCube special = GetComponent<SpecialCube>();

        if (special != null)
            special.End();

        Vector3 cubeVelocity = keepCubeVelocity ? GetComponent<Rigidbody>().linearVelocity : Vector3.zero;

        mainCube.SetActive(false);
        LaunchPieces(cubeVelocity, shotDirection.normalized);

        Destroy(gameObject);
        return true;
    }

    private void LaunchPieces(Vector3 cubeVelocity, Vector3 shotDirection)
    {
        pieces.SetActive(true);

        foreach (Animation anim in pieces.GetComponentsInChildren<Animation>(true))
        {
            anim.Stop();
            anim.enabled = false;
        }

        Vector3 center = transform.position;

        foreach (MeshRenderer piece in pieces.GetComponentsInChildren<MeshRenderer>(true))
        {
            Transform t = piece.transform;
            t.SetParent(null, true);
            piece.gameObject.layer = 2;

            Vector3 outward = (t.position - center).normalized;

            Rigidbody body = piece.gameObject.AddComponent<Rigidbody>();
            body.linearVelocity = cubeVelocity
                + outward * explosionSpeed * Random.Range(0.6f, 1.2f)
                + shotDirection * shotPush;
            body.angularVelocity = Random.insideUnitSphere * maxSpin;

            Destroy(piece.gameObject, pieceLifetime * Random.Range(0.8f, 1.2f));
        }

        Destroy(pieces, pieceLifetime * 1.3f);
    }
}
