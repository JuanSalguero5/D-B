using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleFluidDrive : MonoBehaviour
{
    [Header("Conducción de Auto Real")]
    [Tooltip("Velocidad de avance simulado (afecta qué tan rápido giran las llantas y reacciona el auto)")]
    public float forwardSpeed = 30f;
    [Tooltip("Velocidad de respuesta del volante al girar la trompa")]
    public float turnSpeed = 45f;

    [Header("Ángulos de Maniobra (Game Feel)")]
    [Tooltip("Inclinación lateral visual de las llantas delanteras (Eje Z) por transferencia de peso")]
    public float rotationLean = 5f;
    [Tooltip("Velocidad con la que el chasis se estabiliza")]
    public float rotationSmoothSpeed = 12f;

    [Header("Animación Procedural de Salto (Pitch)")]
    [Tooltip("Ángulo máximo que se inclina la trompa hacia arriba/abajo al saltar")]
    public float maxJumpLeanAngle = 15f;
    [Tooltip("Velocidad de suavizado para la rotación de salto")]
    public float leanSmoothSpeed = 8f;
    [Tooltip("Arrastra aquí el modelo visual (hijo) del coche si quieres inclinar solo la malla. Si se deja vacío, se aplica a la raíz.")]
    public Transform modeloVisualCoche;

    [Header("Animación de Ruedas")]
    public Transform[] frontWheels;
    public Transform[] allWheels;
    public float maxSteerAngle = 25f;
    public float wheelSpinSpeed = 200f;

    [Header("New Input System (Explícito)")]
    public InputActionReference moveActionReference;

    [Header("Físicas de Character Controller")]
    private CharacterController controller;
    private float verticalVelocity;
    private float gravity = 25f;

    private float horizontalInput;
    private float currentYawAngle = 0f; // Guarda la rotación real de la trompa

    [Header("Ajustes Avanzados de Ruedas")]
    public float direccionGiroAvance = 1f;
    private float currentRotationX = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentYawAngle = transform.localEulerAngles.y;
    }

    private void OnEnable()
    {
        if (moveActionReference != null && moveActionReference.action != null)
            moveActionReference.action.Enable();
    }

    private void OnDisable()
    {
        if (moveActionReference != null && moveActionReference.action != null)
            moveActionReference.action.Disable();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // --- LECTURA DEL INPUT ---
        if (moveActionReference != null && moveActionReference.action != null)
        {
            Vector2 inputVector = moveActionReference.action.ReadValue<Vector2>();
            horizontalInput = inputVector.x;
        }

        // 1. ROTACIÓN DE LA TROMPA (Eje Y)
        currentYawAngle += horizontalInput * turnSpeed * Time.deltaTime;

        if (horizontalInput == 0f)
        {
            currentYawAngle = Mathf.MoveTowardsAngle(currentYawAngle, 0f, turnSpeed * 1.5f * Time.deltaTime);
        }
        currentYawAngle = Mathf.Clamp(currentYawAngle, -30f, 30f);

        // 2. CÁLCULO DEL MOVIMIENTO VECTORIAL (Hacia donde mira la trompa)
        Vector3 forwardMovement = transform.forward * forwardSpeed;

        // Procesar gravedad
        if (!controller.isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -1f;
        }

        // Mover el Character Controller plano en base a la dirección frontal
        Vector3 finalVelocity = new Vector3(forwardMovement.x, verticalVelocity, 0f);
        controller.Move(finalVelocity * Time.deltaTime);

        // 3. ANIMACIÓN DE ROTACIÓN PROCEDURAL (YAW Y PITCH DE SALTO)
        CalcularInclinacionYRotacion();

        // 4. Animación Visual de las Ruedas
        AnimarRuedas();
    }

    private void CalcularInclinacionYRotacion()
    {
        float targetPitchAngle = 0f;

        // Si no está tocando el suelo, calculamos el ángulo de cabeceo (Pitch) según el vector de caída/subida
        if (!controller.isGrounded)
        {
            float ratioVelocidad = verticalVelocity / 15f;
            targetPitchAngle = -ratioVelocidad * maxJumpLeanAngle;
            targetPitchAngle = Mathf.Clamp(targetPitchAngle, -maxJumpLeanAngle, maxJumpLeanAngle);
        }

        // Si tienes asignada la malla por separado, rotamos localmente el hijo en X (Salto), y la raíz en Y (Giro)
        if (modeloVisualCoche != null)
        {
            Quaternion targetJumpRot = Quaternion.Euler(targetPitchAngle, 0f, 0f);
            modeloVisualCoche.localRotation = Quaternion.Lerp(modeloVisualCoche.localRotation, targetJumpRot, Time.deltaTime * leanSmoothSpeed);

            transform.localRotation = Quaternion.Euler(0f, currentYawAngle, 0f);
        }
        else
        {
            // Combinación directa en el objeto raíz para evitar volcar el CharacterController de forma física
            Quaternion targetTotalRot = Quaternion.Euler(targetPitchAngle, currentYawAngle, 0f);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetTotalRot, Time.deltaTime * leanSmoothSpeed);
        }
    }

    private void AnimarRuedas()
    {
        float currentScrollSpeed = TrackManager.Instance != null ? TrackManager.Instance.roadScrollSpeed : 30f;

        // 1. Avanzar las llantas sobre su eje X local (simula velocidad del asfalto)
        currentRotationX += currentScrollSpeed * wheelSpinSpeed * direccionGiroAvance * Time.deltaTime;
        currentRotationX %= 360f;

        // 2. Ángulo del volante (Eje Y)
        float targetSteer = horizontalInput * maxSteerAngle;

        // RUEDAS DELANTERAS: Solo manejan Avance (X) y Dirección (Y). Z se congela en 0 estricto.
        foreach (Transform frontWheel in frontWheels)
        {
            if (frontWheel != null)
            {
                frontWheel.localRotation = Quaternion.Euler(currentRotationX, targetSteer, 0f);
            }
        }

        // RUEDAS TRASERAS: Solo manejan Avance (X). Y y Z se congelan en 0 estricto.
        foreach (Transform wheel in allWheels)
        {
            if (wheel != null)
            {
                bool esDelantera = System.Array.Exists(frontWheels, x => x == wheel);
                if (!esDelantera)
                {
                    wheel.localRotation = Quaternion.Euler(currentRotationX, 0f, 0f);
                }
            }
        }
    }

    public void EjecutarSaltoRampa(float fuerzaSalto)
    {
        verticalVelocity = fuerzaSalto;
    }
}