using UnityEngine;

public enum WeaponHand
{
    Left,
    Right
}

public abstract class HandGrabBase : MonoBehaviour
{
    public const string RightGripPointName = "PistolGripPoint";
    public const string LeftGripPointName = "SwordGripPoint";

    public bool alwaysHeld = true;

    [Header("Offset a la mà per defecte (espasa: esquerra / pistola: dreta)")]
    public Vector3 gripPositionOffset = Vector3.zero;
    public Vector3 gripEulerOffset = Vector3.zero;

    [Header("Offset a l'altra mà (espasa: dreta / pistola: esquerra)")]
    public bool customOtherHandOffset = true;
    public bool mirrorOtherHand = true;
    public Vector3 otherHandPositionOffset = Vector3.zero;
    public Vector3 otherHandEulerOffset = Vector3.zero;

    public float grabDistance = 0.35f;
    public float gripPressThreshold = 0.7f;
    public float gripReleaseThreshold = 0.2f;

    [Range(0f, 1f)] public float vibrationFrequency = 1f;
    [Range(0f, 1f)] public float vibrationAmplitude = 0.7f;
    public float vibrationDuration = 0.15f;

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

    protected abstract WeaponHand DefaultHand { get; }

    private Transform HandAnchor =>
        currentHand == WeaponHand.Right ? rightAnchor : leftAnchor;

    private bool InDefaultHand => currentHand == DefaultHand;

    protected virtual void Awake()
    {
        currentHand = DefaultHand;

        rightAnchor = GameObject.Find(RightGripPointName).transform;
        leftAnchor = GameObject.Find(LeftGripPointName).transform;

        rb = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>();

        originalScale = transform.lossyScale;
        transform.SetParent(null, true);
        transform.localScale = originalScale;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (alwaysHeld)
            Hold();
        else
            SetFree();
    }

    private void Update()
    {
        if (alwaysHeld)
            return;

        float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, Controller);

        if (held)
        {
            if (gripValue < gripReleaseThreshold)
                Drop();
        }
        else if (gripValue > gripPressThreshold)
        {
            if (Vector3.Distance(HandAnchor.position, transform.position) <= grabDistance)
                Hold();
        }
    }

    private void LateUpdate()
    {
        if (!held)
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

    private Quaternion CurrentRotationOffset()
    {
        if (InDefaultHand)
            return Quaternion.Euler(gripEulerOffset);

        if (customOtherHandOffset)
            return Quaternion.Euler(otherHandEulerOffset);

        Quaternion q = Quaternion.Euler(gripEulerOffset);

        if (mirrorOtherHand)
            return new Quaternion(q.x, -q.y, -q.z, q.w);

        return q;
    }

    private void SetSolidCollidersEnabled(bool value)
    {
        foreach (Collider col in itemColliders)
        {
            if (!col.isTrigger)
                col.enabled = value;
        }
    }

    public void SetHand(WeaponHand hand)
    {
        if (hand == currentHand)
            return;

        StopVibration();

        currentHand = hand;

        if (held)
            Hold();
    }

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
    }
}
