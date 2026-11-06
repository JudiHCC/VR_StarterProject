using UnityEngine;
using UnityEngine.Events;

public class PistolTriggerInput : MonoBehaviour
{
    public float pressThreshold = 0.6f;
    public float releaseThreshold = 0.3f;

    public UnityEvent onTriggerPressed;
    public UnityEvent onTriggerReleased;

    private PistolHandGrab pistolGrab;
    private bool pressed = false;

    private void Awake()
    {
        pistolGrab = GetComponent<PistolHandGrab>();
    }

    private void Update()
    {
        bool held = pistolGrab.IsHeld;
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, pistolGrab.Controller);

        if (!pressed && held && triggerValue > pressThreshold)
        {
            pressed = true;
            onTriggerPressed.Invoke();
        }
        else if (pressed && (!held || triggerValue < releaseThreshold))
        {
            pressed = false;
            onTriggerReleased.Invoke();
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
