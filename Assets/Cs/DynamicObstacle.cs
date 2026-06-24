using UnityEngine;

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: DynamicObstacle (Controlador de Desplazamiento y Reciclaje de Obstáculos)
/// CASO DE USO: Simular el avance del coche haciendo que los obstáculos se desplacen de forma síncrona en el eje Z 
///              a la velocidad de la carretera, reposicionándolos aleatoriamente al frente al superar el límite visual trasero.
/// DATOS DE ENTRADA: 
///   - minSpawnX (float): Límite horizontal izquierdo para el reposicionamiento aleatorio.
///   - maxSpawnX (float): Límite horizontal derecho para el reposicionamiento aleatorio.
///   - resetZPosition (float): Coordenada Z de reaparición al frente de la pista.
///   - despawnZPosition (float): Umbral límite en Z por detrás del jugador que activa el reciclaje.
///   - playerDrive.roadScrollSpeed (float): Velocidad de la carretera leída en tiempo real.
/// DATOS DE SALIDA: 
///   - transform.position (Vector3): Nueva posición tridimensional del obstáculo en la escena.
/// PRECONDICIÓN: 
///   - El GameManager debe estar instanciado y en estado activo de juego (GameState.Playing).
/// </summary>
public class DynamicObstacle : MonoBehaviour
{
    [Header("Configuración del Obstáculo")]
    public float minSpawnX = -6f;
    public float maxSpawnX = 6f;
    public float resetZPosition = 40f;
    public float despawnZPosition = -10f;

    private float currentSpeed = 30f;

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: Update (Ciclo Lógico por Cuadro)
    /// CASO DE USO: Sincronizar frame a frame la velocidad de traslación del obstáculo con el escenario y validar sus límites de espacio.
    /// DATOS DE ENTRADA: Time.deltaTime (Tiempo transcurrido entre fotogramas).
    /// DATOS DE SALIDA: Modificación del vector de posición e interpolación aleatoria en X mediante Random.Range().
    /// PRECONDICIÓN: Estado de juego activo en Playing.
    /// </summary>
    void Update()
    {
        // El obstáculo solo se mueve si el juego está activo
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        SimpleFluidDrive playerDrive = Object.FindAnyObjectByType<SimpleFluidDrive>();

        if (playerDrive != null)
        {
            currentSpeed = playerDrive.roadScrollSpeed;
        }

        // Mover el obstáculo hacia atrás (hacia el carro)
        transform.Translate(Vector3.back * currentSpeed * Time.deltaTime, Space.World);

        // Si el obstáculo ya pasó de largo al coche, lo reciclamos al fondo en una posición X aleatoria
        if (transform.position.z < despawnZPosition)
        {
            float randomX = Random.Range(minSpawnX, maxSpawnX);
            transform.position = new Vector3(randomX, transform.position.y, resetZPosition);
        }
    }
}