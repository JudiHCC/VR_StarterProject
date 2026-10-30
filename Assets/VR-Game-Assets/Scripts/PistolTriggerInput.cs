using UnityEngine;
using UnityEngine.Events;

public class PistolTriggerInput : MonoBehaviour
{
    public PistolHandGrab pistolGrab;

    public float pressThreshold = 0.6f;
    public float releaseThreshold = 0.3f;

    public UnityEvent onTriggerPressed;
    public UnityEvent onTriggerReleased;

    public bool debugLogs = true;

    private bool pressed = false;

    private void Awake()
    {
        if (pistolGrab == null)
            pistolGrab = GetComponent<PistolHandGrab>();

        if (pistolGrab == null)
            Debug.LogError("[PistolTrigger] Falta PistolHandGrab.");
    }

    private void Update()
    {
        bool held = pistolGrab != null && pistolGrab.IsHeld;

        // El gallet és el del comandament de la mà on és la pistola.
        OVRInput.Controller controller = pistolGrab != null
            ? pistolGrab.Controller
            : OVRInput.Controller.RTouch;

        float triggerValue = OVRInput.Get(
            OVRInput.Axis1D.PrimaryIndexTrigger,
            controller
        );

        if (!pressed && held && triggerValue > pressThreshold)
        {
            pressed = true;
            onTriggerPressed.Invoke();

            if (debugLogs)
                Debug.Log("[PistolTrigger] Gallet premut.");
        }
        else if (pressed && (!held || triggerValue < releaseThreshold))
        {
            pressed = false;
            onTriggerReleased.Invoke();

            if (debugLogs)
                Debug.Log("[PistolTrigger] Gallet deixat.");
        }
    }

    private void OnDisable()
    {
        if (pressed)
        {
            pressed = false;
            onTriggerReleased.Invoke();
        }
    }
}