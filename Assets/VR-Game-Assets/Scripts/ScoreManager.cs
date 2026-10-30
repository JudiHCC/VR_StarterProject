using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Centralitza la puntuació, el temps de partida i els panells de la UI.
// Va a un objecte buit "ScoreManager" dins de [MANAGERS].
public class ScoreManager : MonoBehaviour
{
    public const string HighScoreKey = "HighScore";

    public static ScoreManager Instance { get; private set; }

    [Header("Partida")]
    // Durada de la partida en segons.
    public float gameDuration = 180f;
    public CubeSpawnScripts spawner;

    [Header("Punts")]
    public int gunCubePoints = 100;
    public int swordCubePoints = 100;
    // Punts que es resten quan un cub arriba al CubeKiller.
    public int missedCubePenalty = 50;
    public bool allowNegativeScore = false;

    [Header("UI_Timer")]
    public TMP_Text timeText;
    // PorgressBar_Fill (Image de tipus Filled).
    public Image timerFill;
    // Opcional, si es fa servir un Slider de Unity.
    public Slider timerSlider;
    // La barra mostra el temps que queda de l'onada actual (no de tota la partida).
    public bool progressBarPerWave = true;
    public Color wave1Color = new Color(0.2f, 0.85f, 0.25f);  // Verd
    public Color wave2Color = new Color(1f, 0.85f, 0.1f);     // Groc
    public Color wave3Color = new Color(0.95f, 0.2f, 0.15f);  // Vermell

    [Header("UI_ScoreCurrent")]
    public TMP_Text currentScoreText;

    [Header("UI_ScoreFinal")]
    public GameObject finalPanel;
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;

    [Header("Altres")]
    // Panells que s'amaguen en acabar la partida (UI_Timer, UI_ScoreCurrent).
    public GameObject[] hideOnGameOver;
    // Assigna la càmera VR a tots els Canvas que no en tinguin.
    public bool autoAssignCanvasCamera = true;
    public int lobbySceneIndex = 0;

    [Header("Tornar a jugar")]
    // En acabar la partida, A (dreta) o X (esquerra) reinicien sense haver de clicar la UI.
    public bool restartWithControllerButton = true;
    public OVRInput.Button restartButtonRight = OVRInput.Button.One;
    public OVRInput.Button restartButtonLeft = OVRInput.Button.Three;
    // Si és cert, el botó torna al lobby en lloc de reiniciar la partida.
    public bool restartGoesToLobby = false;
    // Segons d'espera abans d'acceptar el botó, per no reiniciar sense voler.
    public float restartDelay = 1.5f;

    public bool debugLogs = true;

    private int score = 0;
    private float timeLeft;
    private bool gameOver = false;
    private float gameOverTime;

    public int Score => score;
    public bool IsGameOver => gameOver;

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (autoAssignCanvasCamera)
            AssignCanvasCameras();

        if (spawner == null)
            spawner = FindFirstObjectByType<CubeSpawnScripts>();

        timeLeft = gameDuration;
        score = 0;

        if (finalPanel != null)
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

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            UpdateTimerUI();
            EndGame();
            return;
        }

        UpdateTimerUI();
    }

    // Els Canvas World Space necessiten la càmera VR per rebre els clics.
    private void AssignCanvasCameras()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("[Score] No hi ha cap càmera amb el tag MainCamera.");
            return;
        }

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
                canvas.worldCamera = cam;
        }
    }

    public void AddPoints(int points)
    {
        if (gameOver)
            return;

        score += points;

        if (!allowNegativeScore && score < 0)
            score = 0;

        UpdateScoreUI();

        if (debugLogs)
            Debug.Log("[Score] " + (points >= 0 ? "+" : "") + points + " -> " + score);
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
        AddPoints(-missedCubePenalty);
    }

    private void UpdateScoreUI()
    {
        if (currentScoreText != null)
            currentScoreText.text = score.ToString();
    }

    private void UpdateTimerUI()
    {
        int seconds = Mathf.CeilToInt(timeLeft);

        if (timeText != null)
            timeText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");

        float progress = gameDuration > 0f ? timeLeft / gameDuration : 0f;
        Color? barColor = null;

        if (progressBarPerWave && spawner != null)
        {
            // Inici i final de l'onada actual, en segons des del principi de la partida.
            float elapsed = gameDuration - timeLeft;
            float wave1End = spawner.wave1Duration;
            float wave2End = spawner.wave1Duration + spawner.wave2Duration;

            float waveStart, waveEnd;

            if (elapsed < wave1End)
            {
                waveStart = 0f;
                waveEnd = wave1End;
                barColor = wave1Color;
            }
            else if (elapsed < wave2End)
            {
                waveStart = wave1End;
                waveEnd = wave2End;
                barColor = wave2Color;
            }
            else
            {
                // L'onada 3 dura fins al final de la partida.
                waveStart = wave2End;
                waveEnd = gameDuration;
                barColor = wave3Color;
            }

            float waveLength = waveEnd - waveStart;
            progress = waveLength > 0f ? Mathf.Clamp01((waveEnd - elapsed) / waveLength) : 0f;
        }

        if (timerFill != null)
        {
            timerFill.fillAmount = progress;

            if (barColor.HasValue)
                timerFill.color = barColor.Value;
        }

        if (timerSlider != null)
        {
            timerSlider.value = Mathf.Lerp(timerSlider.minValue, timerSlider.maxValue, progress);

            if (barColor.HasValue && timerSlider.fillRect != null)
            {
                Image sliderFill = timerSlider.fillRect.GetComponent<Image>();

                if (sliderFill != null)
                    sliderFill.color = barColor.Value;
            }
        }
    }

    private void CheckRestartInput()
    {
        if (!restartWithControllerButton || Time.time - gameOverTime < restartDelay)
            return;

        if (OVRInput.GetDown(restartButtonRight) || OVRInput.GetDown(restartButtonLeft))
        {
            if (restartGoesToLobby)
                BackToLobby();
            else
                RestartGame();
        }
    }

    private void EndGame()
    {
        gameOver = true;
        gameOverTime = Time.time;

        if (spawner != null)
            spawner.StopSpawning();

        // Esborrem els cubs que queden perquè no restin punts.
        foreach (CubePhysics cube in FindObjectsByType<CubePhysics>(FindObjectsSortMode.None))
            Destroy(cube.gameObject);

        int highScore = GetHighScore();

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (finalScoreText != null)
            finalScoreText.text = score.ToString();

        if (highScoreText != null)
            highScoreText.text = highScore.ToString();

        if (hideOnGameOver != null)
        {
            foreach (GameObject panel in hideOnGameOver)
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }

        if (finalPanel != null)
            finalPanel.SetActive(true);

        if (debugLogs)
            Debug.Log("[Score] Fi de la partida. Score " + score + ", high score " + highScore);
    }

    // Torna a carregar la GameScene des de zero.
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Per al BackButton del panell final.
    public void BackToLobby()
    {
        SceneManager.LoadScene(lobbySceneIndex);
    }
}
