using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RhythmRamp : MonoBehaviour
{
    [Header("Fuerzas de Impulso")]
    public float baseJumpForce = 12f;
    public float overdriveMultiplier = 1.5f;

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
                }

                movement.EjecutarSaltoRampa(finalForce);

                // Opción rápida para feedback: Desactivar la rampa para que no se use dos veces
                gameObject.SetActive(false);
            }
        }
    }
}