using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFluidDrive : MonoBehaviour
{
    [Header("Conducción Fluida")]
    public float steeringSpeed = 20f;
    public float maxHorizontalWidth = 7f;
    public float rotationLean = 12f;

    [Header("Ajuste de Ejes (Fix Rotación)")]
    [Tooltip("Rotación inicial en X que necesita el modelo para no estar de pie")]
    public float baseRotationX = -90f;

    [Header("Estructura del Escenario")]
    public Transform roadTransform;
    public float roadScrollSpeed = 30f;

    private float horizontalInput;
    private float currentX;

    public void OnMove(InputValue value)
    {
        Vector2 inputVector = value.Get<Vector2>();
        horizontalInput = inputVector.x;
    }

    void Update()
    {
        // Detener comportamiento si no se está jugando activamente
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // 1. Movimiento lateral fluido
        currentX += horizontalInput * steeringSpeed * Time.deltaTime;
        currentX = Mathf.Clamp(currentX, -maxHorizontalWidth, maxHorizontalWidth);
        transform.position = new Vector3(currentX, transform.position.y, transform.position.z);

        // 2. CORRECCIÓN DE ROTACIÓN + INCLINACIÓN 
        // Calculamos el balanceo en Z según el input
        float targetLean = -horizontalInput * rotationLean;
        float smoothLean = Mathf.LerpAngle(transform.localEulerAngles.z, targetLean, 10f * Time.deltaTime);

        // Mantenemos fijos tus -90 grados en el eje X, añadimos un leve giro en Y, e inclinamos en Z
        transform.localRotation = Quaternion.Euler(baseRotationX, horizontalInput * 4f, smoothLean);

        // 3. Mover la carretera en bucle infinito
        if (roadTransform != null)
        {
            roadTransform.Translate(Vector3.back * roadScrollSpeed * Time.deltaTime, Space.World);

            if (roadTransform.position.z < -40f)
            {
                roadTransform.position = new Vector3(0, 0, 40f);
            }
        }
    }
}