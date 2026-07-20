using UnityEngine;

/// <summary>
/// NOMBRE DEL COMPORTAMIENTO: PlayerHealth (Administrador de Constantes Vitales del Vehículo)
/// CASO DE USO: Almacenar, reducir y regenerar los puntos de estructura del coche del jugador. 
///              Al llegar a cero (0), notifica al GameManager para transicionar al estado de Game Over.
/// DATOS DE ENTRADA:
///   - maxHealth (int): Capacidad máxima de resistencia estructural.
///   - amount (int): Cantidad de daño recibida desde agentes externos (obstáculos).
/// DATOS DE SALIDA:
///   - currentHealth (int): Nivel de salud actual modificado frame a frame.
/// PRECONDICIÓN:
///   - El objeto que contiene el script debe estar activo en la escena de juego.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public int maxHealth = 100;
    public int currentHealth;
    private DamageInvincibility invincibilityScript;

    void Start()
    {
        currentHealth = maxHealth;
        invincibilityScript = GetComponent<DamageInvincibility>();
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: TakeDamage (Procesador Numérico de Daño)
    /// CASO DE USO: Sustraer puntos de vida de forma controlada y evaluar el estado de destrucción total del coche.
    /// DATOS DE ENTRADA: amount (int).
    /// DATOS DE SALIDA: Decremento de currentHealth. Llama a la lógica de fin de partida si la salud es menor o igual a cero.
    /// PRECONDICIÓN: El coche debe recibir un impacto válido estando con vida previa.
    /// </summary>
    public void TakeDamage(int damage)
    {
        // Ahora llamamos al método IsInvulnerable() con paréntesis
        if (invincibilityScript != null && invincibilityScript.IsInvulnerable())
        {
            Debug.Log("🛡️ [INVULNERABLE] Impacto ignorado.");
            return;
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"💥 [SALUD PLAYER] Daño recibido: {damage}. Salud restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            ManejarMuerte();
        }
        else
        {
            // Solo activamos si el script existe
            if (invincibilityScript != null)
            {
                invincibilityScript.ActivarInvulnerabilidad();
            }
        }
    }

    private void ManejarMuerte()
    {
        Debug.LogError("💀 [GAME OVER] El coche ha quedado destruido estructuralmente.");

        // LLAMADO AL NUEVO ESTADO:
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
        }
    }

    /// <summary>
    /// NOMBRE DEL COMPORTAMIENTO: ResetearSalud (Restaurador de Estructura)
    /// CASO DE USO: Devolver los puntos de salud actuales del vehículo a su valor máximo configurado (maxHealth).
    /// DATOS DE ENTRADA: maxHealth (int).
    /// DATOS DE SALIDA: Asignación de currentHealth = maxHealth.
    /// PRECONDICIÓN: Invocación externa desde el GameManager al iniciar una nueva partida.
    /// </summary>
    public void ResetearSalud()
    {
        currentHealth = maxHealth;
        Debug.Log($"🔧 [SALUD PLAYER] Sistema estructural restaurado. Vida inicial: {currentHealth}/{maxHealth}");
    }
}