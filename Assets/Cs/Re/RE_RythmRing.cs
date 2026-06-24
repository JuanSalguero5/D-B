using UnityEngine;
using TMPro; // Requerido para manipular el texto flotante

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: RE_RhythmRing (Reto de Anillas con Feedback Dinámico de Color)
/// CASO DE USO: El jugador recolecta una anilla; el script calcula el color correspondiente al rango 
///              del nuevo combo (Verde, Azul o Morado) y actualiza un texto flotante antes de reposicionarse.
/// DATOS DE ENTRADA:
///   - floatingComboText (TextMeshPro): Componente de texto flotante en la anilla.
///   - GameManager.Instance.scoreMultiplier (int): Multiplicador actual del juego.
/// DATOS DE SALIDA:
///   - Modificación cromática y de cadena del texto flotante en la escena 3D.
/// PRECONDICIÓN:
///   - El jugador debe activar el Trigger de la anilla.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RE_RhythmRing : MonoBehaviour
{
    [Header("Configuración de Reto y Movimiento")]
    public float minSpawnX = -6f;
    public float maxSpawnX = 6f;
    public float resetZPosition = 40f;
    public float despawnZPosition = -10f;

    [Header("Feedback Visual Dinámico")]
    [Tooltip("Arrastra aquí el TextMeshPro que flotará sobre la anilla")]
    public TextMeshPro floatingComboText;

    private float currentSpeed = 30f;

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        SimpleFluidDrive playerDrive = Object.FindAnyObjectByType<SimpleFluidDrive>();
        if (playerDrive != null)
        {
            currentSpeed = playerDrive.roadScrollSpeed;
        }

        transform.Translate(Vector3.back * currentSpeed * Time.deltaTime, Space.World);

        if (transform.position.z < despawnZPosition)
        {
            PenalizarComboOPasarDeLargo();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        if (other.CompareTag("Player"))
        {
            // 1. Notificar al GameManager para que aumente el combo global
            GameManager.Instance.AgregarAnillaCombo();

            // 2. Mostrar el combo flotante antes de mover la anilla al fondo
            MostrarFeedbackFlotante();

            // 3. Reciclar a la coordenada inicial
            ReciclarAnilla();
        }
    }

    private void MostrarFeedbackFlotante()
    {
        if (floatingComboText != null)
        {
            int multiplicadorActual = GameManager.Instance.scoreMultiplier;
            int comboActual = GameManager.Instance.currentComboCount;

            // Definimos el texto (Ej: "+3 Combo! x2")
            floatingComboText.text = $"+{comboActual} Combo!\nx{multiplicadorActual}";

            // Aplicamos las reglas de color solicitadas según el multiplicador
            if (multiplicadorActual >= 2 && multiplicadorActual <= 4)
            {
                floatingComboText.color = Color.green; // Verde para 2x - 4x
            }
            else if (multiplicadorActual >= 5 && multiplicadorActual <= 7)
            {
                floatingComboText.color = new Color(0f, 0.5f, 1f); // Azul vibrante para 5x - 7x
            }
            else if (multiplicadorActual >= 8 && multiplicadorActual <= 10)
            {
                floatingComboText.color = new Color(0.6f, 0f, 1f); // Morado/Púrpura para 8x - 10x
            }
            else
            {
                floatingComboText.color = Color.white; // Blanco por defecto (1x)
            }
        }
    }

    private void PenalizarComboOPasarDeLargo()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetearComboPorFallo();
        }

        // Si se pasa de largo, limpiamos el texto flotante para que no reaparezca con datos viejos
        if (floatingComboText != null) floatingComboText.text = "";

        ReciclarAnilla();
    }

    private void ReciclarAnilla()
    {
        float randomX = Random.Range(minSpawnX, maxSpawnX);
        transform.position = new Vector3(randomX, transform.position.y, resetZPosition);
    }
}