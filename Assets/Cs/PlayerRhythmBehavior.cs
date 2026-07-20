using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: PlayerRhythmBehavior (Controlador de Multiplicador Rítmico - Overdrive)
/// CASO DE USO: El jugador presiona una tecla de acción rítmica (barra espaciadora) en sincronía con el beat 
///              para duplicar temporalmente la velocidad de avance del entorno y ganar bonificaciones.
/// DATOS DE ENTRADA: 
///   - jumpActionReference (InputActionReference): Entrada de hardware del New Input System (Tecla Espacio).
///   - overdriveDuration (float): Tiempo de duración del estado alterado.
///   - GameManager.Instance.currentState (GameState): Estado lógico del bucle de juego general.
/// DATOS DE SALIDA: 
///   - isOverdriveActive (bool): Estado lógico global que notifica la activación del Overdrive.
///   - TrackManager.Instance.roadScrollSpeed (float): Modificación directa de la velocidad de traslación del escenario encapsulado.
/// PRECONDICIÓN: 
///   - El GameManager debe estar en un estado activo de juego (GameState.Playing).
///   - El Modo Overdrive no debe estar previamente activo (!isOverdriveActive).
/// </summary>
public class PlayerRhythmBehavior : MonoBehaviour
{
    [Header("Configuración del Overdrive")]
    public float overdriveDuration = 3f;
    public bool isOverdriveActive = false;
    public float speedMuliplier = 1.2f;

    [Header("New Input System (Explícito)")]
    public InputActionReference jumpActionReference;

    private float overdriveTimer = 0f;

    void Start()
    {
        // Ya no necesitamos buscar SimpleFluidDrive en el inicio ya que la pista está desacoplada
    }

    private void OnEnable()
    {
        if (jumpActionReference != null && jumpActionReference.action != null)
        {
            jumpActionReference.action.Enable();
            jumpActionReference.action.performed += OnJumpPerformed;
        }
    }

    private void OnDisable()
    {
        if (jumpActionReference != null && jumpActionReference.action != null)
        {
            jumpActionReference.action.performed -= OnJumpPerformed;
            jumpActionReference.action.Disable();
        }
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: OnJumpPerformed (Procesador de Entrada de Teclado)
    /// </summary>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("--> [INPUT SYSTEM] ¡Llamada explícita detectada en el código! <--");

        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Playing)
        {
            if (!isOverdriveActive)
            {
                ActivateOverdrive();
            }
        }
        else if (GameManager.Instance != null)
        {
            Debug.LogWarning("Input detectado, pero el GameManager no está en Playing.");
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // Manejo del temporizador del Overdrive
        if (isOverdriveActive)
        {
            overdriveTimer -= Time.deltaTime;
            if (overdriveTimer <= 0f)
            {
                DeactivateOverdrive();
            }
        }
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ActivateOverdrive
    /// CASO DE USO: Transicionar el motor de juego al estado de velocidad rítmica duplicada alterando el gestor de la pista.
    /// DATOS DE ENTRADA: TrackManager.Instance.roadScrollSpeed original.
    /// DATOS DE SALIDA: Modifica la velocidad del entorno multiplicándola por 2.
    /// PRECONDICIÓN: Evento de salto validado con éxito.
    /// </summary>
    private void ActivateOverdrive()
    {
        isOverdriveActive = true;
        overdriveTimer = overdriveDuration;

        if (TrackManager.Instance != null)
        {
            TrackManager.Instance.roadScrollSpeed *= speedMuliplier;
            Debug.Log($"¡Modo Overdrive Rítmico Activado! Nueva velocidad: {TrackManager.Instance.roadScrollSpeed}");
        }
        else
        {
            Debug.LogError("No se pudo activar el Overdrive porque no se encontró el TrackManager en la escena.");
        }
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: DeactivateOverdrive
    /// CASO DE USO: Retornar las variables de traslación del escenario a sus coeficientes de velocidad base original.
    /// DATOS DE ENTRADA: Coeficiente de velocidad alterado.
    /// DATOS DE SALIDA: Divide TrackManager.Instance.roadScrollSpeed entre 2.
    /// PRECONDICIÓN: El overdriveTimer llegó a cero (0f).
    /// </summary>
    private void DeactivateOverdrive()
    {
        isOverdriveActive = false;

        if (TrackManager.Instance != null)
        {
            TrackManager.Instance.roadScrollSpeed /= speedMuliplier;
            Debug.Log($"Overdrive Terminado. Velocidad restaurada a: {TrackManager.Instance.roadScrollSpeed}");
        }
    }
}