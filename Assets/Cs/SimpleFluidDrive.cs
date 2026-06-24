using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFluidDrive : MonoBehaviour
{
    [Header("Conducción Fluida")]
    public float steeringSpeed = 20f;
    public float maxHorizontalWidth = 7f;
    public float rotationLean = 12f;

    [Header("Estructura del Escenario")]
    public Transform roadTransform;
    public float roadScrollSpeed = 30f;

    [Header("Animación de Ruedas")]
    [Tooltip("Arrastra aquí los Transforms de las ruedas delanteras")]
    public Transform[] frontWheels;
    [Tooltip("Arrastra aquí TODAS las ruedas (Delanteras y Traseras) para que giren hacia adelante")]
    public Transform[] allWheels;
    [Tooltip("Ángulo máximo de giro hacia los lados al presionar dirección")]
    public float maxSteerAngle = 25f;
    [Tooltip("Velocidad de rotación visual de las llantas (simula el avance)")]
    public float wheelSpinSpeed = 200f;

    [Header("New Input System (Explícito)")]
    public InputActionReference moveActionReference;

    [Header("Físicas de Character Controller")]
    private CharacterController controller;
    private float verticalVelocity;
    private float gravity = 25f;

    private float horizontalInput;
    private float currentX;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        roadTransform = GameObject.Find("NombreDeTuCarretera")?.transform;
    }

    private void OnEnable()
    {
        if (moveActionReference != null && moveActionReference.action != null)
        {
            moveActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveActionReference != null && moveActionReference.action != null)
        {
            moveActionReference.action.Disable();
        }
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

        // 1. Movimiento lateral
        currentX += horizontalInput * steeringSpeed * Time.deltaTime;
        currentX = Mathf.Clamp(currentX, -maxHorizontalWidth, maxHorizontalWidth);
        transform.position = new Vector3(currentX, transform.position.y, transform.position.z);

        // 2. ROTACIÓN INTEGRAL (X fija en 0)
        float targetLean = -horizontalInput * rotationLean;
        float smoothLean = Mathf.LerpAngle(transform.localEulerAngles.z, targetLean, 10f * Time.deltaTime);

        // Cambiado a 0f en el eje X para el nuevo modelo
        transform.localRotation = Quaternion.Euler(0f, horizontalInput * 4f, smoothLean);

        // Gravedad
        if (!controller.isGrounded)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -1f;
        }

        Vector3 finalMovement = new Vector3(horizontalInput * steeringSpeed, verticalVelocity, 0f);
        controller.Move(finalMovement * Time.deltaTime);

        // 3. Animación Visual de las Ruedas
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
    [Tooltip("Ajusta este valor si las llantas giran al revés (asigna -1 o 1)")]
    public float direccionGiroAvance = 1f;

    private float currentRotationX = 0f;

    private void AnimarRuedas()
    {
        // 1. Calculamos la rotación acumulada para el avance (Eje X)
        currentRotationX += roadScrollSpeed * wheelSpinSpeed * direccionGiroAvance * Time.deltaTime;

        // Simplificamos el ángulo entre 0 y 360 grados para evitar desbordes de memoria
        currentRotationX %= 360f;

        // 2. Aplicar rotación visual de dirección y avance combinados
        float targetSteer = horizontalInput * maxSteerAngle;

        // RUEDAS DELANTERAS: Combinan el giro del avance (X) y el giro del volante (Y)
        foreach (Transform frontWheel in frontWheels)
        {
            if (frontWheel != null)
            {
                // Forzamos la rotación local exacta calculada en este frame evitando desfases acumulativos
                frontWheel.localRotation = Quaternion.Euler(currentRotationX, targetSteer, 0f);
            }
        }

        // RUEDAS TRASERAS: Solo giran hacia adelante (X), la dirección (Y) se congela en 0
        foreach (Transform wheel in allWheels)
        {
            if (wheel != null)
            {
                // Verificamos si no es una llanta delantera para no sobreescribir su volante
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