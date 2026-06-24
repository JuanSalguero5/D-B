using UnityEngine;

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: ObstacleDamage (Gatillo de Impacto Dañino)
/// CASO DE USO: El coche del jugador interseca el área geométrica de un obstáculo; el script lee su interfaz 
///              de salud y le resta los puntos configurados de forma síncrona.
/// DATOS DE ENTRADA:
///   - damageAmount (int): Coeficiente numérico de daño a transferir.
///   - Collider other: El colisionador volumétrico del Player.
/// DATOS DE SALIDA:
///   - Modificación por llamada de función en el script PlayerHealth.
/// PRECONDICIÓN:
///   - El obstáculo debe tener activada la propiedad Is Trigger en su Collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ObstacleDamage : MonoBehaviour
{
    [Header("Configuración de Daño")]
    public int damageAmount = 15;

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
            return;

        // Si colisiona con el carro (con Tag "Player")
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damageAmount);
            }

            // Reciclamos el obstáculo inmediatamente mandándolo detrás del umbral
            DynamicObstacle dynamicScript = GetComponent<DynamicObstacle>();
            if (dynamicScript != null)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, dynamicScript.despawnZPosition - 1f);
            }
        }
    }
}