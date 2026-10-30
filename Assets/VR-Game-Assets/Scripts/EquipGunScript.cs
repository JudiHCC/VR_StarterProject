using UnityEngine;

public class EquipGunScript : MonoBehaviour
{
public Transform handAnchor;
public GameObject gunPrefab;


public Vector3 localPosition = Vector3.zero;
public Vector3 localRotation = Vector3.zero;

private GameObject spawnedGun;

void Start()
{
    if (handAnchor == null || gunPrefab == null)
    {
        Debug.LogError("Falta assignar la mà o el prefab de la pistola.");
        return;
    }

    // Crear la pistola com a filla de la mà
    spawnedGun = Instantiate(gunPrefab, handAnchor);

    // Ajustar la posició i la rotació respecte a la mà
    spawnedGun.transform.localPosition = localPosition;
    spawnedGun.transform.localRotation = Quaternion.Euler(localRotation);
}


}
