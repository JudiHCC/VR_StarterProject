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
    // En acabar la partida: A (dreta) o X (esquerra) tornen a jugar,
    // B (dreta) o Y (esquerra) tornen al lobby.
    public bool restartWithControllerButton = true;
    public OVRInput.Button restartButtonRight = OVRInput.Button.One;
    public OVRInput.Button restartButtonLeft = OVRInput.Button.Three;
    public OVRInput.Button lobbyButtonRight = OVRInput.Button.Two;
    public OVRInput.Button lobbyButtonLeft = OVRInput.Button.Four;
    // Si és cert, el botó A/X també torna al lobby en lloc de reiniciar la partida.
    public bool restartGoesToLobby = false;

    [Header("Botons del panell final")]
    // Es creen sols copiant el BackButton del panell. Es premen disparant-los.
    public bool createEndButtons = true;
    public string playAgainLabel = "TORNAR A JUGAR (A)";
    public string lobbyLabel = "TORNAR AL LOBBY (B)";
    public string endHintText = "Dispara a un botó o prem A / B";
    // Separació horitzontal entre els dos botons (unitats del Canvas).
    public float endButtonsSpacing = 230f;
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
        else if (OVRInput.GetDown(lobbyButtonRight) || OVRInput.GetDown(lobbyButtonLeft))
        {
            BackToLobby();
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
        {
            finalPanel.SetActive(true);

            if (createEndButtons)
                CreateEndButtons();
        }

        if (debugLogs)
            Debug.Log("[Score] Fi de la partida. Score " + score + ", high score " + highScore);
    }

    // Crea "Tornar a jugar" i "Tornar al lobby" a sota de la puntuació,
    // fent servir el BackButton del panell com a plantilla (mateix estil).
    private void CreateEndButtons()
    {
        Transform template = finalPanel.transform.Find("BackButton");

        if (template == null)
        {
            Debug.LogWarning("[Score] No trobo el BackButton dins del panell final per fer els botons.");
            return;
        }

        RectTransform templateRect = (RectTransform)template;
        Vector2 basePosition = templateRect.anchoredPosition;

        CreateEndButton(template, "PlayAgainButton", playAgainLabel,
            basePosition + new Vector2(-endButtonsSpacing, 0f), RestartGame);
        CreateEndButton(template, "LobbyButton", lobbyLabel,
            basePosition + new Vector2(endButtonsSpacing, 0f), BackToLobby);

        // Text d'ajuda a sota dels botons.
        if (!string.IsNullOrEmpty(endHintText))
        {
            GameObject hint = new GameObject("EndHint", typeof(RectTransform));
            hint.layer = finalPanel.layer;
            hint.transform.SetParent(finalPanel.transform, false);

            RectTransform hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = templateRect.anchorMin;
            hintRect.anchorMax = templateRect.anchorMax;
            hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(1000f, 50f);
            hintRect.anchoredPosition = basePosition + new Vector2(0f, -templateRect.sizeDelta.y * templateRect.localScale.y * 0.5f - 40f);

            TextMeshProUGUI hintText = hint.AddComponent<TextMeshProUGUI>();
            hintText.text = endHintText;
            hintText.fontSize = 30f;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = new Color(1f, 1f, 1f, 0.85f);
        }
    }

    private void CreateEndButton(Transform template, string objectName, string label, Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        GameObject copy = Instantiate(template.gameObject, template.parent, false);
        copy.name = objectName;
        copy.SetActive(true);

        RectTransform rect = (RectTransform)copy.transform;
        rect.anchoredPosition = position;

        // La plantilla portava "BackToLobby" connectat: el traiem i posem l'acció nova.
        Button button = copy.GetComponent<Button>();

        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        TMP_Text text = copy.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
        {
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 6f;
            text.fontSizeMax = Mathf.Max(text.fontSize, 14f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // Collider perquè el tret de la pistola el pugui tocar.
        BoxCollider box = copy.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(rect.rect.width, rect.rect.height, 20f);
        box.center = new Vector3((0.5f - rect.pivot.x) * rect.rect.width, (0.5f - rect.pivot.y) * rect.rect.height, 0f);

        ShootableButton shootable = copy.AddComponent<ShootableButton>();
        shootable.button = button;
        shootable.activeAfter = Time.time + restartDelay;
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
