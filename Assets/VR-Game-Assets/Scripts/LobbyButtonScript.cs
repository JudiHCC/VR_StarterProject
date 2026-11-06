using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyButtonScript : MonoBehaviour
{
    public TMP_Text StartText;
    public TMP_Text CountDownText;
    public TMP_Text BestScoreText;
    public string bestScorePrefix = "BEST SCORE: ";
    public AudioSource clickSound;

    public int countDownSeconds = 3;
    public float goTextTime = 0.5f;
    public string gameSceneName = "GameScene";
    public bool startOnCollision = true;

    private bool gameStarted = false;

    private void Start()
    {
        CountDownText.text = countDownSeconds.ToString();

        if (BestScoreText != null)
            BestScoreText.text = bestScorePrefix + ScoreManager.GetHighScore();
    }

    public void StartGame()
    {
        if (gameStarted)
            return;

        gameStarted = true;

        if (clickSound != null)
            clickSound.Play();

        StartText.gameObject.SetActive(true);
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
            CountDownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        CountDownText.text = "GO!";
        yield return new WaitForSeconds(goTextTime);

        SceneManager.LoadScene(gameSceneName);
    }
}
