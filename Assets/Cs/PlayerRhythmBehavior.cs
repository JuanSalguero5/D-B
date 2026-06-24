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
///   - drivingScript.roadScrollSpeed (float): Modificación directa de la velocidad de traslación del escenario.
/// PRECONDICIÓN: 
///   - El GameManager debe estar en un estado activo de juego (GameState.Playing).
///   - El Modo Overdrive no debe estar previamente activo (!isOverdriveActive).
/// </summary>
public class PlayerRhythmBehavior : MonoBehaviour
{
    [Header("Configuración del Overdrive")]
    public float overdriveDuration = 3f;
    public bool isOverdriveActive = false;

    [Header("New Input System (Explícito)")]
    public InputActionReference jumpActionReference;

    private float overdriveTimer = 0f;
    public SimpleFluidDrive drivingScript;

    void Start()
    {
        // Obtenemos el script de conducción que ya tiene el coche
        drivingScript = GetComponent<SimpleFluidDrive>();
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
    /// CASO DE USO: Capturar la interrupción física del teclado y evaluar las reglas de negocio de juego para activar el multiplicador.
    /// DATOS DE ENTRADA: InputAction.CallbackContext context (Contexto del evento de input).
    /// DATOS DE SALIDA: Invoca el método ActivateOverdrive() si se cumplen las condiciones lógicas.
    /// PRECONDICIÓN: GameManager activo en Playing e inmunidad/estado previo inactivo.
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
    /// CASO DE USO: Transicionar el motor de juego al estado de velocidad rítmica duplicada y alterar los componentes de renderizado.
    /// DATOS DE ENTRADA: drivingScript.roadScrollSpeed original.
    /// DATOS DE SALIDA: Modifica drivingScript.roadScrollSpeed multiplicándolo por 2.
    /// PRECONDICIÓN: Evento de salto validado con éxito.
    /// </summary>
    private void ActivateOverdrive()
    {
        isOverdriveActive = true;
        overdriveTimer = overdriveDuration;

        if (drivingScript != null)
        {
            drivingScript.roadScrollSpeed *= 2f;
        }

        Debug.Log("¡Modo Overdrive Rítmico Activado!");
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: DeactivateOverdrive
    /// CASO DE USO: Retornar las variables de traslación del escenario a sus coeficientes de fricción y velocidad base del juego.
    /// DATOS DE ENTRADA: Coeficiente de velocidad alterado.
    /// DATOS DE SALIDA: Divide drivingScript.roadScrollSpeed entre 2.
    /// PRECONDICIÓN: El overdriveTimer llegó a cero (0f).
    /// </summary>
    private void DeactivateOverdrive()
    {
        isOverdriveActive = false;

        if (drivingScript != null)
        {
            drivingScript.roadScrollSpeed /= 2f;
        }

        Debug.Log("Overdrive Terminado.");
    }
}