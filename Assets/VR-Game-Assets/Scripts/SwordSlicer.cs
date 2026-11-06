using UnityEngine;

public class SwordSlicer : MonoBehaviour
{
    public float minSwingSpeed = 0f;
    public BoxCollider bladeCollider;
    public bool onlyWhenHeld = true;

    private SwordHandGrab swordGrab;
    private Vector3 lastBladePosition;
    private Vector3 bladeVelocity;

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

        FindBladeAxes(bladeCollider.size);
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

    public Vector3 BladeCenter => bladeCollider.transform.TransformPoint(bladeCollider.center);

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

    public Vector3 CutNormal(Vector3 targetVelocity)
    {
        Vector3 bladeLength = bladeCollider.transform.TransformDirection(lengthAxis);
        Vector3 normal = Vector3.Cross(bladeLength, bladeVelocity - targetVelocity);

        if (normal.sqrMagnitude < 0.0001f)
            normal = bladeCollider.transform.TransformDirection(flatAxis);

        return normal.normalized;
    }

    public Vector3 BladeVelocity => bladeVelocity;

    public Vector3 ClosestPointOnBlade(Vector3 worldPoint)
    {
        Vector3 halfLength = Vector3.Scale(lengthAxis, bladeCollider.size) * 0.5f;
        Vector3 a = bladeCollider.transform.TransformPoint(bladeCollider.center - halfLength);
        Vector3 b = bladeCollider.transform.TransformPoint(bladeCollider.center + halfLength);

        Vector3 ab = b - a;
        float t = Vector3.Dot(worldPoint - a, ab) / ab.sqrMagnitude;
        return a + ab * Mathf.Clamp01(t);
    }

    public void OnSliced()
    {
        swordGrab.Vibrate();
    }

    public bool CanSlice()
    {
        if (onlyWhenHeld && !swordGrab.IsHeld)
            return false;

        return bladeVelocity.magnitude >= minSwingSpeed;
    }
}
