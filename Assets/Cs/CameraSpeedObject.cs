using UnityEngine;
using Unity.Cinemachine;

public class CameraSpeedEffects : MonoBehaviour
{
    [Header("Cámaras de Cinemachine")]
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private CinemachineCamera overdriveCamera;
    [SerializeField] private float minSpeed;

    [Header("Configuración de Prioridades")]
    [Tooltip("Prioridad cuando la cámara está activa")]
    [SerializeField] private int activePriority = 15;

    [Tooltip("Prioridad cuando la cámara está inactiva")]
    [SerializeField] private int inactivePriority = 10;

    private bool overdriveActivo = false;

    void Update()
    {
        // Validación de seguridad
        if (TrackManager.Instance == null || normalCamera == null || overdriveCamera == null) return;

        // Comparamos si la velocidad actual es mayor a la velocidad base (30f)
        // Solo entra a cambiar prioridades si el estado del Overdrive cambió respecto al frame anterior
        bool velocidadAlta = TrackManager.Instance.roadScrollSpeed > minSpeed;

        if (velocidadAlta != overdriveActivo)
        {
            overdriveActivo = velocidadAlta;
            CambiarCamara(overdriveActivo);
        }
    }

    private void CambiarCamara(bool usarOverdrive)
    {
        if (usarOverdrive)
        {
            // Le damos más prioridad a la cámara de velocidad para que Cinemachine haga la transición hacia ella
            overdriveCamera.Priority = activePriority;
            normalCamera.Priority = inactivePriority;
        }
        else
        {
            // Regresamos la prioridad a la cámara normal
            normalCamera.Priority = activePriority;
            overdriveCamera.Priority = inactivePriority;
        }
    }
}