using UnityEngine;
using UnityEngine.SceneManagement;
using Autohand;

public class VoidZone : MonoBehaviour
{
    public string lobbySceneName = "LobbyScene";

    private bool triggered = false;

    private void OnCollisionEnter(Collision collision)
    {
        CheckPlayer(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckPlayer(other);
    }

    private void CheckPlayer(Collider other)
    {
        if (triggered || other.GetComponentInParent<AutoHandPlayer>() == null)
            return;

        triggered = true;
        SceneManager.LoadScene(lobbySceneName);
    }
}
