using UnityEngine;
using UnityEngine.UI;
using TMPro; // 1. OBLIGATORIO: Agrega esta librería para usar TextMeshPro

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused }
    public GameState currentState;

    [Header("Paneles de la Interfaz (UI)")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject pauseMenuPanel;

    [Header("Componentes de Texto (TMPro)")]
    public TextMeshProUGUI scoreText; 

    [Header("Componentes del Juego")]
    public GameObject playerVehicle;
    public float currentScore = 0f;

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
        ChangeState(GameState.MainMenu);
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            // El puntaje sube con el tiempo rítmicamente 
            currentScore += Time.deltaTime * 25f;

            // 3. ACTUALIZACIÓN EN TIEMPO REAL: Dibujar el puntaje en la pantalla sin decimales
            if (scoreText != null)
            {
                scoreText.text = "SCORE: " + Mathf.FloorToInt(currentScore).ToString("D5");
               
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        mainMenuPanel.SetActive(currentState == GameState.MainMenu);
        hudPanel.SetActive(currentState == GameState.Playing || currentState == GameState.Paused);
        pauseMenuPanel.SetActive(currentState == GameState.Paused);

        if (currentState == GameState.Paused) Time.timeScale = 0f;
        else Time.timeScale = 1f;
    }

    public void ActionIniciarJuego()
    {
        currentScore = 0f;
        ChangeState(GameState.Playing);
    }

    public void ActionPausar() => ChangeState(GameState.Paused);
    public void ActionReanudar() => ChangeState(GameState.Playing);

    public void ActionSalirYGuardar()
    {
        if (playerVehicle != null)
        {
            PlayerPrefs.SetFloat("SavedPosX", playerVehicle.transform.position.x);
            PlayerPrefs.SetFloat("SavedScore", currentScore);
            PlayerPrefs.Save();
        }
        ChangeState(GameState.MainMenu);
    }

    private void LoadGameProgress()
    {
        if (playerVehicle != null)
        {
            float savedX = PlayerPrefs.GetFloat("SavedPosX", 0f);
            playerVehicle.transform.position = new Vector3(savedX, playerVehicle.transform.position.y, playerVehicle.transform.position.z);
            currentScore = PlayerPrefs.GetFloat("SavedScore", 0f);
        }
    }

    public void ActionCerrarAplicacion() => Application.Quit();
}