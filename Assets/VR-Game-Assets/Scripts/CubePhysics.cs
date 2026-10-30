using UnityEngine;

// Mou el cub cap al jugador i el destrueix quan arriba al CubeKiller.
// Va als dos prefabs: GunCube i SwordCube.
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class CubePhysics : MonoBehaviour
{
    // Nivell de moviment: com més alt, més ràpid. El posa el CubeSpawnManager.
    public float speedLevel = 1f;
    // Metres per segon amb nivell 1.
    public float baseSpeed = 2f;
    public Vector3 direction = Vector3.back;

    // Nom de l'objecte trigger de l'escena que destrueix els cubs.
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
        if (stopped)
            return;

        // GameSpeed.Multiplier el canvien els cubs especials (lent / ràpid).
        rb.linearVelocity = direction.normalized * baseSpeed * speedLevel * GameSpeed.Multiplier;
    }

    // Per quan el cub es trenca i s'ha de quedar quiet.
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

        // El jugador no l'ha destruït a temps.
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.CubeMissed();

        Destroy(gameObject);
    }
}
