using UnityEngine;

// Marca l'espasa com a "talladora".
// La fulla ha de tenir un collider primitiu (BoxCollider o CapsuleCollider)
// marcat com a Is Trigger i adaptat a la seva forma.
// El SwordCube detecta la col·lisió amb aquest collider i es talla amb EzySlice
// fent servir el pla que calcula aquest script.
public class SwordSlicer : MonoBehaviour
{
    // Velocitat mínima de la fulla per tallar (m/s). 0 = només tocar.
    public float minSwingSpeed = 0f;

    // BoxCollider (trigger) de la fulla. Si està buit, es busca als fills.
    public BoxCollider bladeCollider;

    public bool onlyWhenHeld = true;
    public bool debugLogs = true;

    private SwordHandGrab swordGrab;
    private Vector3 lastBladePosition;
    private Vector3 bladeVelocity;

    // Eixos locals de la fulla: el més llarg és la longitud
    // i el més prim és la normal de la cara plana.
    private Vector3 lengthAxis = Vector3.forward;
    private Vector3 flatAxis = Vector3.right;

    private void Awake()
    {
        swordGrab = GetComponent<SwordHandGrab>();

        if (bladeCollider == null)
        {
            foreach (BoxCollider box in GetComponentsInChildren<BoxCollider>(true))
            {
                if (box.isTrigger)
                {
                    bladeCollider = box;
                    break;
                }
            }
        }

        if (bladeCollider != null)
            FindBladeAxes(bladeCollider.size);
        else
            Debug.LogWarning("[Slicer] No trobo el BoxCollider trigger de la fulla, faré servir l'espasa sencera.");

        lastBladePosition = BladeCenter;
    }

    private void FindBladeAxes(Vector3 size)
    {
        Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
        float[] sizes = { Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z) };

        int longest = 0;
        int thinnest = 0;

        for (int i = 1; i < 3; i++)
        {
            if (sizes[i] > sizes[longest])
                longest = i;

            if (sizes[i] < sizes[thinnest])
                thinnest = i;
        }

        lengthAxis = axes[longest];
        flatAxis = axes[thinnest];
    }

    private Transform BladeTransform => bladeCollider != null ? bladeCollider.transform : transform;

    // Centre de la fulla en coordenades del món.
    public Vector3 BladeCenter =>
        bladeCollider != null ? bladeCollider.transform.TransformPoint(bladeCollider.center) : transform.position;

    private void LateUpdate()
    {
        Vector3 position = BladeCenter;

        if (Time.deltaTime > 0f)
        {
            Vector3 velocity = (position - lastBladePosition) / Time.deltaTime;
            bladeVelocity = Vector3.Lerp(bladeVelocity, velocity, 0.5f);
        }

        lastBladePosition = position;
    }

    // Normal del pla de tall: el pla conté la fulla i la direcció del cop.
    // Si l'espasa gairebé no es mou, es talla pel pla de la fulla.
    public Vector3 CutNormal()
    {
        return CutNormal(Vector3.zero);
    }

    // Igual, però fent servir la velocitat de la fulla RELATIVA al cub.
    // Els cubs vénen cap al jugador, i si no es resta la seva velocitat
    // el pla de tall surt inclinat respecte al cop real.
    public Vector3 CutNormal(Vector3 targetVelocity)
    {
        Vector3 bladeLength = BladeTransform.TransformDirection(lengthAxis);
        Vector3 relativeVelocity = bladeVelocity - targetVelocity;
        Vector3 normal = Vector3.Cross(bladeLength, relativeVelocity);

        if (normal.sqrMagnitude < 0.0001f)
            normal = BladeTransform.TransformDirection(flatAxis);

        return normal.normalized;
    }

    public Vector3 BladeVelocity => bladeVelocity;

    // Punt de la fulla (segment de base a punta) més proper a un punt del món.
    // Serveix per saber on ha tocat l'espasa el cub.
    public Vector3 ClosestPointOnBlade(Vector3 worldPoint)
    {
        if (bladeCollider == null)
            return transform.position;

        Vector3 halfLength = Vector3.Scale(lengthAxis, bladeCollider.size) * 0.5f;
        Vector3 a = bladeCollider.transform.TransformPoint(bladeCollider.center - halfLength);
        Vector3 b = bladeCollider.transform.TransformPoint(bladeCollider.center + halfLength);

        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 0f ? Vector3.Dot(worldPoint - a, ab) / ab.sqrMagnitude : 0f;
        return a + ab * Mathf.Clamp01(t);
    }

    // Cridat pel SwordCube quan l'han tallat.
    public void OnSliced()
    {
        if (swordGrab != null)
            swordGrab.Vibrate();
    }

    public bool CanSlice()
    {
        if (onlyWhenHeld && (swordGrab == null || !swordGrab.IsHeld))
            return false;

        return bladeVelocity.magnitude >= minSwingSpeed;
    }
}
