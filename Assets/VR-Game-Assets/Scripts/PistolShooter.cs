using UnityEngine;

// Tret de la pistola amb raig làser.
// S'executa després de PistolHandGrab perquè el raig surti d'on és la pistola aquest frame.
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

    // Segons que es veu el raig a cada tret (es va esvaint).
    public float laserDuration = 0.12f;
    // Temps mínim entre trets.
    public float fireCooldown = 0.15f;

    [Header("Ajuda a apuntar")]
    // Radi del "tub" al voltant del raig on també es compta com a impacte (metres).
    // 0 = només el raig exacte.
    public float aimAssistRadius = 0.03f;

    [Header("Mirilla")]
    // Petit cercle a poca distància del canó per apuntar.
    // No mostra la trajectòria ni avisa si apuntes a un cub.
    public bool showCrosshair = true;
    public float crosshairDistance = 1.2f;
    public float crosshairRadius = 0.015f;
    public float crosshairWidth = 0.0025f;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Vibració")]
    // Vibració curta quan falles (quan encertes, PistolHandGrab ja fa la vibració forta).
    [Range(0f, 1f)] public float shotVibrationAmplitude = 0.25f;
    public float shotVibrationDuration = 0.04f;

    public bool debugLogs = true;

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
        if (nozzle == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Nozzle")
                {
                    nozzle = child;
                    break;
                }
            }
        }

        if (nozzle == null)
            Debug.LogError("[Shooter] Falta assignar el Nozzle.");

        if (shootSound == null)
            shootSound = GetComponentInChildren<AudioSource>();

        pistolGrab = GetComponent<PistolHandGrab>();

        if (laserMaterial == null)
            laserMaterial = new Material(Shader.Find("Sprites/Default"));

        laser = CreateLine("Laser", laserWidth, laserColor);
        CreateCrosshair();
    }

    private void Start()
    {
        // L'arrel del player (mans, càmera...). El raig ignora els seus colliders
        // perquè la pròpia mà no "es mengi" el tret.
        GameObject gripPoint = GameObject.Find(HandGrabBase.RightGripPointName);

        if (gripPoint != null)
            playerRoot = gripPoint.transform.root;
    }

    private LineRenderer CreateLine(string lineName, float width, Color color)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
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

    // Cercle + punt central, dibuixats amb LineRenderer en coordenades locals.
    private void CreateCrosshair()
    {
        // Sense pare: així l'escala de la pistola no deforma el cercle.
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
        crosshair.gameObject.SetActive(false);
    }

    private Vector3 LaserDirection()
    {
        return nozzle.rotation * Quaternion.Euler(laserEulerOffset) * Vector3.forward;
    }

    // Cridar des de On Trigger Pressed. Un tret per clic.
    public void StartLaser()
    {
        Shoot();
    }

    // Es manté perquè l'event On Trigger Released no quedi trencat.
    public void StopLaser()
    {
    }

    // Colliders que el raig ha de travessar: el player i les armes.
    private bool IsIgnored(Collider col)
    {
        if (playerRoot != null && col.transform.root == playerRoot)
            return true;

        // L'espasa i la pròpia pistola.
        if (col.GetComponentInParent<HandGrabBase>() != null)
            return true;

        return false;
    }

    // Busca el primer GunCube sencer al llarg del raig.
    // hitDistance: on s'atura el raig (el cub, una paret o la distància màxima).
    private CubeExplode FindTarget(Vector3 start, Vector3 direction, out float hitDistance)
    {
        float blockDistance = maxDistance;

        // 1) Raig exacte. També troba on s'atura (parets, terra...).
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
                // Els cubs ja trencats (fent l'animació) no han de parar el tret.
                if (cube.IsBroken)
                    continue;

                hitDistance = hit.distance;
                return cube;
            }

            // Els altres triggers no aturen el tret, les parets sí.
            if (!hit.collider.isTrigger)
            {
                blockDistance = hit.distance;
                break;
            }
        }

        hitDistance = blockDistance;

        // 2) Ajuda a apuntar: un "tub" una mica més gruixut, fins a la paret.
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
                // El SphereCast dona distància 0 si ja comença tocant; fem servir la del cub.
                hitDistance = hit.distance > 0f
                    ? hit.distance
                    : Vector3.Dot(cube.transform.position - start, direction);
                return cube;
            }
        }

        return null;
    }

    // Primer botó de la UI al llarg del raig (abans de cap paret).
    private ShootableButton FindButton(Vector3 start, Vector3 direction, out float distance)
    {
        distance = maxDistance;

        RaycastHit[] hits = Physics.RaycastAll(start, direction, maxDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsIgnored(hit.collider))
                continue;

            ShootableButton button = hit.collider.GetComponent<ShootableButton>();

            if (button != null)
            {
                distance = hit.distance;
                return button;
            }

            if (!hit.collider.isTrigger)
                return null;
        }

        return null;
    }

    public void Shoot()
    {
        if (nozzle == null || laser == null)
            return;

        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + fireCooldown;

        if (shootSound != null)
            shootSound.Play();

        Vector3 start = nozzle.position;
        Vector3 direction = LaserDirection();

        // Botons de la UI (panell final): es premen disparant-los.
        float buttonDistance;
        ShootableButton uiButton = FindButton(start, direction, out buttonDistance);

        if (uiButton != null)
        {
            laserLength = buttonDistance;
            laserTimer = laserDuration;
            laser.enabled = true;

            if (pistolGrab != null)
                pistolGrab.Vibrate();

            uiButton.Press();
            return;
        }

        CubeExplode target = FindTarget(start, direction, out laserLength);

        if (target != null)
        {
            if (debugLogs)
                Debug.Log("[Shooter] Cub tocat: " + target.name);

            if (target.BreakCube(direction) && pistolGrab != null)
                pistolGrab.Vibrate();
        }
        else
        {
            ShotVibration();

            if (debugLogs)
                Debug.Log("[Shooter] Tret fallat.");
        }

        laserTimer = laserDuration;
        laser.enabled = true;
    }

    private void ShotVibration()
    {
        if (pistolGrab == null || shotVibrationAmplitude <= 0f)
            return;

        OVRInput.SetControllerVibration(1f, shotVibrationAmplitude, pistolGrab.Controller);
        shotVibrationTimer = shotVibrationDuration;
    }

    private void Update()
    {
        if (shotVibrationTimer > 0f)
        {
            shotVibrationTimer -= Time.deltaTime;

            if (shotVibrationTimer <= 0f && pistolGrab != null)
                OVRInput.SetControllerVibration(0f, 0f, pistolGrab.Controller);
        }
    }

    private void LateUpdate()
    {
        if (nozzle == null)
            return;

        Vector3 start = nozzle.position;
        Vector3 direction = LaserDirection();

        UpdateCrosshair(start, direction);

        if (laser == null || !laser.enabled)
            return;

        laserTimer -= Time.deltaTime;

        if (laserTimer <= 0f)
        {
            laser.enabled = false;
            return;
        }

        // El raig segueix la pistola mentre es veu i es va esvaint.
        Color c = laserColor;
        c.a *= Mathf.Clamp01(laserTimer / laserDuration);
        laser.startColor = c;
        laser.endColor = c;

        laser.SetPosition(0, start);
        laser.SetPosition(1, start + direction * laserLength);
    }

    private void UpdateCrosshair(Vector3 start, Vector3 direction)
    {
        if (crosshair == null)
            return;

        bool visible = showCrosshair && (pistolGrab == null || pistolGrab.IsHeld);
        crosshair.gameObject.SetActive(visible);

        if (!visible)
            return;

        crosshair.SetPositionAndRotation(start + direction * crosshairDistance, Quaternion.LookRotation(direction));
    }

    private void OnDestroy()
    {
        if (crosshair != null)
            Destroy(crosshair.gameObject);
    }

    private void OnDisable()
    {
        laserTimer = 0f;

        if (laser != null)
            laser.enabled = false;

        if (crosshair != null)
            crosshair.gameObject.SetActive(false);

        if (shotVibrationTimer > 0f && pistolGrab != null)
            OVRInput.SetControllerVibration(0f, 0f, pistolGrab.Controller);

        shotVibrationTimer = 0f;
    }
}
