using UnityEngine;

public class SwordCubePickup : MonoBehaviour
{
    public GameObject swordPrefab;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        Transform grip = other.transform.Find("SwordGripPoint");

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

        collected = true;

        GameObject sword = Instantiate(swordPrefab, grip.position, grip.rotation);
        sword.GetComponent<SwordHandGrab>().Hold();

        Destroy(gameObject);
    }
}
