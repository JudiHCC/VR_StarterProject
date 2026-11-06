using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CubePhysics : MonoBehaviour
{
    public float speedLevel = 1f;
    public float baseSpeed = 2f;
    public Vector3 direction = Vector3.back;
    public string killerName = "CubeKiller";

    private Rigidbody rb;
    private bool stopped = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        if (!stopped)
            rb.linearVelocity = direction.normalized * baseSpeed * speedLevel * GameSpeed.Multiplier;
    }

    public void Stop()
    {
        stopped = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stopped || other.name != killerName)
            return;

        ScoreManager.Instance.CubeMissed();
        Destroy(gameObject);
    }
}
