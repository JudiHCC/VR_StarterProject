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
    public float aimAssistRadius = 0.06f;

    [Header("Mira")]
    // Línia fina que mostra on apuntes; es torna verda si apuntes a un GunCube.
    public bool showAimLine = true;
    public float aimLineWidth = 0.004f;
    public float aimLineLength = 15f;
    public Color aimLineColor = new Color(1f, 0.2f, 0.2f, 0.25f);
    public Color aimLineOnTargetColor = new Color(0.2f, 1f, 0.3f, 0.6f);

    [Header("Vibració")]
    // Vibració curta quan falles (quan encertes, PistolHandGrab ja fa la vibració forta).
    [Range(0f, 1f)] public float shotVibrationAmplitude = 0.25f;
    public float shotVibrationDuration = 0.04f;

    public bool debugLogs = true;

    private LineRenderer laser;
    private LineRenderer aimLine;
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
        aimLine = CreateLine("AimLine", aimLineWidth, aimLineColor);
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

        CubeExplode target = FindTarget(start, direction, out laserLength);

        if (target != null)
        {
            if (debugLogs)
                Debug.Log("[Shooter] Cub tocat: " + target.name);

            if (target.BreakCube() && pistolGrab != null)
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

        UpdateAimLine(start, direction);

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

    private void UpdateAimLine(Vector3 start, Vector3 direction)
    {
        if (aimLine == null)
            return;

        bool visible = showAimLine && (pistolGrab == null || pistolGrab.IsHeld);
        aimLine.enabled = visible;

        if (!visible)
            return;

        float distance;
        CubeExplode target = FindTarget(start, direction, out distance);

        float length = target != null ? distance : Mathf.Min(distance, aimLineLength);
        Color color = target != null ? aimLineOnTargetColor : aimLineColor;

        aimLine.startColor = color;
        // Si no apunta a res, la línia s'esvaeix cap al final.
        aimLine.endColor = new Color(color.r, color.g, color.b, target != null ? color.a : 0f);
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, start + direction * length);
    }

    private void OnDisable()
    {
        laserTimer = 0f;

        if (laser != null)
            laser.enabled = false;

        if (aimLine != null)
            aimLine.enabled = false;

        if (shotVibrationTimer > 0f && pistolGrab != null)
            OVRInput.SetControllerVibration(0f, 0f, pistolGrab.Controller);

        shotVibrationTimer = 0f;
    }
}
