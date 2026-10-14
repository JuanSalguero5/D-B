using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleFluidDrive : MonoBehaviour
{
    [Header("Conducci�n de Auto Real")]
    [Tooltip("Velocidad de avance simulado (afecta qu� tan r�pido giran las llantas y reacciona el auto)")]
    public float forwardSpeed = 30f;
    [Tooltip("Velocidad de respuesta del volante al girar la trompa")]
    public float turnSpeed = 45f;

    [Header("�ngulos de Maniobra (Game Feel)")]
    [Tooltip("Inclinaci�n lateral visual de las llantas delanteras (Eje Z) por transferencia de peso")]
    public float rotationLean = 5f;
    [Tooltip("Velocidad con la que el chasis se estabiliza")]
    public float rotationSmoothSpeed = 12f;

    [Header("Animaci�n Procedural de Salto (Pitch)")]
    [Tooltip("�ngulo m�ximo que se inclina la trompa hacia arriba/abajo al saltar")]
    public float maxJumpLeanAngle = 15f;
    [Tooltip("Velocidad de suavizado para la rotaci�n de salto")]
    public float leanSmoothSpeed = 8f;
    [Tooltip("Arrastra aqu� el modelo visual (hijo) del coche si quieres inclinar solo la malla. Si se deja vac�o, se aplica a la ra�z.")]
    public Transform modeloVisualCoche;

    [Header("Animaci�n de Ruedas")]
    public Transform[] frontWheels;
    public Transform[] allWheels;
    public float maxSteerAngle = 25f;
    public float wheelSpinSpeed = 200f;

    [Header("New Input System (Expl�cito)")]
    public InputActionReference moveActionReference;

    [Header("F�sicas de Character Controller")]
    private CharacterController controller;
    private float verticalVelocity;
    private float gravity = 25f;

    private float horizontalInput;
    public float HorizontalInput => horizontalInput; // Para efectos (partículas de giro)
    private float currentYawAngle = 0f; // Guarda la rotaci�n real de la trompa

    [Header("Ajustes Avanzados de Ruedas")]
    public float direccionGiroAvance = 1f;
    private float currentRotationX = 0f;

    [Header("Rebote contra Paredes (Tag Bounce)")]
    [Tooltip("Velocidad lateral con la que el coche rebota alejándose de la pared")]
    public float bounceVelocidad = 8f;
    [Tooltip("Impulso vertical del rebote (pequeño salto contra el muro)")]
    public float bounceImpulsoVertical = 4f;
    [Tooltip("Duración en segundos en la que el jugador pierde el control del coche")]
    public float tiempoPerderControl = 0.8f;
    [Tooltip("Velocidad de giro sin control durante el rebote (grados por segundo)")]
    public float velocidadSpin = 540f;
    [Tooltip("Qué tan rápido frena el impulso lateral del rebote")]
    public float deceleracionRebote = 30f;

    private bool perdiendoControl = false;
    private float controlTimer = 0f;
    private float spinDirection = 1f;
    private float bounceVelocidadLateral = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentYawAngle = transform.localEulerAngles.y;
        perdiendoControl = false;
        controlTimer = 0f;
        bounceVelocidadLateral = 0f;
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

        // 1. ROTACI�N DE LA TROMPA (Eje Y)
        if (perdiendoControl)
        {
            // Durante el rebote la trompa gira sin control del jugador
            currentYawAngle += spinDirection * velocidadSpin * Time.deltaTime;
            controlTimer -= Time.deltaTime;
            if (controlTimer <= 0f)
            {
                perdiendoControl = false;
            }
        }
        else
        {
            currentYawAngle += horizontalInput * turnSpeed * Time.deltaTime;

            if (horizontalInput == 0f)
            {
                currentYawAngle = Mathf.MoveTowardsAngle(currentYawAngle, 0f, turnSpeed * 1.5f * Time.deltaTime);
            }
            currentYawAngle = Mathf.Clamp(currentYawAngle, -30f, 30f);
        }

        // 2. C�LCULO DEL MOVIMIENTO VECTORIAL (Hacia donde mira la trompa)
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

        // Mover el Character Controller plano en base a la direcci�n frontal
        Vector3 finalVelocity;
        if (perdiendoControl)
        {
            // El coche rebota lateralmente alejándose de la pared y frena poco a poco
            bounceVelocidadLateral = Mathf.MoveTowards(bounceVelocidadLateral, 0f, deceleracionRebote * Time.deltaTime);
            finalVelocity = new Vector3(bounceVelocidadLateral, verticalVelocity, 0f);
        }
        else
        {
            finalVelocity = new Vector3(forwardMovement.x, verticalVelocity, 0f);
        }
        controller.Move(finalVelocity * Time.deltaTime);

        // 3. ANIMACI�N DE ROTACI�N PROCEDURAL (YAW Y PITCH DE SALTO)
        CalcularInclinacionYRotacion();

        // 4. Animaci�n Visual de las Ruedas
        AnimarRuedas();
    }

    private void CalcularInclinacionYRotacion()
    {
        float targetPitchAngle = 0f;

        // Si no est� tocando el suelo, calculamos el �ngulo de cabeceo (Pitch) seg�n el vector de ca�da/subida
        if (!controller.isGrounded)
        {
            float ratioVelocidad = verticalVelocity / 15f;
            targetPitchAngle = -ratioVelocidad * maxJumpLeanAngle;
            targetPitchAngle = Mathf.Clamp(targetPitchAngle, -maxJumpLeanAngle, maxJumpLeanAngle);
        }

        // Si tienes asignada la malla por separado, rotamos localmente el hijo en X (Salto), y la ra�z en Y (Giro)
        if (modeloVisualCoche != null)
        {
            Quaternion targetJumpRot = Quaternion.Euler(targetPitchAngle, 0f, 0f);
            modeloVisualCoche.localRotation = Quaternion.Lerp(modeloVisualCoche.localRotation, targetJumpRot, Time.deltaTime * leanSmoothSpeed);

            transform.localRotation = Quaternion.Euler(0f, currentYawAngle, 0f);
        }
        else
        {
            // Combinaci�n directa en el objeto ra�z para evitar volcar el CharacterController de forma f�sica
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

        // 2. �ngulo del volante (Eje Y)
        float targetSteer = horizontalInput * maxSteerAngle;

        // RUEDAS DELANTERAS: Solo manejan Avance (X) y Direcci�n (Y). Z se congela en 0 estricto.
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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        if (perdiendoControl) return;

        if (hit.collider != null && hit.collider.CompareTag("Bounce"))
        {
            // Dirección que aleja el coche de la pared (normal del contacto)
            float direccion;
            if (hit.normal.x != 0f)
            {
                direccion = Mathf.Sign(hit.normal.x);
            }
            else
            {
                direccion = transform.position.x >= 0f ? -1f : 1f;
            }

            bounceVelocidadLateral = direccion * bounceVelocidad;
            verticalVelocity = bounceImpulsoVertical;
            spinDirection = Random.value < 0.5f ? 1f : -1f;
            controlTimer = tiempoPerderControl;
            perdiendoControl = true;

            // Sacudida de cámara por el impacto contra el muro
            CameraShake shake = GetComponentInParent<CameraShake>();
            if (shake == null) shake = gameObject.AddComponent<CameraShake>();
            if (shake != null) shake.Sacudir();

            Debug.Log("BANG! [REBOTE] Chocaste contra el muro Bounce. Perdiste el control del coche.");
        }
    }
}