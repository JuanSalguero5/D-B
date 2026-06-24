using UnityEngine;

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: RhythmRamp (Propulsor Vertical y Desplazamiento de Entorno)
/// CASO DE USO: Mover la rampa hacia el coche a la velocidad del escenario. Al pisarla, inyecta un salto
///              procedural en el CharacterController. Si pasa de largo, se recicla al fondo de la pista.
/// DATOS DE ENTRADA:
///   - baseJumpForce (float): Fuerza base de elevación vertical.
///   - overdriveMultiplier (float): Amplificación de salto en Modo Overdrive.
///   - minSpawnX / maxSpawnX (float): Límites horizontales de la pista para reaparecer.
///   - resetZPosition / despawnZPosition (float): Umbrales de reaparición y reciclaje en el eje Z.
/// DATOS DE SALIDA:
///   - transform.position (Vector3): Modificación de coordenadas espaciales frame a frame.
/// PRECONDICIÓN:
///   - El GameManager debe estar en GameState.Playing.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RhythmRamp : MonoBehaviour
{
    [Header("Fuerzas de Impulso")]
    public float baseJumpForce = 12f;
    public float overdriveMultiplier = 1.5f;

    [Header("Configuración de Movimiento y Reciclaje")]
    public float minSpawnX = -6f;
    public float maxSpawnX = 6f;
    public float resetZPosition = 40f;
    public float despawnZPosition = -10f;

    private float currentSpeed = 30f;

    void Update()
    {
        // La rampa solo se mueve si el juego está activo
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // 1. SINCRONIZAR VELOCIDAD CON LA CARRETERA
        SimpleFluidDrive playerDrive = Object.FindAnyObjectByType<SimpleFluidDrive>();
        if (playerDrive != null)
        {
            currentSpeed = playerDrive.roadScrollSpeed;
        }

        // 2. MOVER LA RAMPA HACIA ATRÁS (Hacia el carro)
        transform.Translate(Vector3.back * currentSpeed * Time.deltaTime, Space.World);

        // 3. RECICLAJE AUTOMÁTICO AL FONDO
        if (transform.position.z < despawnZPosition)
        {
            float randomX = Random.Range(minSpawnX, maxSpawnX);
            transform.position = new Vector3(randomX, transform.position.y, resetZPosition);
        }
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: OnTriggerEnter (Detección de Pisada de Rampa)
    /// CASO DE USO: El coche del jugador entra en el volumen de la rampa y recibe el impulso vertical.
    /// DATOS DE ENTRADA: Collider other (Colisionador del objeto que entra).
    /// DATOS DE SALIDA: Llama a EjecutarSaltoRampa() en el script de conducción del coche.
    /// PRECONDICIÓN: El objeto debe tener el Tag "Player" y el script SimpleFluidDrive.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerRhythmBehavior rhythm = other.GetComponent<PlayerRhythmBehavior>();
            SimpleFluidDrive movement = other.GetComponent<SimpleFluidDrive>();

            if (movement != null)
            {
                float finalForce = baseJumpForce;

                if (rhythm != null && rhythm.isOverdriveActive)
                {
                    finalForce *= overdriveMultiplier;
                    Debug.Log("🚀 [RAMPA] ¡Impulso vertical rítmico potenciado por Overdrive!");
                }
                else
                {
                    Debug.Log("🚗 [RAMPA] Impulso vertical base aplicado.");
                }

                // Inyectamos el salto al coche
                movement.EjecutarSaltoRampa(finalForce);

                // OPCIONAL: Mandamos la rampa al fondo inmediatamente tras pisarla para que no estorbe
                float randomX = Random.Range(minSpawnX, maxSpawnX);
                transform.position = new Vector3(randomX, transform.position.y, resetZPosition);
            }
        }
    }
}