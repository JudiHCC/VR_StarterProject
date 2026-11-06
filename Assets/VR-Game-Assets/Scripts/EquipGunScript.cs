using UnityEngine;

public class EquipGunScript : MonoBehaviour
{
    public Transform handAnchor;
    public GameObject gunPrefab;

    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = Vector3.zero;

    void Start()
    {
        GameObject spawnedGun = Instantiate(gunPrefab, handAnchor);
        spawnedGun.transform.localPosition = localPosition;
        spawnedGun.transform.localRotation = Quaternion.Euler(localRotation);
    }
}
