using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    [Header("Objetivo a Seguir")]
    [SerializeField] private Transform target; // Arrastra aquí a tu Player

    [Header("Configuración de Distancia")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f); // Distancia ideal (atrás y arriba)

    [Header("Fluidez y Amortiguación")]
    [Range(0f, 1f)]
    [SerializeField] private float smoothTime = 0.3f; // Qué tan "suave" es el movimiento (menor tiempo = más rígida)

    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        // Si se te olvida asignarlo en el inspector, lo busca automáticamente por Tag
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    // Usamos LateUpdate para juegos de conducción/movimiento. 
    // Evita el molesto "efecto de temblor" (jitter) porque se ejecuta DESPUÉS de que el coche se movió.
    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Calcular la posición ideal a la que debería ir la cámara
        Vector3 targetPosition = target.position + offset;

        // 2. Interpolar suavemente la posición actual hacia la ideal usando SmoothDamp
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // 3. Aplicar la posición modificada
        transform.position = smoothedPosition;

        // 4. Hacer que la cámara siempre mire al coche de forma sutil
        transform.LookAt(target.position + Vector3.up * 1.5f); // Apunta un poco más arriba del eje del carro
    }
}