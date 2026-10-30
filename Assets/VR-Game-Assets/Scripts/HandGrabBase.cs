using UnityEngine;

public enum WeaponHand
{
    Left,
    Right
}

// Codi comú de les armes que van a la mà (pistola i espasa).
// Per defecte l'arma surt directament a la mà i no es pot deixar anar.
// Si alwaysHeld és fals, s'agafa amb el grip i cau en soltar-lo.
public abstract class HandGrabBase : MonoBehaviour
{
    // Punts d'agafada de cada mà (fills de les mans del player).
    public const string RightGripPointName = "PistolGripPoint";
    public const string LeftGripPointName = "SwordGripPoint";

    public bool alwaysHeld = true;

    // Offset a la mà per defecte de l'arma
    // (espasa = mà esquerra, pistola = mà dreta).
    [Header("Offset a la mà per defecte (espasa: esquerra / pistola: dreta)")]
    public Vector3 gripPositionOffset = Vector3.zero;
    public Vector3 gripEulerOffset = Vector3.zero;

    // Offset quan l'arma està a l'altra mà (si s'intercanvien).
    // IMPORTANT: els valors otherHand només s'apliquen si
    // customOtherHandOffset està marcat. Si no, s'emmiralla l'offset
    // de la mà per defecte, però com que els dos grip points tenen
    // orientacions diferents, el resultat queda desplaçat.
    [Header("Offset a l'altra mà (espasa: dreta / pistola: esquerra)")]
    [Tooltip("Marca-ho perquè s'apliquin els valors Other Hand de sota.")]
    public bool customOtherHandOffset = true;
    public bool mirrorOtherHand = true;
    public Vector3 otherHandPositionOffset = Vector3.zero;
    public Vector3 otherHandEulerOffset = Vector3.zero;

    public float grabDistance = 0.35f;
    public float gripPressThreshold = 0.7f;
    public float gripReleaseThreshold = 0.2f;

    // Vibració del comandament quan l'arma destrueix un cub.
    [Range(0f, 1f)] public float vibrationFrequency = 1f;
    [Range(0f, 1f)] public float vibrationAmplitude = 0.7f;
    public float vibrationDuration = 0.15f;

    public bool debugLogs = true;

    private Coroutine vibrationRoutine;
    private OVRInput.Controller vibratingController = OVRInput.Controller.None;

    private Transform leftAnchor;
    private Transform rightAnchor;
    private WeaponHand currentHand;

    private Rigidbody rb;
    private Collider[] itemColliders;

    private Vector3 originalScale;
    private Vector3 lastPosition;
    private Vector3 handVelocity;

    private bool held = false;

    public bool IsHeld => held;
    public WeaponHand CurrentHand => currentHand;

    public OVRInput.Controller Controller =>
        currentHand == WeaponHand.Right ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;

    // Mà on va l'arma si no es diu el contrari.
    protected abstract WeaponHand DefaultHand { get; }
    protected abstract string LogTag { get; }

    private Transform HandAnchor =>
        currentHand == WeaponHand.Right ? rightAnchor : leftAnchor;

    private bool InDefaultHand => currentHand == DefaultHand;

    protected virtual void Awake()
    {
        currentHand = DefaultHand;

        rightAnchor = FindAnchor(RightGripPointName);
        leftAnchor = FindAnchor(LeftGripPointName);

        if (HandAnchor == null)
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        itemColliders = GetComponentsInChildren<Collider>();

        originalScale = transform.lossyScale;
        transform.SetParent(null, true);
        transform.localScale = originalScale;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (alwaysHeld)
        {
            Hold();
        }
        else
        {
            SetFree();

            if (debugLogs)
                Debug.Log(LogTag + " Llesta per ser recollida.");
        }
    }

    private Transform FindAnchor(string anchorName)
    {
        GameObject anchorObject = GameObject.Find(anchorName);

        if (anchorObject == null)
        {
            Debug.LogError(LogTag + " No trobo " + anchorName + " a l'escena.");
            return null;
        }

        return anchorObject.transform;
    }

    private void Update()
    {
        if (alwaysHeld || HandAnchor == null || rb == null)
            return;

        float gripValue = OVRInput.Get(
            OVRInput.Axis1D.PrimaryHandTrigger,
            Controller
        );

        if (held)
        {
            if (gripValue < gripReleaseThreshold)
                Drop();
        }
        else if (gripValue > gripPressThreshold)
        {
            float distance = Vector3.Distance(
                HandAnchor.position,
                transform.position
            );

            if (distance <= grabDistance)
                Hold();
        }
    }

    private void LateUpdate()
    {
        if (!held || HandAnchor == null)
            return;

        Vector3 position = GripPosition();

        transform.SetPositionAndRotation(position, GripRotation());
        transform.localScale = originalScale;

        if (Time.deltaTime > 0f)
        {
            Vector3 velocity = (position - lastPosition) / Time.deltaTime;
            handVelocity = Vector3.Lerp(handVelocity, velocity, 0.5f);
        }

        lastPosition = position;
    }

    private Vector3 GripPosition()
    {
        return HandAnchor.position + HandAnchor.rotation * CurrentPositionOffset();
    }

    private Quaternion GripRotation()
    {
        return HandAnchor.rotation * CurrentRotationOffset();
    }

    // Offset de posició segons la mà on és l'arma.
    private Vector3 CurrentPositionOffset()
    {
        if (InDefaultHand)
            return gripPositionOffset;

        if (customOtherHandOffset)
            return otherHandPositionOffset;

        if (mirrorOtherHand)
            return new Vector3(-gripPositionOffset.x, gripPositionOffset.y, gripPositionOffset.z);

        return gripPositionOffset;
    }

    // Offset de rotació segons la mà on és l'arma.
    private Quaternion CurrentRotationOffset()
    {
        if (InDefaultHand)
            return Quaternion.Euler(gripEulerOffset);

        if (customOtherHandOffset)
            return Quaternion.Euler(otherHandEulerOffset);

        Quaternion q = Quaternion.Euler(gripEulerOffset);

        // Rotació emmirallada respecte al pla YZ local de la mà.
        if (mirrorOtherHand)
            return new Quaternion(q.x, -q.y, -q.z, q.w);

        return q;
    }

    // Els triggers (p. ex. la fulla de l'espasa) es queden encesos
    // perquè no empenyen res i serveixen per detectar els cubs.
    private void SetSolidCollidersEnabled(bool value)
    {
        foreach (Collider col in itemColliders)
        {
            if (col != null && !col.isTrigger)
                col.enabled = value;
        }
    }

    // Canvia l'arma de mà (per l'intercanvi de mans en temps d'execució).
    public void SetHand(WeaponHand hand)
    {
        if (hand == currentHand)
            return;

        Transform newAnchor = hand == WeaponHand.Right ? rightAnchor : leftAnchor;

        if (newAnchor == null)
            return;

        // Atura la vibració del comandament antic abans de canviar.
        StopVibration();

        currentHand = hand;

        if (held)
            Hold();

        if (debugLogs)
            Debug.Log(LogTag + " Ara a la mà " + (hand == WeaponHand.Right ? "dreta." : "esquerra."));
    }

    public void ResetHand()
    {
        SetHand(DefaultHand);
    }

    // Fa vibrar el comandament de la mà on és l'arma.
    public void Vibrate()
    {
        StopVibration();
        vibrationRoutine = StartCoroutine(VibrateRoutine(Controller));
    }

    private System.Collections.IEnumerator VibrateRoutine(OVRInput.Controller controller)
    {
        vibratingController = controller;
        OVRInput.SetControllerVibration(vibrationFrequency, vibrationAmplitude, controller);

        yield return new WaitForSeconds(vibrationDuration);

        OVRInput.SetControllerVibration(0f, 0f, controller);
        vibratingController = OVRInput.Controller.None;
        vibrationRoutine = null;
    }

    private void StopVibration()
    {
        if (vibrationRoutine != null)
            StopCoroutine(vibrationRoutine);

        if (vibratingController != OVRInput.Controller.None)
            OVRInput.SetControllerVibration(0f, 0f, vibratingController);

        vibratingController = OVRInput.Controller.None;
        vibrationRoutine = null;
    }

    private void OnDisable()
    {
        StopVibration();
    }

    public void Hold()
    {
        if (HandAnchor == null || rb == null)
            return;

        held = true;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.useGravity = false;
        rb.isKinematic = true;

        SetSolidCollidersEnabled(false);

        Vector3 position = GripPosition();

        transform.SetPositionAndRotation(position, GripRotation());
        transform.localScale = originalScale;

        lastPosition = position;
        handVelocity = Vector3.zero;

        if (debugLogs)
            Debug.Log(LogTag + " Agafada.");
    }

    private void SetFree()
    {
        held = false;

        SetSolidCollidersEnabled(true);

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
    }

    public void Drop()
    {
        if (alwaysHeld)
            return;

        transform.SetParent(null, true);
        transform.localScale = originalScale;

        SetFree();

        rb.linearDamping = 0f;
        rb.linearVelocity = handVelocity;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        if (debugLogs)
            Debug.Log(LogTag + " Deixada anar.");
    }
}