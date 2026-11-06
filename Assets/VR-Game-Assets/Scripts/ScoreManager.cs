using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public const string HighScoreKey = "HighScore";
    public const string EndlessHighScoreKey = "HighScoreEndless";

    public static ScoreManager Instance { get; private set; }

    [Header("Partida")]
    public float gameDuration = 180f;
    public bool endlessMode = false;

    [Header("Punts")]
    public int gunCubePoints = 100;
    public int swordCubePoints = 100;
    public int missedCubePenalty = 50;
    public bool allowNegativeScore = false;

    [Header("UI_Timer")]
    public TMP_Text timeText;
    public Image timerFill;
    public Color wave1Color = new Color(0.2f, 0.85f, 0.25f);
    public Color wave2Color = new Color(1f, 0.85f, 0.1f);
    public Color wave3Color = new Color(0.95f, 0.2f, 0.15f);

    [Header("UI_ScoreCurrent")]
    public TMP_Text currentScoreText;

    [Header("UI_ScoreFinal")]
    public GameObject finalPanel;
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;

    [Header("Altres")]
    public GameObject[] hideOnGameOver;
    public int lobbySceneIndex = 0;

    [Header("Tornar a jugar")]
    public OVRInput.Button restartButtonRight = OVRInput.Button.One;
    public OVRInput.Button restartButtonLeft = OVRInput.Button.Three;
    public OVRInput.Button lobbyButtonRight = OVRInput.Button.Two;
    public OVRInput.Button lobbyButtonLeft = OVRInput.Button.Four;
    public float restartDelay = 1.5f;
    public float returnToLobbyDelay = 5f;

    private CubeSpawnScripts spawner;
    private int score = 0;
    private float timeLeft;
    private float timeSurvived = 0f;
    private bool gameOver = false;
    private float gameOverTime;

    public bool IsGameOver => gameOver;

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
        }

        spawner = FindFirstObjectByType<CubeSpawnScripts>();
        timeLeft = gameDuration;

        finalPanel.SetActive(false);

        UpdateScoreUI();
        UpdateTimerUI();
    }

    private void Update()
    {
        if (gameOver)
        {
            CheckRestartInput();
            return;
        }

        if (endlessMode)
        {
            timeSurvived += Time.deltaTime;
        }
        else
        {
            timeLeft -= Time.deltaTime;

            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                UpdateTimerUI();
                EndGame();
                return;
            }
        }

        UpdateTimerUI();
    }

    public void AddPoints(int points)
    {
        if (gameOver)
            return;

        score += points;

        if (!allowNegativeScore && score < 0)
            score = 0;

        UpdateScoreUI();
    }

    public void GunCubeDestroyed()
    {
        AddPoints(gunCubePoints);
    }

    public void SwordCubeDestroyed()
    {
        AddPoints(swordCubePoints);
    }

    public void CubeMissed()
    {
        if (gameOver)
            return;

        if (endlessMode)
            EndGame();
        else
            AddPoints(-missedCubePenalty);
    }

    private void UpdateScoreUI()
    {
        currentScoreText.text = score.ToString();
    }

    private string FormatTime(int seconds)
    {
        return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }

    private void UpdateTimerUI()
    {
        if (endlessMode)
        {
            timeText.text = FormatTime(Mathf.FloorToInt(timeSurvived));
            timerFill.fillAmount = 1f;
            timerFill.color = wave3Color;
            return;
        }

        timeText.text = FormatTime(Mathf.CeilToInt(timeLeft));

        float elapsed = gameDuration - timeLeft;
        float wave1End = spawner.wave1Duration;
        float wave2End = spawner.wave1Duration + spawner.wave2Duration;

        float waveStart, waveEnd;

        if (elapsed < wave1End)
        {
            waveStart = 0f;
            waveEnd = wave1End;
            timerFill.color = wave1Color;
        }
        else if (elapsed < wave2End)
        {
            waveStart = wave1End;
            waveEnd = wave2End;
            timerFill.color = wave2Color;
        }
        else
        {
            waveStart = wave2End;
            waveEnd = gameDuration;
            timerFill.color = wave3Color;
        }

        timerFill.fillAmount = Mathf.Clamp01((waveEnd - elapsed) / (waveEnd - waveStart));
    }

    private void CheckRestartInput()
    {
        if (Time.time - gameOverTime < restartDelay)
            return;

        if (OVRInput.GetDown(restartButtonRight) || OVRInput.GetDown(restartButtonLeft))
            RestartGame();
        else if (OVRInput.GetDown(lobbyButtonRight) || OVRInput.GetDown(lobbyButtonLeft))
            BackToLobby();
    }

    private void EndGame()
    {
        gameOver = true;
        gameOverTime = Time.time;

        spawner.StopSpawning();

        foreach (CubePhysics cube in FindObjectsByType<CubePhysics>(FindObjectsSortMode.None))
            Destroy(cube.gameObject);

        string recordKey = endlessMode ? EndlessHighScoreKey : HighScoreKey;
        int highScore = PlayerPrefs.GetInt(recordKey, 0);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(recordKey, highScore);
            PlayerPrefs.Save();
        }

        finalScoreText.text = score.ToString();
        highScoreText.text = highScore.ToString();

        foreach (GameObject panel in hideOnGameOver)
            panel.SetActive(false);

        finalPanel.SetActive(true);

        Invoke(nameof(BackToLobby), returnToLobbyDelay);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToLobby()
    {
        SceneManager.LoadScene(lobbySceneIndex);
    }
}
