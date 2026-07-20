using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Requerido para reiniciar la escena limpiamente

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: GameManager (Administrador del Ciclo de Vida y Estados del Juego)
/// CASO DE USO: Coordinar las transiciones de pantallas de la interfaz de usuario (Menú, HUD, Pausa, Game Over),
///              calcular el puntaje procedural en tiempo real y controlar la escala de tiempo del motor gráfico.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static bool debeIniciarJugando = false; // Bandera para saltarse el menú al reiniciar

    private const string HI_SCORE_KEY = "HighScore";

    public enum GameState { MainMenu, Playing, Paused, GameOver }
    [Header("Estado del Juego")]
    public GameState currentState;

    [Header("Paneles de la Interfaz (UI)")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;

    [Header("Componentes de Texto (TMPro)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI menuLastScoreText;
    public TextMeshProUGUI gameOverScoreText;

    [Header("Componentes del Juego")]
    public GameObject playerVehicle;
    public float currentScore = 0f;

    [Header("Mecánica de Combo (Anillas)")]
    public TextMeshProUGUI comboText;
    public int currentComboCount = 0;
    public int scoreMultiplier = 1;

    [Header("Configuración de Combo")]
    public float tiempoMaximoParaCombo = 2.0f; // Tiempo en segundos para perder el combo
    private float tiempoRestanteCombo = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        float savedHighScore = PlayerPrefs.GetFloat(HI_SCORE_KEY, 0f);
        UpdateMenuScoreText(savedHighScore);



        // --- CORRECCIÓN DE REINICIO ---
        if (debeIniciarJugando)
        {
            debeIniciarJugando = false;
            ActionIniciarJuego();
        }
        else
        {
            ChangeState(GameState.MainMenu);
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            currentScore += Time.deltaTime * 25f * scoreMultiplier;

            if (scoreText != null)
            {
                scoreText.text = "SCORE: " + Mathf.FloorToInt(currentScore).ToString("D5");

                // El texto del SCORE cambia de color según el multiplicador actual
                if (scoreMultiplier >= 2 && scoreMultiplier <= 4)
                    scoreText.color = Color.green;
                else if (scoreMultiplier >= 5 && scoreMultiplier <= 7)
                    scoreText.color = new Color(0f, 0.5f, 1f); // Azul
                else if (scoreMultiplier >= 8 && scoreMultiplier <= 10)
                    scoreText.color = new Color(0.6f, 0f, 1f); // Morado
                else
                    scoreText.color = Color.white;
            }

            // LÓGICA DEL CRONÓMETRO DE COMBO
            if (currentState == GameState.Playing && currentComboCount > 0)
            {
                tiempoRestanteCombo -= Time.deltaTime;

                if (tiempoRestanteCombo <= 0)
                {
                    ResetearComboPorFallo();
                }
            }

        }
       
    }

    public void AgregarAnillaCombo()
    {
        // Al agarrar, reiniciamos el cronómetro
        tiempoRestanteCombo = tiempoMaximoParaCombo;

        currentComboCount++;
        if (currentComboCount % 3 == 0 && scoreMultiplier < 10)
        {
            scoreMultiplier++;
        }
        ActualizarTextoComboUI();
    }

    private void ActualizarTextoComboUI()
    {
        if (comboText != null)
        {
            if (scoreMultiplier > 1)
            {
                comboText.text = $"Combo: x{scoreMultiplier}";

                if (scoreMultiplier >= 2 && scoreMultiplier <= 4)
                    comboText.color = Color.green;
                else if (scoreMultiplier >= 5 && scoreMultiplier <= 7)
                    comboText.color = new Color(0f, 0.5f, 1f);
                else if (scoreMultiplier >= 8 && scoreMultiplier <= 10)
                    comboText.color = new Color(0.6f, 0f, 1f);
            }
            else
            {
                comboText.text = "";
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        mainMenuPanel.SetActive(currentState == GameState.MainMenu);
        hudPanel.SetActive(currentState == GameState.Playing || currentState == GameState.Paused);
        pauseMenuPanel.SetActive(currentState == GameState.Paused);
        gameOverPanel.SetActive(currentState == GameState.GameOver);

        if (currentState == GameState.Paused || currentState == GameState.GameOver)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        // Control de Audio
        if (AudioManager.Instance != null)
        {
            if (newState == GameState.Playing)
            {
                AudioManager.Instance.ResumeMusic(); // Usa el nuevo método
                AudioManager.Instance.PlayGameMusic();
            }
            else if (newState == GameState.Paused || newState == GameState.GameOver)
            {
                AudioManager.Instance.SetMusicVolume(false);
            }
            else if (newState == GameState.MainMenu)
            {
                AudioManager.Instance.PlayMenuMusic();
            }
        }

        if (currentState == GameState.GameOver && gameOverScoreText != null)
        {
            float highScore = PlayerPrefs.GetFloat(HI_SCORE_KEY, 0f);

            // Verificamos si superamos el récord
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetFloat(HI_SCORE_KEY, highScore);
                PlayerPrefs.Save(); // Forzamos la escritura en disco
                Debug.Log("🏆 ¡NUEVO RÉCORD!");
            }

            gameOverScoreText.text = $"SCORE: {Mathf.FloorToInt(currentScore):D5}\nHI-SCORE: {Mathf.FloorToInt(highScore):D5}";
        }
    }

    // --- ACCIONES DE LOS BOTONES ---

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ActionIniciarJuego
    /// CASO DE USO: Inicializa el juego siempre desde cero, limpia posiciones y restablece la salud.
    /// </summary>
    public void ActionIniciarJuego()
    {
        currentScore = 0f;
        currentComboCount = 0;
        scoreMultiplier = 1;
        ActualizarTextoComboUI();

        if (playerVehicle != null)
        {
            // 1. RESTAURAR SALUD
            PlayerHealth playerHealth = playerVehicle.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ResetearSalud();
            }

            // 2. REGRESAR AL CENTRO (X = 0): Cada partida nueva o reinicio empieza limpio desde el centro
            playerVehicle.transform.position = new Vector3(0f, playerVehicle.transform.position.y, playerVehicle.transform.position.z);
        }

        ChangeState(GameState.Playing);
    }

    public void ActionPausar() => ChangeState(GameState.Paused);
    public void ActionReanudar() => ChangeState(GameState.Playing);

    public void ActionReiniciarJuego()
    {
        debeIniciarJugando = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ActionRegresarAlMenu
    /// CASO DE USO: Abandona la partida actual perdiendo todo el progreso y recarga la escena regresando al Menú Principal limpio.
    /// </summary>
    public void ActionRegresarAlMenu()
    {
        currentScore = 0f;
        currentComboCount = 0;
        scoreMultiplier = 1;

        // Aseguramos que la bandera esté en falso para que el Start() cargue el Menú Principal
        debeIniciarJugando = false;

        // Restablecemos el tiempo del motor antes de cargar la escena para evitar congelamientos
        Time.timeScale = 1f;

        float highScore = PlayerPrefs.GetFloat(HI_SCORE_KEY, 0f);
        UpdateMenuScoreText(highScore);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);


    }

    private void UpdateMenuScoreText(float score)
    {
        if (menuLastScoreText != null)
        {
            menuLastScoreText.text = "GREAT SCORE: " + Mathf.FloorToInt(score);
        }
    }


    public void ResetearComboPorFallo()
    {
        if (scoreMultiplier > 1 || currentComboCount > 0)
        {
            currentComboCount = 0;
            scoreMultiplier = 1;
            Debug.LogWarning("❌ [COMBO ROTO] Perdiste el ritmo de las anillas. Multiplicador reestablecido a 1x.");
            ActualizarTextoComboUI();
        }
    }

    // ELIMINADO: ActionSalirYGuardar(). Ya no es una opción guardar progreso intermedio de coordenadas o puntaje.

    public void ActionCerrarAplicacion() => Application.Quit();
}