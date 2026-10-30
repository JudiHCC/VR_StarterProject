using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

// Gestiona el lobby: mostra el best score i, quan es prem el botó
// de l'AutoHand (event On Pressed), fa sonar el clic, compta 3, 2, 1 i
// carrega la GameScene.
public class LobbyButtonScript : MonoBehaviour
{
    public TMP_Text StartText;
    public TMP_Text CountDownText;
    // Text del lobby on es mostra el best score.
    public TMP_Text BestScoreText;
    public string bestScorePrefix = "BEST SCORE: ";

    // ButtonClickSound (AudioSource amb 3DButtonClickSound).
    public AudioSource clickSound;

    public int countDownSeconds = 3;
    // Segons que es veu el "GO!" abans de canviar d'escena.
    public float goTextTime = 0.5f;
    public string gameSceneName = "GameScene";

    // El joc comença quan la mà xoca amb el botó.
    public bool startOnCollision = true;

    private bool gameStarted = false;

    private void Start()
    {
        // Els textos del compte enrere es veuen des del principi davant del botó.
        if (CountDownText != null)
            CountDownText.text = countDownSeconds.ToString();

        if (BestScoreText != null)
            BestScoreText.text = bestScorePrefix + ScoreManager.GetHighScore();
    }

    // Connectar a l'event On Pressed del PhysicsGadgetButton de l'AutoHand.
    public void StartGame()
    {
        if (gameStarted)
            return;

        gameStarted = true;

        if (clickSound != null)
            clickSound.Play();

        if (StartText != null)
            StartText.gameObject.SetActive(true);

        if (CountDownText != null)
            CountDownText.gameObject.SetActive(true);

        StartCoroutine(DoCountDown());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (startOnCollision)
            StartGame();
    }

    private IEnumerator DoCountDown()
    {
        for (int i = countDownSeconds; i > 0; i--)
        {
            if (CountDownText != null)
                CountDownText.text = i.ToString();

            yield return new WaitForSeconds(1f);
        }

        if (CountDownText != null)
            CountDownText.text = "GO!";

        yield return new WaitForSeconds(goTextTime);

        SceneManager.LoadScene(gameSceneName);
    }
}
