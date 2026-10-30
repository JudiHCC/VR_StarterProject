using UnityEngine;

public class SwordCubePickup : MonoBehaviour
{
    public GameObject swordPrefab;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        // Detectem si hem tocat la mà esquerra.
        Transform grip = other.transform.Find("SwordGripPoint");

        // Si el collider és en un fill de la mà,
        // busquem també entre els seus pares.
        if (grip == null)
        {
            Transform parent = other.transform.parent;

            while (parent != null && grip == null)
            {
                grip = parent.Find("SwordGripPoint");
                parent = parent.parent;
            }
        }

        if (grip == null)
            return;

        if (swordPrefab == null)
        {
            Debug.LogError("Falta assignar Sword Prefab!");
            return;
        }

        collected = true;

        // Creem l'espasa a la mà i la deixem agafada.
        // Si després es deixa anar el grip, caurà com la pistola.
        GameObject sword = Instantiate(swordPrefab, grip.position, grip.rotation);

        SwordHandGrab swordGrab = sword.GetComponent<SwordHandGrab>();

        if (swordGrab == null)
            swordGrab = sword.AddComponent<SwordHandGrab>();

        swordGrab.Hold();

        Destroy(gameObject);
    }
}
