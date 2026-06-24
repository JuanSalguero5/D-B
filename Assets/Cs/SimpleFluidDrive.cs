using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFluidDrive : MonoBehaviour
{
    [Header("Conducción de Auto Real")]
    [Tooltip("Velocidad de avance simulado (afecta qué tan rápido giran las llantas y reacciona el auto)")]
    public float forwardSpeed = 30f;
    [Tooltip("Velocidad de respuesta del volante al girar la trompa")]
    public float turnSpeed = 45f;
    [Tooltip("Límite lateral de la carretera")]
    public float maxHorizontalWidth = 7f;

    [Header("Ángulos de Maniobra (Game Feel)")]
    [Tooltip("Inclinación lateral visual de las llantas delanteras (Eje Z) por transferencia de peso")]
    public float rotationLean = 5f;
    [Tooltip("Velocidad con la que el chasis se estabiliza")]
    public float rotationSmoothSpeed = 12f;

    [Header("Estructura del Escenario")]
    public Transform roadTransform;
    public float roadScrollSpeed = 30f;

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
    private float smoothLeanZ = 0f;     // Guarda la inclinación calculada de forma aislada

    void Start()
    {
        controller = GetComponent<CharacterController>();
        roadTransform = GameObject.Find("NombreDeTuCarretera")?.transform;
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

        // Calculamos la inclinación (Z) en una variable separada sin aplicarla al Transform principal
        float targetLeanZ = horizontalInput * rotationLean;
        smoothLeanZ = Mathf.LerpAngle(smoothLeanZ, targetLeanZ, rotationSmoothSpeed * Time.deltaTime);

        // ¡EL TRUCO!: Forzamos que el coche mantenga X y Z en 0 absolutos. Así el Character Controller jamás se volcará.
        transform.localRotation = Quaternion.Euler(0f, currentYawAngle, 0f);

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

        // Limitar la posición del carro estrictamente a los bordes de la carretera
        float clampedX = Mathf.Clamp(transform.position.x, -maxHorizontalWidth, maxHorizontalWidth);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);

        // 3. Animación Visual de las Ruedas (Aquí absorbe el balanceo estético)
        AnimarRuedas();

        // 4. Mover la carretera en bucle infinito
        if (roadTransform != null)
        {
            roadTransform.Translate(Vector3.back * roadScrollSpeed * Time.deltaTime, Space.World);

            if (roadTransform.position.z < -40f)
            {
                roadTransform.position = new Vector3(0, 0, 40f);
            }
        }
    }

    [Header("Ajustes Avanzados de Ruedas")]
    public float direccionGiroAvance = 1f;
    private float currentRotationX = 0f;

    private void AnimarRuedas()
    {
        // 1. Avanzar las llantas sobre su eje X local (simula velocidad del asfalto)
        currentRotationX += roadScrollSpeed * wheelSpinSpeed * direccionGiroAvance * Time.deltaTime;
        currentRotationX %= 360f;

        // 2. Ángulo del volante (Eje Y)
        float targetSteer = horizontalInput * maxSteerAngle;

        // RUEDAS DELANTERAS: Solo manejan Avance (X) y Dirección (Y). Z se congela en 0 estricto.
        foreach (Transform frontWheel in frontWheels)
        {
            if (frontWheel != null)
            {
                // Al clavar el eje Z en 0f, eliminamos de raíz cualquier bamboleo o desalineación
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