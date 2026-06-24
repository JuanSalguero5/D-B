using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Requerido para reiniciar la escena limpiamente

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: GameManager (Administrador del Ciclo de Vida y Estados del Juego)
/// CASO DE USO: Coordinar las transiciones de pantallas de la interfaz de usuario (Menú, HUD, Pausa, Game Over),
///              calcular el puntaje procedural en tiempo real y controlar la escala de tiempo del motor gráfico.
/// DATOS DE ENTRADA:
///   - currentScore (float): Escalar acumulativo del puntaje basado en tiempo.
///   - playerVehicle (GameObject): Referencia al chasis para leer transformaciones.
/// DATOS DE SALIDA:
///   - currentState (GameState): Estado lógico actual del juego.
///   - Activación/Desactivación de los Canvas de UI (`mainMenuPanel`, `hudPanel`, `pauseMenuPanel`, `gameOverPanel`).
/// PRECONDICIÓN:
///   - Debe existir una única instancia (Patrón Singleton) en la escena.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static bool debeIniciarJugando = false; // Bandera para saltarse el menú al reiniciar

    // Añadimos GameOver a la estructura de estados
    public enum GameState { MainMenu, Playing, Paused, GameOver }
    [Header("Estado del Juego")]
    public GameState currentState;

    [Header("Paneles de la Interfaz (UI)")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel; // NUEVO: Panel de derrota

    [Header("Componentes de Texto (TMPro)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI menuLastScoreText;
    public TextMeshProUGUI gameOverScoreText; // NUEVO: Puntaje final en pantalla de derrota

    [Header("Componentes del Juego")]
    public GameObject playerVehicle;
    public float currentScore = 0f;

    [Header("Mecánica de Combo (Anillas)")]
    public TextMeshProUGUI comboText; // NUEVO: Texto en el HUD para el Multiplicador (ej: "COMBO X5")
    public int currentComboCount = 0;
    public int scoreMultiplier = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("SavedPosX"))
        {
            LoadGameProgress();
        }
        else
        {
            UpdateMenuScoreText(0f);
        }

        // --- CORRECCIÓN DE REINICIO AQUÍ ---
        if (debeIniciarJugando)
        {
            debeIniciarJugando = false; // La bajamos para la próxima vez
            ActionIniciarJuego();       // Nos saltamos el menú y rellenamos vida/puntos de una
        }
        else
        {
            ChangeState(GameState.MainMenu); // Comportamiento normal
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

                // NUEVO: El texto del SCORE cambia de color según el multiplicador actual
                if (scoreMultiplier >= 2 && scoreMultiplier <= 4)
                    scoreText.color = Color.green;
                else if (scoreMultiplier >= 5 && scoreMultiplier <= 7)
                    scoreText.color = new Color(0f, 0.5f, 1f); // Azul
                else if (scoreMultiplier >= 8 && scoreMultiplier <= 10)
                    scoreText.color = new Color(0.6f, 0f, 1f); // Morado
                else
                    scoreText.color = Color.white; // Reseteo a blanco si pierde el combo
            }
        }
    }

    private void ActualizarTextoComboUI()
    {
        if (comboText != null)
        {
            if (scoreMultiplier > 1)
            {
                comboText.text = $"MULTIPLIER: x{scoreMultiplier} (Combo: {currentComboCount})";

                // NUEVO: El texto de Combo en el HUD también se pinta con la misma paleta rítmica
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

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ChangeState (Máquina de Estados de la UI y Motor)
    /// CASO DE USO: Alternar de forma segura la visibilidad de los paneles de juego y congelar el motor físico en estados de pausa/derrota.
    /// DATOS DE ENTRADA: GameState newState.
    /// DATOS DE SALIDA: Time.timeScale (0f o 1f) y asignación booleana de SetActive en objetos gráficos.
    /// PRECONDICIÓN: Recibir un estado válido del enum GameState.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        currentState = newState;

        // Evaluación jerárquica de visibilidad de paneles
        mainMenuPanel.SetActive(currentState == GameState.MainMenu);
        hudPanel.SetActive(currentState == GameState.Playing || currentState == GameState.Paused);
        pauseMenuPanel.SetActive(currentState == GameState.Paused);
        gameOverPanel.SetActive(currentState == GameState.GameOver); // Control del panel nuevo

        // Si está en pausa o perdió, congelamos el flujo de tiempo del juego
        if (currentState == GameState.Paused || currentState == GameState.GameOver)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;

        // Si caemos en Game Over, volcamos el puntaje final obtenido
        if (currentState == GameState.GameOver && gameOverScoreText != null)
        {
            gameOverScoreText.text = "HI - SCORE: " + Mathf.FloorToInt(currentScore).ToString("D5");
        }
    }

    // --- ACCIONES DE LOS BOTONES ---

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ActionIniciarJuego (Botón Jugar / Reintentar desde Menú)
    /// CASO DE USO: Inicializa el puntaje en cero, restablece la salud del vehículo, limpia su posición espacial 
    ///              devolviéndolo al origen (X = 0) para evitar bugs de persistencia tras perder, y activa el bucle de juego.
    /// DATOS DE ENTRADA: playerVehicle (GameObject), PlayerPrefs de posición.
    /// DATOS DE SALIDA: Coordenada transform.position del jugador restablecida y salud al 100%.
    /// PRECONDICIÓN: El juego debe estar en el estado MainMenu.
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


            // 2. EVITAR LA TRAMPA DE POSICIÓN:
            // Si el jugador tiene un progreso guardado real por haber usado "Salir y Guardar", lo cargamos.
            if (PlayerPrefs.HasKey("SavedPosX"))
            {
                LoadGameProgress();
            }
            else
            {
                // Si viene de perder normalmente, lo reseteamos al centro de la pista (X = 0)
                playerVehicle.transform.position = new Vector3(0f, playerVehicle.transform.position.y, playerVehicle.transform.position.z);
            }
        }

        ChangeState(GameState.Playing);
    }

    public void ActionPausar() => ChangeState(GameState.Paused);
    public void ActionReanudar() => ChangeState(GameState.Playing);

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ActionReiniciarJuego (Botón Reintentar)
    /// CASO DE USO: El jugador presiona 'Reintentar' en la pantalla de perder; activa la bandera de salto de menú,
    ///              restablece la escala de tiempo y recarga la escena para empezar a jugar de inmediato.
    /// DATOS DE ENTRADA: Índice de la escena activa.
    /// DATOS DE SALIDA: Carga de la escena y asignación de debeIniciarJugando = true.
    /// PRECONDICIÓN: El estado de juego debe ser GameState.GameOver.
    /// </summary>
    public void ActionReiniciarJuego()
    {
        debeIniciarJugando = true; // <-- Le avisamos al siguiente Start() que vamos directo a jugar
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ActionRegresarAlMenu (Botón Menú)
    /// CASO DE USO: Cancelar la partida tras perder y retornar de manera segura al lienzo del Menú Principal reestableciendo valores base.
    /// DATOS DE ENTRADA: Ninguno.
    /// DATOS DE SALIDA: Transición lógica a GameState.MainMenu.
    /// PRECONDICIÓN: El juego debe encontrarse en estado de GameOver o Pausa.
    /// </summary>
    public void ActionRegresarAlMenu()
    {
        currentScore = 0f;

        // Al regresar al menú tras perder, borramos las llaves temporales para que la próxima partida sea 100% nueva
        if (currentState == GameState.GameOver)
        {
            PlayerPrefs.DeleteKey("SavedPosX");
            PlayerPrefs.DeleteKey("SavedScore");
        }

        ChangeState(GameState.MainMenu);
    }
    public void ActionSalirYGuardar()
    {
        if (playerVehicle != null)
        {
            PlayerPrefs.SetFloat("SavedPosX", playerVehicle.transform.position.x);
            PlayerPrefs.SetFloat("SavedScore", currentScore);
            PlayerPrefs.Save();

            UpdateMenuScoreText(currentScore);
        }
        ChangeState(GameState.MainMenu);
    }

    private void LoadGameProgress()
    {
        if (playerVehicle != null)
        {
            float savedX = PlayerPrefs.GetFloat("SavedPosX", 0f);
            playerVehicle.transform.position = new Vector3(savedX, playerVehicle.transform.position.y, playerVehicle.transform.position.z);

            float savedScore = PlayerPrefs.GetFloat("SavedScore", 0f);
            currentScore = savedScore;

            UpdateMenuScoreText(savedScore);
        }
    }

    private void UpdateMenuScoreText(float score)
    {
        if (menuLastScoreText != null)
        {
            menuLastScoreText.text = "GREAT SCORE: " + Mathf.FloorToInt(score);
        }
    }


    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: AgregarAnillaCombo (Procesador de Recompensas de Nivel)
    /// CASO DE USO: Incrementar el contador de combo rítmico. Cada 3 anillas perfectas, sube el multiplicador
    ///              de puntaje en x1 hasta un tope máximo de 10x. Actualiza el HUD de forma inmediata.
    /// </summary>
    public void AgregarAnillaCombo()
    {
        currentComboCount++;

        // Regla de negocio: Cada 3 anillas recolectadas, subimos un escalón del multiplicador
        if (currentComboCount % 3 == 0 && scoreMultiplier < 10)
        {
            scoreMultiplier++;
            Debug.Log($"🔥 ¡MULTIPLICADOR DE NIVEL SUBIÓ! Ahora ganas: {scoreMultiplier}x puntos.");
        }

        ActualizarTextoComboUI();
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ResetearComboPorFallo (Mecánica de Penalización de Interfaz)
    /// CASO DE USO: El jugador esquiva o no logra pasar por dentro del aro rítmico, lo que provoca la pérdida
    ///              inmediata del multiplicador acumulado regresando al estado base (1x).
    /// </summary>
    public void ResetearComboPorFallo()
    {
        // Solo penalizamos si el jugador ya tenía un combo activo para no saturar la consola
        if (scoreMultiplier > 1 || currentComboCount > 0)
        {
            currentComboCount = 0;
            scoreMultiplier = 1;
            Debug.LogWarning("❌ [COMBO ROTO] Perdiste el ritmo de las anillas. Multiplicador reestablecido a 1x.");
            ActualizarTextoComboUI();
        }
    }

    public void ActionCerrarAplicacion() => Application.Quit();
}