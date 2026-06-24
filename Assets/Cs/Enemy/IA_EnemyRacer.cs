using UnityEngine;

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: IA_EnemyRacer (Inteligencia Artificial de Conductor Autónomo - BoxCast)
/// CASO DE USO: El coche enemigo compite en la pista y escanea el entorno usando un volumen cúbico (BoxCast) 
///              para esquivar de forma segura los obstáculos a lo ancho de su carril.
/// DATOS DE ENTRADA:
///   - boxExtents (Vector3): Dimensiones (mitad del tamaño real) del cubo sensor de la IA.
///   - sensorDistance (float): Longitud hacia adelante del sensor cúbico.
///   - obstacleLayer (LayerMask): Máscara de colisión para filtrar los obstáculos.
/// DATOS DE SALIDA:
///   - transform.position (Vector3): Ajuste del desplazamiento horizontal autónomo del enemigo.
/// PRECONDICIÓN:
///   - El GameManager debe estar en GameState.Playing.
/// </summary>
public class IA_EnemyRacer : MonoBehaviour
{
    public enum IAState { Patrolling, Evading, Aggressive }
    [Header("Estado de la IA")]
    public IAState currentState = IAState.Patrolling;

    [Header("Configuración de Movimiento")]
    public float sideSpeed = 8f;
    public float laneWidth = 3.5f;
    public float maxHorizontalLimit = 6f;

    [Header("Sensores de la IA (BoxCasting)")]
    public float sensorDistance = 15f;
    [Tooltip("El tamaño de la caja (Ancho, Alto, Largo) para el escaneo")]
    public Vector3 boxExtents = new Vector3(1.5f, 1f, 0.5f);
    public LayerMask obstacleLayer;

    [Header("Simulación Humana")]
    public float reactionTime = 0.25f;

    private float targetX;
    private float currentReactionTimer;
    private PlayerRhythmBehavior playerRhythm;

    void Start()
    {
        targetX = transform.position.x;
        playerRhythm = Object.FindAnyObjectByType<PlayerRhythmBehavior>();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        ProcesarSensoresYEstados();
        EjecutarMovimientoAutónomo();
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ProcesarSensoresYEstados (Sensor Volumétrico BoxCast)
    /// CASO DE USO: Lanzar un cubo invisible al frente. Si el volumen intersecta un obstáculo, cambia a estado de evasión.
    /// </summary>
    private void ProcesarSensoresYEstados()
    {
        if (playerRhythm != null && playerRhythm.isOverdriveActive)
        {
            currentState = IAState.Aggressive;
        }
        else
        {
            RaycastHit hit;

            // CAMBIO CLAVE: Usamos BoxCast en lugar de Raycast
            // Parámetros: Posición inicial, Mitad del tamaño del cubo, Dirección, Información del impacto, Rotación del cubo, Distancia, Capa.
            if (Physics.BoxCast(transform.position, boxExtents, Vector3.forward, out hit, transform.rotation, sensorDistance, obstacleLayer))
            {
                currentState = IAState.Evading;
            }
            else
            {
                currentState = IAState.Patrolling;
            }
        }
    }

    private void EjecutarMovimientoAutónomo()
    {
        switch (currentState)
        {
            case IAState.Patrolling:
                currentReactionTimer = 0f;
                break;

            case IAState.Evading:
                currentReactionTimer += Time.deltaTime;
                if (currentReactionTimer >= reactionTime)
                {
                    Debug.Log("🤖 [IA ENEMY] ¡Cubo sensor detectó obstáculo! Evadiendo carril.");
                    CalcularDesvíoEvaporativo();
                    currentReactionTimer = 0f;
                }
                break;

            case IAState.Aggressive:
                sideSpeed = 14f;
                if (Random.Range(0, 100) < 2)
                {
                    CalcularDesvíoEvaporativo();
                }
                break;
        }

        float newX = Mathf.Lerp(transform.position.x, targetX, sideSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    private void CalcularDesvíoEvaporativo()
    {
        if (transform.position.x >= 0f)
        {
            targetX = transform.position.x - laneWidth;
        }
        else
        {
            targetX = transform.position.x + laneWidth;
        }

        targetX = Mathf.Clamp(targetX, -maxHorizontalLimit, maxHorizontalLimit);
    }

    // Dibujar el cubo tridimensional en el editor de Unity para calibrar el ancho visualmente
    private void OnDrawGizmos()
    {
        Gizmos.color = (currentState == IAState.Evading) ? Color.red : Color.yellow;

        // Dibujamos la caja en el origen
        Gizmos.DrawWireCube(transform.position, boxExtents * 2f);

        // Dibujamos una línea y otra caja al final de la distancia para ver el alcance total del túnel de escaneo
        Vector3 finalPosition = transform.position + (Vector3.forward * sensorDistance);
        Gizmos.DrawLine(transform.position, finalPosition);
        Gizmos.DrawWireCube(finalPosition, boxExtents * 2f);
    }
}