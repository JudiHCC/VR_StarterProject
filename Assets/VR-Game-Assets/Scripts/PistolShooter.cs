using UnityEngine;

[DefaultExecutionOrder(100)]
public class PistolShooter : MonoBehaviour
{
    public Transform nozzle;
    public Vector3 laserEulerOffset = Vector3.zero;
    public AudioSource shootSound;

    public float maxDistance = 100f;
    public float laserWidth = 0.01f;
    public Color laserColor = Color.red;
    public Material laserMaterial;
    public float laserDuration = 0.12f;
    public float fireCooldown = 0.15f;

    public float aimAssistRadius = 0.03f;

    public float crosshairDistance = 1.2f;
    public float crosshairRadius = 0.015f;
    public float crosshairWidth = 0.0025f;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.8f);

    [Range(0f, 1f)] public float shotVibrationFrequency = 1f;
    [Range(0f, 1f)] public float shotVibrationAmplitude = 1f;
    public float shotVibrationDuration = 0.2f;
    public float hitVibrationDuration = 0.3f;

    private LineRenderer laser;
    private Transform crosshair;
    private PistolHandGrab pistolGrab;
    private Transform playerRoot;

    private float laserTimer = 0f;
    private float laserLength = 0f;
    private float nextFireTime = 0f;
    private float shotVibrationTimer = 0f;

    private void Awake()
    {
        pistolGrab = GetComponent<PistolHandGrab>();

        if (laserMaterial == null)
            laserMaterial = new Material(Shader.Find("Sprites/Default"));

        laser = CreateLine("Laser", laserWidth, laserColor);
        CreateCrosshair();
    }

    private void Start()
    {
        playerRoot = GameObject.Find(HandGrabBase.RightGripPointName).transform.root;
    }

    private LineRenderer CreateLine(string lineName, float width, Color color)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = laserMaterial;
        line.enabled = false;

        return line;
    }

    private void CreateCrosshair()
    {
        crosshair = new GameObject("PistolCrosshair").transform;

        const int segments = 24;
        LineRenderer ring = CreateLine("Ring", crosshairWidth, crosshairColor);
        ring.transform.SetParent(crosshair, false);
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.alignment = LineAlignment.TransformZ;
        ring.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * crosshairRadius);
        }

        LineRenderer dot = CreateLine("Dot", crosshairWidth * 1.4f, crosshairColor);
        dot.transform.SetParent(crosshair, false);
        dot.useWorldSpace = false;
        dot.alignment = LineAlignment.TransformZ;
        dot.SetPosition(0, new Vector3(-crosshairWidth * 0.7f, 0f, 0f));
        dot.SetPosition(1, new Vector3(crosshairWidth * 0.7f, 0f, 0f));

        ring.enabled = true;
        dot.enabled = true;
    }

    private Vector3 LaserDirection()
    {
        return nozzle.rotation * Quaternion.Euler(laserEulerOffset) * Vector3.forward;
    }

    public void StartLaser()
    {
        Shoot();
    }

    public void StopLaser()
    {
    }

    private bool IsIgnored(Collider col)
    {
        return col.transform.root == playerRoot || col.GetComponentInParent<HandGrabBase>() != null;
    }

    private CubeExplode FindTarget(Vector3 start, Vector3 direction, out float hitDistance)
    {
        float blockDistance = maxDistance;

        RaycastHit[] hits = Physics.RaycastAll(start, direction, maxDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsIgnored(hit.collider))
                continue;

            CubeExplode cube = hit.collider.GetComponentInParent<CubeExplode>();

            if (cube != null)
            {
                if (cube.IsBroken)
                    continue;

                hitDistance = hit.distance;
                return cube;
            }

            if (!hit.collider.isTrigger)
            {
                blockDistance = hit.distance;
                break;
            }
        }

        hitDistance = blockDistance;

        if (aimAssistRadius <= 0f)
            return null;

        RaycastHit[] assistHits = Physics.SphereCastAll(start, aimAssistRadius, direction, blockDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(assistHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in assistHits)
        {
            if (IsIgnored(hit.collider))
                continue;

            CubeExplode cube = hit.collider.GetComponentInParent<CubeExplode>();

            if (cube != null && !cube.IsBroken)
            {
                hitDistance = hit.distance > 0f
                    ? hit.distance
                    : Vector3.Dot(cube.transform.position - start, direction);
                return cube;
            }
        }

        return null;
    }

    public void Shoot()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireCooldown;
        shootSound.Play();

        Vector3 direction = LaserDirection();
        CubeExplode target = FindTarget(nozzle.position, direction, out laserLength);

        bool hit = target != null && target.BreakCube(direction);

        OVRInput.SetControllerVibration(shotVibrationFrequency, shotVibrationAmplitude, pistolGrab.Controller);
        shotVibrationTimer = hit ? hitVibrationDuration : shotVibrationDuration;

        laserTimer = laserDuration;
        laser.enabled = true;
    }

    private void Update()
    {
        if (shotVibrationTimer > 0f)
        {
            shotVibrationTimer -= Time.deltaTime;

            if (shotVibrationTimer <= 0f)
                OVRInput.SetControllerVibration(0f, 0f, pistolGrab.Controller);
        }
    }

    private void LateUpdate()
    {
        Vector3 start = nozzle.position;
        Vector3 direction = LaserDirection();

        crosshair.gameObject.SetActive(pistolGrab.IsHeld);
        crosshair.SetPositionAndRotation(start + direction * crosshairDistance, Quaternion.LookRotation(direction));

        if (!laser.enabled)
            return;

        laserTimer -= Time.deltaTime;

        if (laserTimer <= 0f)
        {
            laser.enabled = false;
            return;
        }

        Color c = laserColor;
        c.a *= Mathf.Clamp01(laserTimer / laserDuration);
        laser.startColor = c;
        laser.endColor = c;

        laser.SetPosition(0, start);
        laser.SetPosition(1, start + direction * laserLength);
    }

    private void OnDisable()
    {
        laserTimer = 0f;
        laser.enabled = false;

        if (crosshair != null)
            crosshair.gameObject.SetActive(false);

        if (shotVibrationTimer > 0f)
            OVRInput.SetControllerVibration(0f, 0f, pistolGrab.Controller);

        shotVibrationTimer = 0f;
    }

    private void OnDestroy()
    {
        if (crosshair != null)
            Destroy(crosshair.gameObject);
    }
}
