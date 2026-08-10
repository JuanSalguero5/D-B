using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Sacudida de camara con impulsos de Cinemachine al chocar con obstaculos.
/// Se puede arrastrar al coche en el Inspector (para tunear la fuerza) o se
/// auto-agrega la primera vez que hay un impacto.
/// Al activarse se encarga de:
///   - Asegurar que el coche tenga un CinemachineImpulseSource (forma Bump = golpe corto).
///   - Asegurar que TODAS las CinemachineCamera de la escena tengan un
///     CinemachineImpulseListener. OJO: el listener con ChannelMask=0 y Gain=0
///     no reacciona a nada, por eso se fuerzan a canal 1 y gain 1.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Sacudida de Camara")]
    [Tooltip("Fuerza del impulso. Sube para que el golpe se sienta mas fuerte, baja para algo sutil.")]
    public float fuerzaShake = 1.2f;

    private CinemachineImpulseSource impulso;

    void Awake()
    {
        // 1. Fuente de impulso sobre este objeto (el coche), cerca de la camara.
        impulso = GetComponent<CinemachineImpulseSource>();
        if (impulso == null)
        {
            impulso = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        // Bump = golpe corto y seco (mejor que el Custom por defecto).
        CinemachineImpulseDefinition def = impulso.ImpulseDefinition;
        def.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        def.ImpulseDuration = 0.15f;

        // 2. Asegurar que las virtual cameras escuchen el impulso (canal 1, gain 1).
        CinemachineCamera[] camaras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        for (int i = 0; i < camaras.Length; i++)
        {
            CinemachineCamera cam = camaras[i];
            if (cam == null) continue;

            CinemachineImpulseListener listener = cam.GetComponent<CinemachineImpulseListener>();
            if (listener == null)
            {
                listener = cam.gameObject.AddComponent<CinemachineImpulseListener>();
            }
            listener.ChannelMask = 1; // mismo canal que el ImpulseSource por defecto
            listener.Gain = 1f;
        }
    }

    public void Sacudir(float fuerza)
    {
        if (impulso != null)
        {
            impulso.GenerateImpulse(fuerza);
        }
    }

    public void Sacudir()
    {
        Sacudir(fuerzaShake);
    }
}
